[CmdletBinding()]
param([switch]$NoBrowser, [switch]$SkipBuild)

try {
    . (Join-Path $PSScriptRoot 'CineSpace.Commands.ps1')
    Add-Type -AssemblyName System.Net.Http
    $url = 'https://localhost:7043/'
    function Test-CineRunning {
        $client = New-Object System.Net.Http.HttpClient
        $client.Timeout = [TimeSpan]::FromSeconds(2)
        try { return $client.GetStringAsync($url + 'api/sistema/estado').GetAwaiter().GetResult().Contains('CineSpace.API') }
        catch { return $false }
        finally { $client.Dispose() }
    }
    if (Test-CineRunning) {
        if ((Invoke-CineDatabase -Modo comprobar) -ne 0) { throw 'El servidor está abierto, pero falta preparar la base. Ejecuta update_DB.bat y vuelve a intentar.' }
        Write-Host ('CineSpace ya está funcionando en ' + $url)
        if (-not $NoBrowser) { Start-Process $url }
        exit 0
    }
    if (-not $SkipBuild) { Invoke-CineBuild }
    if ((Invoke-CineDatabase -Modo comprobar) -ne 0) { throw 'Primero ejecuta update_DB.bat. El arranque no modifica ni reconstruye la base.' }
    $logs = Join-Path $script:CineRoot '.local'
    New-Item -ItemType Directory -Path $logs -Force | Out-Null
    $process = Start-Process -FilePath $script:CineDotnet -ArgumentList @('run','--project',('"' + $script:CineProject + '"'),'--launch-profile','https','--no-build') -WorkingDirectory $script:CineRoot -WindowStyle Hidden -RedirectStandardOutput (Join-Path $logs 'server.log') -RedirectStandardError (Join-Path $logs 'server-error.log') -PassThru
    $timer = [Diagnostics.Stopwatch]::StartNew()
    while ($timer.Elapsed.TotalSeconds -lt 45) {
        if (Test-CineRunning) {
            Write-Host ('CineSpace listo: ' + $url)
            if (-not $NoBrowser) { Start-Process $url }
            exit 0
        }
        if ($process.HasExited) { throw 'El servidor se cerró. Revisa .local/server-error.log y .local/server.log.' }
        Start-Sleep -Milliseconds 500
    }
    throw 'El servidor no respondió por HTTPS. Revisa los logs de .local y el certificado HTTPS de desarrollo del equipo.'
}
catch { Write-Host ('ERROR: ' + $_.Exception.Message) -ForegroundColor Red; exit 1 }
