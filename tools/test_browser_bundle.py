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


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("Usage: python tools/test_browser_bundle.py PUBLISHED_WWWROOT")
    check(Path(sys.argv[1]))
