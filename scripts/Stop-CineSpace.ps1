try {
    . (Join-Path $PSScriptRoot 'CineSpace.Commands.ps1')
    Stop-CineServer
    Write-Host 'CineSpace detenido.'
    exit 0
}
catch { Write-Host ('ERROR: ' + $_.Exception.Message) -ForegroundColor Red; exit 1 }
