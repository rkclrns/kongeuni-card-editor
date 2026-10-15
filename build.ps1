param([switch]$SkipTests)
$ErrorActionPreference = 'Stop'
$project = $PSScriptRoot
$version = '1.0.0'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (!(Test-Path $compiler)) { throw '.NET Framework C# compiler not found.' }
$bin = Join-Path $project 'bin'
$dist = Join-Path $project 'dist'
New-Item -ItemType Directory -Force $bin,$dist,(Join-Path $bin 'fonts'),(Join-Path $bin 'licenses') | Out-Null
$common = @('/nologo','/target:winexe','/optimize+','/codepage:65001','/r:System.dll','/r:System.Core.dll','/r:System.Drawing.dll','/r:System.Windows.Forms.dll')
$icon = Join-Path $project 'assets\app.ico'
$manifest = Join-Path $project 'src\app.manifest'
Push-Location (Join-Path $project 'src')
try {
    & $compiler @common "/out:$bin\KongeuniCardEditor.exe" "/win32icon:$icon" "/win32manifest:$manifest" "/resource:$icon,appicon" @(Get-ChildItem *.cs | ForEach-Object Name)
    if ($LASTEXITCODE -ne 0) { throw 'Editor compilation failed.' }
} finally { Pop-Location }
Copy-Item (Join-Path $project 'src\App.config') (Join-Path $bin 'KongeuniCardEditor.exe.config') -Force
Copy-Item (Join-Path $project 'assets\fonts\*.ttf') (Join-Path $bin 'fonts') -Force
Copy-Item (Join-Path $project 'licenses\Galmuri-OFL.md') (Join-Path $bin 'licenses') -Force
Copy-Item (Join-Path $project '사용법.txt'),(Join-Path $project 'THIRD_PARTY_NOTICES.md') $bin -Force
& $compiler @common "/out:$bin\Uninstall.exe" (Join-Path $project 'packaging\Uninstall.cs') (Join-Path $project 'src\AssemblyInfo.cs') "/win32manifest:$manifest" "/win32icon:$icon"
if ($LASTEXITCODE -ne 0) { throw 'Uninstaller compilation failed.' }
if (!$SkipTests) {
    $qa = Join-Path $project 'work\self-test'
    New-Item -ItemType Directory -Force $qa | Out-Null
    $test = Start-Process (Join-Path $bin 'KongeuniCardEditor.exe') -ArgumentList ('--self-test "' + $qa + '"') -WindowStyle Hidden -PassThru -Wait
    if ($test.ExitCode -ne 0) { throw "Editor tests failed. See $qa" }
}
$portable = Join-Path $dist "KongeuniCardEditor-$version-Portable.zip"
$payload = @('KongeuniCardEditor.exe','KongeuniCardEditor.exe.config','Uninstall.exe','사용법.txt','THIRD_PARTY_NOTICES.md','fonts','licenses') | ForEach-Object { Join-Path $bin $_ }
Compress-Archive -LiteralPath $payload -DestinationPath $portable -Force
$setup = Join-Path $dist "KongeuniCardEditor-$version-Setup.exe"
& $compiler @common '/r:System.IO.Compression.dll' '/r:System.IO.Compression.FileSystem.dll' "/out:$setup" (Join-Path $project 'packaging\Setup.cs') (Join-Path $project 'src\AssemblyInfo.cs') "/win32manifest:$manifest" "/win32icon:$icon" "/resource:$icon,appicon" "/resource:$portable,payload"
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
@($setup,$portable) | ForEach-Object { $hash = Get-FileHash -LiteralPath $_ -Algorithm SHA256; $hash.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($_) } | Set-Content -Encoding ascii (Join-Path $dist 'SHA256SUMS.txt')
Write-Host "Build complete: $dist"
