# Read-only Roslyn semantic analysis. No Emit, Unity launch, compiler process or DLL output.
param([string]$SourceList)
$ErrorActionPreference = 'Stop'
$inventoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$roslynRoot = 'D:/Unity_Editor/2022.3.62f1/Editor/Data/DotNetSdkRoslyn'
Add-Type -Path (Join-Path $roslynRoot 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $roslynRoot 'Microsoft.CodeAnalysis.CSharp.dll')
$response = Get-Content -LiteralPath (Join-Path $inventoryRoot 'Library/Bee/artifacts/1300b0aE.dag/Assembly-CSharp.rsp')
$symbols = [string[]]@($response | Where-Object { $_.StartsWith('-define:') } | ForEach-Object { $_.Substring(8) })
$parse = [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default.WithPreprocessorSymbols($symbols)
$trees = [Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]]::new()
$references = [Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]]::new()
foreach ($entry in $response) {
    if ($entry -match '^-(?:r|reference):"?(.*?)"?$') {
        $referencePath = $Matches[1].Trim('"')
        if (![IO.Path]::IsPathRooted($referencePath)) { $referencePath = Join-Path $inventoryRoot $referencePath }
        $references.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($referencePath))
    }
}
$files = if ($SourceList) { Get-Content -LiteralPath $SourceList } else {
    Get-ChildItem -LiteralPath (Join-Path $inventoryRoot 'Assets') -Filter '*.cs' -Recurse -File |
        Where-Object { $_.FullName -notmatch '[\\/]Editor[\\/]' } | ForEach-Object { $_.FullName }
}
foreach ($file in $files) {
    $trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText($file), $parse, $file, [Text.Encoding]::UTF8, [Threading.CancellationToken]::None))
}
$options = [Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary)
$options = $options.WithAllowUnsafe($true)
$analysis = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create('InventoryReadOnlyAnalysis', $trees, $references, $options)
$errors = @($analysis.GetDiagnostics() | Where-Object { $_.Severity -eq [Microsoft.CodeAnalysis.DiagnosticSeverity]::Error })
if ($errors.Count -gt 0) { $errors | ForEach-Object { Write-Output $_.ToString() }; exit 1 }
Write-Output "PASS: semantic analysis of $($trees.Count) C# files; no build/Emit/DLL output"
