#!/usr/bin/env bash
set -euo pipefail

VERSION="$1"
RID="$2"
case "$RID" in
  osx-x64|osx-arm64) ;;
  *) echo "Unsupported macOS RID: $RID" >&2; exit 1 ;;
esac

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SOURCE="$ROOT/publish/AvaWeather"
test -f "$SOURCE"
STAGING="$ROOT/obj/macos-installer-$RID"
APP="$STAGING/payload/Applications/AvaWeather.app"
CONTENTS="$APP/Contents"
ICONSET="$STAGING/AppIcon.iconset"
ICON="$ROOT/AvaWeather/Assets/app-icon.png"
mkdir -p "$CONTENTS/MacOS" "$CONTENTS/Resources" "$ICONSET" "$ROOT/dist"

cp "$SOURCE" "$CONTENTS/MacOS/AvaWeather"
chmod +x "$CONTENTS/MacOS/AvaWeather"
for size in 16 32 128 256 512; do
  sips -z "$size" "$size" "$ICON" --out "$ICONSET/icon_${size}x${size}.png" >/dev/null
  double=$((size * 2))
  sips -z "$double" "$double" "$ICON" --out "$ICONSET/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "$ICONSET" -o "$CONTENTS/Resources/AppIcon.icns"

python3 - "$CONTENTS/Info.plist" "$VERSION" <<'PY'
import plistlib
import sys
from pathlib import Path

path, version = sys.argv[1:]
info = {
    "CFBundleDevelopmentRegion": "en",
    "CFBundleDisplayName": "AvaWeather",
    "CFBundleExecutable": "AvaWeather",
    "CFBundleIconFile": "AppIcon",
    "CFBundleIdentifier": "io.github.gagarinbefree.AvaWeather",
    "CFBundleInfoDictionaryVersion": "6.0",
    "CFBundleName": "AvaWeather",
    "CFBundlePackageType": "APPL",
    "CFBundleShortVersionString": version,
    "CFBundleVersion": version,
    "LSMinimumSystemVersion": "12.0",
    "NSHighResolutionCapable": True,
}
with Path(path).open("wb") as output:
    plistlib.dump(info, output)
PY

codesign --force --sign - "$APP"
codesign --verify --strict "$APP"
PACKAGE="$ROOT/dist/AvaWeather-setup-$RID.pkg"
pkgbuild --root "$STAGING/payload" --install-location / \
  --identifier io.github.gagarinbefree.AvaWeather --version "$VERSION" "$PACKAGE"
test -s "$PACKAGE"

# Check the package contents without installing it on the build runner.
EXPANDED="$STAGING/expanded"
pkgutil --expand-full "$PACKAGE" "$EXPANDED"
PACKAGED_EXE="$(find "$EXPANDED" -path '*/Applications/AvaWeather.app/Contents/MacOS/AvaWeather' -type f -print -quit)"
test -n "$PACKAGED_EXE"
cmp "$CONTENTS/MacOS/AvaWeather" "$PACKAGED_EXE"
test -f "${PACKAGED_EXE%/MacOS/AvaWeather}/Resources/AppIcon.icns"
codesign --verify --strict "${PACKAGED_EXE%/Contents/MacOS/AvaWeather}"
