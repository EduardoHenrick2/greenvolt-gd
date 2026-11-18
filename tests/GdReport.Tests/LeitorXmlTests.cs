namespace GdReport.Tests;

public class LeitorXmlTests
{
    private const string XmlDuasCompensacoesNaMesmaLinha =
        """
        <Relatorio>
          <Linha>
            <Instalacao>3001</Instalacao>
            <Qtd_compensacao>100,00</Qtd_compensacao>
            <Qtd_compensacao>50,50</Qtd_compensacao>
          </Linha>
        </Relatorio>
        """;

    [Fact]
    public void LerXml_DeveSomarAsDuasTagsQtdCompensacaoDaMesmaLinha()
    {
        var registros = LeitorXml.LerXml(XmlDuasCompensacoesNaMesmaLinha);

        var registro = Assert.Single(registros);
        Assert.Equal(150.50m, registro.QtdCompensacao);
    }

    [Fact]
    public void LerXml_NaoDeveSomarQtdCompensacaoEntreLinhasDistintas()
    {
        const string xml =
            """
            <Relatorio>
              <Linha>
                <Instalacao>3001</Instalacao>
                <Qtd_compensacao>100,00</Qtd_compensacao>
              </Linha>
              <Linha>
                <Instalacao>3002</Instalacao>
                <Qtd_compensacao>50,50</Qtd_compensacao>
              </Linha>
            </Relatorio>
            """;

        var registros = LeitorXml.LerXml(xml);

        Assert.Equal(2, registros.Count);
        Assert.Equal(100.00m, registros[0].QtdCompensacao);
        Assert.Equal(50.50m, registros[1].QtdCompensacao);
    }

    [Fact]
    public void LerXml_DeveInterpretarFormatosNumericosMisturadosPeloConteudo()
    {
        const string xml =
            """
            <Relatorio>
              <Linha>
                <Instalacao>3001</Instalacao>
                <Qtd_consumo>2.250,00</Qtd_consumo>
                <Qtd_geracao>8000.0</Qtd_geracao>
              </Linha>
            </Relatorio>
            """;

        var registro = Assert.Single(LeitorXml.LerXml(xml));

        Assert.Equal(2250.00m, registro.QtdConsumo);
        Assert.Equal(8000.0m, registro.QtdGeracao);
    }

    [Fact]
    public void LerXml_DeveMapearTodosOsCampos()
    {
        const string xml =
            """
            <Relatorio>
              <Linha>
                <Instalacao>3001</Instalacao>
                <Modalidade>Geradora</Modalidade>
                <Periodo>2026-09</Periodo>
                <Periodo_uso>2026-10</Periodo_uso>
                <Quota>25,00</Quota>
                <Qtd_consumo>1.000,00</Qtd_consumo>
                <Qtd_recebimento>200,00</Qtd_recebimento>
                <Qtd_transferencia>300,00</Qtd_transferencia>
                <Qtd_compensacao>400,00</Qtd_compensacao>
                <Qtd_geracao>500,00</Qtd_geracao>
                <Qtd_saldo_ant>600,00</Qtd_saldo_ant>
                <Qtd_saldo_atual>700,00</Qtd_saldo_atual>
              </Linha>
            </Relatorio>
            """;

        var registro = Assert.Single(LeitorXml.LerXml(xml));

        Assert.Equal("3001", registro.Instalacao);
        Assert.Equal("Geradora", registro.Modalidade);
        Assert.Equal("2026-09", registro.Periodo);
        Assert.Equal("2026-10", registro.PeriodoUso);
        Assert.Equal(25.00m, registro.Quota);
        Assert.Equal(1000.00m, registro.QtdConsumo);
        Assert.Equal(200.00m, registro.QtdRecebimento);
        Assert.Equal(300.00m, registro.QtdTransferencia);
        Assert.Equal(400.00m, registro.QtdCompensacao);
        Assert.Equal(500.00m, registro.QtdGeracao);
        Assert.Equal(600.00m, registro.QtdSaldoAnt);
        Assert.Equal(700.00m, registro.QtdSaldoAtual);
    }

    [Fact]
    public void LerXml_DeveTratarCamposAusentesComoVazioOuZero()
    {
        const string xml =
            """
            <Relatorio>
              <Linha>
                <Instalacao>3001</Instalacao>
              </Linha>
            </Relatorio>
            """;

        var registro = Assert.Single(LeitorXml.LerXml(xml));

        Assert.Equal("3001", registro.Instalacao);
        Assert.Equal(string.Empty, registro.Modalidade);
        Assert.Equal(string.Empty, registro.Periodo);
        Assert.Equal(string.Empty, registro.PeriodoUso);
        Assert.Equal(0m, registro.Quota);
        Assert.Equal(0m, registro.QtdConsumo);
        Assert.Equal(0m, registro.QtdRecebimento);
        Assert.Equal(0m, registro.QtdTransferencia);
        Assert.Equal(0m, registro.QtdCompensacao);
        Assert.Equal(0m, registro.QtdGeracao);
        Assert.Equal(0m, registro.QtdSaldoAnt);
        Assert.Equal(0m, registro.QtdSaldoAtual);
    }

    [Fact]
    public void LerXml_DevePropagarFormatExceptionParaValorInvalido()
    {
        const string xml =
            """
            <Relatorio>
              <Linha>
                <Instalacao>3001</Instalacao>
                <Qtd_consumo>abc</Qtd_consumo>
              </Linha>
            </Relatorio>
            """;

        Assert.Throws<FormatException>(() => LeitorXml.LerXml(xml));
    }

    [Theory]
    [InlineData("100,00000%", "100.00000")]
    [InlineData("20,00000%", "20.00000")]
    [InlineData("8,00000", "8.00000")]
    public void LerXml_DeveAceitarQuotaComOuSemSimboloDePercentual(string quotaXml, string esperado)
    {
        var xml = $"<Relatorio><Linha><Instalacao>3001</Instalacao><Quota>{quotaXml}</Quota></Linha></Relatorio>";

        var registro = Assert.Single(LeitorXml.LerXml(xml));

        Assert.Equal(decimal.Parse(esperado, System.Globalization.CultureInfo.InvariantCulture), registro.Quota);
    }

    [Fact]
    public void LerArquivo_DeveLerOArquivoRealDaCemig()
    {
        var registros = LeitorXml.LerArquivo(CaminhoArquivoReal());

        Assert.NotEmpty(registros);
        Assert.All(registros, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Instalacao));
            Assert.False(string.IsNullOrWhiteSpace(r.Modalidade));
            Assert.False(string.IsNullOrWhiteSpace(r.Periodo));
            Assert.False(string.IsNullOrWhiteSpace(r.PeriodoUso));
        });
    }

    [Fact]
    public void LerArquivo_DeveLerOsValoresDoArquivoRealComOsAcentosECamposNumericosCorretos()
    {
        var registros = LeitorXml.LerArquivo(CaminhoArquivoReal());

        Assert.Equal(14, registros.Count);

        var usina = registros[0];
        Assert.Equal("3010000000", usina.Instalacao);
        Assert.Equal("Geração compartilhada - Geradora", usina.Modalidade);
        Assert.Equal(100m, usina.Quota);
        Assert.Equal(11500m, usina.QtdGeracao);

        // Formato brasileiro e duas tags Qtd_compensacao na mesma linha (2.250,00 + 0,00).
        var recebedora = registros[2];
        Assert.Equal("3010000101", recebedora.Instalacao);
        Assert.Equal(2250m, recebedora.QtdConsumo);
        Assert.Equal(2250m, recebedora.QtdCompensacao);

        // Formato com ponto decimal e duas tags Qtd_compensacao (7400.0 + 500.0).
        var formatoPonto = registros[4];
        Assert.Equal("3010000202", formatoPonto.Instalacao);
        Assert.Equal("2026-08", formatoPonto.Periodo);
        Assert.Equal("2026-09", formatoPonto.PeriodoUso);
        Assert.Equal(8000m, formatoPonto.QtdConsumo);
        Assert.Equal(7900m, formatoPonto.QtdCompensacao);
    }

    private static string CaminhoArquivoReal()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "GreenVoltGd.slnx")))
        {
            pasta = pasta.Parent;
        }

        Assert.NotNull(pasta);
        return Path.Combine(pasta.FullName, "data", "Relatorio_GD_UFV_Exemplo_2026-09.xml");
    }

    [Fact]
    public void LerArquivo_DeveLerOMesmoXmlDeUmArquivo()
    {
        var caminho = Path.GetTempFileName();
        try
        {
            File.WriteAllText(caminho, XmlDuasCompensacoesNaMesmaLinha);

            var registros = LeitorXml.LerArquivo(caminho);

            var registro = Assert.Single(registros);
            Assert.Equal("3001", registro.Instalacao);
            Assert.Equal(150.50m, registro.QtdCompensacao);
        }
        finally
        {
            File.Delete(caminho);
        }
    }
}
