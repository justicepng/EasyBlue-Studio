$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$scriptDir = $PSScriptRoot
if (-not $scriptDir) { $scriptDir = (Get-Location).Path }

$csFile = Join-Path $scriptDir "src\EasyBlueStudio.cs"
$manifest = Join-Path $scriptDir "app.manifest"
$binDir = Join-Path $scriptDir "bin"
if (-not (Test-Path $binDir)) { New-Item -ItemType Directory -Path $binDir | Out-Null }
$outFile = Join-Path $binDir "EasyBlueStudio.exe"
$wpfLib = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF"

$refs = @(
    "PresentationFramework.dll",
    "PresentationCore.dll",
    "WindowsBase.dll",
    "System.Xaml.dll",
    "System.dll",
    "System.Drawing.dll",
    "System.Windows.Forms.dll",
    "System.Management.dll"
) -join ","

$args = @(
    "/target:winexe",
    "/platform:anycpu",
    "/optimize+",
    "/lib:`"$wpfLib`"",
    "/win32manifest:`"$manifest`"",
    "/r:$refs",
    "/out:`"$outFile`"",
    "`"$csFile`""
)

Write-Host "Compiling EasyBlue Studio 2026..."
$proc = Start-Process -FilePath $csc -ArgumentList $args -NoNewWindow -Wait -PassThru
if ($proc.ExitCode -eq 0) {
    Write-Host "BUILD SUCCESS! Output: $outFile"
    Get-Item $outFile | Select-Object Name, Length, LastWriteTime
} else {
    Write-Host "BUILD FAILED with exit code $($proc.ExitCode)"
}
