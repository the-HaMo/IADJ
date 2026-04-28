#!/usr/bin/env bash
# =====================================================================
# PULL del repo IADJ (Mac / Linux).
# Uso: ./pull.sh desde la carpeta scripts-equipo/.
# =====================================================================

set -e

cd "$(dirname "$0")/.."

echo
echo "============================"
echo "     PULL IADJ"
echo "============================"
echo

if [ ! -d ".git" ]; then
    echo "[ERROR] Este script debe estar en scripts-equipo/ dentro del repo IADJ."
    exit 1
fi

if pgrep -x "Unity" > /dev/null; then
    echo "[ERROR] Unity esta abierto. Cierralo antes de hacer pull."
    exit 1
fi

if ! git diff-index --quiet HEAD --; then
    echo "[ERROR] Tienes cambios locales sin commitear:"
    echo
    git status --short
    echo
    echo "Antes de hacer pull tienes que:"
    echo "  - O bien hacer commit con push.sh"
    echo "  - O bien guardar tus cambios con: git stash"
    exit 1
fi

echo "Trayendo cambios del repo..."
echo
git pull --rebase

echo
echo "============================"
echo "     PULL OK"
echo "============================"
echo "Ahora abre Unity para que reimporte lo nuevo."
