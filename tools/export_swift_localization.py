"""Export shared resources for macOS."""
from pathlib import Path
import locale
import shutil
import sys
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
source = root / "Weather.Localization"
destination = root / "Widgets" / "macOS" / "Resources"


def outdated(path: Path) -> None:
    system_language = "ru" if (locale.getlocale()[0] or "").startswith("ru") else "en"
    suffix = ".ru" if system_language == "ru" else ""
    catalog = ET.parse(source / f"Strings{suffix}.resx")
    message = next(item.findtext("value") for item in catalog.findall("data")
                   if item.attrib["name"] == "LocalizationExportOutdated")
    raise SystemExit(message.format(path))


def quote(value: str) -> str:
    return value.replace("\\", "\\\\").replace('"', '\\"').replace("\n", "\\n")


for language, suffix in (("en", ""), ("ru", ".ru")):
    entries = {}
    for name in ("Strings",):
        tree = ET.parse(source / f"{name}{suffix}.resx")
        entries.update({item.attrib["name"]: item.findtext("value", default="") for item in tree.findall("data")})
    path = destination / f"{language}.lproj" / "Localizable.strings"
    path.parent.mkdir(parents=True, exist_ok=True)
    content = "".join(f'"{quote(key)}" = "{quote(value)}";\n'
                      for key, value in sorted(entries.items()))
    if "--check" in sys.argv:
        if not path.exists() or path.read_text(encoding="utf-8") != content:
            outdated(path)
    else:
        path.write_text(content, encoding="utf-8")

for original, exported in (
    (root / "SharedAssets" / "Themes" / "default.json", destination / "default-theme.json"),
    (root / "SharedAssets" / "Weather" / "weather-conditions.json", destination / "weather-conditions.json"),
):
    content = original.read_text(encoding="utf-8")
    if "--check" in sys.argv:
        if not exported.exists() or exported.read_text(encoding="utf-8") != content:
            outdated(exported)
    else:
        with exported.open("w", encoding="utf-8", newline="\n") as target:
            target.write(content)

icon_source = root / "SharedAssets" / "Branding" / "app-icon.png"
icon_export = root / "Widgets" / "macOS" / "Host" / "Assets.xcassets" / "AppIcon.appiconset" / "AppIcon.png"
if "--check" in sys.argv:
    if not icon_export.exists() or icon_export.read_bytes() != icon_source.read_bytes():
        outdated(icon_export)
else:
    shutil.copyfile(icon_source, icon_export)
