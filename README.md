# TemizPC

`TemizPC` is a Windows-only cleanup utility for personal use. It is built with `C#`, `WPF`, and `.NET 8`, always requests administrator privileges, and exposes cleanup actions as clear, non-technical cards in both Turkish and English.

![TemizPC logo](branding/TemizPC.svg)

## What it does

- Offers a safe `Recommended Cleanup` preset plus individual task selection.
- Runs cleanup actions with native C# file operations where possible.
- Skips locked files instead of crashing and writes structured logs under `%LocalAppData%\TemizPC\Logs`.
- Checks GitHub-hosted updates from inside the app when the packaged release is installed.
- Produces a signed Windows installer from GitHub Actions when `main` changes and signing secrets are configured.

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

- Triggers on pushes to `main`
- Computes version `0.1.<run_number>`
- Fails closed if signing secrets are missing
- Publishes the WPF app as `win-x64`
- Uses `Velopack` to create installer/update assets
- Uploads the release to GitHub Releases

Required secrets:

- `CODESIGN_CERTIFICATE_BASE64`
- `CODESIGN_CERTIFICATE_PASSWORD`
- `CODESIGN_TIMESTAMP_URL` (optional; defaults to DigiCert timestamp)

## Notes

- The original batch snippet contained an `exit` before the local temp cleanup line. In this app, local temp cleanup is implemented as its own proper task instead of preserving that bug.
- No arbitrary user-supplied command execution is included in v1.
- The packaged release target is `win-x64`; local debug builds remain normal desktop builds.
