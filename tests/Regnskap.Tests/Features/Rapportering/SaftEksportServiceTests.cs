using System.Xml.Linq;
using FluentAssertions;
using Regnskap.Application.Features.Rapportering;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Bilagsregistrering;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Kontoplan;
using Regnskap.Domain.Features.Kundereskontro;
namespace Regnskap.Tests.Features.Rapportering;

public class FakeSaftDataProvider : ISaftDataProvider
{
    public List<Konto> Kontoer { get; set; } = new();
    public List<Kunde> Kunder { get; set; } = new();
    public List<Regnskap.Domain.Features.Leverandorreskontro.Leverandor> Leverandorer { get; set; } = new();
    public List<MvaKode> MvaKoder { get; set; } = new();
    public List<BilagSerie> BilagSerier { get; set; } = new();
    public List<Bilag> BilagListe { get; set; } = new();

    public Task<List<Konto>> HentKontoerAsync(CancellationToken ct = default)
        => Task.FromResult(Kontoer);

    public Task<List<Kunde>> HentKunderAsync(CancellationToken ct = default)
        => Task.FromResult(Kunder);

    public Task<List<Regnskap.Domain.Features.Leverandorreskontro.Leverandor>> HentLeverandorerAsync(CancellationToken ct = default)
        => Task.FromResult(Leverandorer);

    public Task<List<MvaKode>> HentMvaKoderAsync(CancellationToken ct = default)
        => Task.FromResult(MvaKoder);

    public Task<List<BilagSerie>> HentBilagSerierAsync(CancellationToken ct = default)
        => Task.FromResult(BilagSerier);

    public Task<List<Bilag>> HentBilagMedPosteringerAsync(int ar, int fraPeriode, int tilPeriode, CancellationToken ct = default)
        => Task.FromResult(BilagListe);
}

public class SaftEksportServiceTests
{
    private static readonly XNamespace SaftNs = "urn:StandardAuditFile-Taxation-Financial:NO";

    private readonly FakeRapporteringRepository _repo;
    private readonly FakeSaftDataProvider _dataProvider;
    private readonly SaftEksportService _sut;

    public SaftEksportServiceTests()
    {
        _repo = new FakeRapporteringRepository();
        _dataProvider = new FakeSaftDataProvider();
        _sut = new SaftEksportService(_repo, _dataProvider);

        // Standard konfigurasjon
        _repo.SettKonfigurasjon(new Domain.Features.Rapportering.RapportKonfigurasjon
        {
            Id = Guid.NewGuid(),
            Firmanavn = "Test AS",
            Organisasjonsnummer = "123456789",
            Adresse = "Testveien 1",
            Postnummer = "0001",
            Poststed = "Oslo",
            Landskode = "NO",
            ErMvaRegistrert = true,
            Valuta = "NOK"
        });
    }

    [Fact]
    public async Task GenererSaftXml_ReturnererGyldigXml()
    {
        // Arrange
        SetupMinimalData();

        // Act
        var stream = await _sut.GenererSaftXmlAsync(2026);

        // Assert
        stream.Should().NotBeNull();
        stream.Position.Should().Be(0);

        var doc = XDocument.Load(stream);
        doc.Root.Should().NotBeNull();
        doc.Root!.Name.LocalName.Should().Be("AuditFile");
    }

    [Fact]
    public async Task GenererSaftXml_HarKorrektHeader()
    {
        SetupMinimalData();

        var stream = await _sut.GenererSaftXmlAsync(2026);
        var doc = XDocument.Load(stream);

        var header = doc.Root!.Element(SaftNs + "Header");
        header.Should().NotBeNull();
        header!.Element(SaftNs + "AuditFileVersion")!.Value.Should().Be("1.30");
        header.Element(SaftNs + "AuditFileCountry")!.Value.Should().Be("NO");

        var company = header.Element(SaftNs + "Company")!;
        company.Element(SaftNs + "RegistrationNumber")!.Value.Should().Be("123456789");
        company.Element(SaftNs + "Name")!.Value.Should().Be("Test AS");
    }

    [Fact]
    public async Task GenererSaftXml_InkludererMasterFiles()
    {
        SetupMinimalData();

        var stream = await _sut.GenererSaftXmlAsync(2026);
        var doc = XDocument.Load(stream);

        var masterFiles = doc.Root!.Element(SaftNs + "MasterFiles");
        masterFiles.Should().NotBeNull();
        masterFiles!.Element(SaftNs + "GeneralLedgerAccounts").Should().NotBeNull();
        masterFiles.Element(SaftNs + "Customers").Should().NotBeNull();
        masterFiles.Element(SaftNs + "Suppliers").Should().NotBeNull();
        masterFiles.Element(SaftNs + "TaxTable").Should().NotBeNull();
    }

    [Fact]
    public async Task GenererSaftXml_InkludererGeneralLedgerEntries()
    {
        SetupMinimalData();

        var bilag = new Bilag
        {
            Id = Guid.NewGuid(),
            Bilagsnummer = 1,
            Ar = 2026,
            Type = BilagType.Manuelt,
            Bilagsdato = new DateOnly(2026, 1, 15),
            Registreringsdato = DateTime.UtcNow,
            Beskrivelse = "Testbilag",
            ErBokfort = true,
            SerieKode = "MAN",
            Posteringer = new List<Postering>
            {
                new() { Id = Guid.NewGuid(), Linjenummer = 1, Kontonummer = "1920",
                    Side = BokforingSide.Debet, Belop = new Belop(1000m), Beskrivelse = "Debet" },
                new() { Id = Guid.NewGuid(), Linjenummer = 2, Kontonummer = "3000",
                    Side = BokforingSide.Kredit, Belop = new Belop(1000m), Beskrivelse = "Kredit" },
            }
        };

        _dataProvider.BilagListe.Add(bilag);

        var stream = await _sut.GenererSaftXmlAsync(2026);
        var doc = XDocument.Load(stream);

        var entries = doc.Root!.Element(SaftNs + "GeneralLedgerEntries");
        entries.Should().NotBeNull();
        entries!.Element(SaftNs + "NumberOfEntries")!.Value.Should().Be("1");
        entries.Element(SaftNs + "TotalDebit")!.Value.Should().Be("1000.00");
        entries.Element(SaftNs + "TotalCredit")!.Value.Should().Be("1000.00");
    }

    [Fact]
    public async Task GenererSaftXml_UtenKonfigurasjon_KasterException()
    {
        _repo.SettKonfigurasjon(null!);

        var action = async () => await _sut.GenererSaftXmlAsync(2026);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ValiderSaftXml_GyldigXml_ReturnererIngenFeil()
    {
        SetupMinimalData();
        var stream = await _sut.GenererSaftXmlAsync(2026);

        var feil = await _sut.ValiderSaftXmlAsync(stream);

        feil.Should().BeEmpty();
    }

    [Fact]
    public async Task ValiderSaftXml_UgyldigXml_ReturnererFeil()
    {
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("<invalid>xml</invalid>"));

        var feil = await _sut.ValiderSaftXmlAsync(stream);

        feil.Should().NotBeEmpty();
    }

    private void SetupMinimalData()
    {
        var kontogruppe = new Kontogruppe
        {
            Id = Guid.NewGuid(), Gruppekode = 19, Navn = "Bank",
            Kontotype = Kontotype.Eiendel, Normalbalanse = Normalbalanse.Debet
        };

        _dataProvider.Kontoer.Add(new Konto
        {
            Id = Guid.NewGuid(),
            Kontonummer = "1920",
            Navn = "Bankkonto",
            Kontotype = Kontotype.Eiendel,
            Normalbalanse = Normalbalanse.Debet,
            StandardAccountId = "1920",
            KontogruppeId = kontogruppe.Id,
            Kontogruppe = kontogruppe,
            GrupperingsKategori = GrupperingsKategori.RF1167,
            GrupperingsKode = "1920"
        });

        _dataProvider.MvaKoder.Add(new MvaKode
        {
            Id = Guid.NewGuid(),
            Kode = "0",
            Beskrivelse = "Ingen MVA",
            StandardTaxCode = "0",
            Sats = 0m,
            Retning = MvaRetning.Ingen
        });

        _dataProvider.BilagSerier.Add(new BilagSerie
        {
            Id = Guid.NewGuid(),
            Kode = "MAN",
            Navn = "Manuelle bilag",
            StandardType = BilagType.Manuelt,
            SaftJournalId = "MAN"
        });
    }
}
