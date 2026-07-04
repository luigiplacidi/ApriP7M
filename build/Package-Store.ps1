$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$storeDir = Join-Path $repoRoot 'artifacts\store'
$msixOut = Join-Path $repoRoot 'artifacts\Apri P7M x64.msix'
$uploadOut = Join-Path $repoRoot 'artifacts\Apri P7M StoreUpload x64.msixupload'

Set-Location $repoRoot

# Self-contained: lo Store NON installa .NET per conto dell'app. Senza
# runtime incluso, sui PC senza .NET 10 l'app chiede di installare il
# Desktop Runtime al primo avvio.
dotnet publish src\ApriP7M.App\ApriP7M.App.csproj `
    -c Release `
    -p:Platform=x64 `
    -r win-x64 `
    --self-contained true `
    -p:UapAppxPackageBuildMode=StoreUpload `
    -p:AppxBundle=Never `
    -p:AppxPackageSigningEnabled=false `
    -p:AppxSymbolPackageEnabled=false `
    -p:IncludeDebugSymbolsProjectOutputGroup=false `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $storeDir

$msix = Get-ChildItem src\ApriP7M.App\AppPackages -Recurse -Filter *.msix |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

$upload = Get-ChildItem src\ApriP7M.App\AppPackages -Recurse -Filter *.msixupload |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $msix) {
    throw 'Pacchetto MSIX non trovato.'
}

if ($null -eq $upload) {
    throw 'Pacchetto MSIXUPLOAD non trovato.'
}

Copy-Item -LiteralPath $msix.FullName -Destination $msixOut -Force
Copy-Item -LiteralPath $upload.FullName -Destination $uploadOut -Force

Get-Item -LiteralPath $msixOut, $uploadOut | Select-Object FullName, Length, LastWriteTime
