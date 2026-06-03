$ErrorActionPreference = "Stop"

$sourceRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $sourceRoot
$sourceProject = Join-Path $sourceRoot "LaboPass.csproj"
$publishDir = Join-Path $sourceRoot "bin\portable-win-x64"
$exeSource = Join-Path $publishDir "LaboPass.exe"
$exeDestination = Join-Path $root "LaboPass.exe"
$vaultPath = Join-Path $root "vault.json"

if (-not (Test-Path -LiteralPath $sourceProject)) {
    throw "Projet introuvable: $sourceProject"
}

if (Test-Path -LiteralPath $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

dotnet publish $sourceProject -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true -o $publishDir

Copy-Item -LiteralPath $exeSource -Destination $exeDestination -Force

if (-not (Test-Path -LiteralPath $vaultPath)) {
    "[]" | Set-Content -LiteralPath $vaultPath -Encoding UTF8
}

Write-Host "Publication terminee: $exeDestination"
