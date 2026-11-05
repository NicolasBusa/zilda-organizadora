param([string]$TestDirectory = (Join-Path $PSScriptRoot ('..\tests\verify-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))), [switch]$Interface)
$ErrorActionPreference = 'Stop'
$taskTests = [System.IO.Path]::GetFullPath($TestDirectory)
New-Item -ItemType Directory -Force -Path $taskTests | Out-Null
& (Join-Path $PSScriptRoot 'build.ps1')
$taskExe = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\build\ZildaOrganizadora.exe'))
function Invoke-ZildaTestProcess([string]$Executable,[string[]]$Arguments) {
    $taskChild = Start-Process -FilePath $Executable -ArgumentList $Arguments -WindowStyle Hidden -Wait -PassThru
    if ($taskChild.ExitCode -ne 0) { throw "Processo de teste terminou com código $($taskChild.ExitCode)." }
}
Invoke-ZildaTestProcess $taskExe @('--self-test','--data-dir',('"' + (Join-Path $taskTests 'dados') + '"'))
Get-Content (Join-Path $taskTests 'dados\resultados.txt')
if ($Interface) {
    $taskSmoke = Join-Path $taskTests 'interface'
    New-Item -ItemType Directory -Force -Path $taskSmoke | Out-Null
    $taskProbe = 'using System; using System.IO; class Probe { static void Main() { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"launch-proof.txt"),"OK"); }}'
    Set-Content -LiteralPath (Join-Path $taskSmoke 'Probe.cs') -Value $taskProbe -Encoding utf8
    & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:winexe ('/out:' + (Join-Path $taskSmoke 'LauncherProbe.exe')) (Join-Path $taskSmoke 'Probe.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Não foi possível compilar o executável de prova.' }
    Invoke-ZildaTestProcess $taskExe @('--smoke','--data-dir',('"' + $taskSmoke + '"'))
    $taskResult = Get-Content -Raw (Join-Path $taskSmoke 'smoke\result.txt')
    foreach ($taskExpected in @('manager=True','Grade=','Coluna=','Linha=','Leque=','Anel=','click-multi=True','click-single=True','hover-single=True')) { if (-not $taskResult.Contains($taskExpected)) { throw "Verificação de interface falhou: $taskExpected" } }
    if (-not (Test-Path (Join-Path $taskSmoke 'launch-proof.txt'))) { throw 'Abertura pelo Shell não confirmada.' }
    Write-Output 'PASS Navegação, cinco painéis, modos de abertura e lançamento pelo Shell.'
}
Write-Output ('Resultados e artefatos: ' + $taskTests)
