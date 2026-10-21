using System.Globalization;
using System.Text;

namespace CineSpace.Client.Services;

public static class BusquedaCatalogo
{
    public static string Normalizar(string texto)
    {
        var resultado = new StringBuilder();
        foreach (var letra in texto.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(letra) != UnicodeCategory.NonSpacingMark)
                resultado.Append(char.IsLetterOrDigit(letra) ? char.ToLowerInvariant(letra) : ' ');
        return string.Join(' ', resultado.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
    public static int Relevancia(string texto, string consulta)
    {
        var valor = Normalizar(texto); var termino = Normalizar(consulta);
        if (termino.Length == 0 || valor == termino) return 0;
        if (valor.StartsWith(termino, StringComparison.Ordinal)) return 1;
        if (valor.Contains(termino, StringComparison.Ordinal)) return 2;
        return termino.Split(' ').All(t => valor.Contains(t, StringComparison.Ordinal)) ? 3 : int.MaxValue;
    }
}
