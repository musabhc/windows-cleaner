param(
    [string]$Subject = "CN=TemizPC Self-Signed",

    [Parameter(Mandatory = $true)]
    [string]$Password,

    [int]$ValidYears = 3,

    [string]$OutputDirectory = "",

    [switch]$InstallToTrustedStores
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
    $OutputDirectory = Join-Path $root "artifacts\signing"
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$securePassword = ConvertTo-SecureString -String $Password -AsPlainText -Force
$certificate = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject $Subject `
    -FriendlyName "TemizPC Self-Signed Code Signing" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyAlgorithm RSA `
    -KeyLength 4096 `
    -HashAlgorithm SHA256 `
    -KeyExportPolicy Exportable `
    -NotAfter (Get-Date).AddYears($ValidYears)

$pfxPath = Join-Path $OutputDirectory "TemizPC-codesign-selfsigned.pfx"
$cerPath = Join-Path $OutputDirectory "TemizPC-codesign-selfsigned.cer"
$pfxBase64Path = Join-Path $OutputDirectory "codesign-certificate-base64.txt"
$cerBase64Path = Join-Path $OutputDirectory "codesign-public-certificate-base64.txt"
$instructionsPath = Join-Path $OutputDirectory "github-secrets-instructions.txt"

Export-PfxCertificate `
    -Cert $certificate `
    -FilePath $pfxPath `
    -Password $securePassword | Out-Null

Export-Certificate `
    -Cert $certificate `
    -FilePath $cerPath `
    -Type CERT | Out-Null

[Convert]::ToBase64String([System.IO.File]::ReadAllBytes($pfxPath)) |
    Set-Content -Path $pfxBase64Path -NoNewline -Encoding ascii

[Convert]::ToBase64String([System.IO.File]::ReadAllBytes($cerPath)) |
    Set-Content -Path $cerBase64Path -NoNewline -Encoding ascii

$instructions = @"
GitHub secret values:

  CODESIGN_CERTIFICATE_BASE64
    Copy the contents of:
    $pfxBase64Path

  CODESIGN_CERTIFICATE_PASSWORD
    Use the password you passed to this script.

Optional:

  CODESIGN_TIMESTAMP_URL
    Leave empty or use http://timestamp.digicert.com

Public certificate for trusted installs:
  $cerPath

Base64 copy of the public certificate:
  $cerBase64Path
"@

Set-Content -Path $instructionsPath -Value $instructions -Encoding utf8

if ($InstallToTrustedStores) {
    Import-Certificate -FilePath $cerPath -CertStoreLocation "Cert:\CurrentUser\Root" | Out-Null
    Import-Certificate -FilePath $cerPath -CertStoreLocation "Cert:\CurrentUser\TrustedPublisher" | Out-Null
}

Write-Host "Self-signed code-signing certificate created."
Write-Host "PFX: $pfxPath"
Write-Host "CER: $cerPath"
Write-Host "PFX Base64: $pfxBase64Path"
Write-Host "Instructions: $instructionsPath"
Write-Host "Thumbprint: $($certificate.Thumbprint)"

