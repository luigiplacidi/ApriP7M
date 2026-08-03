# Crea un pacchetto MSIX firmato con un certificato di TEST, installabile in
# locale (sideload) per verificare il comportamento della versione pacchettizzata
# senza passare dal Microsoft Store. NON usare questo certificato per la
# distribuzione: serve solo a provare l'app sul proprio PC.

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$manifest = Join-Path $repoRoot 'src\ApriP7M.App\Package.appxmanifest'
$msix = Join-Path $repoRoot 'artifacts\Apri P7M x64.msix'
$outDir = Join-Path $repoRoot 'artifacts\sideload'
$cerOut = Join-Path $outDir 'ApriP7M-Test.cer'
$pfxOut = Join-Path $outDir 'ApriP7M-Test.pfx'
$msixOut = Join-Path $outDir 'Apri P7M Test x64.msix'

if (-not (Test-Path $msix)) {
    throw "Manca $msix. Esegui prima build/Package-Store.ps1."
}

# Publisher esatto dal manifest: il soggetto del certificato deve coincidere.
[xml]$xml = Get-Content -LiteralPath $manifest
$publisher = $xml.Package.Identity.Publisher
Write-Host "Publisher del pacchetto: $publisher"

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# 1) Certificato self-signed di test (code signing), valido 1 anno.
$cert = New-SelfSignedCertificate `
    -Type Custom `
    -Subject $publisher `
    -KeyUsage DigitalSignature `
    -FriendlyName 'Apri P7M Test (sideload)' `
    -CertStoreLocation 'Cert:\CurrentUser\My' `
    -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')

$pwd = ConvertTo-SecureString -String 'ApriP7M-test' -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath $pfxOut -Password $pwd | Out-Null
Export-Certificate -Cert $cert -FilePath $cerOut | Out-Null

# 2) signtool: dal Windows SDK installato o dal pacchetto nuget build-tools.
$signCandidates = @()
$signCandidates += Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin\*\x64\signtool.exe' -ErrorAction SilentlyContinue
$signCandidates += Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\microsoft.windows.sdk.buildtools\*\bin\*\x64\signtool.exe') -ErrorAction SilentlyContinue
$signtool = $signCandidates | Sort-Object FullName -Descending | Select-Object -First 1
if ($null -eq $signtool) {
    throw 'signtool.exe non trovato (Windows SDK o pacchetto nuget build-tools).'
}
Write-Host "signtool: $($signtool.FullName)"

# 3) Firma una copia del pacchetto.
Copy-Item -LiteralPath $msix -Destination $msixOut -Force
& $signtool.FullName sign /fd SHA256 /f $pfxOut /p 'ApriP7M-test' $msixOut
if ($LASTEXITCODE -ne 0) { throw 'Firma del pacchetto non riuscita.' }

# Rimuove il certificato dallo store personale: il .pfx resta nei file.
Remove-Item -Path ("Cert:\CurrentUser\My\" + $cert.Thumbprint) -Force -ErrorAction SilentlyContinue

Write-Host ''
Write-Host 'Pacchetto di test pronto:' -ForegroundColor Green
Write-Host "  $msixOut"
Write-Host "  $cerOut  (certificato da fidare)"
Write-Host ''
Write-Host 'Per installarlo:' -ForegroundColor Cyan
Write-Host '  1) Doppio clic su ApriP7M-Test.cer -> Installa certificato ->'
Write-Host '     Computer locale -> Autorità di certificazione radice attendibili.'
Write-Host '  2) Doppio clic sul .msix -> Installa (o: Add-AppxPackage).'
Write-Host '  3) Apri un .p7m e verifica che l''anteprima PDF appaia.'
