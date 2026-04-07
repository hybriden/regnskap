using System.Text;
using FluentAssertions;
using Regnskap.Application.Features.Bank;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Bankavstemming;

namespace Regnskap.Tests.Features.Bank;

public class Camt053ImportServiceTests
{
    private readonly FakeBankRepository _bankRepo;
    private readonly FakeKundeRepoForBank _kundeRepo;
    private readonly FakeLeverandorRepoForBank _leverandorRepo;
    private readonly BankMatchingService _matchingService;
    private readonly Camt053ImportService _sut;
    private readonly Bankkonto _testKonto;

    public Camt053ImportServiceTests()
    {
        _bankRepo = new FakeBankRepository();
        _kundeRepo = new FakeKundeRepoForBank();
        _leverandorRepo = new FakeLeverandorRepoForBank();
        _matchingService = new BankMatchingService(_bankRepo, _kundeRepo, _leverandorRepo, new FakeBilagRegistreringServiceForBank());
        _sut = new Camt053ImportService(_bankRepo, _matchingService);

        _testKonto = new Bankkonto
        {
            Id = Guid.NewGuid(),
            Kontonummer = "12345678903",
            Iban = "NO9386011117947",
            Banknavn = "DNB",
            Beskrivelse = "Driftskonto",
            Hovedbokkontonummer = "1920",
            HovedbokkkontoId = Guid.NewGuid()
        };
        _bankRepo.Bankkontoer.Add(_testKonto);
    }

    private static Stream LagCamt053Xml(string meldingsId = "MSG001", string iban = "NO9386011117947",
        decimal inngaende = 10000m, decimal utgaende = 12500m,
        List<(string cdtDbt, decimal amt, string? kid, string? date)>? bevegelser = null)
    {
        bevegelser ??= new List<(string, decimal, string?, string?)>
        {
            ("CRDT", 2500m, "0001000000013", "2026-03-15")
        };

        var ntryXml = new StringBuilder();
        foreach (var (cdtDbt, amt, kid, date) in bevegelser)
        {
            var kidXml = kid != null
                ? $@"<NtryDtls><TxDtls><RmtInf><Strd><CdtrRefInf><Ref>{kid}</Ref></CdtrRefInf></Strd></RmtInf></TxDtls></NtryDtls>"
                : "";
            ntryXml.Append($@"
            <Ntry>
                <Amt Ccy=""NOK"">{amt.ToString(System.Globalization.CultureInfo.InvariantCulture)}</Amt>
                <CdtDbtInd>{cdtDbt}</CdtDbtInd>
                <BookgDt><Dt>{date ?? "2026-03-15"}</Dt></BookgDt>
                {kidXml}
            </Ntry>");
        }

        var xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Document xmlns=""urn:iso:std:iso:20022:tech:xsd:camt.053.001.02"">
    <BkToCstmrStmt>
        <GrpHdr>
            <MsgId>{meldingsId}</MsgId>
        </GrpHdr>
        <Stmt>
            <Id>STMT001</Id>
            <Acct><Id><IBAN>{iban}</IBAN></Id></Acct>
            <Bal>
                <Tp><CdOrPrtry><Cd>OPBD</Cd></CdOrPrtry></Tp>
                <Amt Ccy=""NOK"">{inngaende.ToString(System.Globalization.CultureInfo.InvariantCulture)}</Amt>
                <CdtDbtInd>CRDT</CdtDbtInd>
            </Bal>
            <Bal>
                <Tp><CdOrPrtry><Cd>CLBD</Cd></CdOrPrtry></Tp>
                <Amt Ccy=""NOK"">{utgaende.ToString(System.Globalization.CultureInfo.InvariantCulture)}</Amt>
                <CdtDbtInd>CRDT</CdtDbtInd>
            </Bal>
            <FrToDt>
                <FrDt>2026-03-01</FrDt>
                <ToDt>2026-03-31</ToDt>
            </FrToDt>
            {ntryXml}
        </Stmt>
    </BkToCstmrStmt>
</Document>";

        return new MemoryStream(Encoding.UTF8.GetBytes(xml));
    }

    [Fact]
    public async Task Importer_GyldigCamt053_OppretterKontoutskriftOgBevegelser()
    {
        using var xml = LagCamt053Xml();
        var resultat = await _sut.Importer(_testKonto.Id, xml, "test.xml");

        resultat.MeldingsId.Should().Be("MSG001");
        resultat.AntallBevegelser.Should().Be(1);
        resultat.PeriodeFra.Should().Be(new DateOnly(2026, 3, 1));
        resultat.PeriodeTil.Should().Be(new DateOnly(2026, 3, 31));
        _bankRepo.Kontoutskrifter.Should().HaveCount(1);
        _bankRepo.Bevegelser.Should().HaveCount(1);
    }

    [Fact]
    public async Task Importer_ParserSaldoerKorrekt()
    {
        using var xml = LagCamt053Xml(inngaende: 50000m, utgaende: 52500m);
        var resultat = await _sut.Importer(_testKonto.Id, xml, "test.xml");

        resultat.InngaendeSaldo.Should().Be(50000m);
        resultat.UtgaendeSaldo.Should().Be(52500m);
    }

    [Fact]
    public async Task Importer_DuplikatMeldingsId_KasterException()
    {
        using var xml1 = LagCamt053Xml(meldingsId: "DUP001");
        await _sut.Importer(_testKonto.Id, xml1, "test1.xml");

        using var xml2 = LagCamt053Xml(meldingsId: "DUP001");
        var act = () => _sut.Importer(_testKonto.Id, xml2, "test2.xml");
        await act.Should().ThrowAsync<ImportDuplikatException>();
    }

    [Fact]
    public async Task Importer_FeilIban_KasterException()
    {
        using var xml = LagCamt053Xml(iban: "NO0000000000000");
        var act = () => _sut.Importer(_testKonto.Id, xml, "test.xml");
        await act.Should().ThrowAsync<ImportFeilKontoException>();
    }

    [Fact]
    public async Task Importer_UgyldigXml_KasterException()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("dette er ikke xml"));
        var act = () => _sut.Importer(_testKonto.Id, stream, "test.xml");
        await act.Should().ThrowAsync<ImportUgyldigXmlException>();
    }

    [Fact]
    public async Task Importer_FlereBevegelser_ParserAlle()
    {
        var bevegelser = new List<(string, decimal, string?, string?)>
        {
            ("CRDT", 5000m, null, "2026-03-10"),
            ("DBIT", 2500m, null, "2026-03-12"),
            ("CRDT", 1000m, "0001000000026", "2026-03-15")
        };
        using var xml = LagCamt053Xml(bevegelser: bevegelser, utgaende: 13500m);
        var resultat = await _sut.Importer(_testKonto.Id, xml, "test.xml");

        resultat.AntallBevegelser.Should().Be(3);
        _bankRepo.Bevegelser.Should().HaveCount(3);
        _bankRepo.Bevegelser.Count(b => b.Retning == BankbevegelseRetning.Inn).Should().Be(2);
        _bankRepo.Bevegelser.Count(b => b.Retning == BankbevegelseRetning.Ut).Should().Be(1);
    }

    [Fact]
    public async Task Importer_ParserKidFraCamt053()
    {
        var bevegelser = new List<(string, decimal, string?, string?)>
        {
            ("CRDT", 2500m, "0001000000013", "2026-03-15")
        };
        using var xml = LagCamt053Xml(bevegelser: bevegelser);
        await _sut.Importer(_testKonto.Id, xml, "test.xml");

        _bankRepo.Bevegelser.First().KidNummer.Should().Be("0001000000013");
    }
}
