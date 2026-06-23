param (
    [string]$RemotePath = "\\172.20.20.210\c$\inetpub\wwwroot\LibrarySystem",
    [string]$LocalStaging = ".\LocalStaging"
)

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host " Starting Automated Remote Deployment to IIS" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan

# Resolve absolute paths
$ProjectRoot = $PSScriptRoot
if (-not $ProjectRoot) { $ProjectRoot = Get-Location }

$LocalStagingPath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($ProjectRoot, $LocalStaging))
$RemoteBackendPath = Join-Path $RemotePath "Backend"
$RemoteFrontendPath = Join-Path $RemotePath "Frontend"

# 1. Verify remote path accessibility
if (-not (Test-Path $RemotePath)) {
    Write-Error "Cannot access remote server path at $RemotePath. Please ensure you have network access and permissions."
    Exit 1
}

# 2. Cleanup old local staging folder
if (Test-Path $LocalStagingPath) {
    Remove-Item -Path $LocalStagingPath -Recurse -Force
}
$LocalBackendStaging = Join-Path $LocalStagingPath "Backend"
$LocalFrontendStaging = Join-Path $LocalStagingPath "Frontend"
New-Item -ItemType Directory -Force -Path $LocalBackendStaging | Out-Null
New-Item -ItemType Directory -Force -Path $LocalFrontendStaging | Out-Null

# 3. Build & Publish Backend locally
Write-Host "`n[1/6] Building and publishing .NET Backend locally..." -ForegroundColor Yellow
dotnet publish "Backend\ApiGateway\ApiGateway.csproj" -c Release -o $LocalBackendStaging
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to publish ApiGateway"; Exit 1 }

$Subfolders = @("Auth", "Catalog", "Operations", "Document")
foreach ($folder in $Subfolders) {
    dotnet publish "Backend\$folder.Service\$folder.Service.csproj" -c Release -o (Join-Path $LocalBackendStaging $folder)
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to publish $folder.Service"; Exit 1 }
}

# 4. Build Frontend locally
Write-Host "`n[2/6] Building Angular Frontend locally..." -ForegroundColor Yellow
Set-Location ".\Frontend\LibraryApp"
npm run build -- --configuration production
if ($LASTEXITCODE -ne 0) {
    Set-Location $ProjectRoot
    Write-Error "Failed to build Angular Frontend"
    Exit 1
}
Copy-Item -Path ".\dist\LibraryApp\browser\*" -Destination $LocalFrontendStaging -Recurse -Force
Set-Location $ProjectRoot

# Generate web.config for Angular routing in local staging
$webConfigContent = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
<system.webServer>
  <rewrite>
    <rules>
      <rule name="Angular Routes" stopProcessing="true">
        <match url=".*" />
        <conditions logicalGrouping="MatchAll">
          <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
          <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
        </conditions>
        <action type="Rewrite" url="/index.html" />
      </rule>
    </rules>
  </rewrite>
</system.webServer>
</configuration>
"@
Set-Content -Path (Join-Path $LocalFrontendStaging "web.config") -Value $webConfigContent

# 5. Take remote server offline to unlock DLL files
Write-Host "`n[3/6] Taking remote IIS server offline to unlock DLL files..." -ForegroundColor Yellow
$OfflineContent = "<html><body><h2>System Undergoing Maintenance. Please try again shortly.</h2></body></html>"
Set-Content -Path (Join-Path $RemoteBackendPath "app_offline.htm") -Value $OfflineContent
foreach ($folder in $Subfolders) {
    Set-Content -Path (Join-Path $RemoteBackendPath "$folder\app_offline.htm") -Value $OfflineContent
}
Start-Sleep -Seconds 2  # Give IIS time to release locks

# 6. Back up remote production configuration files
Write-Host "`n[4/6] Backing up remote production appsettings.json configuration files..." -ForegroundColor Yellow
$BackupDir = Join-Path $ProjectRoot "ProductionBackup"
if (Test-Path $BackupDir) { Remove-Item -Path $BackupDir -Recurse -Force }
New-Item -ItemType Directory -Path $BackupDir | Out-Null

if (Test-Path (Join-Path $RemoteBackendPath "appsettings.json")) {
    Copy-Item -Path (Join-Path $RemoteBackendPath "appsettings.json") -Destination $BackupDir -Force
}
foreach ($folder in $Subfolders) {
    $remoteConfig = Join-Path $RemoteBackendPath "$folder\appsettings.json"
    if (Test-Path $remoteConfig) {
        Copy-Item -Path $remoteConfig -Destination (New-Item -ItemType Directory -Path (Join-Path $BackupDir $folder) -Force) -Force
    }
}

# 7. Copy new files to remote server
Write-Host "`n[5/6] Deploying new backend and frontend files to the remote server..." -ForegroundColor Yellow

# Copy Backend Files (excluding local appsettings.json to avoid accidental overwrites)
Copy-Item -Path "$LocalBackendStaging\*" -Destination $RemoteBackendPath -Recurse -Force -Exclude "appsettings.json", "appsettings.Development.json"

# Copy Frontend Files
Copy-Item -Path "$LocalFrontendStaging\*" -Destination $RemoteFrontendPath -Recurse -Force

# 8. Restore production appsettings.json files
Write-Host "`n[6/6] Restoring production appsettings.json configurations..." -ForegroundColor Yellow
if (Test-Path (Join-Path $BackupDir "appsettings.json")) {
    Copy-Item -Path (Join-Path $BackupDir "appsettings.json") -Destination $RemoteBackendPath -Force
}
foreach ($folder in $Subfolders) {
    $backupConfig = Join-Path $BackupDir "$folder\appsettings.json"
    if (Test-Path $backupConfig) {
        Copy-Item -Path $backupConfig -Destination (Join-Path $RemoteBackendPath $folder) -Force
    }
}

# 9. Bring remote server back online
Write-Host "`nBringing remote IIS server back online..." -ForegroundColor Green
Remove-Item -Path (Join-Path $RemoteBackendPath "app_offline.htm") -ErrorAction SilentlyContinue
foreach ($folder in $Subfolders) {
    Remove-Item -Path (Join-Path $RemoteBackendPath "$folder\app_offline.htm") -ErrorAction SilentlyContinue
}

# Clean local staging and backups
Remove-Item -Path $LocalStagingPath -Recurse -Force
Remove-Item -Path $BackupDir -Recurse -Force

Write-Host "`n==============================================" -ForegroundColor Green
Write-Host " Deployment Complete & Server is Online!" -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Green
