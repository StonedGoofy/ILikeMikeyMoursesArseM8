@echo off

set "params=%*"
cd /d "%~dp0" && ( if exist "%temp%\getadmin.vbs" del "%temp%\getadmin.vbs" ) && fsutil dirty query %systemdrive% 1>nul 2>nul || (  echo Set UAC = CreateObject^("Shell.Application"^) : UAC.ShellExecute "cmd.exe", "/k cd ""%~sdp0"" && %~s0 %params%", "", "runas", 1 >> "%temp%\getadmin.vbs" && "%temp%\getadmin.vbs" && exit /B )

SET PROJECT_PATH=%~dp0

REG ADD HKLM /F>nul 2>&1

IF NOT %ERRORLEVEL%==0 goto :no_permissions

:msc_path

echo Please paste the path to My Summer Car folder without the trailing slash.
echo An example path would be: "C:\Program Files (x86)\Steam\steamapps\common\My Summer Car"
set /p GAME_PATH=Path to My Summer Car:

if not exist "%GAME_PATH%/mysummercar.exe" goto invalid_path

setx /m MSCMP_GAME_PATH "%GAME_PATH%"

echo My Summer Car path has been written.

echo Preparing build folder structure.

if not exist %PROJECT_PATH%\bin mkdir %PROJECT_PATH%\bin

if not exist %PROJECT_PATH%\bin\Release mkdir %PROJECT_PATH%\bin\Release
copy %PROJECT_PATH%\3rdparty\steamapi\steam_api64.dll %PROJECT_PATH%\bin\Release
copy %PROJECT_PATH%\data\steam_appid.txt %PROJECT_PATH%\bin\Release
echo Release prepared.

if not exist "%PROJECT_PATH%\bin\Public Release" mkdir "%PROJECT_PATH%\bin\Public Release"
copy %PROJECT_PATH%\3rdparty\steamapi\steam_api64.dll "%PROJECT_PATH%\bin\Public Release"
copy %PROJECT_PATH%\data\steam_appid.txt "%PROJECT_PATH%\bin\Public Release"
echo Public release prepared.

if not exist %PROJECT_PATH%\bin\Debug mkdir %PROJECT_PATH%\bin\Debug
copy %PROJECT_PATH%\3rdparty\steamapi\steam_api64.dll %PROJECT_PATH%\bin\Debug
copy %PROJECT_PATH%\data\steam_appid.txt %PROJECT_PATH%\bin\Debug
echo Debug prepared.

echo Workspace has been setup
echo.
pause
exit

:invalid_path

echo.
echo Invalid path to My Summer Car.
echo Unable to find %GAME_PATH%/mysummercar.exe.
echo.
echo.
goto msc_path

:no_permissions
echo Please run setup.bat as administrator.
pause
