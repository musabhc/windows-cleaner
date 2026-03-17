param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$RepositoryUrl,

    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$dotnetHome = Join-Path $root ".dotnet"
$toolDir = Join-Path $root ".tools"
$publishDir = Join-Path $root "artifacts\publish\$Version\$Runtime"
$releasesDir = Join-Path $root "artifacts\Releases\$Version"
$releaseNotesPath = Join-Path $root "artifacts\release-notes-$Version.md"
$certificateDirectory = Join-Path $root "artifacts\signing"
$certificatePath = Join-Path $certificateDirectory "codesign.pfx"
$iconPath = Join-Path $root "src\TemizPC.App\Assets\TemizPC.ico"
$splashPath = Join-Path $root "src\TemizPC.App\Assets\TemizPC.png"
$appProject = Join-Path $root "src\TemizPC.App\TemizPC.App.csproj"
$testProject = Join-Path $root "tests\TemizPC.Tests\TemizPC.Tests.csproj"

$certificateBase64 = $env:CODESIGN_CERTIFICATE_BASE64
$certificatePassword = $env:CODESIGN_CERTIFICATE_PASSWORD
$publicCertificatePath = Join-Path $releasesDir "TemizPC-signing.cer"
$timestampUrl = if ([string]::IsNullOrWhiteSpace($env:CODESIGN_TIMESTAMP_URL)) {
    "http://timestamp.digicert.com"
}
else {
    $env:CODESIGN_TIMESTAMP_URL
}
$hasSigningCertificate = -not [string]::IsNullOrWhiteSpace($certificateBase64) -and -not [string]::IsNullOrWhiteSpace($certificatePassword)

New-Item -ItemType Directory -Force -Path $dotnetHome, $toolDir, $publishDir, $releasesDir, $certificateDirectory | Out-Null
$env:DOTNET_CLI_HOME = $dotnetHome

Write-Host "Restoring projects..."
dotnet restore $appProject -r $Runtime /p:Version=$Version
dotnet restore $testProject

Write-Host "Building projects..."
dotnet build $appProject -c $Configuration --no-restore /p:Version=$Version
dotnet build $testProject -c $Configuration --no-restore

Write-Host "Running tests..."
dotnet test $testProject -c $Configuration --no-build -v m

Write-Host "Publishing WPF application..."
dotnet publish `
    $appProject `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    --no-restore `
    -o $publishDir `
    /p:Version=$Version `
    /p:PublishSingleFile=false

@{
    githubRepositoryUrl = $RepositoryUrl
    allowPrerelease = $false
} | ConvertTo-Json | Set-Content -Path (Join-Path $publishDir "release-settings.json") -Encoding utf8

$releaseNotes = @"
# TemizPC $Version

Automated release from $RepositoryUrl
Commit: $env:GITHUB_SHA
"@

Set-Content -Path $releaseNotesPath -Value $releaseNotes -Encoding utf8

Write-Host "Installing Velopack CLI..."
dotnet tool update --tool-path $toolDir vpk --version 0.0.1298

$vpk = Join-Path $toolDir "vpk.exe"
$packArguments = @(
    "pack",
    "--packId", "Musa.TemizPC",
    "--packVersion", $Version,
    "--packDir", $publishDir,
    "--mainExe", "TemizPC.App.exe",
    "--packTitle", "TemizPC",
    "--packAuthors", "Musa",
    "--outputDir", $releasesDir,
    "--channel", "win",
    "--runtime", $Runtime,
    "--icon", $iconPath,
    "--splashImage", $splashPath,
    "--releaseNotes", $releaseNotesPath,
    "--delta", "None"
)

if ($hasSigningCertificate) {
    Write-Host "Preparing signing certificate..."
    [System.IO.File]::WriteAllBytes($certificatePath, [Convert]::FromBase64String($certificateBase64))

    $certificateFlags = `
        [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable `
        -bor `
        [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::EphemeralKeySet

    $publicCertificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new(
        $certificatePath,
        $certificatePassword,
        $certificateFlags)

    try {
        [System.IO.File]::WriteAllBytes(
            $publicCertificatePath,
            $publicCertificate.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert))
    }
    finally {
        $publicCertificate.Dispose()
    }

    $signParams = "/fd SHA256 /f `"$certificatePath`" /p `"$certificatePassword`" /tr `"$timestampUrl`" /td SHA256"
    $packArguments += @("--signParams", $signParams)
    Write-Host "Packaging signed release..."
    Write-Host "Public signing certificate exported to: $publicCertificatePath"
}
else {
    Write-Host "No code-signing certificate configured. Packaging unsigned release."
}

Write-Host "Packing installer..."
& $vpk @packArguments

Write-Host "Uploading release to GitHub..."
& $vpk upload github `
    --repoUrl $RepositoryUrl `
    --token $env:GITHUB_TOKEN `
    --outputDir $releasesDir `
    --channel "win" `
    --publish `
    --merge `
    --releaseName "TemizPC $Version" `
    --tag "v$Version" `
    --targetCommitish $env:GITHUB_SHA

Write-Host "Release completed: $Version"
