"""
Django ORM models mapped *database-first* to the shared RangeOps schema.

`managed = False` means Django does not create, alter, or drop these tables --
`db/schema.sql` owns them, and the C# console (EF Core) maps to the very same
tables. Django here is a read-only reporting view over the shared database.
"""
from django.db import models


class Mission(models.Model):
    name = models.CharField(max_length=120)
    aircraft = models.CharField(max_length=60)
    scheduled_start = models.DateTimeField()
    scheduled_end = models.DateTimeField()
    status = models.CharField(max_length=20)
    created_at = models.DateTimeField()

    class Meta:
        managed = False
        db_table = "missions"
        ordering = ["-scheduled_start"]

    def __str__(self):
        return f"{self.name} ({self.aircraft})"

    @property
    def is_active(self):
        return self.status == "ACTIVE"


class TestRun(models.Model):
    mission = models.ForeignKey(
        Mission, on_delete=models.DO_NOTHING, db_column="mission_id",
        related_name="test_runs",
    )
    name = models.CharField(max_length=120)
    status = models.CharField(max_length=20)
    started_at = models.DateTimeField(null=True)
    ended_at = models.DateTimeField(null=True)
    notes = models.TextField(null=True)

    class Meta:
        managed = False
        db_table = "test_runs"
        ordering = ["id"]

    def __str__(self):
        return self.name


class TelemetrySample(models.Model):
    test_run = models.ForeignKey(
        TestRun, on_delete=models.DO_NOTHING, db_column="test_run_id",
        related_name="samples",
    )
    sample_ts = models.DateTimeField()
    altitude_ft = models.FloatField()
    airspeed_kt = models.FloatField()
    vertical_speed_fpm = models.FloatField()
    link_dropout = models.BooleanField()

    class Meta:
        managed = False
        db_table = "telemetry_samples"
        ordering = ["sample_ts"]
