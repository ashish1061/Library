param (
    [string]$StagingPath = ".\OfflineStaging",
    [string]$ZipName = "LibrarySystem_Production.zip"
)

Write-Host "======================================" -ForegroundColor Cyan
Write-Host " Building Offline Deployment Package" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

# Resolve absolute paths to prevent Set-Location relative path issues
$ProjectRoot = $PSScriptRoot
if (-not $ProjectRoot) { $ProjectRoot = Get-Location }

if (![System.IO.Path]::IsPathRooted($StagingPath)) {
    $StagingPath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($ProjectRoot, $StagingPath))
}
if (![System.IO.Path]::IsPathRooted($ZipName)) {
    $ZipPath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($ProjectRoot, $ZipName))
} else {
    $ZipPath = $ZipName
}

# Cleanup old staging
if (Test-Path $StagingPath) { Remove-Item -Path $StagingPath -Recurse -Force }
if (Test-Path $ZipPath) { Remove-Item -Path $ZipPath -Force }

$BackendPath = Join-Path $StagingPath "LibrarySystem\Backend"
$FrontendPath = Join-Path $StagingPath "LibrarySystem\Frontend"

New-Item -ItemType Directory -Force -Path $BackendPath | Out-Null
New-Item -ItemType Directory -Force -Path $FrontendPath | Out-Null

# 1. Build Backend
Write-Host "Building .NET Backend Microservices..." -ForegroundColor Yellow

# Publish API Gateway to Root Backend folder
dotnet publish (Join-Path $ProjectRoot "Backend\ApiGateway\ApiGateway.csproj") -c Release -o $BackendPath
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to build ApiGateway"; Exit 1 }

# Publish microservices to subdirectories
dotnet publish (Join-Path $ProjectRoot "Backend\Auth.Service\Auth.Service.csproj") -c Release -o (Join-Path $BackendPath "Auth")
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to build Auth.Service"; Exit 1 }
Copy-Item -Path (Join-Path $ProjectRoot "darwinbox_response.json") -Destination (Join-Path $BackendPath "Auth\") -Force

dotnet publish (Join-Path $ProjectRoot "Backend\Catalog.Service\Catalog.Service.csproj") -c Release -o (Join-Path $BackendPath "Catalog")
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to build Catalog.Service"; Exit 1 }

dotnet publish (Join-Path $ProjectRoot "Backend\Operations.Service\Operations.Service.csproj") -c Release -o (Join-Path $BackendPath "Operations")
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to build Operations.Service"; Exit 1 }

dotnet publish (Join-Path $ProjectRoot "Backend\Document.Service\Document.Service.csproj") -c Release -o (Join-Path $BackendPath "Document")
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to build Document.Service"; Exit 1 }

# 2. Build Frontend
Write-Host "Building Angular Frontend (Production Mode)..." -ForegroundColor Yellow
$FrontendSrc = Join-Path $ProjectRoot "Frontend\LibraryApp"
Set-Location $FrontendSrc

npm install --legacy-peer-deps
if ($LASTEXITCODE -ne 0) { Set-Location $ProjectRoot; Write-Error "Failed npm install"; Exit 1 }

npm run build -- --configuration production
if ($LASTEXITCODE -ne 0) { Set-Location $ProjectRoot; Write-Error "Failed Angular build"; Exit 1 }

Write-Host "Copying Frontend to Staging..." -ForegroundColor Yellow
Copy-Item -Path ".\dist\LibraryApp\browser\*" -Destination $FrontendPath -Recurse -Force
Set-Location $ProjectRoot

# Create web.config for Angular SPA routing
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
Set-Content -Path (Join-Path $FrontendPath "web.config") -Value $webConfigContent

# Helper function to disable WebDAV in web.config to prevent CORS block on PUT/DELETE/OPTIONS
function Disable-WebDAVInWebConfig {
    param (
        [string]$WebConfigPath
    )
    if (Test-Path $WebConfigPath) {
        try {
            [xml]$xml = Get-Content $WebConfigPath
            
            $systemWebServer = $xml.SelectSingleNode("//system.webServer")
            
            if ($null -ne $systemWebServer) {
                # 1. Disable WebDAV Handler
                $handlers = $systemWebServer.handlers
                if ($null -eq $handlers) {
                    $handlers = $xml.CreateElement("handlers")
                    $systemWebServer.AppendChild($handlers) | Out-Null
                }
                $hasWebDAVRemove = $false
                if ($null -ne $handlers.remove) {
                    foreach ($remove in $handlers.remove) {
                        if ($remove.name -eq "WebDAV") { $hasWebDAVRemove = $true }
                    }
                }
                if (-not $hasWebDAVRemove) {
                    $removeNode = $xml.CreateElement("remove")
                    $removeNode.SetAttribute("name", "WebDAV")
                    $handlers.PrependChild($removeNode) | Out-Null
                }
                
                # 2. Disable WebDAV Module
                $modules = $systemWebServer.modules
                if ($null -eq $modules) {
                    $modules = $xml.CreateElement("modules")
                    $systemWebServer.AppendChild($modules) | Out-Null
                }
                $hasWebDAVModuleRemove = $false
                if ($null -ne $modules.remove) {
                    foreach ($remove in $modules.remove) {
                        if ($remove.name -eq "WebDAVModule") { $hasWebDAVModuleRemove = $true }
                    }
                }
                if (-not $hasWebDAVModuleRemove) {
                    $removeNode = $xml.CreateElement("remove")
                    $removeNode.SetAttribute("name", "WebDAVModule")
                    $modules.PrependChild($removeNode) | Out-Null
                }
                
                $xml.Save($WebConfigPath)
                Write-Host "Successfully disabled WebDAV in $WebConfigPath" -ForegroundColor Green
            }
        } catch {
            Write-Warning "Could not update $WebConfigPath for WebDAV: $_"
        }
    }
}

function Add-SecurityHeadersInWebConfig {
    param (
        [string]$WebConfigPath
    )
    if (Test-Path $WebConfigPath) {
        try {
            [xml]$xml = Get-Content $WebConfigPath
            
            $systemWebServer = $xml.SelectSingleNode("//system.webServer")
            if ($null -eq $systemWebServer) {
                $systemWebServer = $xml.CreateElement("system.webServer")
                $xml.DocumentElement.AppendChild($systemWebServer) | Out-Null
            }

            # 1. requestFiltering removeServerHeader="true"
            $security = $systemWebServer.security
            if ($null -eq $security) {
                $security = $xml.CreateElement("security")
                $systemWebServer.AppendChild($security) | Out-Null
            }
            $requestFiltering = $security.requestFiltering
            if ($null -eq $requestFiltering) {
                $requestFiltering = $xml.CreateElement("requestFiltering")
                $security.AppendChild($requestFiltering) | Out-Null
            }
            $requestFiltering.SetAttribute("removeServerHeader", "true")

            # 2. httpProtocol customHeaders
            $httpProtocol = $systemWebServer.httpProtocol
            if ($null -eq $httpProtocol) {
                $httpProtocol = $xml.CreateElement("httpProtocol")
                $systemWebServer.AppendChild($httpProtocol) | Out-Null
            }
            $customHeaders = $httpProtocol.customHeaders
            if ($null -eq $customHeaders) {
                $customHeaders = $xml.CreateElement("customHeaders")
                $httpProtocol.AppendChild($customHeaders) | Out-Null
            }

            # Define headers to add and remove
            $headersToRemove = @("X-Powered-By")
            $headersToAdd = @{
                "Strict-Transport-Security" = "max-age=31536000; includeSubDomains"
                "Content-Security-Policy" = "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdnjs.cloudflare.com; font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com; img-src 'self' data: https:;"
                "Permissions-Policy" = "geolocation=(), microphone=(), camera=()"
                "X-Frame-Options" = "SAMEORIGIN"
                "X-Content-Type-Options" = "nosniff"
            }

            foreach ($header in $headersToRemove) {
                $hasRemove = $false
                if ($null -ne $customHeaders.remove) {
                    foreach ($removeNode in $customHeaders.remove) {
                        if ($removeNode.name -eq $header) { $hasRemove = $true }
                    }
                }
                if (-not $hasRemove) {
                    $node = $xml.CreateElement("remove")
                    $node.SetAttribute("name", $header)
                    $customHeaders.AppendChild($node) | Out-Null
                }
            }

            foreach ($header in $headersToAdd.Keys) {
                $hasAdd = $false
                if ($null -ne $customHeaders.add) {
                    foreach ($addNode in $customHeaders.add) {
                        if ($addNode.name -eq $header) {
                            $addNode.SetAttribute("value", $headersToAdd[$header])
                            $hasAdd = $true
                        }
                    }
                }
                if (-not $hasAdd) {
                    $node = $xml.CreateElement("add")
                    $node.SetAttribute("name", $header)
                    $node.SetAttribute("value", $headersToAdd[$header])
                    $customHeaders.AppendChild($node) | Out-Null
                }
            }
            
            $xml.Save($WebConfigPath)
            Write-Host "Successfully injected Security Headers in $WebConfigPath" -ForegroundColor Green
        } catch {
            Write-Warning "Could not update $WebConfigPath for Security Headers: $_"
        }
    }
}

Write-Host "Disabling WebDAV and injecting Security Headers in staging web.config files..." -ForegroundColor Yellow
$StagingWebConfigs = @(
    "$FrontendPath\web.config",
    "$BackendPath\web.config",
    "$BackendPath\Auth\web.config",
    "$BackendPath\Catalog\web.config",
    "$BackendPath\Operations\web.config",
    "$BackendPath\Document\web.config"
)
foreach ($config in $StagingWebConfigs) {
    Disable-WebDAVInWebConfig -WebConfigPath $config
    Add-SecurityHeadersInWebConfig -WebConfigPath $config
}

Write-Host "Copying Setup and Key Configuration Scripts to Staging..." -ForegroundColor Yellow
Copy-Item -Path (Join-Path $ProjectRoot "Setup-IIS-Production.ps1") -Destination (Join-Path $StagingPath "LibrarySystem\") -Force
Copy-Item -Path (Join-Path $ProjectRoot "Configure-Production-Keys.ps1") -Destination (Join-Path $StagingPath "LibrarySystem\") -Force

# 3. Zip the Package
Write-Host "Zipping the Deployment Package..." -ForegroundColor Yellow
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory((Join-Path $StagingPath "LibrarySystem"), $ZipPath)

Write-Host "======================================" -ForegroundColor Green
Write-Host " Package created successfully!" -ForegroundColor Green
Write-Host " File: $ZipPath" -ForegroundColor Green
Write-Host " Copy this ZIP to your offline server, extract it to C:\inetpub\wwwroot\LibrarySystem, and configure IIS." -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
