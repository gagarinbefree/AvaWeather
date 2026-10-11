"""Prepare a published standalone Blazor site for GitHub Pages."""

import json
import os
import sys
from pathlib import Path


def prepare(root: Path, api_key: str) -> None:
    if not api_key.strip():
        raise ValueError("WEATHER_API_KEY is required for the web release")

    index_path = root / "index.html"
    index = index_path.read_text(encoding="utf-8")
    source_base = '<base href="/" />'
    if index.count(source_base) != 1:
        raise ValueError("Expected the root base href in the published index")
    index_path.write_text(
        index.replace(source_base, '<base href="/AvaWeather/" />'),
        encoding="utf-8",
    )

    settings_path = root / "appsettings.json"
    settings = json.loads(settings_path.read_text(encoding="utf-8"))
    settings["WeatherApi"]["ApiKey"] = api_key
    settings_path.write_text(
        json.dumps(settings, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    (root / ".nojekyll").touch()


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("Usage: prepare_blazor_pages.py PUBLISHED_WWWROOT")
    prepare(Path(sys.argv[1]), os.environ.get("WEATHER_API_KEY", ""))
