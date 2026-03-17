# TemizPC

`TemizPC` is a Windows-only cleanup utility for personal use. It is built with `C#`, `WPF`, and `.NET 8`, always requests administrator privileges, and exposes cleanup actions as clear, non-technical cards in both Turkish and English.

![TemizPC logo](branding/TemizPC.svg)

## What it does

- Offers a safe `Recommended Cleanup` preset plus individual task selection.
- Runs cleanup actions with native C# file operations where possible.
- Skips locked files instead of crashing and writes structured logs under `%LocalAppData%\TemizPC\Logs`.
- Checks GitHub-hosted updates from inside the app when the packaged release is installed.
- Produces a Windows installer from GitHub Actions when `master` or `main` changes. If signing secrets are configured it is signed; otherwise it is published unsigned.

## Built-in cleanup tasks

- Recent items history
- Windows Temp
- User Temp
- Recycle Bin
- Thumbnail cache
- Windows error reports
- Delivery Optimization cache
- Prefetch cache
- Windows Update download cache
- Crash dumps
- DISM component store cleanup

`Prefetch`, `Windows Update download cache`, crash dump cleanup, and `DISM` are intentionally marked as advanced tasks.

## Local development

Prerequisites:

- Windows 10/11
- .NET 8 SDK

Commands:

```powershell
dotnet restore src/TemizPC.App/TemizPC.App.csproj
dotnet restore tests/TemizPC.Tests/TemizPC.Tests.csproj
dotnet build src/TemizPC.App/TemizPC.App.csproj -c Debug
dotnet build tests/TemizPC.Tests/TemizPC.Tests.csproj -c Debug
dotnet test tests/TemizPC.Tests/TemizPC.Tests.csproj -c Debug --no-build
```

The solution file is included for IDE navigation, but the CI pipeline intentionally restores/builds projects directly.

## Update configuration

The app reads `release-settings.json` next to the executable. During packaged releases, the release workflow overwrites that file with the current GitHub repository URL so in-app update checks can use GitHub Releases.

For local packaged builds, you can set:

```json
{
  "githubRepositoryUrl": "https://github.com/<owner>/temizpc",
  "allowPrerelease": false
}
```

## GitHub Actions

### `ci.yml`

- Restores the app and test projects
- Builds the WPF app
- Builds the xUnit tests
- Runs the test project

### `release.yml`

- Triggers on pushes to `master` or `main`
- Computes version `0.1.<run_number>`
- Publishes the WPF app as `win-x64`
- Uses `Velopack` to create installer/update assets
- Uploads the release to GitHub Releases
- Signs the installer only when signing secrets are present

Required secrets:

- `CODESIGN_CERTIFICATE_BASE64` (optional, for signed releases)
- `CODESIGN_CERTIFICATE_PASSWORD` (optional, for signed releases)
- `CODESIGN_TIMESTAMP_URL` (optional; defaults to DigiCert timestamp)

## Self-signed signing flow

For private use with your own machines or friends' machines, you can use a self-signed code-signing certificate.

1. Create the certificate locally:

```powershell
./build/New-SelfSignedCodeSigningCert.ps1 -Password "change-this-password"
```

This creates:

- `artifacts/signing/TemizPC-codesign-selfsigned.pfx`
- `artifacts/signing/TemizPC-codesign-selfsigned.cer`
- `artifacts/signing/codesign-certificate-base64.txt`
- `artifacts/signing/github-secrets-instructions.txt`

2. Add GitHub secrets:

- `CODESIGN_CERTIFICATE_BASE64`: contents of `codesign-certificate-base64.txt`
- `CODESIGN_CERTIFICATE_PASSWORD`: the password you passed to the script

3. Trust the certificate on every machine that should run the signed app without certificate warnings:

```powershell
./build/Install-CodeSigningTrust.ps1 -CertificatePath ./artifacts/signing/TemizPC-codesign-selfsigned.cer -Scope CurrentUser
```

Use `-Scope LocalMachine` from an elevated PowerShell session if you want to trust it for all users on that PC.

Notes:

- This is suitable for personal/friend distribution, not for broad public distribution.
- Windows SmartScreen reputation is still separate. Even with a trusted self-signed certificate, new downloads may still show reputation warnings on machines that do not trust your certificate yet.
- During a signed release, the pipeline exports `TemizPC-signing.cer` into the release output so you can distribute the public certificate alongside the installer.

## Notes

- The original batch snippet contained an `exit` before the local temp cleanup line. In this app, local temp cleanup is implemented as its own proper task instead of preserving that bug.
- No arbitrary user-supplied command execution is included in v1.
- The packaged release target is `win-x64`; local debug builds remain normal desktop builds.
- A truly free public code-signing certificate for Windows desktop apps is generally not available. Unsigned releases work, but Windows SmartScreen may show warnings until you buy a certificate.
