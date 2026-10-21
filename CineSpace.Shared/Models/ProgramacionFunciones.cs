using System.Globalization;

namespace CineSpace.Shared.Models;

public sealed class ProgramacionFunciones
{
    public string HoraInicio { get; set; } = "14:00";
    public int DuracionMinutos { get; set; } = 120;
    public int LimpiezaMinutos { get; set; } = 20;
    public List<string> Horarios { get; set; } = new();
    public bool Configurada { get; set; } = true;
    public bool EnPreventa { get; set; }
    public ProgramacionFunciones Copiar() => new() { HoraInicio = HoraInicio, DuracionMinutos = DuracionMinutos, LimpiezaMinutos = LimpiezaMinutos, Horarios = new(Horarios), Configurada = Configurada, EnPreventa = EnPreventa };
    public static bool LeerHora(string valor, out int minutos)
    {
        minutos = 0;
        if (!TimeOnly.TryParseExact(valor, new[] { "HH:mm", "HH:mm:ss" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var hora) || hora.Second != 0) return false;
        minutos = hora.Hour * 60 + hora.Minute; return true;
    }
    public static string Hora(int minutos) => $"{minutos % 1440 / 60:00}:{minutos % 60:00}";
    public static string Duracion(int minutos) => minutos >= 60 ? $"{minutos / 60} h{(minutos % 60 > 0 ? $" {minutos % 60} min" : "")}" : $"{minutos} min";
    public void ValidarCampos()
    {
        if (!LeerHora(HoraInicio, out _)) throw new ArgumentException("Elige una hora de inicio válida.");
        if (DuracionMinutos is < 1 or > 720) throw new ArgumentException("La duración de la película debe estar entre 1 minuto y 12 horas.");
        if (LimpiezaMinutos is < 0 or > 360) throw new ArgumentException("El tiempo de limpieza debe estar entre 0 minutos y 6 horas.");
    }
    public List<FuncionSugerida> Sugerencias()
    {
        ValidarCampos(); LeerHora(HoraInicio, out var inicio);
        var resultado = new List<FuncionSugerida>();
        for (var minuto = inicio; minuto < 1440; minuto += DuracionMinutos + LimpiezaMinutos)
            resultado.Add(new(minuto, minuto + DuracionMinutos, minuto + DuracionMinutos + LimpiezaMinutos));
        return resultado;
    }
    public void ValidarSeleccion()
    {
        ValidarCampos();
        if (Horarios.Count == 0) throw new ArgumentException("Selecciona al menos una función para guardar la película.");
        if (!Configurada) return;
        LeerHora(HoraInicio, out var inicio);
        var minutos = new List<int>();
        foreach (var horario in Horarios)
        {
            if (!LeerHora(horario, out var minuto) || minuto < inicio) throw new ArgumentException("Hay funciones anteriores a la hora de inicio. Recalcula los horarios.");
            minutos.Add(minuto);
        }
        minutos.Sort();
        for (var i = 1; i < minutos.Count; i++)
            if (minutos[i] - minutos[i - 1] < DuracionMinutos + LimpiezaMinutos)
                throw new ArgumentException($"Las funciones de {Hora(minutos[i - 1])} y {Hora(minutos[i])} se solapan. Deja tiempo para la película y la limpieza.");
    }
    public static ProgramacionFunciones Desde(Pelicula pelicula) => new() { HoraInicio = pelicula.HoraPrimeraFuncion, DuracionMinutos = pelicula.DuracionMinutos, LimpiezaMinutos = pelicula.LimpiezaMinutos, Horarios = new(pelicula.HorariosDisponibles ?? new()), Configurada = pelicula.ProgramacionConfigurada, EnPreventa = pelicula.EnPreventa };
    public void Aplicar(Pelicula pelicula) { pelicula.HoraPrimeraFuncion = HoraInicio; pelicula.DuracionMinutos = DuracionMinutos; pelicula.LimpiezaMinutos = LimpiezaMinutos; pelicula.HorariosDisponibles = new(Horarios); pelicula.ProgramacionConfigurada = Configurada; pelicula.EnPreventa = EnPreventa; }
}

public sealed record FuncionSugerida(int Inicio, int Fin, int SalaLista)
{
    public string HoraInicio => ProgramacionFunciones.Hora(Inicio);
    public string HoraFin => ProgramacionFunciones.Hora(Fin) + (Fin >= 1440 ? " (+1 día)" : "");
    public string HoraSalaLista => ProgramacionFunciones.Hora(SalaLista) + (SalaLista >= 1440 ? " (+1 día)" : "");
}
