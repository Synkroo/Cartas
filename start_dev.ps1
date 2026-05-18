param(
    [switch]$NoUnity,
    [switch]$NoBrowser,
    [switch]$NoCode,
    [switch]$NoOpencode
)

$ErrorActionPreference = "Continue"
$ProjectPath = "D:\Unity\Proyectos\Juego de cartas incremental"

Write-Host "=== Iniciando entorno de desarrollo ===" -ForegroundColor Cyan
Write-Host "Proyecto: $ProjectPath" -ForegroundColor Gray

# 1. YouTube playlist
if (-not $NoBrowser) {
    Write-Host "[1/4] Abriendo YouTube..." -ForegroundColor Yellow
    Start-Process "https://www.youtube.com/watch?v=aT2lLk1yEMY&list=RDaT2lLk1yEMY&start_radio=1"
}

# 2. Visual Studio Code
if (-not $NoCode) {
    Write-Host "[2/4] Abriendo VS Code..." -ForegroundColor Yellow
    $codePath = "$env:LOCALAPPDATA\Programs\Microsoft VS Code\Code.exe"
    if (Test-Path $codePath) {
        Start-Process $codePath -ArgumentList $ProjectPath
    } else {
        $devenv = "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe"
        if (Test-Path $devenv) {
            Start-Process $devenv -ArgumentList "$ProjectPath\JuegoDeCartas.sln"
        } else {
            Write-Host "  ⚠ No se encontró VS Code ni Visual Studio" -ForegroundColor Red
        }
    }
}

# 3. Unity Hub + proyecto
if (-not $NoUnity) {
    Write-Host "[3/4] Abriendo Unity Hub..." -ForegroundColor Yellow
    $unityHub = "D:\Unity\Unity Hub\Unity Hub.exe"
    if (Test-Path $unityHub) {
        Start-Process $unityHub -ArgumentList "--open-project", $ProjectPath
        Write-Host "  Esperando a que Unity cargue (60s)..." -ForegroundColor Gray
        Start-Sleep -Seconds 60
        Write-Host "  ✅ El servidor MCP debería iniciarse automáticamente (AutoStart activado)" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ No se encontró Unity Hub" -ForegroundColor Red
    }
}

# 4. opencode en el proyecto
if (-not $NoOpencode) {
    Write-Host "[4/4] Iniciando opencode..." -ForegroundColor Yellow
    try {
        Start-Process "opencode" -WorkingDirectory $ProjectPath
        Write-Host "  ✅ opencode iniciado" -ForegroundColor Green
    } catch {
        Write-Host "  ⚠ Error al iniciar opencode: $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Todo listo ===" -ForegroundColor Cyan
Write-Host "Consejos:" -ForegroundColor Gray
Write-Host "  • Si el servidor MCP no arranca solo, ve a Unity > Window > MCP for Unity > Start Server" -ForegroundColor Gray
Write-Host "  • Para saltar partes usa: -NoUnity -NoBrowser -NoCode -NoOpencode" -ForegroundColor Gray
