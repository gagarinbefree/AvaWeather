"""Check that the published browser app stays small and keeps DI handlers."""

import json
import re
import sys
from pathlib import Path


def check(root: Path) -> None:
    framework = root / "_framework"
    boot = (framework / "dotnet.js").read_text(encoding="utf-8")
    match = re.search(r"/\*json-start\*/(\{.*?\})/\*json-end\*/", boot, re.S)
    assert match is not None, "Published .NET boot manifest is missing"
    resources = json.loads(match.group(1))["resources"]
    assemblies = resources["assembly"]

    # The UI chooses Russian after the runtime starts, so its satellite must be
    # present in the deployed bundle for startup.js to load it in every browser.
    russian = resources.get("satelliteResources", {}).get("ru", [])
    assert any(asset["name"].startswith("Weather.Localization.resources.") for asset in russian), \
        "Russian UI resources are missing from the browser bundle"
    for asset in russian:
        assert (framework / "ru" / asset["name"]).is_file(), \
            f"Published Russian resource is missing: {asset['name']}"

    assert len(assemblies) <= 120, f"Too many browser assemblies: {len(assemblies)}"
    groups = ("wasmNative", "coreAssembly", "assembly", "jsModuleNative", "jsModuleRuntime")
    compressed_bytes = sum(
        (framework / (asset["name"] + ".br")).stat().st_size
        for group in groups for asset in resources[group]
    )
    assert compressed_bytes <= 9_000_000, f"Browser boot payload is too large: {compressed_bytes} bytes"

    app = next(asset for asset in assemblies if asset["name"].startswith("Application."))
    app_bytes = (framework / app["name"]).read_bytes()
    for handler in (b"GetCurrentWeatherQueryHandler", b"GetForecastQueryHandler"):
        assert handler in app_bytes, f"Trimming removed {handler.decode()}"

    ui = next(asset for asset in assemblies if asset["name"].startswith("AvaWeather.UI."))
    ui_bytes = (framework / ui["name"]).read_bytes()
    for weight in ("Light", "Regular", "SemiBold", "Bold"):
        font = f"Inter-{weight}.ttf".encode()
        assert font in ui_bytes, f"Trimming removed {font.decode()}"

    fonts = Path(__file__).resolve().parents[1] / "SharedAssets" / "Fonts"
    for weight in ("Regular", "SemiBold"):
        name = f"Inter-{weight}.ttf"
        assert (root / "fonts" / name).read_bytes() == (fonts / name).read_bytes(), \
            f"Published {name} differs from the shared font"


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("Usage: python tools/test_browser_bundle.py PUBLISHED_WWWROOT")
    check(Path(sys.argv[1]))
