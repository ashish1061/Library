param (
    [string]$PhysicalPath = "C:\inetpub\wwwroot\LibrarySystem\Backend",
    [string]$JwtKey = "",
    [string]$ConnectionString = ""
)

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host " Configuring Production JWT Keys & Logs" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan

# 1. Resolve JWT Key
if ([string]::IsNullOrEmpty($JwtKey)) {
    # Generate a secure 32-byte key
    $bytes = New-Object Byte[] 32
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($bytes)
    $JwtKey = [Convert]::ToBase64String($bytes)
    Write-Host "No JWT key provided. Generated a secure 32-byte production key: $JwtKey" -ForegroundColor Yellow
} else {
    if ($JwtKey.Length -lt 32) {
        Write-Warning "The provided JWT Key is less than 32 characters/bytes. We recommend a key of at least 256 bits (32 bytes)."
    }
}

$Subfolders = @("Auth", "Catalog", "Operations", "Document")

# 2. Update appsettings.json files
foreach ($folder in $Subfolders) {
    $configPath = "$PhysicalPath\$folder\appsettings.json"
    if (Test-Path $configPath) {
        Write-Host "Updating appsettings.json in $folder..." -ForegroundColor Yellow
        $json = Get-Content $configPath -Raw | ConvertFrom-Json
        
        # Ensure Jwt object exists
        if ($null -eq $json.Jwt) {
            $json | Add-Member -MemberType NoteProperty -Name "Jwt" -Value @{ Key = "" }
        }
        
        $json.Jwt.Key = $JwtKey
        
        # Optionally update connection string if provided
        if ($ConnectionString -ne "") {
            if ($null -ne $json.ConnectionStrings) {
                $json.ConnectionStrings.LibraryDB = $ConnectionString
            }
        }
        
        $json | ConvertTo-Json -Depth 20 | Out-File $configPath -Encoding utf8 -Force
        Write-Host "Successfully updated JWT Key in $configPath" -ForegroundColor Green
    } else {
        Write-Warning "Could not find appsettings.json at $configPath"
    }
}

# 3. Enable stdout logging in web.configs for troubleshooting
foreach ($folder in $Subfolders) {
    $webConfigPath = "$PhysicalPath\$folder\web.config"
    if (Test-Path $webConfigPath) {
        Write-Host "Enabling stdout logging in $folder/web.config..." -ForegroundColor Yellow
        [xml]$xml = Get-Content $webConfigPath
        $aspNetCore = $xml.SelectSingleNode("//aspNetCore")
        if ($null -ne $aspNetCore) {
            $aspNetCore.SetAttribute("stdoutLogEnabled", "true")
            $xml.Save($webConfigPath)
            
            # Ensure stdout logs directory exists
            $logsPath = "$PhysicalPath\$folder\logs"
            if (!(Test-Path $logsPath)) {
                New-Item -ItemType Directory -Path $logsPath | Out-Null
            }
            Write-Host "Enabled stdout logs in $webConfigPath" -ForegroundColor Green
        }
    }
}

Write-Host "Configuration completed. Please restart IIS App Pools for changes to take effect." -ForegroundColor Green
