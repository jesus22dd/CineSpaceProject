Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:CineRoot = Split-Path -Parent $PSScriptRoot
$script:CineProject = Join-Path $script:CineRoot 'CineSpace.Server/CineSpace.Server.csproj'
$script:CineDotnet = (Get-Command dotnet.exe -CommandType Application -ErrorAction Stop).Source

function Stop-CineServer {
    $expected = [IO.Path]::GetFullPath((Join-Path $script:CineRoot 'CineSpace.Server/bin/Debug/net8.0/CineSpace.Server.exe'))
    Get-Process -Name CineSpace.Server -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $expected } | Stop-Process -Force
}

function Invoke-CineBuild {
    Push-Location $script:CineRoot
    try {
        & $script:CineDotnet build $script:CineProject --nologo --verbosity quiet | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar CineSpace. Revisa el error anterior.' }
    }
    finally { Pop-Location }
}

function Invoke-CineDatabase {
    param([ValidateSet('comprobar','actualizar','enlazar')][string]$Modo)
    $previousAspnet = $env:ASPNETCORE_ENVIRONMENT
    $previousDotnet = $env:DOTNET_ENVIRONMENT
    Push-Location (Join-Path $script:CineRoot 'CineSpace.Server')
    try {
        $env:ASPNETCORE_ENVIRONMENT = 'Development'
        $env:DOTNET_ENVIRONMENT = 'Development'
        $assembly = Join-Path $script:CineRoot 'CineSpace.Server/bin/Debug/net8.0/CineSpace.Server.dll'
        & $script:CineDotnet $assembly --database $Modo | ForEach-Object { Write-Host $_ }
        return $LASTEXITCODE
    }
    finally {
        $env:ASPNETCORE_ENVIRONMENT = $previousAspnet
        $env:DOTNET_ENVIRONMENT = $previousDotnet
        Pop-Location
    }
}
