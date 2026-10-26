param([switch]$ModelOnly)
$ErrorActionPreference = 'Stop'
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$testPath = Join-Path $PSScriptRoot 'MiniKeysTests.exe'
$appPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'MiniKeys.exe'
& $compilerPath /nologo /target:exe /platform:anycpu /utf8output "/out:$testPath" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll "$PSScriptRoot\MiniKeysTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
if ($ModelOnly) { & $testPath $appPath --model-only }
else { & $testPath $appPath }
if ($LASTEXITCODE -ne 0) { throw 'Verification failed' }
