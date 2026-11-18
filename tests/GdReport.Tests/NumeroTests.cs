using System.Globalization;

namespace GdReport.Tests;

public class NumeroTests
{
    public static TheoryData<string, decimal> ValoresValidos => new()
    {
        { "2.250,00", 2250.00m },
        { "8000.0", 8000.0m },
        { "8.000,00", 8000.00m },
        { "150,5", 150.5m },
        { "0,00", 0m },
        { "-150,5", -150.5m },
        { "  150,5  ", 150.5m },
        { "8.000", 8m },
        { "1.234,567", 1234.567m },
        { "0,123456", 0.123456m },
    };

    [Theory]
    [MemberData(nameof(ValoresValidos))]
    public void Parse_DeveConverterValoresValidos(string valor, decimal esperado)
    {
        Assert.Equal(esperado, Numero.Parse(valor));
    }

    [Fact]
    public void Parse_DeveIgnorarCulturaDoSistema()
    {
        var culturaOriginal = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal(150.5m, Numero.Parse("150,5"));
            Assert.Equal(8000.0m, Numero.Parse("8000.0"));
        }
        finally
        {
            CultureInfo.CurrentCulture = culturaOriginal;
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_DeveRetornarZeroParaValorVazio(string? valor)
    {
        Assert.Equal(0m, Numero.Parse(valor));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1.2.3")]
    [InlineData("1,2,3")]
    [InlineData("1.234,5.6")]
    [InlineData("1,5.0")]
    [InlineData("79228162514264337593543950336")]
    public void Parse_DeveLancarFormatExceptionParaValorInvalido(string valor)
    {
        Assert.Throws<FormatException>(() => Numero.Parse(valor));
    }

    [Fact]
    public void Parse_MensagemDaFormatExceptionDeveConterOValorRecebido()
    {
        var excecao = Assert.Throws<FormatException>(() => Numero.Parse("abc"));

        Assert.Contains("abc", excecao.Message);
    }
}
