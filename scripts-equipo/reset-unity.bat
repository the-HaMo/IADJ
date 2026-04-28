@echo off
REM =====================================================================
REM ULTIMO RECURSO: borra todo lo que Unity regenera por si algo se
REM corrompio (Library, Temp, Logs, csproj, sln, .vscode).
REM
REM NO TOCA tu codigo ni tus assets. NO toca el repo.
REM Despues de ejecutarlo, abre Unity y dejara que reimporte todo
REM (tarda algunos minutos la primera vez).
REM =====================================================================

setlocal

echo.
echo === RESET UNITY (borra solo cache local) ===
echo.

REM Comprobar Unity cerrado
tasklist /FI "IMAGENAME eq Unity.exe" 2>NUL | find /I /N "Unity.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo [ERROR] Cierra Unity antes de continuar.
    pause
    exit /b 1
)

set PROY=IADJ_Proyecto

if not exist "%PROY%" (
    echo [ERROR] No encuentro la carpeta %PROY%. Ejecuta esto en la raiz del repo.
    pause
    exit /b 1
)

echo Borrando cache local de Unity...
if exist "%PROY%\Library"      rmdir /s /q "%PROY%\Library"
if exist "%PROY%\Temp"         rmdir /s /q "%PROY%\Temp"
if exist "%PROY%\Logs"         rmdir /s /q "%PROY%\Logs"
if exist "%PROY%\obj"          rmdir /s /q "%PROY%\obj"
if exist "%PROY%\.vscode"      rmdir /s /q "%PROY%\.vscode"
if exist "%PROY%\UserSettings" rmdir /s /q "%PROY%\UserSettings"

del /q "%PROY%\*.csproj" 2>nul
del /q "%PROY%\*.sln"    2>nul

echo.
echo === LISTO ===
echo Abre Unity. La primera apertura tardara varios minutos
echo regenerando la cache (es normal).
echo.
pause
