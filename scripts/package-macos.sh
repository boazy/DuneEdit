#!/usr/bin/env bash
set -euo pipefail
script_directory=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
app_icon="$script_directory/../src/DuneEdit.Desktop/Assets/AppIcon/DuneEdit.icns"


if [[ $# -lt 2 || $# -gt 3 ]]; then
  echo "Usage: $0 <publish-directory> <output.dmg> [semantic-version]" >&2
  exit 64
fi

publish_directory=$1
output_dmg=$2
semantic_version=${3:-1.0.0}
bundle_version=${semantic_version%%[-+]*}
app_name=DuneEdit
executable_name=DuneEdit.Desktop
bundle_identifier=io.github.boazy.DuneEdit

if [[ ! -d "$publish_directory" ]]; then
  echo "Publish directory does not exist: $publish_directory" >&2
  exit 66
fi

if [[ ! -f "$app_icon" ]]; then
  echo "macOS application icon does not exist: $app_icon" >&2
  exit 66
fi

if [[ ! -x "$publish_directory/$executable_name" ]]; then
  echo "Published application executable is missing or not executable: $publish_directory/$executable_name" >&2
  exit 66
fi

if [[ "$output_dmg" != *.dmg ]]; then
  echo "Output path must end in .dmg: $output_dmg" >&2
  exit 64
fi

output_directory=$(dirname "$output_dmg")
mkdir -p "$output_directory"
output_directory=$(cd "$output_directory" && pwd)
output_dmg="$output_directory/$(basename "$output_dmg")"

work_root=$(mktemp -d "${TMPDIR:-/tmp}/duneedit-package.XXXXXX")
trap 'rm -rf "$work_root"' EXIT

dmg_root="$work_root/dmg"
app_bundle="$dmg_root/$app_name.app"
contents_directory="$app_bundle/Contents"
macos_directory="$contents_directory/MacOS"
resources_directory="$contents_directory/Resources"
info_plist="$contents_directory/Info.plist"

mkdir -p "$macos_directory" "$resources_directory"
/usr/bin/ditto "$publish_directory" "$macos_directory"
/usr/bin/ditto "$app_icon" "$resources_directory/DuneEdit.icns"

/usr/bin/plutil -create xml1 "$info_plist"
/usr/bin/plutil -insert CFBundleDevelopmentRegion -string en "$info_plist"
/usr/bin/plutil -insert CFBundleDisplayName -string "$app_name" "$info_plist"
/usr/bin/plutil -insert CFBundleExecutable -string "$executable_name" "$info_plist"
/usr/bin/plutil -insert CFBundleIconFile -string DuneEdit.icns "$info_plist"
/usr/bin/plutil -insert CFBundleIdentifier -string "$bundle_identifier" "$info_plist"
/usr/bin/plutil -insert CFBundleInfoDictionaryVersion -string 6.0 "$info_plist"
/usr/bin/plutil -insert CFBundleName -string "$app_name" "$info_plist"
/usr/bin/plutil -insert CFBundlePackageType -string APPL "$info_plist"
/usr/bin/plutil -insert CFBundleGetInfoString -string "$semantic_version" "$info_plist"
/usr/bin/plutil -insert CFBundleShortVersionString -string "$bundle_version" "$info_plist"
/usr/bin/plutil -insert CFBundleVersion -string "$bundle_version" "$info_plist"
/usr/bin/plutil -insert NSHighResolutionCapable -bool YES "$info_plist"

/usr/bin/codesign --force --deep --sign - --timestamp=none "$app_bundle"
/usr/bin/codesign --verify --deep --strict "$app_bundle"

ln -s /Applications "$dmg_root/Applications"
rm -f "$output_dmg"
/usr/bin/hdiutil create \
  -quiet \
  -volname "$app_name" \
  -srcfolder "$dmg_root" \
  -format UDZO \
  -ov \
  "$output_dmg"

echo "Created $output_dmg"
