@echo off
powershell.exe -NoProfile -File "%~dp0ai-dev.ps1" %*
exit /b %ERRORLEVEL%
