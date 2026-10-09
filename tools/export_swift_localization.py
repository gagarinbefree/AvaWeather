"""Export the shared RESX catalog as macOS localization resources."""
from pathlib import Path
import locale
import sys
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
source = root / "Weather.Localization"
destination = root / "Widgets" / "macOS" / "Resources"


def quote(value: str) -> str:
    return value.replace("\\", "\\\\").replace('"', '\\"').replace("\n", "\\n")


for language, suffix in (("en", ""), ("ru", ".ru")):
    entries = {}
    for name in ("Strings", "WeatherConditions"):
        tree = ET.parse(source / f"{name}{suffix}.resx")
        entries.update({item.attrib["name"]: item.findtext("value", default="") for item in tree.findall("data")})
    path = destination / f"{language}.lproj" / "Localizable.strings"
    path.parent.mkdir(parents=True, exist_ok=True)
    content = "".join(f'"{quote(key)}" = "{quote(value)}";\n'
                      for key, value in sorted(entries.items()))
    if "--check" in sys.argv:
        if not path.exists() or path.read_text(encoding="utf-8") != content:
            system_language = "ru" if (locale.getlocale()[0] or "").startswith("ru") else "en"
            suffix = ".ru" if system_language == "ru" else ""
            catalog = ET.parse(source / f"Strings{suffix}.resx")
            message = next(item.findtext("value") for item in catalog.findall("data")
                           if item.attrib["name"] == "LocalizationExportOutdated")
            raise SystemExit(message.format(path))
    else:
        path.write_text(content, encoding="utf-8")
