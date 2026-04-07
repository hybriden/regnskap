namespace Regnskap.Application.Features.Fakturering;

using Regnskap.Domain.Features.Fakturering;

/// <summary>
/// Service for fakturagenerering og -haandtering.
/// </summary>
public interface IFaktureringService
{
    Task<Faktura> OpprettFakturaAsync(OpprettFakturaRequest request, CancellationToken ct = default);
    Task<Faktura> OppdaterFakturaAsync(Guid fakturaId, OpprettFakturaRequest request, CancellationToken ct = default);
    Task<Faktura> UtstedeFakturaAsync(Guid fakturaId, CancellationToken ct = default);
    Task<Faktura> OpprettKreditnotaAsync(Guid originalFakturaId, OpprettKreditnotaRequest request, CancellationToken ct = default);
    Task KansellerFakturaAsync(Guid fakturaId, CancellationToken ct = default);
    Task<Faktura?> HentFakturaAsync(Guid fakturaId, CancellationToken ct = default);
    Task<FakturaSokResultat> SokFakturaerAsync(FakturaSokFilter filter, CancellationToken ct = default);
}

/// <summary>
/// Service for EHF/PEPPOL XML-generering.
/// </summary>
public interface IEhfService
{
    Task<byte[]> GenererEhfXmlAsync(Guid fakturaId, CancellationToken ct = default);
    Task<bool> ValiderEhfXml(byte[] xml);
}

/// <summary>
/// Service for PDF-generering.
/// </summary>
public interface IFakturaPdfService
{
    Task<byte[]> GenererPdfAsync(Guid fakturaId, CancellationToken ct = default);
}

// --- DTOs ---

public record OpprettFakturaRequest(
    Guid KundeId,
    DateOnly? Leveringsdato,
    DateOnly? LeveringsperiodeSlutt,
    string? Bestillingsnummer,
    string? KjopersReferanse,
    string? VaarReferanse,
    string? EksternReferanse,
    string? Merknad,
    string Valutakode = "NOK",
    FakturaLeveringsformat Leveringsformat = FakturaLeveringsformat.Epost,
    List<FakturaLinjeRequest>? Linjer = null
);

public record FakturaLinjeRequest(
    string Beskrivelse,
    decimal Antall,
    Enhet Enhet,
    decimal Enhetspris,
    string MvaKode,
    Guid KontoId,
    decimal MvaSats = 25m,
    string Kontonummer = "3000",
    RabattType? RabattType = null,
    decimal? RabattProsent = null,
    decimal? RabattBelop = null,
    string? Avdelingskode = null,
    string? Prosjektkode = null
);

public record OpprettKreditnotaRequest(
    string Krediteringsaarsak,
    string? KjopersReferanse,
    List<KreditnotaLinjeRequest>? Linjer
);

public record KreditnotaLinjeRequest(
    int OpprinneligLinjenummer,
    decimal Antall
);

public record FakturaSokResultat(
    List<Faktura> Fakturaer,
    int TotaltAntall,
    int Side,
    int Antall
);
