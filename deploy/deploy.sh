#!/usr/bin/env bash
# Serverda kodni GitHub'dan yangilab, ilovani qayta ishga tushiradi.
# Ishlatish:  bash /opt/pravamarkaz/src/deploy/deploy.sh
set -euo pipefail

SRC_DIR="${SRC_DIR:-/opt/pravamarkaz/src}"     # git clone qilingan papka
APP_DIR="${APP_DIR:-/var/www/pravamarkaz}"     # ilova ishlaydigan papka
SERVICE="${SERVICE:-pravamarkaz}"              # systemd xizmat nomi
BUILD_DIR="$SRC_DIR/.publish"

echo "==> Kod yangilanmoqda ($SRC_DIR)"
cd "$SRC_DIR"
git pull --ff-only

echo "==> Build (Release)"
rm -rf "$BUILD_DIR"
dotnet publish "$SRC_DIR/PravaMarkaz.csproj" -c Release -o "$BUILD_DIR" --nologo

echo "==> Ilova to'xtatilmoqda"
sudo systemctl stop "$SERVICE" || true

echo "==> Fayllar ko'chirilmoqda ($APP_DIR)"
mkdir -p "$APP_DIR"
# App_Data (rasmlar, kalitlar) va appsettings.Production.json saqlanib qoladi
rsync -a --exclude 'App_Data/' --exclude 'appsettings.Production.json' "$BUILD_DIR/" "$APP_DIR/"

echo "==> Ilova ishga tushirilmoqda"
sudo systemctl start "$SERVICE"
sleep 3
sudo systemctl --no-pager --lines=5 status "$SERVICE" || true

echo "==> Tayyor: $(git log -1 --format='%h %s')"
