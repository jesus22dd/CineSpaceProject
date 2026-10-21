[CmdletBinding()]
param([ValidateSet('actualizar','comprobar','enlazar')][string]$Modo = 'actualizar')

try {
    . (Join-Path $PSScriptRoot 'CineSpace.Commands.ps1')
    Write-Host 'CineSpace: actualización manual de Supabase.'
    Write-Host 'Se usa la conexión privada del JSON Development. No se requiere token de CLI.'
    if ($Modo -eq 'actualizar') {
        Write-Host 'Deteniendo la aplicación en este equipo antes de actualizar Supabase.'
        Stop-CineServer
    }
    Invoke-CineBuild
    $result = Invoke-CineDatabase -Modo $Modo
    exit $result
}
catch { Write-Host ('ERROR: ' + $_.Exception.Message) -ForegroundColor Red; exit 1 }
