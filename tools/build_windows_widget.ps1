param(
    [Parameter(Mandatory = $true)][string]$Version,
    [Parameter(Mandatory = $true)][ValidateSet('x64', 'arm64')][string]$Architecture,
    [string]$OutputDirectory = 'dist'
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$output = Join-Path $root $OutputDirectory
$stage = Join-Path $root "Widgets/Windows/obj/package-$Architecture"
$published = Join-Path $root "Widgets/Windows/obj/publish-$Architecture"
New-Item -ItemType Directory -Force -Path $output, $stage, $published | Out-Null

$publishArgs = @(
    'publish', (Join-Path $root 'Widgets/Windows/AvaWeather.Widget.Windows.csproj'),
    '-c', 'Release', '-r', "win-$Architecture", '--self-contained', 'true',
    '-p:WindowsPackageType=None', "-p:Version=$Version", '-p:UseAppHost=true',
    '-o', $published
)
if (Test-Path (Join-Path $root 'AvaWeather/obj/EmbeddedWeatherApiKey.g.cs')) {
    $publishArgs += '-p:EmbeddedKeySource=obj/EmbeddedWeatherApiKey.g.cs'
}
dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw 'Windows widget publish failed.' }

Copy-Item -Path (Join-Path $published '*') -Destination $stage -Recurse -Force
$assets = Join-Path $stage 'Assets'
New-Item -ItemType Directory -Force -Path $assets | Out-Null
Copy-Item (Join-Path $root 'AvaWeather/Assets/app-icon.png') (Join-Path $assets 'app-icon.png')
Copy-Item (Join-Path $root 'AvaWeather/Assets/app-icon.png') (Join-Path $assets 'widget-preview.png')

$manifest = Get-Content -Raw (Join-Path $root 'Widgets/Windows/Package.appxmanifest')
$manifest = $manifest.Replace('Version="1.0.0.0"', "Version=`"$Version.0`"")
$manifest = $manifest.Replace('ProcessorArchitecture="x64"', "ProcessorArchitecture=`"$Architecture`"")
[System.IO.File]::WriteAllText((Join-Path $stage 'AppxManifest.xml'), $manifest)

$sdkBin = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Directory |
    Where-Object { $_.Name -match '^10\.' } | Sort-Object Name -Descending | Select-Object -First 1
if (-not $sdkBin) { throw 'Windows SDK packaging tools are missing.' }
$makeAppx = Join-Path $sdkBin.FullName 'x64/makeappx.exe'
$signTool = Join-Path $sdkBin.FullName 'x64/signtool.exe'
if (-not (Test-Path $makeAppx) -or -not (Test-Path $signTool)) { throw 'makeappx or signtool is missing.' }
$package = Join-Path $output "AvaWeather-widget-win-$Architecture.msix"
& $makeAppx pack /d $stage /p $package /o
if ($LASTEXITCODE -ne 0) { throw 'MSIX packaging failed.' }

# A development certificate makes the MSIX installable after its public certificate is trusted.
$certificate = New-SelfSignedCertificate -Type CodeSigningCert -Subject 'CN=AvaWeather Development' `
    -CertStoreLocation 'Cert:\CurrentUser\My' -KeyExportPolicy Exportable `
    -NotAfter (Get-Date).AddYears(2)
$publicCert = Join-Path $output "AvaWeather-widget-win-$Architecture.cer"
Export-Certificate -Cert $certificate -FilePath $publicCert | Out-Null
$pfx = Join-Path $env:TEMP "avaweather-widget-$Architecture.pfx"
$password = ConvertTo-SecureString ([guid]::NewGuid().ToString('N')) -AsPlainText -Force
try {
    Export-PfxCertificate -Cert $certificate -FilePath $pfx -Password $password | Out-Null
    $plainPassword = [System.Net.NetworkCredential]::new('', $password).Password
    & $signTool sign /fd SHA256 /f $pfx /p $plainPassword $package
    if ($LASTEXITCODE -ne 0) { throw 'MSIX signing failed.' }
} finally {
    Remove-Item -LiteralPath $pfx -ErrorAction SilentlyContinue
    Remove-Item -Path "Cert:\CurrentUser\My\$($certificate.Thumbprint)" -ErrorAction SilentlyContinue
}
