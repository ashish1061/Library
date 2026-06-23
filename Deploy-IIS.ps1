param (
    [string]$DeployPath = "C:\inetpub\wwwroot\LibrarySystem",
    [string]$BackendDomain = "library-api.jindalstainless.com",
    [string]$FrontendDomain = "library.jindalstainless.com"
)

# Ensure script is running as Administrator
if (-Not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] 'Administrator')) {
    Write-Warning "Please run this script as an Administrator."
    Exit
}

Import-Module WebAdministration

$FrontendPath = "$DeployPath\Frontend"
$BackendPath = "$DeployPath\Backend"

Write-Host "======================================" -ForegroundColor Cyan
Write-Host " Starting Library System Deployment" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

# 1. Clean / Prepare Directories
Write-Host "Preparing deployment directories..." -ForegroundColor Yellow
if (!(Test-Path $DeployPath)) { New-Item -ItemType Directory -Force -Path $DeployPath | Out-Null }
if (!(Test-Path $FrontendPath)) { New-Item -ItemType Directory -Force -Path $FrontendPath | Out-Null }
if (!(Test-Path $BackendPath)) { New-Item -ItemType Directory -Force -Path $BackendPath | Out-Null }

# Stop AppPools if they exist to unlock files
$Pools = @("LibraryAppPool", "LibraryApiPool", "LibraryAuthPool", "LibraryCatalogPool", "LibraryOperationsPool", "LibraryDocumentPool")
foreach ($pool in $Pools) {
    if (Get-WebAppPoolState -Name $pool -ErrorAction SilentlyContinue) {
        Stop-WebAppPool -Name $pool
    }
}
Start-Sleep -Seconds 2

# 2. Build and Publish Backend (.NET)
Write-Host "Building and Publishing .NET Backend..." -ForegroundColor Yellow

# Publish API Gateway to Root Backend folder
dotnet publish "Backend\ApiGateway\ApiGateway.csproj" -c Release -o $BackendPath
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to build ApiGateway."; Exit 1 }

# Publish microservices to subdirectories
dotnet publish "Backend\Auth.Service\Auth.Service.csproj" -c Release -o "$BackendPath\Auth"
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to build Auth.Service."; Exit 1 }
Copy-Item -Path ".\darwinbox_response.json" -Destination "$BackendPath\Auth\" -Force

dotnet publish "Backend\Catalog.Service\Catalog.Service.csproj" -c Release -o "$BackendPath\Catalog"
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to build Catalog.Service."; Exit 1 }

dotnet publish "Backend\Operations.Service\Operations.Service.csproj" -c Release -o "$BackendPath\Operations"
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to build Operations.Service."; Exit 1 }

dotnet publish "Backend\Document.Service\Document.Service.csproj" -c Release -o "$BackendPath\Document"
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to build Document.Service."; Exit 1 }

# 3. Build and Publish Frontend (Angular)
Write-Host "Building Angular Frontend..." -ForegroundColor Yellow
Set-Location ".\Frontend\LibraryApp"
npm install --legacy-peer-deps
if ($LASTEXITCODE -ne 0) { Set-Location "..\.."; Write-Error "Failed to run npm install."; Exit 1 }
npm run build -- --configuration development
if ($LASTEXITCODE -ne 0) { Set-Location "..\.."; Write-Error "Failed to build Angular frontend."; Exit 1 }

Write-Host "Copying Frontend files to IIS..." -ForegroundColor Yellow
Copy-Item -Path ".\dist\LibraryApp\browser\*" -Destination $FrontendPath -Recurse -Force
Set-Location "..\.."

# 4. Create web.config for Angular SPA Routing
Write-Host "Generating web.config for Angular..." -ForegroundColor Yellow
$WebConfigPath = "$FrontendPath\web.config"
$WebConfigContent = @"
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
Set-Content -Path $WebConfigPath -Value $WebConfigContent

# Locate or create localhost developer certificate for HTTPS bindings
Write-Host "Locating or creating localhost developer certificate..." -ForegroundColor Yellow
$cert = Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -like "*CN=localhost*" } | Select-Object -First 1
if (-not $cert) {
    $cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -like "*CN=localhost*" } | Select-Object -First 1
}
if (-not $cert) {
    Write-Host "Creating self-signed localhost certificate..." -ForegroundColor Yellow
    $cert = New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "cert:\LocalMachine\My" -FriendlyName "Localhost Dev Cert" -ErrorAction SilentlyContinue
}

$CertThumbprint = ""
if ($cert) {
    Write-Host "Using certificate with Thumbprint: $($cert.Thumbprint)" -ForegroundColor Green
    $CertThumbprint = $cert.Thumbprint
} else {
    Write-Warning "Could not find or create a localhost SSL certificate. HTTPS bindings will fail."
}

# Helper function to ensure a binding exists without throwing an error or duplicate key errors
function Ensure-WebBinding {
    param (
        [string]$SiteName,
        [string]$Protocol,
        [int]$Port,
        [string]$HostHeader,
        [string]$IPAddress = "*"
    )
    
    $binding = Get-WebBinding -Name $SiteName -Protocol $Protocol -Port $Port -HostHeader $HostHeader -IPAddress $IPAddress -ErrorAction SilentlyContinue
    if (-not $binding) {
        Write-Host "Adding $Protocol binding on port $Port for $HostHeader..." -ForegroundColor Yellow
        if ($Protocol -eq "https") {
            New-WebBinding -Name $SiteName -Protocol "https" -Port $Port -HostHeader $HostHeader -IPAddress $IPAddress -SslFlags 0 -ErrorAction SilentlyContinue
        } else {
            New-WebBinding -Name $SiteName -Protocol "http" -Port $Port -HostHeader $HostHeader -IPAddress $IPAddress -ErrorAction SilentlyContinue
        }
    } else {
        Write-Host "$Protocol binding on port $Port for $HostHeader already exists." -ForegroundColor Gray
    }

    # If protocol is HTTPS and we have a certificate thumbprint, ensure SSL binding is configured
    if ($Protocol -eq "https" -and $CertThumbprint -ne "") {
        $sslBinding = Get-Item -Path "IIS:\SslBindings\*!$Port" -ErrorAction SilentlyContinue
        if (-not $sslBinding) {
            Write-Host "Binding SSL certificate (Thumbprint: $CertThumbprint) to port $Port..." -ForegroundColor Yellow
            $certPath = "cert:\LocalMachine\My\$CertThumbprint"
            Get-Item $certPath | New-Item "IIS:\SslBindings\*!$Port" -Force -ErrorAction SilentlyContinue
        } else {
            Write-Host "SSL certificate already bound to port $Port." -ForegroundColor Gray
        }
    }
}

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


# 5. IIS Configuration
Write-Host "Configuring IIS App Pools and Sites..." -ForegroundColor Yellow

foreach ($pool in $Pools) {
    if (!(Get-WebAppPoolState -Name $pool -ErrorAction SilentlyContinue)) {
        New-WebAppPool -Name $pool
    }
    Set-ItemProperty "IIS:\AppPools\$pool" -Name "managedRuntimeVersion" -Value "" # No Managed Code
}

# In local mode, remove the Gateway website (LibraryAPI) if it exists to prevent port clashes
if (Get-Website -Name "LibraryAPI" -ErrorAction SilentlyContinue) {
    Write-Host "Removing Gateway site LibraryAPI for local direct port setup..." -ForegroundColor Yellow
    Remove-WebSite -Name "LibraryAPI"
}

# Helper function to ensure site exists on direct port and path
function Ensure-WebSiteDirect {
    param (
        [string]$SiteName,
        [int]$Port,
        [string]$PhysicalPath,
        [string]$AppPool,
        [string]$HostHeader = "",
        [string]$Protocol = "http"
    )
    
    if (Get-Website -Name $SiteName -ErrorAction SilentlyContinue) {
        Write-Host "Website $SiteName already exists. Updating physical path and application pool..." -ForegroundColor Yellow
        Set-WebConfigurationProperty -Filter "system.applicationHost/sites/site[@name='$SiteName']/application[@path='/']/virtualDirectory[@path='/']" -Name "physicalPath" -Value $PhysicalPath -ErrorAction SilentlyContinue
        Set-WebConfigurationProperty -Filter "system.applicationHost/sites/site[@name='$SiteName']/application[@path='/']" -Name "applicationPool" -Value $AppPool -ErrorAction SilentlyContinue
    } else {
        Write-Host "Creating Website $SiteName on port $Port ($Protocol)..." -ForegroundColor Yellow
        if ($HostHeader -ne "") {
            New-WebSite -Name $SiteName -Port $Port -HostHeader $HostHeader -PhysicalPath $PhysicalPath -ApplicationPool $AppPool -Protocol $Protocol -Force
        } else {
            New-WebSite -Name $SiteName -Port $Port -PhysicalPath $PhysicalPath -ApplicationPool $AppPool -Protocol $Protocol -Force
        }
    }
    
    # Ensure binding is present
    Ensure-WebBinding -SiteName $SiteName -Protocol $Protocol -Port $Port -HostHeader $HostHeader
}

# Configure Frontend Site on port 4201 only
if (Get-Website -Name "LibraryApp" -ErrorAction SilentlyContinue) {
    Write-Host "Website LibraryApp already exists. Clearing existing HTTP bindings to enforce port 4201 only..." -ForegroundColor Yellow
    Get-WebBinding -Name "LibraryApp" | ForEach-Object {
        Remove-WebBinding -Name "LibraryApp" -BindingInformation $_.bindingInformation -ErrorAction SilentlyContinue
    }
}
Ensure-WebSiteDirect -SiteName "LibraryApp" -Port 4201 -PhysicalPath $FrontendPath -AppPool "LibraryAppPool" -HostHeader "" -Protocol "http"

# Configure Standalone Backend Sites on ports 7001-7004
Ensure-WebSiteDirect -SiteName "Library_Auth" -Port 7001 -PhysicalPath "$BackendPath\Auth" -AppPool "LibraryAuthPool" -Protocol "https"
Ensure-WebSiteDirect -SiteName "Library_Catalog" -Port 7002 -PhysicalPath "$BackendPath\Catalog" -AppPool "LibraryCatalogPool" -Protocol "https"
Ensure-WebSiteDirect -SiteName "Library_Operations" -Port 7003 -PhysicalPath "$BackendPath\Operations" -AppPool "LibraryOperationsPool" -Protocol "https"
Ensure-WebSiteDirect -SiteName "Library_Document" -Port 7004 -PhysicalPath "$BackendPath\Document" -AppPool "LibraryDocumentPool" -Protocol "https"

# Create Virtual Directories under LibraryApp
Write-Host "Configuring Virtual Directories in IIS..." -ForegroundColor Yellow
$VDirs = @(
    @{ Name = "assets"; Path = "$FrontendPath\assets" },
    @{ Name = "media"; Path = "$FrontendPath\media" }
)

foreach ($vdir in $VDirs) {
    $vdirPath = "IIS:\Sites\LibraryApp\$($vdir.Name)"
    if (Test-Path $vdirPath) {
        Write-Host "Virtual directory $($vdir.Name) already exists. Updating physical path..." -ForegroundColor Yellow
        Set-WebConfigurationProperty -Filter "system.applicationHost/sites/site[@name='LibraryApp']/application[@path='/']/virtualDirectory[@path='/$($vdir.Name)']" -Name "physicalPath" -Value $vdir.Path
    } else {
        Write-Host "Creating Virtual directory $($vdir.Name)..." -ForegroundColor Yellow
        New-WebVirtualDirectory -Site "LibraryApp" -Name $vdir.Name -PhysicalPath $vdir.Path | Out-Null
    }
}

# Ensure index.html is the default document using AppCmd
Write-Host "Setting index.html as Default Document..." -ForegroundColor Yellow
$appcmd = "$env:systemroot\system32\inetsrv\appcmd.exe"
& $appcmd set config "LibraryApp" /section:defaultDocument /+files.[value='index.html'] | Out-Null

# Disable WebDAV in all published web.config files to prevent CORS preflight OPTIONS and PUT failures
Write-Host "Disabling WebDAV in web.config files..." -ForegroundColor Yellow
$WebConfigs = @(
    "$BackendPath\Auth\web.config",
    "$BackendPath\Catalog\web.config",
    "$BackendPath\Operations\web.config",
    "$BackendPath\Document\web.config"
)
foreach ($config in $WebConfigs) {
    Disable-WebDAVInWebConfig -WebConfigPath $config
}

# Start App Pools
foreach ($pool in $Pools) {
    Start-WebAppPool -Name $pool -ErrorAction SilentlyContinue
}

# Start Websites
$LocalSites = @("LibraryApp", "Library_Auth", "Library_Catalog", "Library_Operations", "Library_Document")
foreach ($site in $LocalSites) {
    Start-WebSite -Name $site -ErrorAction SilentlyContinue
}

Write-Host "======================================" -ForegroundColor Green
Write-Host " Local IIS Deployment Completed Successfully!" -ForegroundColor Green
Write-Host " Frontend URL: http://localhost:4201" -ForegroundColor Green
Write-Host " Auth Service URL: http://localhost:7001" -ForegroundColor Green
Write-Host " Catalog Service URL: http://localhost:7002" -ForegroundColor Green
Write-Host " Operations Service URL: http://localhost:7003" -ForegroundColor Green
Write-Host " Document Service URL: http://localhost:7004" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
