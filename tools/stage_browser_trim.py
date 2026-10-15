"""Stage a .NET-host smoke test with the browser publish's trimmed assemblies."""

import shutil
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "tools" / "BrowserTrimSmoke" / "bin" / "Release" / "net10.0"
LINKED = ROOT / "AvaWeather.Browser" / "obj" / "Release" / "net10.0-browser" / "linked"
STAGE = ROOT / "artifacts" / "browser-trim-smoke"

if not (OUTPUT / "BrowserTrimSmoke.dll").is_file():
    raise FileNotFoundError("Build tools/BrowserTrimSmoke/BrowserTrimSmoke.csproj first")

shutil.copytree(OUTPUT, STAGE, dirs_exist_ok=True)
for assembly in ("Application", "Domain", "Infrastructure", "Localization", "AutoMapper", "MediatR"):
    source = LINKED / f"{assembly}.dll"
    if not source.is_file():
        raise FileNotFoundError(f"Publish the browser app first: {source}")
    shutil.copy2(source, STAGE / source.name)

print(f"Staged trimmed browser services in {STAGE}")
