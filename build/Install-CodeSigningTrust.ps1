param(
    [Parameter(Mandatory = $true)]
    [string]$CertificatePath,

    [ValidateSet("CurrentUser", "LocalMachine")]
    [string]$Scope = "CurrentUser"
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

if (-not (Test-Path -Path $CertificatePath -PathType Leaf)) {
    throw "Certificate file not found: $CertificatePath"
}

if ($Scope -eq "LocalMachine") {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "LocalMachine trust installation requires an elevated PowerShell session."
    }
}

$resolvedPath = (Resolve-Path $CertificatePath).Path
$certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($resolvedPath)

Import-Certificate -FilePath $resolvedPath -CertStoreLocation "Cert:\$Scope\Root" | Out-Null
Import-Certificate -FilePath $resolvedPath -CertStoreLocation "Cert:\$Scope\TrustedPublisher" | Out-Null

Write-Host "Installed certificate trust for scope: $Scope"
Write-Host "Subject: $($certificate.Subject)"
Write-Host "Thumbprint: $($certificate.Thumbprint)"
Write-Host "Stores: Root, TrustedPublisher"
