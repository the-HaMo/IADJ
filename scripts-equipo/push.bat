@echo off
REM =====================================================================
REM PUSH al repo IADJ.
REM
REM Uso: doble clic en push.bat (debe estar en scripts-equipo\ del repo).
REM
REM Hace:
REM   1) Verifica que Unity este cerrado (evita locks).
REM   2) Te muestra que ha cambiado.
REM   3) Te pide un mensaje de commit.
REM   4) git add . (solo lo que NO esta en .gitignore).
REM   5) git commit y git push.
REM
REM Si fallara el push porque hay cambios remotos, te dice que hagas pull.
REM =====================================================================

setlocal enabledelayedexpansion

cd /d "%~dp0\.."

echo.
echo ============================
echo     PUSH IADJ
echo ============================
echo.

REM 0. Comprobar repo git
if not exist ".git" (
    echo [ERROR] Este script tiene que estar en scripts-equipo\ dentro del repo IADJ.
    pause
    exit /b 1
)

REM 1. Comprobar Unity cerrado
tasklist /FI "IMAGENAME eq Unity.exe" 2>NUL | find /I "Unity.exe" >NUL
if not errorlevel 1 (
    echo [AVISO] Unity esta abierto. Es muy recomendable cerrarlo antes
    echo         de hacer commit (evita commitear ficheros que Unity esta
    echo         escribiendo a medias).
    echo.
    set /p continuar="Continuar de todos modos? (s/N): "
    if /I not "!continuar!"=="s" (
        echo Cancelado.
        pause
        exit /b 0
    )
)

REM 2. Mostrar que va a commitear
echo Cambios detectados:
echo --------------------
git status --short
echo --------------------
echo.

REM Si no hay cambios, salir
git diff --quiet HEAD --
if not errorlevel 1 (
    git ls-files --others --exclude-standard --error-unmatch . >NUL 2>&1
    if errorlevel 1 (
        echo No hay nada que commitear.
        pause
        exit /b 0
    )
)

REM 3. Pedir mensaje de commit
echo.
set /p mensaje="Mensaje de commit (descriptivo, ej: 'Anade mapa de influencia'): "

if "!mensaje!"=="" (
    echo [ERROR] El mensaje no puede estar vacio.
    pause
    exit /b 1
)

REM 4. Add + commit
echo.
echo Haciendo add y commit...
git add .
git commit -m "!mensaje!"
if errorlevel 1 (
    echo [ERROR] Fallo el commit. Revisa el mensaje de error arriba.
    pause
    exit /b 1
)

REM 5. Push
echo.
echo Subiendo al repositorio remoto...
git push
if errorlevel 1 (
    echo.
    echo [ERROR] Fallo el push. Lo mas probable es que hay cambios remotos
    echo         que tienes que traer primero. Ejecuta pull.bat y luego
    echo         vuelve a hacer push.
    pause
    exit /b 1
)

echo.
echo ============================
echo     PUSH OK
echo ============================
echo Tus compañeros ya pueden hacer pull con pull.bat
echo.
pause
