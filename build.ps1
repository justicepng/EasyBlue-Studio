$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$srcDir = "G:\내 드라이브\Antigravity\ETC\EasyBlue-Modern\01_EasyBlue_Modern_Manager"
$csFile = "$srcDir\EasyBlueStudio.cs"
$manifest = "$srcDir\app.manifest"
$outFile = "$srcDir\EasyBlueStudio.exe"
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
