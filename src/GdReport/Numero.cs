using System.Globalization;

namespace GdReport;

public static class Numero
{
    public static decimal Parse(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return 0;
        }

        var texto = valor.Trim();

        if (texto.Contains(','))
        {
            var posicaoVirgula = texto.IndexOf(',');

            if (texto.LastIndexOf(',') != posicaoVirgula || texto.LastIndexOf('.') > posicaoVirgula)
            {
                throw new FormatException($"Valor numérico inválido: '{valor}'.");
            }

            texto = texto.Replace(".", string.Empty).Replace(',', '.');
        }

        const NumberStyles estilo = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

        if (!decimal.TryParse(texto, estilo, CultureInfo.InvariantCulture, out var resultado))
        {
            throw new FormatException($"Valor numérico inválido: '{valor}'.");
        }

        return resultado;
    }
}
