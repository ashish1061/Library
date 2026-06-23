@echo off
echo =======================================================
echo  Library Management System - Starting All Services
echo =======================================================

:: Kill any stale dotnet watch processes to free ports
echo Stopping any existing dotnet processes...
taskkill /F /IM dotnet.exe >nul 2>&1
timeout /t 2 /nobreak >nul

:: -------------------------------------------------------
:: Backend Microservices (HTTPS profile, hot-reload)
:: -------------------------------------------------------
echo Starting Auth Service         (https://localhost:7001)...
start "Auth Service     [7001]" cmd /k "cd /d "%~dp0Backend\Auth.Service" && dotnet watch run --launch-profile https"
timeout /t 3 /nobreak >nul

echo Starting Catalog Service      (https://localhost:7002)...
start "Catalog Service  [7002]" cmd /k "cd /d "%~dp0Backend\Catalog.Service" && dotnet watch run --launch-profile https"
timeout /t 3 /nobreak >nul

echo Starting Operations Service   (https://localhost:7003)...
start "Operations Svc   [7003]" cmd /k "cd /d "%~dp0Backend\Operations.Service" && dotnet watch run --launch-profile https"
timeout /t 3 /nobreak >nul

echo Starting Document Service     (https://localhost:7004)...
start "Document Service [7004]" cmd /k "cd /d "%~dp0Backend\Document.Service" && dotnet watch run --launch-profile https"
timeout /t 3 /nobreak >nul

echo Starting API Gateway          (https://localhost:7035)...
start "API Gateway      [7035]" cmd /k "cd /d "%~dp0Backend\ApiGateway" && dotnet watch run --launch-profile https"
timeout /t 3 /nobreak >nul

:: -------------------------------------------------------
:: Frontend Angular App (port 4201)
:: -------------------------------------------------------
echo Starting Angular Frontend     (http://localhost:4201)...
start "Angular Frontend [4201]" cmd /k "cd /d "%~dp0Frontend\LibraryApp" && npm start"

echo.
echo =======================================================
echo  All services are starting in separate windows!
echo.
echo  Frontend : http://localhost:4201
echo  Auth     : https://localhost:7001/swagger
echo  Catalog  : https://localhost:7002/swagger
echo  Ops      : https://localhost:7003/swagger
echo  Document : https://localhost:7004/swagger
echo  Gateway  : https://localhost:7035/swagger
echo =======================================================
echo.
echo  Wait 60-90 seconds for all services to compile...
echo =======================================================
