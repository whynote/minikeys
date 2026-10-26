$ErrorActionPreference = 'Stop'
$appPath = Join-Path $PSScriptRoot 'MiniKeys.exe'
if (-not (Test-Path -LiteralPath $appPath)) { throw 'Run build.ps1 first.' }
$version = ([System.Diagnostics.FileVersionInfo]::GetVersionInfo($appPath).FileVersion -replace '\.0$', '')
$releasePath = Join-Path $PSScriptRoot "dist\v$version"
New-Item -ItemType Directory -Path $releasePath -Force | Out-Null
Copy-Item -LiteralPath $appPath -Destination (Join-Path $releasePath 'MiniKeys.exe') -Force
$files = @('MiniKeys.exe', 'README.md', 'README.en.md', 'CHANGELOG.md', 'CHANGELOG.en.md', 'LICENSE', 'docs') | ForEach-Object { Join-Path $PSScriptRoot $_ }
Compress-Archive -LiteralPath $files -DestinationPath (Join-Path $releasePath "MiniKeys-v$version-win-portable.zip") -Force
Get-ChildItem -LiteralPath $releasePath | Select-Object Name, Length
