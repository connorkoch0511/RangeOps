"""Read-only reporting views over the shared RangeOps database."""
from django.db.models import Count, Max, Min, Q
from django.shortcuts import get_object_or_404, render

from .models import Mission, TelemetrySample, TestRun

# Statuses that can be filtered from the schedule-board cards.
_FILTERABLE = {"ACTIVE", "PLANNED", "COMPLETE"}


def mission_list(request):
    """Schedule board: every mission with a rollup of its test runs.

    Supports optional filtering by ?status= and free-text ?q= (name/aircraft).
    """
    status = (request.GET.get("status") or "").upper()
    query = (request.GET.get("q") or "").strip()

    missions = Mission.objects.all().annotate(run_count=Count("test_runs"))
    if status in _FILTERABLE:
        missions = missions.filter(status=status)
    else:
        status = ""  # normalize unknown/absent to "All"
    if query:
        missions = missions.filter(
            Q(name__icontains=query) | Q(aircraft__icontains=query)
        )

    # Counts are always over the full set so the cards act as stable filters.
    base = Mission.objects.all()
    counts = {
        "total": base.count(),
        "active": base.filter(status="ACTIVE").count(),
        "planned": base.filter(status="PLANNED").count(),
    }
    return render(
        request,
        "ops/mission_list.html",
        {
            "missions": missions,
            "counts": counts,
            "active_status": status,
            "query": query,
        },
    )


def mission_detail(request, mission_id):
    mission = get_object_or_404(Mission, pk=mission_id)
    runs = mission.test_runs.all()
    return render(
        request,
        "ops/mission_detail.html",
        {"mission": mission, "runs": runs},
    )


# Version-independent asset names → these always resolve to the latest release.
_RELEASE_BASE = "https://github.com/connorkoch0511/RangeOps/releases/latest/download"


def downloads(request):
    """Download page for the cross-platform operator console."""
    builds = [
        {"label": "macOS (Apple Silicon)", "file": "RangeOps-Console-osx-arm64.zip"},
        {"label": "macOS (Intel)", "file": "RangeOps-Console-osx-x64.zip"},
        {"label": "Windows (x64)", "file": "RangeOps-Console-win-x64.zip"},
        {"label": "Linux (x64)", "file": "RangeOps-Console-linux-x64.zip"},
    ]
    for b in builds:
        b["url"] = f"{_RELEASE_BASE}/{b['file']}"
    return render(request, "ops/downloads.html", {"builds": builds})


def run_detail(request, run_id):
    """Telemetry report for a single test run, with a data-link dropout summary."""
    run = get_object_or_404(TestRun, pk=run_id)
    samples = run.samples.all()
    summary = samples.aggregate(
        n=Count("id"),
        max_alt=Max("altitude_ft"),
        max_ias=Max("airspeed_kt"),
        first_ts=Min("sample_ts"),
        last_ts=Max("sample_ts"),
    )
    dropout_count = samples.filter(link_dropout=True).count()
    # Cap the plotted series so the page stays light with large runs.
    series = list(
        samples.values("sample_ts", "altitude_ft", "airspeed_kt", "link_dropout")[:500]
    )
    return render(
        request,
        "ops/run_detail.html",
        {
            "run": run,
            "summary": summary,
            "dropout_count": dropout_count,
            "series": series,
        },
    )
