"""Prepare the published Avalonia Browser bundle for GitHub Pages."""

import json
import hashlib
import os
import re
import sys
from html import escape
from pathlib import Path


def prepare(root: Path, api_key: str, telemetry_dsn: str = "", openweather_key: str = "") -> None:
    if not api_key.strip():
        raise ValueError("WEATHER_API_KEY is required for the web release")

    index_path = root / "index.html"
    index = index_path.read_text(encoding="utf-8")
    source_base = '<base href="/" />'
    if index.count(source_base) != 1:
        raise ValueError("Expected the root base href in the published index")

    settings_path = root / "appsettings.json"
    settings = json.loads(settings_path.read_text(encoding="utf-8"))
    settings["WeatherApi"]["ApiKey"] = api_key
    settings.setdefault("OpenWeatherMap", {})["ApiKey"] = openweather_key
    settings.setdefault("Telemetry", {})["Dsn"] = telemetry_dsn

    index = index.replace(source_base, '<base href="/AvaWeather/" />')
    boot_path = root / "_framework" / "dotnet.js"
    resources = None
    if boot_path.is_file():
        match = re.search(r"/\*json-start\*/(\{.*?\})/\*json-end\*/",
                          boot_path.read_text(encoding="utf-8"), re.S)
        if match is None:
            raise ValueError("Published .NET boot manifest is missing")
        resources = json.loads(match.group(1))["resources"]
        native = resources["wasmNative"][0]
        name = native["name"]
        if Path(name).name != name or not name.endswith(".wasm") or \
                not (root / "_framework" / name).is_file():
            raise ValueError("Published native WASM file is missing")
        if index.count("</head>") != 1:
            raise ValueError("Expected one head element in the published index")
        preload = (f'<link rel="preload" href="_framework/{escape(name, quote=True)}" '
                   f'as="fetch" type="application/wasm" crossorigin '
                   f'integrity="{escape(native["hash"], quote=True)}" />')
        index = index.replace("</head>", f"    {preload}\n</head>")

    index_path.write_text(index, encoding="utf-8")
    settings_path.write_text(json.dumps(settings, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    for changed_file in (index_path, settings_path):
        for suffix in (".br", ".gz"):
            changed_file.with_name(changed_file.name + suffix).unlink(missing_ok=True)
    worker = root / "sw.js"
    if worker.is_file() and resources is not None:
        assets = {path.relative_to(root).as_posix() for path in root.rglob("*")
                  if path.is_file() and "_framework" not in path.relative_to(root).parts
                  and path.suffix.lower() in {".html", ".js", ".css", ".ico", ".png", ".ttf", ".webmanifest"}
                  and path.name != "sw.js"}
        assets.add("_framework/dotnet.js")
        for group, entries in resources.items():
            if isinstance(entries, list):
                assets.update(f"_framework/{asset['name']}" for asset in entries)
            elif group == "satelliteResources":
                for culture, satellites in entries.items():
                    assets.update(f"_framework/{culture}/{asset['name']}" for asset in satellites)
        for asset in assets:
            path = root / asset
            if not path.resolve().is_relative_to(root.resolve()) or not path.is_file():
                raise ValueError(f"Invalid offline asset: {asset}")
        version = hashlib.sha256(worker.read_bytes())
        for asset in sorted(assets):
            version.update(asset.encode("utf-8"))
            version.update((root / asset).read_bytes())
        (root / "offline-assets.json").write_text(json.dumps(sorted(assets), ensure_ascii=False) + "\n", encoding="utf-8")
        worker.write_text(worker.read_text(encoding="utf-8").replace("__OFFLINE_VERSION__", version.hexdigest()[:16]), encoding="utf-8")
        for suffix in (".br", ".gz"):
            worker.with_name(worker.name + suffix).unlink(missing_ok=True)
    (root / ".nojekyll").touch()


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("Usage: prepare_avalonia_pages.py PUBLISHED_WWWROOT")
    prepare(Path(sys.argv[1]), os.environ.get("WEATHER_API_KEY", ""),
            os.environ.get("HAWK_DSN", ""), os.environ.get("OPENWEATHERMAP_API_KEY", ""))
