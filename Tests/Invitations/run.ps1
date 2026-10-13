param(
    [Parameter(Mandatory=$true)][string]$JavaPacket,
    [string]$Compiler = 'D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$output = Join-Path $repo 'Temp\invitation-tests'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$sources = @(
    (Join-Path $PSScriptRoot 'StoreTests.cs'),
    (Join-Path $repo 'Assets\Scripts\myReader.cs'),
    (Join-Path $repo 'Assets\Scripts\myWriter.cs'),
    (Join-Path $repo 'Assets\Scripts\Notifications\NotificationModels.cs'),
    (Join-Path $repo 'Assets\Scripts\Notifications\NotificationStore.cs'),
    (Join-Path $repo 'Assets\Scripts\Notifications\NotificationProtocol.cs'),
    (Join-Path $repo 'Assets\Scripts\WorldChat\WorldChatStore.cs'),
    (Join-Path $repo 'Assets\Scripts\WorldChat\WorldChatProtocol.cs')
)
$exe = Join-Path $output 'StoreTests.exe'
& $Compiler /nologo /target:exe "/out:$exe" @sources
if ($LASTEXITCODE -ne 0) { throw 'Client invitation test compilation failed' }
& $exe $JavaPacket
if ($LASTEXITCODE -ne 0) { throw 'Client invitation tests failed' }
