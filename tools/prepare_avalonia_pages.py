"""Prepare the published Avalonia Browser bundle for GitHub Pages."""

import json
import os
import re
import sys
from html import escape
from pathlib import Path


def prepare(root: Path, api_key: str) -> None:
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

    index = index.replace(source_base, '<base href="/AvaWeather/" />')
    boot_path = root / "_framework" / "dotnet.js"
    if boot_path.is_file():
        match = re.search(r"/\*json-start\*/(\{.*?\})/\*json-end\*/",
                          boot_path.read_text(encoding="utf-8"), re.S)
        if match is None:
            raise ValueError("Published .NET boot manifest is missing")
        native = json.loads(match.group(1))["resources"]["wasmNative"][0]
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
    (root / ".nojekyll").touch()


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("Usage: prepare_avalonia_pages.py PUBLISHED_WWWROOT")
    prepare(Path(sys.argv[1]), os.environ.get("WEATHER_API_KEY", ""))
