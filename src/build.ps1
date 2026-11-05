$ErrorActionPreference = 'Stop'
$taskSource = $PSScriptRoot
$taskBuild = Join-Path $taskSource '..\build'
New-Item -ItemType Directory -Force -Path $taskBuild | Out-Null
$framework = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$references = @('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xaml.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$references += @('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path $framework ('WPF\' + $_)) }
$sourceFiles = @('AssemblyInfo.cs','Models.cs','Native.cs','Visuals.cs','Manager.cs','App.cs','SelfTests.cs') | ForEach-Object { Join-Path $taskSource $_ }
$iconFile = Join-Path $taskSource 'zilda.ico'
$iconOption = @()
if (Test-Path -LiteralPath $iconFile) { $iconOption = @('/win32icon:' + $iconFile) }
& (Join-Path $framework 'csc.exe') /nologo /utf8output /target:winexe /platform:x64 /optimize+ ('/out:' + (Join-Path $taskBuild 'ZildaOrganizadora.exe')) ('/win32manifest:' + (Join-Path $taskSource 'app.manifest')) $iconOption $references $sourceFiles
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar Zilda.' }
Copy-Item -LiteralPath (Join-Path $taskSource 'ZildaOrganizadora.exe.config') -Destination $taskBuild
Write-Output 'ZildaOrganizadora.exe compilado.'
