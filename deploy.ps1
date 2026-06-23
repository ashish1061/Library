# ==============================================================================
# Library Management System - Deployment Script
# This script builds the Angular frontend and publishes all .NET microservices,
# preparing them for IIS deployment. It also generates the required web.config
# for the Angular application to support HTML5 routing in IIS.
# ==============================================================================

$ErrorActionPreference = "Stop"

# Define Paths
$ProjectRoot = $PSScriptRoot
$PublishDir = Join-Path $ProjectRoot "publish"
$FrontendDir = Join-Path $ProjectRoot "Frontend\LibraryApp"
$BackendDir = Join-Path $ProjectRoot "Backend"

$Services = @(
    "ApiGateway",
    "Auth.Service",
    "Catalog.Service",
    "Operations.Service",
    "Document.Service"
)

Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host " Starting Deployment Build Process" -ForegroundColor Cyan
Write-Host " Publish Directory: $PublishDir" -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan

# 1. Clean previous publish directory
if (Test-Path $PublishDir) {
    Write-Host "Cleaning previous publish directory..." -ForegroundColor Yellow
    Remove-Item -Path $PublishDir -Recurse -Force
}
New-Item -ItemType Directory -Path $PublishDir | Out-Null

# ==============================================================================
# 2. Build and Publish Frontend (Angular)
# ==============================================================================
Write-Host "`n[1/3] Building Angular Frontend..." -ForegroundColor Green
Set-Location $FrontendDir

Write-Host "Running npm install..." -ForegroundColor DarkGray
npm install

Write-Host "Running npm run build..." -ForegroundColor DarkGray
# Note: Ensure package.json has a "build" script, usually "ng build --configuration production"
npm run build -- --configuration production

# Copy Angular build to publish directory
$FrontendPublishPath = Join-Path $PublishDir "Frontend"
$AngularDistDir = Join-Path $FrontendDir "dist\library-app\browser" # Update if your dist folder name differs

if (-Not (Test-Path $AngularDistDir)) {
    # Fallback for older Angular versions
    $AngularDistDir = Join-Path $FrontendDir "dist\library-app"
    if (-Not (Test-Path $AngularDistDir)) {
        $AngularDistDir = Join-Path $FrontendDir "dist"
    }
}

Write-Host "Copying frontend build files to $FrontendPublishPath" -ForegroundColor DarkGray
Copy-Item -Path "$AngularDistDir\*" -Destination $FrontendPublishPath -Recurse -Force

# Create web.config for Angular HTML5 routing in IIS
$AngularWebConfigPath = Join-Path $FrontendPublishPath "web.config"
$AngularWebConfigContent = @"
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
            <!-- Ignore API routes if hosted on the same domain, though usually they are separate sites -->
            <add input="{REQUEST_URI}" pattern="^/(api)" negate="true" />
          </conditions>
          <action type="Rewrite" url="/index.html" />
        </rule>
      </rules>
    </rewrite>
    <!-- Add static content MIME types just in case -->
    <staticContent>
      <remove fileExtension=".json" />
      <mimeMap fileExtension=".json" mimeType="application/json" />
      <remove fileExtension=".woff2" />
      <mimeMap fileExtension=".woff2" mimeType="font/woff2" />
    </staticContent>
  </system.webServer>
</configuration>
"@

Set-Content -Path $AngularWebConfigPath -Value $AngularWebConfigContent
Write-Host "Created web.config for Angular routing." -ForegroundColor Green

# ==============================================================================
# 3. Build and Publish Backend Microservices
# ==============================================================================
Write-Host "`n[2/3] Publishing .NET Microservices..." -ForegroundColor Green

foreach ($service in $Services) {
    $ServiceSourcePath = Join-Path $BackendDir $service
    $ServicePublishPath = Join-Path $PublishDir "Backend\$service"
    
    if (Test-Path $ServiceSourcePath) {
        Write-Host "Publishing $service..." -ForegroundColor Cyan
        Set-Location $ServiceSourcePath
        
        # dotnet publish handles restoring, building, and generating the IIS web.config via AspNetCoreModuleV2
        dotnet publish -c Release -o $ServicePublishPath /p:UseAppHost=false
    } else {
        Write-Host "Warning: Service $service not found at $ServiceSourcePath" -ForegroundColor Yellow
    }
}

# ==============================================================================
# 4. Finish
# ==============================================================================
Set-Location $ProjectRoot
Write-Host "`n=======================================================" -ForegroundColor Cyan
Write-Host " Deployment Build Complete!" -ForegroundColor Green
Write-Host " All published files are located in: $PublishDir" -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host "`nInstructions for IIS Setup:" -ForegroundColor Yellow
Write-Host "1. Frontend: Point an IIS Website to 'publish\Frontend'. Install the 'URL Rewrite' IIS module if not installed."
Write-Host "2. Backend: Create separate IIS Applications/Websites for each service in 'publish\Backend'."
Write-Host "3. Ensure the 'ASP.NET Core Hosting Bundle' is installed on the server."
