#!/usr/bin/env bash
# =====================================================================
# PUSH al repo IADJ (Mac / Linux).
# Uso: ./push.sh desde la carpeta scripts-equipo/.
# =====================================================================

set -e

cd "$(dirname "$0")/.."

echo
echo "============================"
echo "     PUSH IADJ"
echo "============================"
echo

if [ ! -d ".git" ]; then
    echo "[ERROR] Este script debe estar en scripts-equipo/ dentro del repo IADJ."
    exit 1
fi

if pgrep -x "Unity" > /dev/null; then
    echo "[AVISO] Unity esta abierto. Es muy recomendable cerrarlo."
    read -p "Continuar de todos modos? (s/N): " continuar
    if [ "$continuar" != "s" ] && [ "$continuar" != "S" ]; then
        echo "Cancelado."
        exit 0
    fi
fi

echo "Cambios detectados:"
echo "--------------------"
git status --short
echo "--------------------"
echo

if git diff --quiet HEAD -- && [ -z "$(git ls-files --others --exclude-standard)" ]; then
    echo "No hay nada que commitear."
    exit 0
fi

read -p "Mensaje de commit: " mensaje

if [ -z "$mensaje" ]; then
    echo "[ERROR] El mensaje no puede estar vacio."
    exit 1
fi

echo
echo "Haciendo add y commit..."
git add .
git commit -m "$mensaje"

echo
echo "Subiendo al repositorio remoto..."
if ! git push; then
    echo
    echo "[ERROR] Fallo el push. Probablemente hay cambios remotos."
    echo "Ejecuta ./pull.sh y luego vuelve a intentarlo."
    exit 1
fi

echo
echo "============================"
echo "     PUSH OK"
echo "============================"
echo "Tus compañeros ya pueden hacer pull con pull.sh"
