namespace CineSpace.Shared.Models;

public enum EtiquetaBanner
{
    SinEtiqueta = 0,
    Estreno,
    Preventa,
    Oferta,
    Reestreno,
    UltimosDias,
    Exclusivo,
    Recomendado,
    EstrenoSemana,
    FuncionEspecial
}

public static class EtiquetasBanner
{
    public static string Nombre(this EtiquetaBanner etiqueta) => etiqueta switch
    {
        EtiquetaBanner.SinEtiqueta => "Sin etiqueta",
        EtiquetaBanner.Estreno => "Estreno",
        EtiquetaBanner.Preventa => "Preventa",
        EtiquetaBanner.Oferta => "Oferta",
        EtiquetaBanner.Reestreno => "Reestreno",
        EtiquetaBanner.UltimosDias => "Últimos días",
        EtiquetaBanner.Exclusivo => "Exclusivo",
        EtiquetaBanner.Recomendado => "Recomendado",
        EtiquetaBanner.EstrenoSemana => "Estreno de la semana",
        EtiquetaBanner.FuncionEspecial => "Función especial",
        _ => "Sin etiqueta"
    };
    public static EtiquetaBanner DesdeMensaje(string? mensaje)
    {
        if (string.Equals(mensaje?.Trim(), "En oferta", StringComparison.OrdinalIgnoreCase)) return EtiquetaBanner.Oferta;
        return Enum.GetValues<EtiquetaBanner>().FirstOrDefault(e => string.Equals(e.Nombre(), mensaje?.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
