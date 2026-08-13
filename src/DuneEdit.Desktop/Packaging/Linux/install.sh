#!/usr/bin/env bash
set -euo pipefail

source_root=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
data_home=${XDG_DATA_HOME:-"$HOME/.local/share"}
application_root=${DUNEEDIT_INSTALL_ROOT:-"$data_home/DuneEdit"}
desktop_directory="$data_home/applications"
icon_directory="$data_home/icons/hicolor"
desktop_id=io.github.boazy.DuneEdit
executable="$application_root/DuneEdit.Desktop"

if [[ ! -x "$source_root/DuneEdit.Desktop" ]]; then
  echo "DuneEdit.Desktop is missing or not executable beside this installer." >&2
  exit 66
fi

mkdir -p "$application_root" "$desktop_directory" "$icon_directory"
if [[ "$source_root" != "$application_root" ]]; then
  cp -R "$source_root/." "$application_root/"
fi
cp -R "$source_root/share/icons/hicolor/." "$icon_directory/"

while IFS= read -r line; do
  if [[ "$line" == Exec=* ]]; then
    printf 'Exec="%s" %%f\n' "$executable"
  elif [[ "$line" == TryExec=* ]]; then
    printf 'TryExec="%s"\n' "$executable"
  else
    printf '%s\n' "$line"
  fi
done < "$source_root/share/applications/$desktop_id.desktop" \
  > "$desktop_directory/$desktop_id.desktop"
chmod +x "$desktop_directory/$desktop_id.desktop"

if command -v update-desktop-database >/dev/null 2>&1; then
  update-desktop-database "$desktop_directory"
fi
if command -v gtk-update-icon-cache >/dev/null 2>&1; then
  gtk-update-icon-cache --force --ignore-theme-index "$icon_directory" >/dev/null
fi

printf 'Installed DuneEdit to %s\n' "$application_root"
printf 'Registered launcher %s\n' "$desktop_directory/$desktop_id.desktop"
