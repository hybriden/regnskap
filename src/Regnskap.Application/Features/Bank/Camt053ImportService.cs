namespace Regnskap.Application.Features.Bank;

using System.Security.Cryptography;
using System.Xml.Linq;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Bankavstemming;

/// <summary>
/// Service for import og parsing av CAMT.053 XML-filer.
/// Implementerer FR-B01 fra spesifikasjonen.
/// </summary>
public class Camt053ImportService : ICamt053ImportService
{
    private readonly IBankRepository _repo;
    private readonly IBankMatchingService _matchingService;

    private static readonly XNamespace Ns = "urn:iso:std:iso:20022:tech:xsd:camt.053.001.02";

    public Camt053ImportService(IBankRepository repo, IBankMatchingService matchingService)
    {
        _repo = repo;
        _matchingService = matchingService;
    }

    public async Task<ImportKontoutskriftResultat> Importer(Guid bankkontoId, Stream fil, string filnavn)
    {
        var bankkonto = await _repo.HentBankkonto(bankkontoId)
            ?? throw new KeyNotFoundException($"Bankkonto {bankkontoId} ikke funnet.");

        // Les og hash filen
        using var memStream = new MemoryStream();
        await fil.CopyToAsync(memStream);
        var filBytes = memStream.ToArray();
        var filHash = Convert.ToHexString(SHA256.HashData(filBytes));

        // Parse XML
        XDocument doc;
        try
        {
            memStream.Position = 0;
            doc = await XDocument.LoadAsync(memStream, LoadOptions.None, CancellationToken.None);
        }
        catch (Exception ex)
        {
            throw new ImportUgyldigXmlException(ex.Message);
        }

        // Finn namespace (stott flere CAMT.053 versjoner)
        var ns = DetekterNamespace(doc);

        var grpHdr = doc.Descendants(ns + "GrpHdr").FirstOrDefault()
            ?? throw new ImportUgyldigXmlException("Mangler GrpHdr-element.");
        var meldingsId = grpHdr.Element(ns + "MsgId")?.Value
            ?? throw new ImportUgyldigXmlException("Mangler MsgId i GrpHdr.");

        // Duplikatkontroll
        if (await _repo.KontoutskriftFinnes(bankkontoId, meldingsId))
            throw new ImportDuplikatException(meldingsId);

        var stmt = doc.Descendants(ns + "Stmt").FirstOrDefault()
            ?? throw new ImportUgyldigXmlException("Mangler Stmt-element.");

        // Valider konto-matching
        ValiderKontoMatch(stmt, ns, bankkonto);

        // Parse saldoer
        var (inngaende, utgaende) = ParseSaldoer(stmt, ns);

        // Parse periode
        var frDtToTm = stmt.Descendants(ns + "FrDtTm").FirstOrDefault();
        var toDtToTm = stmt.Descendants(ns + "ToDtTm").FirstOrDefault();
        var frDt = stmt.Descendants(ns + "FrDt").FirstOrDefault();
        var toDt = stmt.Descendants(ns + "ToDt").FirstOrDefault();

        var periodeFra = frDtToTm != null
            ? DateOnly.FromDateTime(DateTime.Parse(frDtToTm.Value))
            : frDt != null ? DateOnly.Parse(frDt.Value) : DateOnly.FromDateTime(DateTime.UtcNow);
        var periodeTil = toDtToTm != null
            ? DateOnly.FromDateTime(DateTime.Parse(toDtToTm.Value))
            : toDt != null ? DateOnly.Parse(toDt.Value) : DateOnly.FromDateTime(DateTime.UtcNow);

        // Opprett kontoutskrift
        var kontoutskrift = new Kontoutskrift
        {
            Id = Guid.NewGuid(),
            BankkontoId = bankkontoId,
            MeldingsId = meldingsId,
            UtskriftId = stmt.Element(ns + "Id")?.Value ?? meldingsId,
            Sekvensnummer = stmt.Element(ns + "ElctrncSeqNb")?.Value,
            OpprettetAvBank = DateTime.UtcNow,
            PeriodeFra = periodeFra,
            PeriodeTil = periodeTil,
            InngaendeSaldo = new Belop(inngaende),
            UtgaendeSaldo = new Belop(utgaende),
            OriginalFilsti = filnavn,
            FilHash = filHash,
            Status = KontoutskriftStatus.Importert
        };

        // Parse bevegelser (Ntry-noder)
        var ntryElements = stmt.Elements(ns + "Ntry").ToList();
        decimal sumInn = 0m, sumUt = 0m;

        foreach (var ntry in ntryElements)
        {
            var bevegelse = ParseNtry(ntry, ns, kontoutskrift.Id, bankkontoId);
            kontoutskrift.Bevegelser.Add(bevegelse);

            if (bevegelse.Retning == BankbevegelseRetning.Inn)
                sumInn += bevegelse.Belop.Verdi;
            else
                sumUt += bevegelse.Belop.Verdi;
        }

        kontoutskrift.AntallBevegelser = ntryElements.Count;
        kontoutskrift.SumInn = new Belop(sumInn);
        kontoutskrift.SumUt = new Belop(sumUt);

        await _repo.LeggTilKontoutskrift(kontoutskrift);
        await _repo.LagreEndringerAsync();

        // Kjoer automatisk matching
        var antallMatchet = await _matchingService.AutoMatch(bankkontoId);

        // Oppdater status
        var antallIkkeMatchet = kontoutskrift.AntallBevegelser - antallMatchet;
        if (antallIkkeMatchet == 0)
            kontoutskrift.Status = KontoutskriftStatus.Ferdig;
        else if (antallMatchet > 0)
            kontoutskrift.Status = KontoutskriftStatus.DelvisBehandlet;

        await _repo.LagreEndringerAsync();

        return new ImportKontoutskriftResultat(
            kontoutskrift.Id,
            meldingsId,
            periodeFra,
            periodeTil,
            inngaende,
            utgaende,
            kontoutskrift.AntallBevegelser,
            antallMatchet,
            antallIkkeMatchet
        );
    }

    private static XNamespace DetekterNamespace(XDocument doc)
    {
        var root = doc.Root;
        if (root == null) throw new ImportUgyldigXmlException("Tomt XML-dokument.");

        var ns = root.GetDefaultNamespace();
        if (ns != XNamespace.None) return ns;

        // Fallback: prov kjente CAMT.053 namespaces
        var knownNs = new[]
        {
            "urn:iso:std:iso:20022:tech:xsd:camt.053.001.02",
            "urn:iso:std:iso:20022:tech:xsd:camt.053.001.08",
            "urn:iso:std:iso:20022:tech:xsd:camt.053.001.06"
        };

        foreach (var testNs in knownNs)
        {
            if (doc.Descendants(XNamespace.Get(testNs) + "GrpHdr").Any())
                return XNamespace.Get(testNs);
        }

        // Ingen namespace
        return XNamespace.None;
    }

    private static void ValiderKontoMatch(XElement stmt, XNamespace ns, Bankkonto bankkonto)
    {
        var ibanElement = stmt.Descendants(ns + "IBAN").FirstOrDefault();
        if (ibanElement != null && bankkonto.Iban != null)
        {
            var filIban = ibanElement.Value.Replace(" ", "");
            var kontoIban = bankkonto.Iban.Replace(" ", "");
            if (!filIban.Equals(kontoIban, StringComparison.OrdinalIgnoreCase))
                throw new ImportFeilKontoException();
        }
        // TODO: Avklar med arkitekt - skal vi ogsaa matche paa norsk kontonummer?
    }

    private static (decimal inngaende, decimal utgaende) ParseSaldoer(XElement stmt, XNamespace ns)
    {
        decimal inngaende = 0m, utgaende = 0m;

        foreach (var bal in stmt.Elements(ns + "Bal"))
        {
            var tpCd = bal.Element(ns + "Tp")?.Element(ns + "CdOrPrtry")?.Element(ns + "Cd")?.Value;
            var amtEl = bal.Element(ns + "Amt");
            if (amtEl == null) continue;

            var amt = decimal.Parse(amtEl.Value, System.Globalization.CultureInfo.InvariantCulture);
            var cdtDbt = bal.Element(ns + "CdtDbtInd")?.Value;
            if (cdtDbt == "DBIT") amt = -amt;

            if (tpCd == "OPBD") inngaende = amt;
            else if (tpCd == "CLBD") utgaende = amt;
        }

        return (inngaende, utgaende);
    }

    private static Bankbevegelse ParseNtry(XElement ntry, XNamespace ns, Guid kontoutskriftId, Guid bankkontoId)
    {
        var cdtDbtInd = ntry.Element(ns + "CdtDbtInd")?.Value ?? "CRDT";
        var retning = cdtDbtInd == "DBIT" ? BankbevegelseRetning.Ut : BankbevegelseRetning.Inn;

        var amtEl = ntry.Element(ns + "Amt");
        var belop = amtEl != null
            ? decimal.Parse(amtEl.Value, System.Globalization.CultureInfo.InvariantCulture)
            : 0m;
        var valuta = amtEl?.Attribute("Ccy")?.Value ?? "NOK";

        var bookgDt = ntry.Element(ns + "BookgDt")?.Element(ns + "Dt")?.Value;
        var valDt = ntry.Element(ns + "ValDt")?.Element(ns + "Dt")?.Value;

        // Transaction details
        var txDtls = ntry.Descendants(ns + "TxDtls").FirstOrDefault();
        var kid = txDtls?.Descendants(ns + "CdtrRefInf").FirstOrDefault()
            ?.Element(ns + "Ref")?.Value;
        var endToEndId = txDtls?.Descendants(ns + "EndToEndId").FirstOrDefault()?.Value;

        // Motpart
        string? motpart = null;
        if (retning == BankbevegelseRetning.Inn)
            motpart = txDtls?.Descendants(ns + "Dbtr").FirstOrDefault()?.Element(ns + "Nm")?.Value;
        else
            motpart = txDtls?.Descendants(ns + "Cdtr").FirstOrDefault()?.Element(ns + "Nm")?.Value;

        var motpartKonto = txDtls?.Descendants(ns + "IBAN").FirstOrDefault()?.Value;
        var bankRef = txDtls?.Descendants(ns + "MsgId").FirstOrDefault()?.Value;
        var beskrivelse = ntry.Element(ns + "AddtlNtryInf")?.Value
            ?? txDtls?.Element(ns + "AddtlTxInf")?.Value;

        return new Bankbevegelse
        {
            Id = Guid.NewGuid(),
            KontoutskriftId = kontoutskriftId,
            BankkontoId = bankkontoId,
            Retning = retning,
            Belop = new Belop(belop),
            Valutakode = valuta,
            Bokforingsdato = bookgDt != null ? DateOnly.Parse(bookgDt) : DateOnly.FromDateTime(DateTime.UtcNow),
            Valuteringsdato = valDt != null ? DateOnly.Parse(valDt) : null,
            KidNummer = kid,
            EndToEndId = endToEndId,
            Motpart = motpart,
            MotpartKonto = motpartKonto,
            BankReferanse = bankRef,
            Beskrivelse = beskrivelse,
            Status = BankbevegelseStatus.IkkeMatchet
        };
    }
}
