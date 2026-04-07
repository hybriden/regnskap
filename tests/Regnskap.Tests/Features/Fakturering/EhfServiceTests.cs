using System.Xml.Linq;
using FluentAssertions;
using Regnskap.Application.Features.Fakturering;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Fakturering;
using Regnskap.Domain.Features.Kundereskontro;

namespace Regnskap.Tests.Features.Fakturering;

public class EhfServiceTests
{
    private readonly FakeFakturaRepository _fakturaRepo;
    private readonly EhfService _ehfService;

    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

    public EhfServiceTests()
    {
        _fakturaRepo = new FakeFakturaRepository();
        _ehfService = new EhfService(_fakturaRepo);

        _fakturaRepo.Selskapsinfo = new Selskapsinfo
        {
            Id = Guid.NewGuid(),
            Navn = "Test Firma AS",
            Organisasjonsnummer = "987654321",
            Bankkontonummer = "12345678901",
            Adresse1 = "Storgata 1",
            Postnummer = "0100",
            Poststed = "Oslo",
            ErMvaRegistrert = true,
            PeppolId = "0192:987654321"
        };
    }

    [Fact]
    public async Task GenererEhfXml_UtstedtFaktura_GenererGyldigXml()
    {
        var faktura = LagUtstedtFaktura();
        _fakturaRepo.Fakturaer.Add(faktura);

        var xml = await _ehfService.GenererEhfXmlAsync(faktura.Id);

        xml.Should().NotBeEmpty();

        var doc = XDocument.Load(new MemoryStream(xml));
        doc.Root.Should().NotBeNull();

        var customId = doc.Root!.Element(Cbc + "CustomizationID");
        customId.Should().NotBeNull();
        customId!.Value.Should().Contain("peppol");
    }

    [Fact]
    public async Task GenererEhfXml_InneholderFakturanummer()
    {
        var faktura = LagUtstedtFaktura();
        _fakturaRepo.Fakturaer.Add(faktura);

        var xml = await _ehfService.GenererEhfXmlAsync(faktura.Id);

        var doc = XDocument.Load(new MemoryStream(xml));
        var id = doc.Root!.Element(Cbc + "ID");
        id.Should().NotBeNull();
        id!.Value.Should().Be(faktura.FakturaId);
    }

    [Fact]
    public async Task GenererEhfXml_InneholderInvoiceLines()
    {
        var faktura = LagUtstedtFaktura();
        _fakturaRepo.Fakturaer.Add(faktura);

        var xml = await _ehfService.GenererEhfXmlAsync(faktura.Id);

        var doc = XDocument.Load(new MemoryStream(xml));
        var lines = doc.Root!.Elements(Cac + "InvoiceLine");
        lines.Should().HaveCount(1);
    }

    [Fact]
    public async Task ValiderEhfXml_GyldigDokument_ReturnererTrue()
    {
        var faktura = LagUtstedtFaktura();
        _fakturaRepo.Fakturaer.Add(faktura);

        var xml = await _ehfService.GenererEhfXmlAsync(faktura.Id);

        var erGyldig = await _ehfService.ValiderEhfXml(xml);
        erGyldig.Should().BeTrue();
    }

    [Fact]
    public async Task ValiderEhfXml_UgyldigXml_ReturnererFalse()
    {
        var ugyldig = System.Text.Encoding.UTF8.GetBytes("dette er ikke xml");

        var erGyldig = await _ehfService.ValiderEhfXml(ugyldig);
        erGyldig.Should().BeFalse();
    }

    [Fact]
    public async Task GenererEhfXml_IkkeUtstedt_KasterException()
    {
        var faktura = LagUtstedtFaktura();
        faktura.Status = FakturaStatus.Utkast;
        _fakturaRepo.Fakturaer.Add(faktura);

        var act = () => _ehfService.GenererEhfXmlAsync(faktura.Id);

        await act.Should().ThrowAsync<FakturaException>();
    }

    private Faktura LagUtstedtFaktura()
    {
        var fakturaId = Guid.NewGuid();
        var faktura = new Faktura
        {
            Id = fakturaId,
            KundeId = Guid.NewGuid(),
            Kunde = new Kunde
            {
                Id = Guid.NewGuid(),
                Kundenummer = "000100",
                Navn = "Kunde AS",
                Organisasjonsnummer = "123456789",
                Adresse1 = "Kundegata 1",
                Postnummer = "0200",
                Poststed = "Oslo",
                Landkode = "NO",
                PeppolId = "0192:123456789"
            },
            Status = FakturaStatus.Utstedt,
            Dokumenttype = FakturaDokumenttype.Faktura,
            Fakturanummer = 42,
            FakturanummerAr = 2026,
            Fakturadato = new DateOnly(2026, 4, 1),
            Forfallsdato = new DateOnly(2026, 5, 1),
            Valutakode = "NOK",
            KjopersReferanse = "REF-001",
            KidNummer = "0001000000421",
            Bankkontonummer = "12345678901",
            BelopEksMva = new Belop(10000m),
            MvaBelop = new Belop(2500m),
            BelopInklMva = new Belop(12500m)
        };

        var linje = new FakturaLinje
        {
            Id = Guid.NewGuid(),
            FakturaId = fakturaId,
            Linjenummer = 1,
            Beskrivelse = "Konsulenttimer",
            Antall = 10,
            Enhet = Enhet.Timer,
            Enhetspris = new Belop(1000m),
            Nettobelop = new Belop(10000m),
            MvaKode = "3",
            MvaSats = 25m,
            MvaBelop = new Belop(2500m),
            Bruttobelop = new Belop(12500m),
            KontoId = Guid.NewGuid(),
            Kontonummer = "3000"
        };
        faktura.Linjer.Add(linje);

        faktura.MvaLinjer.Add(new FakturaMvaLinje
        {
            Id = Guid.NewGuid(),
            FakturaId = fakturaId,
            MvaKode = "3",
            MvaSats = 25m,
            Grunnlag = new Belop(10000m),
            MvaBelop = new Belop(2500m),
            EhfTaxCategoryId = "S"
        });

        return faktura;
    }
}
