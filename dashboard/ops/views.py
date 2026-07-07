"""Read-only reporting views over the shared RangeOps database."""
from django.db.models import Count, Max, Min
from django.shortcuts import get_object_or_404, render

from .models import Mission, TelemetrySample, TestRun


def mission_list(request):
    """Schedule board: every mission with a rollup of its test runs."""
    missions = (
        Mission.objects.all()
        .annotate(run_count=Count("test_runs"))
    )
    counts = {
        "total": Mission.objects.count(),
        "active": Mission.objects.filter(status="ACTIVE").count(),
        "planned": Mission.objects.filter(status="PLANNED").count(),
    }
    return render(
        request,
        "ops/mission_list.html",
        {"missions": missions, "counts": counts},
    )


def mission_detail(request, mission_id):
    mission = get_object_or_404(Mission, pk=mission_id)
    runs = mission.test_runs.all()
    return render(
        request,
        "ops/mission_detail.html",
        {"mission": mission, "runs": runs},
    )


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
