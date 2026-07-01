$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $repoRoot 'artifacts\ApriP7M-win-x64'
$payloadZip = Join-Path $repoRoot 'artifacts\ApriP7M-payload.zip'
$installerDir = Join-Path $repoRoot 'artifacts\installer-publish'
$installerExe = Join-Path $repoRoot 'artifacts\Apri P7M Setup x64.exe'

function Assert-ChildPath {
    param(
        [Parameter(Mandatory = $true)][string]$Parent,
        [Parameter(Mandatory = $true)][string]$Child
    )

    $parentFull = [System.IO.Path]::GetFullPath($Parent).TrimEnd('\') + '\'
    $childFull = [System.IO.Path]::GetFullPath($Child)
    if (-not $childFull.StartsWith($parentFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Percorso non valido: $childFull"
    }
}

function Remove-ExtraMuiLanguages {
    param([Parameter(Mandatory = $true)][string]$Root)

    $rootFull = (Resolve-Path -LiteralPath $Root).Path
    Get-ChildItem -LiteralPath $rootFull -Directory -Recurse |
        Where-Object {
            $_.Name -ne 'it-IT' -and
            (Get-ChildItem -LiteralPath $_.FullName -File -Filter '*.mui' -ErrorAction SilentlyContinue | Select-Object -First 1)
        } |
        Sort-Object FullName -Descending |
        ForEach-Object {
            Assert-ChildPath -Parent $rootFull -Child $_.FullName
            Remove-Item -LiteralPath $_.FullName -Recurse -Force
        }
}

function Optimize-PublishOutput {
    param([Parameter(Mandatory = $true)][string]$Root)

    $rootFull = (Resolve-Path -LiteralPath $Root).Path
    Remove-ExtraMuiLanguages -Root $rootFull

    $patterns = @(
        '*.pdb',
        'mscordaccore*.dll',
        'mscordbi.dll',
        'Microsoft.DiaSymReader.Native.amd64.dll',
        'onnxruntime.dll',
        'DirectML.dll',
        'Microsoft.Windows.AI.*.dll',
        'workloads*.json'
    )

    foreach ($pattern in $patterns) {
        Get-ChildItem -LiteralPath $rootFull -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue |
            Remove-Item -Force
    }

    $latoDir = Join-Path $rootFull 'LatoFont'
    if (Test-Path -LiteralPath $latoDir) {
        Get-ChildItem -LiteralPath $latoDir -File |
            Where-Object { $_.Name -notin @('Lato-Regular.ttf', 'Lato-Bold.ttf', 'OFL.txt') } |
            Remove-Item -Force
    }
}

Set-Location $repoRoot

dotnet publish src\ApriP7M.App\ApriP7M.App.csproj `
    -c Release `
    -p:Platform=x64 `
    -p:WindowsPackageType=None `
    -r win-x64 `
    --self-contained true `
    -o $publishDir

Optimize-PublishOutput -Root $publishDir

if (Test-Path -LiteralPath $payloadZip) {
    Remove-Item -LiteralPath $payloadZip -Force
}
Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $payloadZip -Force

if (Test-Path -LiteralPath $installerDir) {
    Remove-Item -LiteralPath $installerDir -Recurse -Force
}

if (Test-Path -LiteralPath $installerExe) {
    Remove-Item -LiteralPath $installerExe -Force
}

dotnet publish src\ApriP7M.Installer\ApriP7M.Installer.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -o $installerDir

Copy-Item -LiteralPath (Join-Path $installerDir 'ApriP7M-Setup-x64.exe') -Destination $installerExe -Force

Get-Item -LiteralPath $installerExe, $payloadZip | Select-Object FullName, Length, LastWriteTime
