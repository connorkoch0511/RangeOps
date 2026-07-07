using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using RangeOps.Core.Data;
using RangeOps.Core.Models;
using RangeOps.Core.Telemetry;

namespace RangeOps.Console.ViewModels;

/// <summary>
/// Operator console view-model. Handles mission scheduling (writes to Postgres
/// via EF Core) and live telemetry capture (streams from the C sensor-sim and
/// persists each sample). This is the same MVVM shape you'd write in WPF:
/// observable properties + commands, no logic in the view.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly CaptureService _capture = new();
    private CancellationTokenSource? _captureCts;

    public ObservableCollection<Mission> Missions { get; } = new();
    public ObservableCollection<TestRun> TestRuns { get; } = new();

    [ObservableProperty] private Mission? _selectedMission;
    [ObservableProperty] private TestRun? _selectedRun;

    // --- new-mission form ---
    [ObservableProperty] private string _newMissionName = "";
    [ObservableProperty] private string _newMissionAircraft = "F-16C";
    [ObservableProperty] private int _newMissionDurationMin = 90;

    // --- live telemetry readout ---
    [ObservableProperty] private double _liveAltitude;
    [ObservableProperty] private double _liveAirspeed;
    [ObservableProperty] private double _liveVerticalSpeed;
    [ObservableProperty] private bool _liveFault;
    [ObservableProperty] private int _sampleCount;
    [ObservableProperty] private int _faultCount;
    [ObservableProperty] private bool _isCapturing;
    [ObservableProperty] private string _status = "Ready.";

    public MainWindowViewModel()
    {
        // Fire-and-forget initial load; safe because it only touches the DB.
        _ = LoadMissionsAsync();
    }

    partial void OnSelectedMissionChanged(Mission? value) => _ = LoadRunsAsync();

    partial void OnSelectedRunChanged(TestRun? value) =>
        StartCaptureCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private async Task LoadMissionsAsync()
    {
        try
        {
            await using var db = new RangeOpsContext();
            var missions = await db.Missions
                .OrderByDescending(m => m.ScheduledStart)
                .ToListAsync();
            Missions.Clear();
            foreach (var m in missions) Missions.Add(m);
            Status = $"Loaded {Missions.Count} missions.";
        }
        catch (Exception ex)
        {
            Status = $"DB error: {ex.Message}";
        }
    }

    private async Task LoadRunsAsync()
    {
        TestRuns.Clear();
        if (SelectedMission is null) return;
        await using var db = new RangeOpsContext();
        var runs = await db.TestRuns
            .Where(r => r.MissionId == SelectedMission.Id)
            .OrderBy(r => r.Id)
            .ToListAsync();
        foreach (var r in runs) TestRuns.Add(r);
    }

    [RelayCommand]
    private async Task CreateMissionAsync()
    {
        if (string.IsNullOrWhiteSpace(NewMissionName))
        {
            Status = "Mission name is required.";
            return;
        }
        var start = DateTime.UtcNow.AddMinutes(30);
        var mission = new Mission
        {
            Name = NewMissionName.Trim(),
            Aircraft = NewMissionAircraft.Trim(),
            ScheduledStart = start,
            ScheduledEnd = start.AddMinutes(Math.Max(1, NewMissionDurationMin)),
            Status = "PLANNED",
        };
        await using var db = new RangeOpsContext();
        db.Missions.Add(mission);
        await db.SaveChangesAsync();
        // Give every new mission a default first test run to capture against.
        db.TestRuns.Add(new TestRun
        {
            MissionId = mission.Id, Name = "Telemetry capture 1", Status = "PENDING",
        });
        await db.SaveChangesAsync();

        NewMissionName = "";
        await LoadMissionsAsync();
        SelectedMission = Missions.FirstOrDefault(m => m.Id == mission.Id);
        Status = $"Scheduled mission #{mission.Id}.";
    }

    private bool CanStartCapture() => SelectedRun is not null && !IsCapturing;

    [RelayCommand(CanExecute = nameof(CanStartCapture))]
    private async Task StartCaptureAsync()
    {
        if (SelectedRun is null) return;
        var runId = SelectedRun.Id;
        _captureCts = new CancellationTokenSource();
        IsCapturing = true;
        SampleCount = 0;
        FaultCount = 0;
        StartCaptureCommand.NotifyCanExecuteChanged();
        StopCaptureCommand.NotifyCanExecuteChanged();

        try
        {
            Status = "Capturing telemetry…";
            // Delegate to the shared CaptureService; update the live readout on
            // the UI thread as each sample lands.
            var result = await _capture.CaptureAsync(runId, _captureCts.Token, onSample: r =>
                Dispatcher.UIThread.Post(() =>
                {
                    LiveAltitude = r.AltitudeFt;
                    LiveAirspeed = r.AirspeedKt;
                    LiveVerticalSpeed = r.VerticalSpeedFpm;
                    LiveFault = r.Fault;
                    SampleCount++;
                    if (r.Fault) FaultCount++;
                }));
            Status = $"Run complete: {result.Verdict} " +
                     $"({result.Samples} samples, {result.Faults} faults).";
        }
        catch (Exception ex)
        {
            Status = $"Capture error: {ex.Message}";
        }
        finally
        {
            IsCapturing = false;
            LiveFault = false;
            await LoadRunsAsync();
            StartCaptureCommand.NotifyCanExecuteChanged();
            StopCaptureCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanStopCapture() => IsCapturing;

    [RelayCommand(CanExecute = nameof(CanStopCapture))]
    private void StopCapture() => _captureCts?.Cancel();
}
