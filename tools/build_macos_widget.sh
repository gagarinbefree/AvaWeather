#!/usr/bin/env bash
set -euo pipefail

VERSION="$1"
ARCH="$2"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$ROOT/Widgets/macOS"
DERIVED="$PROJECT/obj/derived-$ARCH"
mkdir -p "$ROOT/dist" "$PROJECT/Shared" "$DERIVED"

python3 "$ROOT/tools/export_swift_localization.py"

swift "$ROOT/tools/embed_swift_key.swift" "$PROJECT/Shared/EmbeddedKey.generated.swift"
(
  cd "$PROJECT"
  xcodegen generate --spec project.yml
)
xcodebuild -project "$PROJECT/AvaWeatherWidget.xcodeproj" \
  -scheme AvaWeatherWidgetHost -configuration Release \
  -destination 'generic/platform=macOS' -derivedDataPath "$DERIVED" \
  -arch "$ARCH" CODE_SIGNING_ALLOWED=NO \
  MARKETING_VERSION="$VERSION" CURRENT_PROJECT_VERSION="${GITHUB_RUN_NUMBER:-1}" build

APP="$DERIVED/Build/Products/Release/AvaWeatherWidgetHost.app"
test -d "$APP/Contents/PlugIns/WeatherWidgetExtension.appex"
plutil -lint "$APP/Contents/Info.plist" "$APP/Contents/PlugIns/WeatherWidgetExtension.appex/Contents/Info.plist"
codesign --force --sign - --entitlements "$PROJECT/Network.entitlements" "$APP/Contents/PlugIns/WeatherWidgetExtension.appex"
codesign --force --sign - --entitlements "$PROJECT/Network.entitlements" "$APP"
codesign --verify --deep --strict "$APP"
ditto -c -k --sequesterRsrc --keepParent "$APP" "$ROOT/dist/AvaWeather-widget-osx-$ARCH.zip"
