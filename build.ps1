# Собирает dist\fastGPT-Setup.exe. Нужен только .NET Framework 4, он уже есть в Windows.
$ErrorActionPreference = 'Stop'

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$dist = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force $dist | Out-Null

& $csc /nologo /optimize+ /target:winexe /codepage:65001 "/out:$dist\fastGPT-Setup.exe" "/win32manifest:$PSScriptRoot\app.manifest" "$PSScriptRoot\fastGPT.cs"
if ($LASTEXITCODE -ne 0) { throw 'Сборка не удалась' }

(Get-FileHash "$dist\fastGPT-Setup.exe" -Algorithm SHA256).Hash
