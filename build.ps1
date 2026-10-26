$ErrorActionPreference = 'Stop'
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outputPath = Join-Path $PSScriptRoot 'MiniKeys.exe'
& $compilerPath /nologo /target:winexe /platform:anycpu /optimize+ /utf8output "/out:$outputPath" "/win32manifest:$PSScriptRoot\app.manifest" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Runtime.Serialization.dll /reference:System.Core.dll "$PSScriptRoot\src\MiniKeys.cs" "$PSScriptRoot\src\AssemblyInfo.cs"
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
Get-Item -LiteralPath $outputPath | Select-Object FullName, Length
