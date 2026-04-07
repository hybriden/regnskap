namespace Regnskap.Application.Features.Fakturering;

using Regnskap.Application.Features.Bilagsregistrering;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Fakturering;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Kontoplan;
using Regnskap.Domain.Features.Kundereskontro;

/// <summary>
/// Implementerer IFaktureringService med full faktureringsflyt.
/// </summary>
public class FaktureringService : IFaktureringService
{
    private readonly IFakturaRepository _fakturaRepo;
    private readonly IHovedbokRepository _hovedbokRepo;
    private readonly IKundeReskontroRepository _kundeRepo;
    private readonly IBilagRegistreringService _bilagService;

    public FaktureringService(
        IFakturaRepository fakturaRepo,
        IHovedbokRepository hovedbokRepo,
        IKundeReskontroRepository kundeRepo,
        IBilagRegistreringService bilagService)
    {
        _fakturaRepo = fakturaRepo;
        _hovedbokRepo = hovedbokRepo;
        _kundeRepo = kundeRepo;
        _bilagService = bilagService;
    }

    public async Task<Faktura> OpprettFakturaAsync(OpprettFakturaRequest request, CancellationToken ct = default)
    {
        if (request.Linjer == null || request.Linjer.Count == 0)
            throw new FakturaIngenLinjerException();

        var kunde = await _kundeRepo.HentKundeAsync(request.KundeId, ct)
            ?? throw new FakturaKundeIkkeFunnetException(request.KundeId);

        if (kunde.ErSperret)
            throw new FakturaKundeSperretException(kunde.Kundenummer);

        var faktura = new Faktura
        {
            Id = Guid.NewGuid(),
            KundeId = request.KundeId,
            Dokumenttype = FakturaDokumenttype.Faktura,
            Status = FakturaStatus.Utkast,
            Leveringsdato = request.Leveringsdato,
            LeveringsperiodeSlutt = request.LeveringsperiodeSlutt,
            Bestillingsnummer = request.Bestillingsnummer,
            KjopersReferanse = request.KjopersReferanse,
            VaarReferanse = request.VaarReferanse,
            EksternReferanse = request.EksternReferanse,
            Merknad = request.Merknad,
            Valutakode = request.Valutakode,
            Leveringsformat = request.Leveringsformat
        };

        var linjenummer = 1;
        foreach (var linjeReq in request.Linjer)
        {
            var linje = OpprettLinje(faktura.Id, linjenummer++, linjeReq);
            FakturaBeregning.BeregnLinje(linje);
            faktura.Linjer.Add(linje);
        }

        FakturaBeregning.BeregnTotaler(faktura);

        var mvaLinjer = FakturaBeregning.BeregnMvaLinjer(faktura);
        foreach (var ml in mvaLinjer)
            faktura.MvaLinjer.Add(ml);

        await _fakturaRepo.LeggTilAsync(faktura, ct);
        await _fakturaRepo.LagreEndringerAsync(ct);

        return faktura;
    }

    public async Task<Faktura> OppdaterFakturaAsync(Guid fakturaId, OpprettFakturaRequest request, CancellationToken ct = default)
    {
        var faktura = await _fakturaRepo.HentMedLinjerAsync(fakturaId, ct)
            ?? throw new FakturaKundeIkkeFunnetException(fakturaId);

        if (faktura.Status != FakturaStatus.Utkast)
            throw new FakturaIkkeUtkastException();

        if (request.Linjer == null || request.Linjer.Count == 0)
            throw new FakturaIngenLinjerException();

        // Oppdater felter
        faktura.Leveringsdato = request.Leveringsdato;
        faktura.LeveringsperiodeSlutt = request.LeveringsperiodeSlutt;
        faktura.Bestillingsnummer = request.Bestillingsnummer;
        faktura.KjopersReferanse = request.KjopersReferanse;
        faktura.VaarReferanse = request.VaarReferanse;
        faktura.EksternReferanse = request.EksternReferanse;
        faktura.Merknad = request.Merknad;
        faktura.Valutakode = request.Valutakode;
        faktura.Leveringsformat = request.Leveringsformat;

        // Erstatt linjer
        faktura.Linjer.Clear();
        faktura.MvaLinjer.Clear();

        var linjenummer = 1;
        foreach (var linjeReq in request.Linjer)
        {
            var linje = OpprettLinje(faktura.Id, linjenummer++, linjeReq);
            FakturaBeregning.BeregnLinje(linje);
            faktura.Linjer.Add(linje);
        }

        FakturaBeregning.BeregnTotaler(faktura);

        var mvaLinjer = FakturaBeregning.BeregnMvaLinjer(faktura);
        foreach (var ml in mvaLinjer)
            faktura.MvaLinjer.Add(ml);

        await _fakturaRepo.LagreEndringerAsync(ct);
        return faktura;
    }

    public async Task<Faktura> UtstedeFakturaAsync(Guid fakturaId, CancellationToken ct = default)
    {
        var faktura = await _fakturaRepo.HentMedLinjerAsync(fakturaId, ct)
            ?? throw new FakturaKundeIkkeFunnetException(fakturaId);

        if (faktura.Status != FakturaStatus.Utkast)
            throw new FakturaIkkeUtkastException();

        var kunde = await _kundeRepo.HentKundeAsync(faktura.KundeId, ct)
            ?? throw new FakturaKundeIkkeFunnetException(faktura.KundeId);

        var selskapsinfo = await _fakturaRepo.HentSelskapsinfoAsync(ct);

        // FR-F10 steg 2: Valider at perioden er aapen
        var fakturadato = DateOnly.FromDateTime(DateTime.Today);
        var periode = await _hovedbokRepo.HentPeriodeForDatoAsync(fakturadato, ct);
        if (periode != null && !periode.ErApen)
            throw new FakturaPeriodeLukketException();

        // Steg 3: Sett fakturadato
        faktura.Fakturadato = fakturadato;

        // Steg 4: Beregn forfallsdato (FR-F04)
        faktura.Forfallsdato = FakturaBeregning.BeregnForfallsdato(
            fakturadato, kunde.Betalingsbetingelse, kunde.EgendefinertBetalingsfrist);

        // Steg 5: Tildel fakturanummer (FR-F01)
        var ar = fakturadato.Year;
        faktura.FakturanummerAr = ar;
        faktura.Fakturanummer = await _fakturaRepo.NesteNummerAsync(ar, faktura.Dokumenttype, ct);

        // Steg 6: Beregn alle belop (sikre at de er oppdatert)
        foreach (var linje in faktura.Linjer)
            FakturaBeregning.BeregnLinje(linje);
        FakturaBeregning.BeregnTotaler(faktura);

        // Steg 7: Generer KID (FR-F05)
        var kidAlgoritme = kunde.KidAlgoritme ?? selskapsinfo?.StandardKidAlgoritme ?? KidAlgoritme.MOD10;
        try
        {
            faktura.KidNummer = KidGenerator.Generer(
                kunde.Kundenummer, faktura.Fakturanummer.Value, kidAlgoritme);
        }
        catch (InvalidOperationException) when (kidAlgoritme == KidAlgoritme.MOD11)
        {
            // MOD11 fallback til MOD10
            faktura.KidNummer = KidGenerator.Generer(
                kunde.Kundenummer, faktura.Fakturanummer.Value, KidAlgoritme.MOD10);
        }

        // Steg 8: Sett betalingsinfo
        if (selskapsinfo != null)
        {
            faktura.Bankkontonummer = selskapsinfo.Bankkontonummer;
            faktura.Iban = selskapsinfo.Iban;
            faktura.Bic = selskapsinfo.Bic;
        }

        // Steg 9-10: Opprett bilag med posteringer for dobbelt bokholderi (FR-F06)
        var posteringer = ByggFakturaBilagPosteringer(faktura);
        var bilagType = faktura.Dokumenttype == FakturaDokumenttype.Kreditnota
            ? BilagType.Kreditnota
            : BilagType.UtgaendeFaktura;

        var bilagRequest = new OpprettBilagRequest(
            bilagType,
            fakturadato,
            $"Faktura {faktura.FakturaId} - {kunde.Kundenummer}",
            faktura.FakturaId,
            "UF", // Utgaende Faktura serie
            posteringer,
            BokforDirekte: true);

        var bilag = await _bilagService.OpprettOgBokforBilagAsync(bilagRequest, ct);
        faktura.BilagId = bilag.Id;

        // Opprett i kundereskontro
        var erKreditnota = faktura.Dokumenttype == FakturaDokumenttype.Kreditnota;
        var kundeFaktura = new KundeFaktura
        {
            Id = Guid.NewGuid(),
            KundeId = faktura.KundeId,
            Fakturanummer = faktura.Fakturanummer!.Value,
            Type = erKreditnota ? KundeTransaksjonstype.Kreditnota : KundeTransaksjonstype.Faktura,
            Fakturadato = fakturadato,
            Forfallsdato = faktura.Forfallsdato!.Value,
            Leveringsdato = faktura.Leveringsdato,
            Beskrivelse = $"{(erKreditnota ? "Kreditnota" : "Faktura")} {faktura.FakturaId}",
            BelopEksMva = faktura.BelopEksMva,
            MvaBelop = faktura.MvaBelop,
            BelopInklMva = faktura.BelopInklMva,
            GjenstaendeBelop = erKreditnota ? new Belop(-faktura.BelopInklMva.Verdi) : faktura.BelopInklMva,
            Status = KundeFakturaStatus.Utstedt,
            KidNummer = faktura.KidNummer,
            Valutakode = faktura.Valutakode,
            BilagId = bilag.Id,
            EksternReferanse = faktura.EksternReferanse,
            Bestillingsnummer = faktura.Bestillingsnummer
        };
        await _kundeRepo.LeggTilFakturaAsync(kundeFaktura, ct);
        faktura.KundeFakturaId = kundeFaktura.Id;

        // Steg 11: Sett status
        faktura.Status = FakturaStatus.Utstedt;

        // Reberegn MVA-linjer
        faktura.MvaLinjer.Clear();
        var mvaLinjer = FakturaBeregning.BeregnMvaLinjer(faktura);
        foreach (var ml in mvaLinjer)
            faktura.MvaLinjer.Add(ml);

        await _fakturaRepo.LagreEndringerAsync(ct);
        return faktura;
    }

    public async Task<Faktura> OpprettKreditnotaAsync(Guid originalFakturaId, OpprettKreditnotaRequest request, CancellationToken ct = default)
    {
        var original = await _fakturaRepo.HentMedLinjerAsync(originalFakturaId, ct)
            ?? throw new KreditnotaOriginalIkkeFunnetException(originalFakturaId);

        if (original.Status != FakturaStatus.Utstedt)
            throw new KreditnotaOriginalIkkeFunnetException(originalFakturaId);

        if (string.IsNullOrWhiteSpace(request.Krediteringsaarsak))
            throw new FakturaException("KREDITNOTA_MANGLER_AARSAK", "Krediteringsaarsak er obligatorisk.");

        var kunde = await _kundeRepo.HentKundeAsync(original.KundeId, ct)
            ?? throw new FakturaKundeIkkeFunnetException(original.KundeId);

        // Sjekk at allerede kreditert belop + ny kreditering ikke overstiger original
        var alleredeKreditert = original.Kreditnotaer
            .Where(k => k.Status == FakturaStatus.Utstedt)
            .Sum(k => k.BelopInklMva.Verdi);

        var kreditnota = new Faktura
        {
            Id = Guid.NewGuid(),
            KundeId = original.KundeId,
            Dokumenttype = FakturaDokumenttype.Kreditnota,
            Status = FakturaStatus.Utkast,
            KreditertFakturaId = originalFakturaId,
            Krediteringsaarsak = request.Krediteringsaarsak,
            KjopersReferanse = request.KjopersReferanse ?? original.KjopersReferanse,
            Bestillingsnummer = original.Bestillingsnummer,
            VaarReferanse = original.VaarReferanse,
            Valutakode = original.Valutakode,
            Leveringsformat = original.Leveringsformat
        };

        if (request.Linjer == null || request.Linjer.Count == 0)
        {
            // Full kreditering
            var linjenummer = 1;
            foreach (var origLinje in original.Linjer.OrderBy(l => l.Linjenummer))
            {
                var linje = new FakturaLinje
                {
                    Id = Guid.NewGuid(),
                    FakturaId = kreditnota.Id,
                    Linjenummer = linjenummer++,
                    Beskrivelse = origLinje.Beskrivelse,
                    Antall = origLinje.Antall,
                    Enhet = origLinje.Enhet,
                    Enhetspris = origLinje.Enhetspris,
                    MvaKode = origLinje.MvaKode,
                    MvaSats = origLinje.MvaSats,
                    KontoId = origLinje.KontoId,
                    Kontonummer = origLinje.Kontonummer,
                    Avdelingskode = origLinje.Avdelingskode,
                    Prosjektkode = origLinje.Prosjektkode,
                    RabattType = origLinje.RabattType,
                    RabattProsent = origLinje.RabattProsent,
                    RabattBelop = origLinje.RabattBelop
                };
                FakturaBeregning.BeregnLinje(linje);
                kreditnota.Linjer.Add(linje);
            }
        }
        else
        {
            // Delvis kreditering
            var linjenummer = 1;
            foreach (var kreditLinje in request.Linjer)
            {
                var origLinje = original.Linjer.FirstOrDefault(l => l.Linjenummer == kreditLinje.OpprinneligLinjenummer)
                    ?? throw new FakturaException("KREDITNOTA_UGYLDIG_LINJE",
                        $"Linje {kreditLinje.OpprinneligLinjenummer} finnes ikke paa original faktura.");

                if (kreditLinje.Antall > origLinje.Antall)
                    throw new KreditnotaBelopOverskriderException();

                var linje = new FakturaLinje
                {
                    Id = Guid.NewGuid(),
                    FakturaId = kreditnota.Id,
                    Linjenummer = linjenummer++,
                    Beskrivelse = origLinje.Beskrivelse,
                    Antall = kreditLinje.Antall,
                    Enhet = origLinje.Enhet,
                    Enhetspris = origLinje.Enhetspris,
                    MvaKode = origLinje.MvaKode,
                    MvaSats = origLinje.MvaSats,
                    KontoId = origLinje.KontoId,
                    Kontonummer = origLinje.Kontonummer,
                    Avdelingskode = origLinje.Avdelingskode,
                    Prosjektkode = origLinje.Prosjektkode
                };
                FakturaBeregning.BeregnLinje(linje);
                kreditnota.Linjer.Add(linje);
            }
        }

        FakturaBeregning.BeregnTotaler(kreditnota);

        // Valider at total kreditert belop ikke overstiger original
        if (alleredeKreditert + kreditnota.BelopInklMva.Verdi > original.BelopInklMva.Verdi)
            throw new KreditnotaBelopOverskriderException();

        var mvaLinjer = FakturaBeregning.BeregnMvaLinjer(kreditnota);
        foreach (var ml in mvaLinjer)
            kreditnota.MvaLinjer.Add(ml);

        await _fakturaRepo.LeggTilAsync(kreditnota, ct);
        await _fakturaRepo.LagreEndringerAsync(ct);

        return kreditnota;
    }

    public async Task KansellerFakturaAsync(Guid fakturaId, CancellationToken ct = default)
    {
        var faktura = await _fakturaRepo.HentAsync(fakturaId, ct)
            ?? throw new FakturaKundeIkkeFunnetException(fakturaId);

        if (faktura.Status != FakturaStatus.Utkast)
            throw new FakturaIkkeUtkastException();

        faktura.Status = FakturaStatus.Kansellert;
        faktura.IsDeleted = true;

        await _fakturaRepo.LagreEndringerAsync(ct);
    }

    public async Task<Faktura?> HentFakturaAsync(Guid fakturaId, CancellationToken ct = default)
    {
        return await _fakturaRepo.HentMedLinjerAsync(fakturaId, ct);
    }

    public async Task<FakturaSokResultat> SokFakturaerAsync(FakturaSokFilter filter, CancellationToken ct = default)
    {
        var fakturaer = await _fakturaRepo.SokAsync(filter, ct);
        var totalt = await _fakturaRepo.TellAsync(filter, ct);
        return new FakturaSokResultat(fakturaer, totalt, filter.Side, filter.Antall);
    }

    private static FakturaLinje OpprettLinje(Guid fakturaId, int linjenummer, FakturaLinjeRequest req)
    {
        return new FakturaLinje
        {
            Id = Guid.NewGuid(),
            FakturaId = fakturaId,
            Linjenummer = linjenummer,
            Beskrivelse = req.Beskrivelse,
            Antall = req.Antall,
            Enhet = req.Enhet,
            Enhetspris = new Belop(req.Enhetspris),
            MvaKode = req.MvaKode,
            MvaSats = req.MvaSats,
            KontoId = req.KontoId,
            Kontonummer = req.Kontonummer,
            RabattType = req.RabattType,
            RabattProsent = req.RabattProsent,
            RabattBelop = req.RabattBelop.HasValue ? new Belop(req.RabattBelop.Value) : null,
            Avdelingskode = req.Avdelingskode,
            Prosjektkode = req.Prosjektkode
        };
    }

    /// <summary>
    /// FR-F06: Bygg bilag posteringer for utgaende faktura/kreditnota.
    /// Faktura: Debet 1500 Kundefordringer, Kredit 3xxx Salgsinntekt, Kredit 2700 Utg. MVA
    /// Kreditnota: Speilvendt (debet/kredit byttet).
    /// </summary>
    internal static List<OpprettPosteringRequest> ByggFakturaBilagPosteringer(Faktura faktura)
    {
        var posteringer = new List<OpprettPosteringRequest>();
        var erKreditnota = faktura.Dokumenttype == FakturaDokumenttype.Kreditnota;

        // Inntektsposteringer per linje (kredit for faktura, debet for kreditnota)
        foreach (var linje in faktura.Linjer)
        {
            posteringer.Add(new OpprettPosteringRequest(
                linje.Kontonummer.Length > 0 ? linje.Kontonummer : "3000",
                erKreditnota ? BokforingSide.Debet : BokforingSide.Kredit,
                linje.Nettobelop.Verdi,
                linje.Beskrivelse,
                linje.MvaKode,
                linje.Avdelingskode,
                linje.Prosjektkode,
                KundeId: null,
                LeverandorId: null));

            // MVA-postering per linje
            if (linje.MvaBelop.Verdi > 0)
            {
                posteringer.Add(new OpprettPosteringRequest(
                    HentMvaKonto(linje.MvaKode),
                    erKreditnota ? BokforingSide.Debet : BokforingSide.Kredit,
                    linje.MvaBelop.Verdi,
                    $"Utg. MVA {linje.MvaSats}%",
                    null,
                    null,
                    null,
                    KundeId: null,
                    LeverandorId: null));
            }
        }

        // Kundefordringer-postering (1500) - debet for faktura, kredit for kreditnota
        posteringer.Add(new OpprettPosteringRequest(
            "1500",
            erKreditnota ? BokforingSide.Kredit : BokforingSide.Debet,
            faktura.BelopInklMva.Verdi,
            $"Kundefordringer - {faktura.FakturaId}",
            null,
            null,
            null,
            KundeId: null,
            LeverandorId: null));

        return posteringer;
    }

    /// <summary>
    /// Hent MVA-konto basert pa MvaKode for utgaende MVA.
    /// </summary>
    private static string HentMvaKonto(string? mvaKode)
    {
        return mvaKode switch
        {
            "3" => "2700",  // Utg. MVA 25%
            "31" => "2710", // Utg. MVA 15%
            "33" => "2710", // Utg. MVA 12%
            _ => "2700"     // Fallback
        };
    }
}
