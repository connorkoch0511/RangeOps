"""
End-to-end tests for the RangeOps dashboard, driven by a real browser
(Playwright/Chromium). Each test navigates a page, asserts on what a user
actually sees, and captures a screenshot into docs/screenshots/.

Run against a local dev server (default) or the live deployment:

    # local (start `./run.sh` first, DB seeded via db/seed_demo.py)
    pytest e2e/

    # live production
    BASE_URL=https://rangeops-dashboard.vercel.app pytest e2e/

The screenshots used in the README are produced by this same suite, so they
can never drift from what the app renders.
"""
import os
import pathlib

import pytest
from playwright.sync_api import Page, expect

BASE_URL = os.environ.get("BASE_URL", "http://localhost:8000").rstrip("/")
SHOTS = pathlib.Path(__file__).resolve().parents[2] / "docs" / "screenshots"
SHOTS.mkdir(parents=True, exist_ok=True)


def _shoot(page: Page, name: str, height: int):
    """Frame the screenshot tightly to the content (no dead space)."""
    page.set_viewport_size({"width": 1200, "height": height})
    page.wait_for_timeout(150)
    page.screenshot(path=SHOTS / name)


def test_schedule_board(page: Page):
    """The home page lists scheduled missions with a status rollup."""
    page.goto(f"{BASE_URL}/")

    expect(page.get_by_role("heading", name="RangeOps")).to_be_visible()
    expect(page.get_by_text("Avionics Regression 7")).to_be_visible()
    expect(page.get_by_text("Envelope Expansion 4A")).to_be_visible()
    expect(page.locator(".pill.ACTIVE").first).to_be_visible()

    _shoot(page, "01-schedule.png", height=400)


def test_mission_detail(page: Page):
    """Clicking a mission shows its test runs."""
    page.goto(f"{BASE_URL}/")
    page.get_by_role("link", name="Envelope Expansion 4A").click()

    expect(page.get_by_text("Climb to FL250")).to_be_visible()
    expect(page.get_by_text("Level accel M0.9")).to_be_visible()

    _shoot(page, "02-mission-detail.png", height=360)


def test_run_telemetry_with_dropouts(page: Page):
    """The telemetry report renders the summary and the dropout-marked chart."""
    page.set_viewport_size({"width": 1200, "height": 620})
    page.goto(f"{BASE_URL}/")
    page.get_by_role("link", name="Envelope Expansion 4A").click()
    page.get_by_role("link", name="Climb to FL250").click()

    # summary cards + a non-zero link-dropout count
    expect(page.get_by_text("Link dropouts")).to_be_visible()
    expect(page.get_by_text("Max alt (ft)")).to_be_visible()
    expect(page.locator(".card .n.fault")).not_to_have_text("0")

    # the chart is drawn to a <canvas> by chart.js after load
    page.wait_for_selector("#chart")
    page.wait_for_timeout(600)

    _shoot(page, "03-run-telemetry.png", height=610)
