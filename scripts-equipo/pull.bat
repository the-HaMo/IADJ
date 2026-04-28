@echo off
REM =====================================================================
REM PULL del repo IADJ.
REM
REM Uso: doble clic en pull.bat (debe estar en la raiz del repo).
REM
REM Hace:
REM   1) Verifica que Unity este cerrado.
REM   2) Si tienes cambios sin commitear, te avisa y para.
REM   3) git pull --rebase (limpia el historial).
REM   4) Te dice que abras Unity para que reimporte.
REM =====================================================================

setlocal enabledelayedexpansion

REM Movernos a la carpeta donde esta este .bat (la raiz del repo)
cd /d "%~dp0\.."

echo.
echo ============================
echo     PULL IADJ
echo ============================
echo.

REM 0. Comprobar que estamos en un repo git
if not exist ".git" (
    echo [ERROR] Este script tiene que estar en scripts-equipo\ dentro del repo IADJ.
    echo No encuentro la carpeta .git en %CD%
    pause
    exit /b 1
)

REM 1. Comprobar Unity cerrado
tasklist /FI "IMAGENAME eq Unity.exe" 2>NUL | find /I "Unity.exe" >NUL
if not errorlevel 1 (
    echo [ERROR] Unity esta abierto. Cierralo antes de hacer pull.
    pause
    exit /b 1
)

REM 2. Comprobar que no hay cambios locales sin commitear
git update-index --refresh >NUL 2>&1
git diff-index --quiet HEAD --
if errorlevel 1 (
    echo [ERROR] Tienes cambios locales sin commitear:
    echo.
    git status --short
    echo.
    echo Antes de hacer pull tienes que:
    echo   - O bien hacer commit con push.bat
    echo   - O bien guardar tus cambios con: git stash
    echo.
    pause
    exit /b 1
)

REM 3. Pull con rebase
echo Trayendo cambios del repo...
echo.
git pull --rebase
if errorlevel 1 (
    echo.
    echo [ERROR] Conflictos en el rebase. Resuelvelos a mano y luego ejecuta:
    echo   git add ^<archivo^>
    echo   git rebase --continue
    echo.
    pause
    exit /b 1
)

echo.
echo ============================
echo     PULL OK
echo ============================
echo.
echo Ahora abre Unity. La primera vez tardara unos segundos
echo en reimportar lo nuevo, es normal.
echo.
pause
