param (
    [string]$PhysicalPath = "C:\inetpub\wwwroot\LibrarySystem",
    [string]$FrontendDomain = "library.jindalstainless.com",
    [string]$BackendDomain = "library-api.jindalstainless.com",
    [string]$CertThumbprint = "" # Optional: Add your certificate thumbprint here if you know it
)

# Ensure script is running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Warning "Please run this script as Administrator."
    Exit
}

Import-Module WebAdministration

Write-Host "======================================" -ForegroundColor Cyan
Write-Host " Configuring IIS for Library System" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

$FrontendPath = "$PhysicalPath\Frontend"
$BackendPath = "$PhysicalPath\Backend"

# Helper function to ensure a binding exists without throwing an error or duplicate key errors
function Ensure-WebBinding {
    param (
        [string]$SiteName,
        [string]$Protocol,
        [int]$Port,
        [string]$HostHeader,
        [string]$IPAddress = "*",
        [bool]$Ssl = $false
    )
    
    $binding = Get-WebBinding -Name $SiteName -Protocol $Protocol -Port $Port -HostHeader $HostHeader -IPAddress $IPAddress -ErrorAction SilentlyContinue
    if (-not $binding) {
        Write-Host "Adding $Protocol binding on port $Port for $HostHeader..." -ForegroundColor Yellow
        if ($Ssl) {
            # New-WebBinding with HTTPS protocol will configure SslFlags
            New-WebBinding -Name $SiteName -Protocol $Protocol -Port $Port -HostHeader $HostHeader -IPAddress $IPAddress -SslFlags 1 -ErrorAction SilentlyContinue
        } else {
            New-WebBinding -Name $SiteName -Protocol $Protocol -Port $Port -HostHeader $HostHeader -IPAddress $IPAddress -ErrorAction SilentlyContinue
        }
    } else {
        Write-Host "$Protocol binding on port $Port for $HostHeader already exists." -ForegroundColor Gray
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


# 1. Clean up old obsolete microservice sites (which are now sub-apps of LibraryAPI)
Write-Host "Removing obsolete standalone microservice sites if they exist..." -ForegroundColor Yellow
$ObsoleteSites = @("Library_Auth", "Library_Catalog", "Library_Operations", "Library_Document")
foreach ($site in $ObsoleteSites) {
    if (Get-Website -Name $site -ErrorAction SilentlyContinue) { 
        Write-Host "Removing obsolete site $site..." -ForegroundColor Yellow
        Remove-Website -Name $site 
    }
}

# 2. Create or Update Application Pools
Write-Host "Configuring Application Pools..." -ForegroundColor Yellow
$Pools = @("LibraryAppPool", "LibraryApiPool", "LibraryAuthPool", "LibraryCatalogPool", "LibraryOperationsPool", "LibraryDocumentPool")
foreach ($pool in $Pools) {
    if (!(Get-WebAppPoolState -Name $pool -ErrorAction SilentlyContinue)) {
        Write-Host "Creating App Pool $pool..." -ForegroundColor Yellow
        New-WebAppPool -Name $pool
    }
    Set-ItemProperty -Path "IIS:\AppPools\$pool" -Name "managedRuntimeVersion" -Value "" # No Managed Code
}

# 3. Create or Update Frontend Site (LibraryApp)
Write-Host "Configuring Frontend Site (LibraryApp)..." -ForegroundColor Yellow
if (Get-Website -Name "LibraryApp" -ErrorAction SilentlyContinue) {
    Write-Host "Website LibraryApp already exists. Updating physical path and application pool..." -ForegroundColor Yellow
    Set-ItemProperty -Path "IIS:\Sites\LibraryApp" -Name physicalPath -Value $FrontendPath
    Set-ItemProperty -Path "IIS:\Sites\LibraryApp" -Name applicationPool -Value "LibraryAppPool"
} else {
    Write-Host "Creating Frontend Site LibraryApp..." -ForegroundColor Yellow
    New-Website -Name "LibraryApp" -PhysicalPath $FrontendPath -ApplicationPool "LibraryAppPool" -Port 80 -HostHeader $FrontendDomain
}

# Ensure standard bindings are present
Ensure-WebBinding -SiteName "LibraryApp" -Protocol "http" -Port 80 -HostHeader $FrontendDomain
Ensure-WebBinding -SiteName "LibraryApp" -Protocol "https" -Port 443 -HostHeader $FrontendDomain -Ssl $true

if ($CertThumbprint -ne "") {
    Write-Host "Binding SSL certificate to Frontend..." -ForegroundColor Yellow
    $certPath = "cert:\LocalMachine\My\$CertThumbprint"
    Get-Item $certPath | New-Item "IIS:\SslBindings\*!443!$FrontendDomain" -ErrorAction SilentlyContinue
}

# Ensure index.html is the default document using AppCmd
Write-Host "Setting index.html as Default Document..." -ForegroundColor Yellow
$appcmd = "$env:systemroot\system32\inetsrv\appcmd.exe"
& $appcmd set config "LibraryApp" /section:defaultDocument /+files.[value='index.html'] | Out-Null

# 4. Create or Update Backend Gateway Site (LibraryAPI)
Write-Host "Configuring Backend Gateway Site (LibraryAPI)..." -ForegroundColor Yellow
if (Get-Website -Name "LibraryAPI" -ErrorAction SilentlyContinue) {
    Write-Host "Website LibraryAPI already exists. Updating physical path and application pool..." -ForegroundColor Yellow
    Set-ItemProperty -Path "IIS:\Sites\LibraryAPI" -Name physicalPath -Value $BackendPath
    Set-ItemProperty -Path "IIS:\Sites\LibraryAPI" -Name applicationPool -Value "LibraryApiPool"
} else {
    Write-Host "Creating Backend Gateway Site LibraryAPI..." -ForegroundColor Yellow
    New-Website -Name "LibraryAPI" -PhysicalPath $BackendPath -ApplicationPool "LibraryApiPool" -Port 80 -HostHeader $BackendDomain
}

# Ensure standard bindings are present (Ports 80 & 443 only)
Ensure-WebBinding -SiteName "LibraryAPI" -Protocol "http" -Port 80 -HostHeader $BackendDomain
Ensure-WebBinding -SiteName "LibraryAPI" -Protocol "https" -Port 443 -HostHeader $BackendDomain -Ssl $true
Ensure-WebBinding -SiteName "LibraryAPI" -Protocol "http" -Port 80 -HostHeader "localhost"

if ($CertThumbprint -ne "") {
    Write-Host "Binding SSL certificate to Backend Gateway..." -ForegroundColor Yellow
    $certPath = "cert:\LocalMachine\My\$CertThumbprint"
    Get-Item $certPath | New-Item "IIS:\SslBindings\*!443!$BackendDomain" -ErrorAction SilentlyContinue
}

# 5. Create or Update Sub-Applications under LibraryAPI
Write-Host "Configuring Sub-Applications in IIS..." -ForegroundColor Yellow
$SubApps = @(
    @{ Name = "Auth"; Path = "$BackendPath\Auth"; Pool = "LibraryAuthPool" },
    @{ Name = "Catalog"; Path = "$BackendPath\Catalog"; Pool = "LibraryCatalogPool" },
    @{ Name = "Operations"; Path = "$BackendPath\Operations"; Pool = "LibraryOperationsPool" },
    @{ Name = "Document"; Path = "$BackendPath\Document"; Pool = "LibraryDocumentPool" }
)

foreach ($app in $SubApps) {
    $appPath = "IIS:\Sites\LibraryAPI\$($app.Name)"
    if (Test-Path $appPath) {
        Write-Host "Sub-application $($app.Name) already exists. Updating physical path and pool..." -ForegroundColor Yellow
        Set-WebConfigurationProperty -Filter "system.applicationHost/sites/site[@name='LibraryAPI']/application[@path='/$($app.Name)']/virtualDirectory[@path='/']" -Name "physicalPath" -Value $app.Path
        Set-WebConfigurationProperty -Filter "system.applicationHost/sites/site[@name='LibraryAPI']/application[@path='/$($app.Name)']" -Name "applicationPool" -Value $app.Pool
    } else {
        Write-Host "Creating Sub-application $($app.Name)..." -ForegroundColor Yellow
        New-WebApplication -Site "LibraryAPI" -Name $app.Name -PhysicalPath $app.Path -ApplicationPool $app.Pool | Out-Null
    }
}

# Create or Update Virtual Directories under LibraryApp
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

# Disable WebDAV in all published web.config files to prevent CORS preflight OPTIONS and PUT failures
Write-Host "Disabling WebDAV in web.config files..." -ForegroundColor Yellow
$WebConfigs = @(
    "$BackendPath\web.config",
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
$ProdSites = @("LibraryApp", "LibraryAPI")
foreach ($site in $ProdSites) {
    Start-WebSite -Name $site -ErrorAction SilentlyContinue
}

Write-Host "======================================" -ForegroundColor Green
Write-Host " IIS Configuration Complete!" -ForegroundColor Green
if ($CertThumbprint -eq "") {
    Write-Host " NOTE: Please verify or manually configure SSL certificates in IIS for your HTTPS bindings." -ForegroundColor Yellow
}
Write-Host "======================================" -ForegroundColor Green
