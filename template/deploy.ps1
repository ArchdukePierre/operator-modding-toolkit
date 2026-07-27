# Build and deploy a mod, waiting for the game to close if it is running.
#
# The DLL is locked while the game is open. A copy into a locked path can fail without raising, so the
# deployed size is checked at the end. A stale DLL wastes a whole test cycle.
param(
    [string]$Project = "ExampleMod\ExampleMod.csproj",
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\OPERATOR",
    [switch]$Launch
)

$ErrorActionPreference = "Stop"
$name = [IO.Path]::GetFileNameWithoutExtension($Project)

dotnet build $Project -c Release -v quiet
if ($LASTEXITCODE -ne 0) { throw "build failed" }

$src = Join-Path (Split-Path $Project) "bin\Release\net6.0\$name.dll"
$dst = Join-Path $GameDir "Mods\$name.dll"

while (Get-Process -Name "OPERATOR" -ErrorAction SilentlyContinue) {
    Write-Host "waiting for OPERATOR to exit..."
    Start-Sleep -Seconds 2
}

Copy-Item $src $dst -Force
$f = Get-Item $dst
$s = Get-Item $src
if ($f.Length -ne $s.Length) { throw "deployed size does not match build output" }
Write-Host "deployed $($f.Length) bytes to $dst"

if ($Launch) { Start-Process "steam://rungameid/1913370" }
