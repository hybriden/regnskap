namespace Regnskap.Application.Features.Leverandorreskontro;

using System.Text;
using System.Xml;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Leverandorreskontro;

public class BetalingsforslagService : IBetalingsforslagService
{
    private readonly ILeverandorReskontroRepository _repo;

    public BetalingsforslagService(ILeverandorReskontroRepository repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// FR-L04: Generer betalingsforslag.
    /// </summary>
    public async Task<BetalingsforslagDto> GenererAsync(GenererBetalingsforslagRequest request, CancellationToken ct = default)
    {
        // Hent alle forfalne, godkjente fakturaer
        var fakturaer = await _repo.HentForfalteFakturaerForBetalingAsync(
            request.ForfallTilOgMed, request.LeverandorIder, ct);

        if (request.InkluderKunGodkjente)
            fakturaer = fakturaer.Where(f => f.Status == FakturaStatus.Godkjent).ToList();
        else
            fakturaer = fakturaer.Where(f =>
                f.Status == FakturaStatus.Godkjent || f.Status == FakturaStatus.Registrert).ToList();

        // Filtrer bort sperrede
        fakturaer = fakturaer
            .Where(f => !f.ErSperret && !f.Leverandor.ErSperret)
            .Where(f => f.GjenstaendeBelop.Verdi > 0)
            .ToList();

        var forslagsnummer = await _repo.NesteForslagsnummerAsync(ct);

        var forslag = new Betalingsforslag
        {
            Id = Guid.NewGuid(),
            Forslagsnummer = forslagsnummer,
            Beskrivelse = $"Betalingsforslag {forslagsnummer} - forfall t.o.m. {request.ForfallTilOgMed:yyyy-MM-dd}",
            Opprettdato = DateOnly.FromDateTime(DateTime.UtcNow),
            Betalingsdato = request.Betalingsdato,
            ForfallTilOgMed = request.ForfallTilOgMed,
            Status = BetalingsforslagStatus.Utkast,
            FraBankkontoId = request.FraBankkontoId,
            FraKontonummer = request.FraKontonummer
        };

        foreach (var faktura in fakturaer)
        {
            var linje = new BetalingsforslagLinje
            {
                Id = Guid.NewGuid(),
                BetalingsforslagId = forslag.Id,
                LeverandorFakturaId = faktura.Id,
                LeverandorId = faktura.LeverandorId,
                Belop = faktura.GjenstaendeBelop,
                MottakerKontonummer = faktura.Leverandor.Bankkontonummer,
                MottakerIban = faktura.Leverandor.Iban,
                MottakerBic = faktura.Leverandor.Bic,
                KidNummer = faktura.KidNummer,
                Melding = faktura.KidNummer == null
                    ? $"Faktura {faktura.EksternFakturanummer}"
                    : null,
                EndToEndId = Guid.NewGuid().ToString("N")[..16],
                ErInkludert = true
            };
            forslag.Linjer.Add(linje);
        }

        forslag.OppdaterTotaler();

        await _repo.LeggTilBetalingsforslagAsync(forslag, ct);
        await _repo.LagreEndringerAsync(ct);

        return MapToDto(forslag);
    }

    public async Task<BetalingsforslagDto> HentAsync(Guid id, CancellationToken ct = default)
    {
        var forslag = await _repo.HentBetalingsforslagMedLinjerAsync(id, ct)
            ?? throw new BetalingsforslagIkkeFunnetException(id);
        return MapToDto(forslag);
    }

    public async Task<BetalingsforslagDto> GodkjennAsync(Guid id, string godkjentAv, CancellationToken ct = default)
    {
        var forslag = await _repo.HentBetalingsforslagMedLinjerAsync(id, ct)
            ?? throw new BetalingsforslagIkkeFunnetException(id);

        if (forslag.Status != BetalingsforslagStatus.Utkast)
            throw new BetalingsforslagStatusException(
                $"Forslag kan kun godkjennes fra status Utkast. Navarende: {forslag.Status}");

        forslag.Status = BetalingsforslagStatus.Godkjent;
        forslag.GodkjentAv = godkjentAv;
        forslag.GodkjentTidspunkt = DateTime.UtcNow;

        // Oppdater fakturastatus
        foreach (var linje in forslag.Linjer.Where(l => l.ErInkludert))
        {
            var faktura = await _repo.HentFakturaAsync(linje.LeverandorFakturaId, ct);
            if (faktura != null)
            {
                faktura.Status = FakturaStatus.IBetalingsforslag;
                await _repo.OppdaterFakturaAsync(faktura, ct);
            }
        }

        await _repo.OppdaterBetalingsforslagAsync(forslag, ct);
        await _repo.LagreEndringerAsync(ct);

        return MapToDto(forslag);
    }

    /// <summary>
    /// FR-L05: Generer pain.001 betalingsfil.
    /// </summary>
    public async Task<byte[]> GenererFilAsync(Guid id, CancellationToken ct = default)
    {
        var forslag = await _repo.HentBetalingsforslagMedLinjerAsync(id, ct)
            ?? throw new BetalingsforslagIkkeFunnetException(id);

        if (forslag.Status != BetalingsforslagStatus.Godkjent)
            throw new BetalingsforslagStatusException(
                $"Fil kan kun genereres for godkjente forslag. Navarende: {forslag.Status}");

        var inkluderte = forslag.Linjer.Where(l => l.ErInkludert).ToList();
        if (inkluderte.Count == 0)
            throw new BetalingsforslagStatusException("Ingen inkluderte linjer i forslaget.");

        var xml = GenererPain001Xml(forslag, inkluderte);

        forslag.Status = BetalingsforslagStatus.FilGenerert;
        forslag.FilGenererTidspunkt = DateTime.UtcNow;
        forslag.BetalingsfilReferanse = $"pain001_{forslag.Forslagsnummer}_{DateTime.UtcNow:yyyyMMddHHmmss}.xml";

        await _repo.OppdaterBetalingsforslagAsync(forslag, ct);
        await _repo.LagreEndringerAsync(ct);

        return xml;
    }

    public async Task MarkerSendtAsync(Guid id, CancellationToken ct = default)
    {
        var forslag = await _repo.HentBetalingsforslagAsync(id, ct)
            ?? throw new BetalingsforslagIkkeFunnetException(id);

        if (forslag.Status != BetalingsforslagStatus.FilGenerert)
            throw new BetalingsforslagStatusException(
                $"Forslag kan kun sendes fra status FilGenerert. Navarende: {forslag.Status}");

        forslag.Status = BetalingsforslagStatus.SendtTilBank;
        forslag.SendtTilBankTidspunkt = DateTime.UtcNow;

        await _repo.OppdaterBetalingsforslagAsync(forslag, ct);
        await _repo.LagreEndringerAsync(ct);
    }

    public async Task KansellerAsync(Guid id, CancellationToken ct = default)
    {
        var forslag = await _repo.HentBetalingsforslagMedLinjerAsync(id, ct)
            ?? throw new BetalingsforslagIkkeFunnetException(id);

        if (forslag.Status != BetalingsforslagStatus.Utkast)
            throw new BetalingsforslagStatusException(
                $"Kun utkast kan kanselleres. Navarende: {forslag.Status}");

        forslag.Status = BetalingsforslagStatus.Kansellert;

        await _repo.OppdaterBetalingsforslagAsync(forslag, ct);
        await _repo.LagreEndringerAsync(ct);
    }

    public async Task EkskluderLinjeAsync(Guid forslagId, Guid linjeId, CancellationToken ct = default)
    {
        var forslag = await _repo.HentBetalingsforslagMedLinjerAsync(forslagId, ct)
            ?? throw new BetalingsforslagIkkeFunnetException(forslagId);

        if (forslag.Status != BetalingsforslagStatus.Utkast)
            throw new BetalingsforslagStatusException("Kan kun endre linjer i utkast.");

        var linje = forslag.Linjer.FirstOrDefault(l => l.Id == linjeId)
            ?? throw new ArgumentException($"Linje {linjeId} finnes ikke i forslaget.");

        linje.ErInkludert = false;
        forslag.OppdaterTotaler();

        await _repo.OppdaterBetalingsforslagAsync(forslag, ct);
        await _repo.LagreEndringerAsync(ct);
    }

    public async Task InkluderLinjeAsync(Guid forslagId, Guid linjeId, CancellationToken ct = default)
    {
        var forslag = await _repo.HentBetalingsforslagMedLinjerAsync(forslagId, ct)
            ?? throw new BetalingsforslagIkkeFunnetException(forslagId);

        if (forslag.Status != BetalingsforslagStatus.Utkast)
            throw new BetalingsforslagStatusException("Kan kun endre linjer i utkast.");

        var linje = forslag.Linjer.FirstOrDefault(l => l.Id == linjeId)
            ?? throw new ArgumentException($"Linje {linjeId} finnes ikke i forslaget.");

        linje.ErInkludert = true;
        forslag.OppdaterTotaler();

        await _repo.OppdaterBetalingsforslagAsync(forslag, ct);
        await _repo.LagreEndringerAsync(ct);
    }

    /// <summary>
    /// Generer pain.001 (ISO 20022) XML.
    /// </summary>
    private static byte[] GenererPain001Xml(Betalingsforslag forslag, List<BetalingsforslagLinje> linjer)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8
        };

        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("Document", "urn:iso:std:iso:20022:tech:xsd:pain.001.001.03");

            writer.WriteStartElement("CstmrCdtTrfInitn");

            // GrpHdr
            writer.WriteStartElement("GrpHdr");
            writer.WriteElementString("MsgId", Guid.NewGuid().ToString("N")[..16]);
            writer.WriteElementString("CreDtTm", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"));
            writer.WriteElementString("NbOfTxs", linjer.Count.ToString());
            writer.WriteElementString("CtrlSum", linjer.Sum(l => l.Belop.Verdi).ToString("F2"));
            writer.WriteStartElement("InitgPty");
            writer.WriteElementString("Nm", "Regnskap AS");
            writer.WriteEndElement(); // InitgPty
            writer.WriteEndElement(); // GrpHdr

            // PmtInf
            writer.WriteStartElement("PmtInf");
            writer.WriteElementString("PmtInfId", $"PMT-{forslag.Forslagsnummer}");
            writer.WriteElementString("PmtMtd", "TRF");
            writer.WriteElementString("NbOfTxs", linjer.Count.ToString());
            writer.WriteElementString("CtrlSum", linjer.Sum(l => l.Belop.Verdi).ToString("F2"));

            writer.WriteStartElement("ReqdExctnDt");
            writer.WriteElementString("Dt", forslag.Betalingsdato.ToString("yyyy-MM-dd"));
            writer.WriteEndElement();

            // DbtrAcct
            writer.WriteStartElement("DbtrAcct");
            writer.WriteStartElement("Id");
            if (forslag.FraKontonummer != null)
                writer.WriteElementString("Othr", forslag.FraKontonummer);
            writer.WriteEndElement();
            writer.WriteEndElement(); // DbtrAcct

            // CdtTrfTxInf per linje
            foreach (var linje in linjer)
            {
                writer.WriteStartElement("CdtTrfTxInf");

                writer.WriteStartElement("PmtId");
                writer.WriteElementString("EndToEndId", linje.EndToEndId ?? Guid.NewGuid().ToString("N")[..16]);
                writer.WriteEndElement();

                writer.WriteStartElement("Amt");
                writer.WriteStartElement("InstdAmt");
                writer.WriteAttributeString("Ccy", "NOK");
                writer.WriteString(linje.Belop.Verdi.ToString("F2"));
                writer.WriteEndElement();
                writer.WriteEndElement(); // Amt

                // CdtrAcct
                writer.WriteStartElement("CdtrAcct");
                writer.WriteStartElement("Id");
                if (!string.IsNullOrEmpty(linje.MottakerIban))
                {
                    writer.WriteElementString("IBAN", linje.MottakerIban);
                }
                else if (!string.IsNullOrEmpty(linje.MottakerKontonummer))
                {
                    writer.WriteStartElement("Othr");
                    writer.WriteElementString("Id", linje.MottakerKontonummer);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndElement(); // CdtrAcct

                // RmtInf
                writer.WriteStartElement("RmtInf");
                if (!string.IsNullOrEmpty(linje.KidNummer))
                {
                    writer.WriteStartElement("Strd");
                    writer.WriteStartElement("CdtrRefInf");
                    writer.WriteElementString("Ref", linje.KidNummer);
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
                else if (!string.IsNullOrEmpty(linje.Melding))
                {
                    writer.WriteElementString("Ustrd", linje.Melding);
                }
                writer.WriteEndElement(); // RmtInf

                writer.WriteEndElement(); // CdtTrfTxInf
            }

            writer.WriteEndElement(); // PmtInf
            writer.WriteEndElement(); // CstmrCdtTrfInitn
            writer.WriteEndElement(); // Document
            writer.WriteEndDocument();
        }

        return ms.ToArray();
    }

    internal static BetalingsforslagDto MapToDto(Betalingsforslag f) => new(
        f.Id,
        f.Forslagsnummer,
        f.Beskrivelse,
        f.Opprettdato,
        f.Betalingsdato,
        f.ForfallTilOgMed,
        f.Status,
        f.TotalBelop.Verdi,
        f.AntallBetalinger,
        f.FraKontonummer,
        f.BetalingsfilReferanse,
        f.GodkjentAv,
        f.GodkjentTidspunkt,
        f.Linjer.Select(l => new BetalingsforslagLinjeDto(
            l.Id,
            l.LeverandorFakturaId,
            l.Leverandor?.Navn ?? "",
            l.Leverandor?.Leverandornummer ?? "",
            l.LeverandorFaktura?.EksternFakturanummer ?? "",
            l.Belop.Verdi,
            l.MottakerKontonummer,
            l.KidNummer,
            l.ErInkludert,
            l.ErUtfort,
            l.Feilmelding
        )).ToList()
    );
}
