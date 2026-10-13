param(
    [Parameter(Mandatory=$true)][string]$JavaPacket,
    [string]$Compiler = 'D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$output = Join-Path $repo 'Build\world-chat-tests'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$sources = @(
    (Join-Path $PSScriptRoot 'WorldChatTests.cs'),
    (Join-Path $repo 'Tests\Invitations\StoreTests.cs'),
    (Join-Path $repo 'Assets\Scripts\myReader.cs'),
    (Join-Path $repo 'Assets\Scripts\myWriter.cs'),
    (Join-Path $repo 'Assets\Scripts\Notifications\NotificationModels.cs'),
    (Join-Path $repo 'Assets\Scripts\Notifications\NotificationStore.cs'),
    (Join-Path $repo 'Assets\Scripts\Notifications\NotificationProtocol.cs'),
    (Join-Path $repo 'Assets\Scripts\WorldChat\WorldChatStore.cs'),
    (Join-Path $repo 'Assets\Scripts\WorldChat\WorldChatComposer.cs'),
    (Join-Path $repo 'Assets\Scripts\WorldChat\WorldChatTicker.cs'),
    (Join-Path $repo 'Assets\Scripts\WorldChat\WorldChatProtocol.cs')
)
$exe = Join-Path $output 'WorldChatTests.exe'
& $Compiler /nologo /target:exe /main:WorldChatTests "/out:$exe" @sources
if ($LASTEXITCODE -ne 0) { throw 'World-chat test compilation failed' }
& $exe $JavaPacket (Join-Path (Split-Path $JavaPacket -Parent) 'world-chat-utf.bin')
if ($LASTEXITCODE -ne 0) { throw 'World-chat tests failed' }

$formExe = Join-Path $output 'ChatFormTests.exe'
$formSources = @(
    (Join-Path $PSScriptRoot 'ChatFormTests.cs'),
    (Join-Path $repo 'Assets\Scripts\ChatTextField.cs'),
    (Join-Path $repo 'Assets\Scripts\WorldChat\WorldChatStore.cs'),
    (Join-Path $repo 'Assets\Scripts\WorldChat\WorldChatComposer.cs'),
    (Join-Path $repo 'Assets\Scripts\WorldChat\WorldChatTicker.cs')
)
& $Compiler /nologo /target:exe "/out:$formExe" @formSources
if ($LASTEXITCODE -ne 0) { throw 'Chat form test compilation failed' }
& $formExe
if ($LASTEXITCODE -ne 0) { throw 'Chat form tests failed' }
