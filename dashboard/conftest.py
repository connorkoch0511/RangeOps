"""
pytest fixtures for the dashboard.

Because the models are `managed = False`, Django's test runner creates an empty
test database but *not* the tables. We load the same `db/schema.sql` the whole
suite shares, so tests run against the real schema the app queries in
production -- not a Django-invented one.
"""
import pathlib

import pytest
from django.db import connection

SCHEMA = pathlib.Path(__file__).resolve().parent.parent / "db" / "schema.sql"


@pytest.fixture(scope="session")
def django_db_setup(django_db_setup, django_db_blocker):
    with django_db_blocker.unblock(), connection.cursor() as cur:
        cur.execute(SCHEMA.read_text())
    yield
