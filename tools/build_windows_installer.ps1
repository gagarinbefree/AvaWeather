param(
    [Parameter(Mandatory = $true)][ValidateSet('x64', 'arm64')][string]$Architecture,
    [Parameter(Mandatory = $true)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$publishedExe = Join-Path $root 'publish/AvaWeather.exe'
if (-not (Test-Path -LiteralPath $publishedExe)) { throw "Missing published application: $publishedExe" }

$compiler = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -First 1
if (-not $compiler) {
    $compiler = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6/ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6/ISCC.exe')
    ) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $compiler) { throw 'Inno Setup 6 compiler (ISCC.exe) was not found' }

$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null
& $compiler "/DAppVersion=$Version" "/DTargetArch=$Architecture" "/O$dist" (Join-Path $PSScriptRoot 'AvaWeather.iss')
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed with exit code $LASTEXITCODE" }

$installer = Join-Path $dist "AvaWeather-setup-win-$Architecture.exe"
if (-not (Test-Path -LiteralPath $installer)) { throw "Missing installer: $installer" }

# Exercise the installation and uninstallation, and verify the installed payload.
$tempRoot = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { [System.IO.Path]::GetTempPath() }
$installDir = Join-Path $tempRoot "AvaWeather-installer-check-$Architecture"
$installed = Start-Process -FilePath $installer -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/NOCANCEL', "/DIR=`"$installDir`"") -Wait -PassThru -WindowStyle Hidden
if ($installed.ExitCode -ne 0) { throw "Silent install failed with exit code $($installed.ExitCode)" }
$installedExe = Join-Path $installDir 'AvaWeather.exe'
if (-not (Test-Path -LiteralPath $installedExe)) { throw "Installer did not install $installedExe" }
$sourceHash = (Get-FileHash -LiteralPath $publishedExe -Algorithm SHA256).Hash
$installedHash = (Get-FileHash -LiteralPath $installedExe -Algorithm SHA256).Hash
if ($sourceHash -ne $installedHash) { throw 'Installed executable differs from the published executable' }
$uninstaller = Join-Path $installDir 'unins000.exe'
if (-not (Test-Path -LiteralPath $uninstaller)) { throw 'Installer did not register an uninstaller' }
$uninstalled = Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART') -Wait -PassThru -WindowStyle Hidden
if ($uninstalled.ExitCode -ne 0) { throw "Silent uninstall failed with exit code $($uninstalled.ExitCode)" }
if (Test-Path -LiteralPath $installedExe) { throw 'Uninstaller did not remove the application' }
