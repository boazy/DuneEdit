#!/usr/bin/env bash
set -euo pipefail

script_directory=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
repository_root=$(cd "$script_directory/.." && pwd)
asset_root="$repository_root/src/DuneEdit.Desktop/Assets/AppIcon"
default_source_svg="$asset_root/Source/rising-maw.svg"
source_svg=${1:-"$default_source_svg"}
if [[ $# -ge 2 ]]; then
  small_source_svg=$2
elif [[ "$source_svg" == "$default_source_svg" ]]; then
  small_source_svg="$asset_root/Small/rising-maw.svg"
else
  small_source_svg="$source_svg"
fi
preview_root="$asset_root/Preview"
linux_root="$asset_root/Linux/hicolor"

for tool in rsvg-convert magick iconutil; do
  if ! command -v "$tool" >/dev/null 2>&1; then
    echo "Required icon tool is unavailable: $tool" >&2
    exit 69
  fi
done

for source in "$source_svg" "$small_source_svg"; do
  if [[ ! -f "$source" ]]; then
    echo "Icon source does not exist: $source" >&2
    exit 66
  fi
done

work_root=$(mktemp -d "${TMPDIR:-/tmp}/duneedit-icons.XXXXXX")
trap 'rm -rf "$work_root"' EXIT

render_png() {
  local size=$1
  local output=$2
  local source=${3:-"$source_svg"}
  mkdir -p "$(dirname "$output")"
  rsvg-convert --width "$size" --height "$size" --output "$output" "$source"
}

render_png 512 "$asset_root/DuneEdit.png"

linux_sizes=(16 24 32 48 64 128 256 512)
for size in "${linux_sizes[@]}"; do
  render_source=$source_svg
  if (( size <= 32 )); then
    render_source=$small_source_svg
  fi
  render_png "$size" "$linux_root/${size}x${size}/apps/io.github.boazy.DuneEdit.png" "$render_source"
done
mkdir -p "$linux_root/scalable/apps"
cp "$source_svg" "$linux_root/scalable/apps/io.github.boazy.DuneEdit.svg"

windows_sizes=(16 20 24 32 40 48 64 128 256)
windows_images=()
for size in "${windows_sizes[@]}"; do
  output="$work_root/windows-$size.png"
  render_source=$source_svg
  if (( size <= 32 )); then
    render_source=$small_source_svg
  fi
  render_png "$size" "$output" "$render_source"
  windows_images+=("$output")
done
magick "${windows_images[@]}" "$asset_root/DuneEdit.ico"

iconset="$work_root/DuneEdit.iconset"
mkdir -p "$iconset"
render_png 16 "$iconset/icon_16x16.png" "$small_source_svg"
render_png 32 "$iconset/icon_16x16@2x.png" "$small_source_svg"
render_png 32 "$iconset/icon_32x32.png" "$small_source_svg"
render_png 64 "$iconset/icon_32x32@2x.png"
render_png 128 "$iconset/icon_128x128.png"
render_png 256 "$iconset/icon_128x128@2x.png"
render_png 256 "$iconset/icon_256x256.png"
render_png 512 "$iconset/icon_256x256@2x.png"
render_png 512 "$iconset/icon_512x512.png"
render_png 1024 "$iconset/icon_512x512@2x.png"
iconutil --convert icns --output "$asset_root/DuneEdit.icns" "$iconset"

mkdir -p "$preview_root"
for option in "$asset_root"/Source/*.svg; do
  option_name=$(basename "$option" .svg)
  rsvg-convert --width 1024 --height 1024 \
    --output "$preview_root/$option_name.png" "$option"
done
magick \
  "$preview_root/rising-maw.png" \
  "$preview_root/wormsign.png" \
  "$preview_root/desert-seal.png" \
  -resize 384x384 -background '#101C43' +append \
  "$preview_root/icon-options.png"

echo "Generated app icons from $source_svg"
