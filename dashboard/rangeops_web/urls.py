from django.urls import path

from ops import views

urlpatterns = [
    path("", views.mission_list, name="mission_list"),
    path("missions/<int:mission_id>/", views.mission_detail, name="mission_detail"),
    path("runs/<int:run_id>/", views.run_detail, name="run_detail"),
    path("downloads/", views.downloads, name="downloads"),
]
