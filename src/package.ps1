param([string]$OutputDirectory = (Join-Path $PSScriptRoot '..\outputs'))
$ErrorActionPreference = 'Stop'
$taskOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
$taskBuild = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\build'))
New-Item -ItemType Directory -Force -Path $taskOutput,$taskBuild | Out-Null
if (-not (Test-Path (Join-Path $PSScriptRoot 'zilda.ico'))) { & (Join-Path $PSScriptRoot 'make-icon.ps1') }
& (Join-Path $PSScriptRoot 'build.ps1')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LEIA-ME.txt') -Destination $taskBuild
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LICENCA.txt') -Destination $taskBuild
Add-Type -AssemblyName System.IO.Compression.FileSystem
$taskPayload = Join-Path $taskBuild 'payload.zip'
if (Test-Path -LiteralPath $taskPayload) { Remove-Item -LiteralPath $taskPayload }
$taskZip = [System.IO.Compression.ZipFile]::Open($taskPayload,[System.IO.Compression.ZipArchiveMode]::Create)
try { foreach ($taskName in @('ZildaOrganizadora.exe','ZildaOrganizadora.exe.config','LEIA-ME.txt','LICENCA.txt')) { [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($taskZip,(Join-Path $taskBuild $taskName),$taskName) | Out-Null } } finally { $taskZip.Dispose() }
$framework = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$taskReferences = @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$taskInstaller = Join-Path $taskOutput 'Zilda-Organizadora-Instalar.exe'
& (Join-Path $framework 'csc.exe') /nologo /utf8output /target:winexe /platform:x64 /optimize+ ('/out:' + $taskInstaller) ('/win32icon:' + (Join-Path $PSScriptRoot 'zilda.ico')) ('/win32manifest:' + (Join-Path $PSScriptRoot 'app.manifest')) ('/resource:' + $taskPayload + ',zilda-payload.zip') $taskReferences (Join-Path $PSScriptRoot 'Installer.cs') (Join-Path $PSScriptRoot 'AssemblyInfo.cs')
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar instalador.' }
$taskPortable = Join-Path $taskOutput 'Zilda-Organizadora-Portatil.zip'
if (Test-Path -LiteralPath $taskPortable) { Remove-Item -LiteralPath $taskPortable }
Copy-Item -LiteralPath $taskPayload -Destination $taskPortable
Write-Output 'Instalador e ZIP portátil criados.'
