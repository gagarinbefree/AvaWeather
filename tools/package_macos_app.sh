#!/usr/bin/env bash
set -euo pipefail

version="$1"
rid="$2"
publish_dir="$3"
dist_dir="$4"
icon_png="AvaWeather/Assets/app-icon.png"
work_dir="${RUNNER_TEMP:-/tmp}/avaweather-${rid}"
iconset="$work_dir/AvaWeather.iconset"
bundle="$work_dir/AvaWeather.app"

mkdir -p "$iconset" "$bundle/Contents/MacOS" "$bundle/Contents/Resources" "$dist_dir"

while read -r filename size; do
  sips -s format png -z "$size" "$size" "$icon_png" --out "$iconset/$filename" >/dev/null
done <<'SIZES'
icon_16x16.png 16
icon_16x16@2x.png 32
icon_32x32.png 32
icon_32x32@2x.png 64
icon_128x128.png 128
icon_128x128@2x.png 256
icon_256x256.png 256
icon_256x256@2x.png 512
icon_512x512.png 512
icon_512x512@2x.png 1024
SIZES

iconutil -c icns "$iconset" -o "$bundle/Contents/Resources/AvaWeather.icns"
cp "$publish_dir/AvaWeather" "$bundle/Contents/MacOS/AvaWeather"
chmod +x "$bundle/Contents/MacOS/AvaWeather"

cat > "$bundle/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleIdentifier</key><string>com.gagarinbefree.avaweather</string>
  <key>CFBundleName</key><string>AvaWeather</string>
  <key>CFBundleDisplayName</key><string>AvaWeather</string>
  <key>CFBundleExecutable</key><string>AvaWeather</string>
  <key>CFBundleIconFile</key><string>AvaWeather.icns</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleVersion</key><string>$version</string>
  <key>CFBundleShortVersionString</key><string>$version</string>
  <key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
PLIST

plutil -lint "$bundle/Contents/Info.plist"
tar -C "$work_dir" -czf "$dist_dir/AvaWeather-${rid}.app.tar.gz" AvaWeather.app
