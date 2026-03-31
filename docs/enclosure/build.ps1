# ============================================================
# build.ps1 — Kompilacja obudowy OpenSCAD do STL
# ============================================================
# Użycie:
#   .\build.ps1              — Eksport bottom + top STL
#   .\build.ps1 -Preview     — Otwórz złożony podgląd w GUI
#   .\build.ps1 -Part bottom — Eksport tylko dolnej części
#   .\build.ps1 -Png         — Generuj podgląd PNG
# ============================================================

param(
    [ValidateSet("bottom", "top", "all")]
    [string]$Part = "all",
    [switch]$Preview,
    [switch]$Png
)

$ErrorActionPreference = "Stop"
$scadFile = Join-Path $PSScriptRoot "enclosure.scad"
$outputDir = Join-Path $PSScriptRoot "output"

# --- Szukaj OpenSCAD ---
$openscad = Get-Command openscad -ErrorAction SilentlyContinue
if (-not $openscad) {
    $candidates = @(
        "$env:ProgramFiles\OpenSCAD\openscad.exe",
        "${env:ProgramFiles(x86)}\OpenSCAD\openscad.exe",
        "$env:LOCALAPPDATA\Programs\OpenSCAD\openscad.exe"
    )
    foreach ($c in $candidates) {
        if (Test-Path $c) { $openscad = $c; break }
    }
}

if (-not $openscad) {
    Write-Host "BLAD: OpenSCAD nie znaleziony!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Zainstaluj OpenSCAD:" -ForegroundColor Yellow
    Write-Host "  winget install OpenSCAD.OpenSCAD"
    Write-Host "  lub: https://openscad.org/downloads.html"
    Write-Host ""
    Write-Host "Po instalacji uruchom ponownie terminal."
    exit 1
}

$exe = if ($openscad -is [System.Management.Automation.CommandInfo]) { $openscad.Source } else { $openscad }
Write-Host "OpenSCAD: $exe" -ForegroundColor Green

# --- Podgląd GUI ---
if ($Preview) {
    Write-Host "Otwieram podglad zlozonej obudowy..." -ForegroundColor Cyan
    Start-Process $exe @($scadFile)
    exit 0
}

# --- Tworzenie katalogu output ---
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

# --- Funkcja eksportu ---
function Export-Part {
    param([string]$PartName, [string]$Format = "stl")
    
    $outFile = Join-Path $outputDir "enclosure_$PartName.$Format"
    Write-Host "Eksportuje: $PartName -> $outFile" -ForegroundColor Cyan
    
    $args_list = @("-o", $outFile, "-D", "part=`"$PartName`"", $scadFile)
    
    $proc = Start-Process -FilePath $exe -ArgumentList $args_list -NoNewWindow -Wait -PassThru
    
    if ($proc.ExitCode -eq 0 -and (Test-Path $outFile)) {
        $size = (Get-Item $outFile).Length / 1KB
        Write-Host "  OK — $([math]::Round($size, 1)) KB" -ForegroundColor Green
    } else {
        Write-Host "  BLAD eksportu!" -ForegroundColor Red
    }
}

# --- Eksport STL ---
$parts = switch ($Part) {
    "all"    { @("bottom", "top") }
    default  { @($Part) }
}

$sw = [System.Diagnostics.Stopwatch]::StartNew()

foreach ($p in $parts) {
    Export-Part -PartName $p -Format "stl"
}

# --- Opcjonalnie PNG ---
if ($Png) {
    Write-Host ""
    Write-Host "Generuje podglad PNG..." -ForegroundColor Cyan
    $pngFile = Join-Path $outputDir "enclosure_preview.png"
    $png_args = @("-o", $pngFile, 
                  "-D", "part=`"assembled`"",
                  "--imgsize=1920,1080",
                  "--camera=30,25,15,0,0,0",
                  $scadFile)
    Start-Process -FilePath $exe -ArgumentList $png_args -NoNewWindow -Wait
    if (Test-Path $pngFile) {
        Write-Host "  PNG: $pngFile" -ForegroundColor Green
    }
}

$sw.Stop()
Write-Host ""
Write-Host "Gotowe w $([math]::Round($sw.Elapsed.TotalSeconds, 1))s" -ForegroundColor Green
Write-Host "Pliki w: $outputDir"
