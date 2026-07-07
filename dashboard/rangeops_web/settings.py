"""Django settings for the RangeOps web dashboard."""
import os
from pathlib import Path

BASE_DIR = Path(__file__).resolve().parent.parent

SECRET_KEY = os.environ.get(
    "DJANGO_SECRET_KEY", "dev-only-insecure-key-change-in-prod"
)
DEBUG = os.environ.get("DJANGO_DEBUG", "1") == "1"
ALLOWED_HOSTS = ["*"]

INSTALLED_APPS = [
    "django.contrib.contenttypes",
    "django.contrib.staticfiles",
    "ops",
]

MIDDLEWARE = [
    "django.middleware.security.SecurityMiddleware",
    # Serves static files (chart.js) on Vercel's serverless runtime.
    "whitenoise.middleware.WhiteNoiseMiddleware",
    "django.middleware.common.CommonMiddleware",
]

ROOT_URLCONF = "rangeops_web.urls"

TEMPLATES = [
    {
        "BACKEND": "django.template.backends.django.DjangoTemplates",
        "DIRS": [],
        "APP_DIRS": True,
        "OPTIONS": {"context_processors": []},
    },
]

WSGI_APPLICATION = "rangeops_web.wsgi.application"

# Shared PostgreSQL database -- the same instance the C# console writes to.
# In production (Vercel), a managed Postgres (Neon) provides a single
# connection URL; locally we use the discrete docker-compose vars.
_DATABASE_URL = os.environ.get("DATABASE_URL") or os.environ.get("POSTGRES_URL")
if _DATABASE_URL:
    import dj_database_url

    _db = dj_database_url.parse(_DATABASE_URL, conn_max_age=0, ssl_require=True)
    # psycopg3 uses server-side prepared statements by default, which break on
    # Neon's pgbouncer (transaction pooling). Disable them for the pooled URL.
    _db.setdefault("OPTIONS", {})["prepare_threshold"] = None
    DATABASES = {"default": _db}
else:
    DATABASES = {
        "default": {
            "ENGINE": "django.db.backends.postgresql",
            "NAME": os.environ.get("POSTGRES_DB", "rangeops"),
            "USER": os.environ.get("POSTGRES_USER", "rangeops"),
            "PASSWORD": os.environ.get("POSTGRES_PASSWORD", "rangeops"),
            "HOST": os.environ.get("POSTGRES_HOST", "localhost"),
            "PORT": os.environ.get("POSTGRES_PORT", "5544"),
        }
    }

STATIC_URL = "static/"
STATIC_ROOT = BASE_DIR / "staticfiles"
# Serve static files straight from the app's static dirs (no collectstatic
# step needed on the serverless build).
WHITENOISE_USE_FINDERS = True
DEFAULT_AUTO_FIELD = "django.db.models.BigAutoField"
USE_TZ = True
TIME_ZONE = "UTC"
