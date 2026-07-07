"""Unit + integration tests for the RangeOps dashboard."""
from datetime import timedelta

import pytest
from django.urls import reverse
from django.utils import timezone

from ops.models import Mission, TelemetrySample, TestRun


def _mission(**kw):
    now = timezone.now()
    defaults = dict(
        name="Test Mission",
        aircraft="F-16C",
        scheduled_start=now,
        scheduled_end=now + timedelta(hours=1),
        status="PLANNED",
        created_at=now,
    )
    defaults.update(kw)
    return Mission.objects.create(**defaults)


def test_is_active_property():
    """Pure unit test -- no database needed."""
    m = Mission(status="ACTIVE")
    assert m.is_active is True
    m.status = "PLANNED"
    assert m.is_active is False


@pytest.mark.django_db
def test_mission_list_rolls_up_counts(client):
    _mission(status="ACTIVE")
    _mission(status="PLANNED")
    _mission(status="PLANNED")

    resp = client.get(reverse("mission_list"))

    assert resp.status_code == 200
    assert resp.context["counts"] == {"total": 3, "active": 1, "planned": 2}


@pytest.mark.django_db
def test_run_detail_counts_fault_samples(client):
    m = _mission(status="ACTIVE")
    run = TestRun.objects.create(mission=m, name="Climb", status="RUNNING")
    now = timezone.now()
    for i in range(10):
        TelemetrySample.objects.create(
            test_run=run,
            sample_ts=now + timedelta(seconds=i),
            altitude_ft=1000 + i * 200,
            airspeed_kt=250 + i,
            vertical_speed_fpm=2000,
            fault_injected=(i in (4, 5, 6)),  # three faulted samples
        )

    resp = client.get(reverse("run_detail", args=[run.id]))

    assert resp.status_code == 200
    assert resp.context["summary"]["n"] == 10
    assert resp.context["fault_count"] == 3
    assert resp.context["summary"]["max_alt"] == 2800


@pytest.mark.django_db
def test_mission_detail_lists_runs(client):
    m = _mission()
    TestRun.objects.create(mission=m, name="Run A", status="PASS")
    TestRun.objects.create(mission=m, name="Run B", status="FAIL")

    resp = client.get(reverse("mission_detail", args=[m.id]))

    assert resp.status_code == 200
    assert list(resp.context["runs"].values_list("name", flat=True)) == ["Run A", "Run B"]
