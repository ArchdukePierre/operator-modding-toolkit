# InteropInspector wrapper.
# Usage: .\run.ps1 "<type-fullname-regex>" [dir-of-dlls]
# Example: .\run.ps1 "LoadoutManager|WeaponV3|GunStats"
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Pattern,

    [Parameter(Position = 1)]
    [string]$Dir = "C:\Program Files (x86)\Steam\steamapps\common\OPERATOR\MelonLoader\Il2CppAssemblies"
)

$dll = Join-Path $PSScriptRoot "bin\Release\net8.0\InteropInspector.dll"
if (-not (Test-Path $dll)) {
    Write-Host "building..." -ForegroundColor Yellow
    dotnet build -c Release (Join-Path $PSScriptRoot "InteropInspector.csproj")
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

dotnet $dll $Dir $Pattern
exit $LASTEXITCODE
