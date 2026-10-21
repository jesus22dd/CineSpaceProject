namespace CineSpace.Shared.Models;

public static class ReservaEntradas
{
    public static bool CantidadValida(int adultos, int ninos, int disponibles) => adultos >= 0 && ninos >= 0 && (long)adultos + ninos > 0 && (long)adultos + ninos <= disponibles;
    public static bool ButacasCompletas(int adultos, int ninos, int butacas) => adultos >= 0 && ninos >= 0 && butacas > 0 && (long)adultos + ninos == butacas;
    public static bool PuedeAgregarButaca(int elegidas, int adultos, int ninos) => adultos >= 0 && ninos >= 0 && elegidas >= 0 && (long)adultos + ninos > elegidas;
}
