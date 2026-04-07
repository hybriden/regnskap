using Regnskap.Domain.Features.Kontoplan;

namespace Regnskap.Application.Features.Kontoplan;

public class KontoService : IKontoService
{
    private readonly IKontoplanRepository _repository;

    public KontoService(IKontoplanRepository repository)
    {
        _repository = repository;
    }

    public async Task<Konto?> HentKontoAsync(string kontonummer, CancellationToken ct = default)
    {
        return await _repository.HentKontoAsync(kontonummer, ct);
    }

    public async Task<bool> KontoFinnesOgErAktivAsync(string kontonummer, CancellationToken ct = default)
    {
        var konto = await _repository.HentKontoAsync(kontonummer, ct);
        return konto is { ErAktiv: true };
    }

    public async Task<Konto> HentKontoEllerKastAsync(string kontonummer, CancellationToken ct = default)
    {
        var konto = await _repository.HentKontoAsync(kontonummer, ct);
        if (konto is null)
            throw new KontoIkkeFunnetException(kontonummer);
        if (!konto.ErAktiv)
            throw new KontoInaktivException(kontonummer);
        return konto;
    }

    public async Task<IReadOnlyList<Konto>> HentKontoerForGruppeAsync(int gruppekode, CancellationToken ct = default)
    {
        return await _repository.HentKontoerAsync(gruppekode: gruppekode, ct: ct);
    }

    public async Task<Kontotype> HentKontotypeAsync(string kontonummer, CancellationToken ct = default)
    {
        var konto = await HentKontoEllerKastAsync(kontonummer, ct);
        return konto.Kontotype;
    }

    public async Task<Normalbalanse> HentNormalbalanseAsync(string kontonummer, CancellationToken ct = default)
    {
        var konto = await HentKontoEllerKastAsync(kontonummer, ct);
        return konto.Normalbalanse;
    }

    public async Task<Konto> OpprettKontoAsync(OpprettKontoRequest request, CancellationToken ct = default)
    {
        // FR-1: Kontonummerformat - 4-6 siffer, forste siffer 1-8
        ValiderKontonummerFormat(request.Kontonummer);

        // Sjekk at kontonummer er unikt
        if (await _repository.KontoFinnesAsync(request.Kontonummer, ct))
            throw new ArgumentException($"Kontonummer {request.Kontonummer} er allerede i bruk.");

        // FR-3: Kontonummer-gruppe-konsistens
        var forventedGruppe = int.Parse(request.Kontonummer[..2]);
        if (request.Gruppekode != forventedGruppe)
            throw new ArgumentException($"Kontonummer {request.Kontonummer} tilhorer ikke gruppe {request.Gruppekode}.");

        // Sjekk at kontogruppen eksisterer
        var gruppe = await _repository.HentKontogruppeAsync(request.Gruppekode, ct)
            ?? throw new ArgumentException($"Kontogruppe {request.Gruppekode} finnes ikke.");

        // FR-11: Kontoklasse-til-kontotype-konsistens
        ValiderKontotypeForKontoklasse(request.Kontonummer, request.Kontotype);

        // FR-10: Kontotype-til-normalbalanse-mapping
        var normalbalanse = BestemNormalbalanse(request.Kontotype);

        // FR-14: StandardAccountID ma matche kontoklasse
        if (request.StandardAccountId.Length >= 1 && request.StandardAccountId[0] != request.Kontonummer[0])
            throw new ArgumentException($"StandardAccountId {request.StandardAccountId} matcher ikke kontoklassen for konto {request.Kontonummer}.");

        // FR-4: Underkonto-prefix
        Guid? overordnetKontoId = null;
        if (!string.IsNullOrEmpty(request.OverordnetKontonummer))
        {
            var overordnet = await _repository.HentKontoAsync(request.OverordnetKontonummer, ct)
                ?? throw new ArgumentException($"Overordnet konto {request.OverordnetKontonummer} finnes ikke.");

            if (!request.Kontonummer.StartsWith(request.OverordnetKontonummer))
                throw new ArgumentException($"Underkonto ma starte med overordnet kontonummer {request.OverordnetKontonummer}.");

            overordnetKontoId = overordnet.Id;
        }

        // Valider MVA-kode hvis satt
        if (!string.IsNullOrEmpty(request.StandardMvaKode))
        {
            if (!await _repository.MvaKodeFinnesAsync(request.StandardMvaKode, ct))
                throw new ArgumentException($"MVA-kode {request.StandardMvaKode} finnes ikke.");
        }

        var konto = new Konto
        {
            Id = Guid.NewGuid(),
            Kontonummer = request.Kontonummer,
            Navn = request.Navn,
            NavnEn = request.NavnEn,
            Kontotype = request.Kontotype,
            Normalbalanse = normalbalanse,
            KontogruppeId = gruppe.Id,
            StandardAccountId = request.StandardAccountId,
            GrupperingsKategori = request.GrupperingsKategori,
            GrupperingsKode = request.GrupperingsKode,
            ErAktiv = true,
            ErSystemkonto = false,
            ErBokforbar = request.ErBokforbar,
            StandardMvaKode = request.StandardMvaKode,
            Beskrivelse = request.Beskrivelse,
            OverordnetKontoId = overordnetKontoId,
            KreverAvdeling = request.KreverAvdeling,
            KreverProsjekt = request.KreverProsjekt
        };

        await _repository.LeggTilKontoAsync(konto, ct);
        await _repository.LagreEndringerAsync(ct);
        return konto;
    }

    public async Task<Konto> OppdaterKontoAsync(string kontonummer, OppdaterKontoRequest request, CancellationToken ct = default)
    {
        var konto = await _repository.HentKontoAsync(kontonummer, ct)
            ?? throw new KontoIkkeFunnetException(kontonummer);

        // FR-6: Systemkontoer -- beskyttede felter
        // Kontonummer, Kontotype, Kontogruppe, og ErSystemkonto kan ikke endres pa systemkontoer.
        if (konto.ErSystemkonto)
        {
            ValiderSystemkontoEndring(konto, kontonummer, request);
        }

        konto.Navn = request.Navn;
        konto.NavnEn = request.NavnEn;
        konto.ErAktiv = request.ErAktiv;
        konto.ErBokforbar = request.ErBokforbar;
        konto.StandardMvaKode = request.StandardMvaKode;
        konto.Beskrivelse = request.Beskrivelse;
        konto.GrupperingsKategori = request.GrupperingsKategori;
        konto.GrupperingsKode = request.GrupperingsKode;
        konto.KreverAvdeling = request.KreverAvdeling;
        konto.KreverProsjekt = request.KreverProsjekt;

        await _repository.LagreEndringerAsync(ct);
        return konto;
    }

    public async Task SlettKontoAsync(string kontonummer, CancellationToken ct = default)
    {
        var konto = await _repository.HentKontoAsync(kontonummer, ct)
            ?? throw new KontoIkkeFunnetException(kontonummer);

        // FR-5: Systemkontoer kan ikke slettes
        if (konto.ErSystemkonto)
            throw new SystemkontoSlettingException(kontonummer);

        // FR-7: Konto med posteringer kan ikke slettes
        if (await _repository.KontoHarPosteringerAsync(kontonummer, ct))
            throw new KontoHarPosteringerException(kontonummer);

        // FR-8: Konto med aktive underkontoer kan ikke slettes
        if (await _repository.KontoHarAktiveUnderkontoerAsync(kontonummer, ct))
            throw new KontoHarUnderkontoerException(kontonummer);

        konto.IsDeleted = true;
        await _repository.LagreEndringerAsync(ct);
    }

    public async Task DeaktiverKontoAsync(string kontonummer, CancellationToken ct = default)
    {
        var konto = await _repository.HentKontoAsync(kontonummer, ct)
            ?? throw new KontoIkkeFunnetException(kontonummer);

        konto.ErAktiv = false;
        await _repository.LagreEndringerAsync(ct);
    }

    public async Task AktiverKontoAsync(string kontonummer, CancellationToken ct = default)
    {
        var konto = await _repository.HentKontoAsync(kontonummer, ct)
            ?? throw new KontoIkkeFunnetException(kontonummer);

        konto.ErAktiv = true;
        await _repository.LagreEndringerAsync(ct);
    }

    public async Task<(List<Konto> Data, int TotaltAntall)> HentKontoerAsync(
        int? kontoklasse = null, Kontotype? kontotype = null, int? gruppekode = null,
        bool? erAktiv = null, bool? erBokforbar = null, string? sok = null,
        int side = 1, int antall = 50, CancellationToken ct = default)
    {
        antall = Math.Min(antall, 500);
        var data = await _repository.HentKontoerAsync(kontoklasse, kontotype, gruppekode, erAktiv, erBokforbar, sok, side, antall, ct);
        var totalt = await _repository.TellKontoerAsync(kontoklasse, kontotype, gruppekode, erAktiv, erBokforbar, sok, ct);
        return (data, totalt);
    }

    public async Task<Konto?> HentKontoMedDetaljerAsync(string kontonummer, CancellationToken ct = default)
    {
        return await _repository.HentKontoMedDetaljerAsync(kontonummer, ct);
    }

    public async Task<List<Konto>> SokKontoerAsync(string query, int antall = 10, CancellationToken ct = default)
    {
        return await _repository.SokKontoerAsync(query, antall, ct);
    }

    // --- FR-6: Systemkonto-beskyttelse ---

    /// <summary>
    /// FR-6: Systemkontoer har beskyttede felter som ikke kan endres.
    /// Beskyttede felter: Kontonummer, Kontotype, Kontogruppe, ErSystemkonto.
    /// </summary>
    private static void ValiderSystemkontoEndring(Konto konto, string kontonummer, OppdaterKontoRequest request)
    {
        // Kontonummer: Sjekk at URL-parameter matcher eksisterende kontonummer
        if (kontonummer != konto.Kontonummer)
            throw new SystemkontoFeltEndringException(konto.Kontonummer, "Kontonummer");

        // Kontotype: Kan ikke endres pa systemkontoer
        if (request.Kontotype.HasValue && request.Kontotype.Value != konto.Kontotype)
            throw new SystemkontoFeltEndringException(konto.Kontonummer, "Kontotype");

        // Kontogruppe: Kan ikke endres pa systemkontoer
        if (request.Gruppekode.HasValue)
        {
            var forventetGruppe = int.Parse(konto.Kontonummer[..2]);
            if (request.Gruppekode.Value != forventetGruppe)
                throw new SystemkontoFeltEndringException(konto.Kontonummer, "Kontogruppe");
        }

        // ErSystemkonto: Kan ikke fjernes fra systemkontoer
        if (request.ErSystemkonto.HasValue && request.ErSystemkonto.Value != konto.ErSystemkonto)
            throw new SystemkontoFeltEndringException(konto.Kontonummer, "ErSystemkonto");
    }

    // --- Valideringsmetoder ---

    /// <summary>
    /// FR-1: Kontonummer ma vaere 4-6 siffer. Forste siffer 1-8.
    /// </summary>
    public static void ValiderKontonummerFormat(string kontonummer)
    {
        if (string.IsNullOrWhiteSpace(kontonummer))
            throw new ArgumentException("Kontonummer er pakreved.");

        if (kontonummer.Length < 4 || kontonummer.Length > 6)
            throw new ArgumentException("Kontonummer ma vaere 4-6 siffer.");

        if (!kontonummer.All(char.IsDigit))
            throw new ArgumentException("Kontonummer ma kun inneholde siffer.");

        var forsteSiffer = kontonummer[0] - '0';
        if (forsteSiffer < 1 || forsteSiffer > 8)
            throw new ArgumentException("Kontonummer ma starte med 1-8.");
    }

    /// <summary>
    /// FR-11: Kontoklasse-til-kontotype-konsistens.
    /// </summary>
    public static void ValiderKontotypeForKontoklasse(string kontonummer, Kontotype kontotype)
    {
        var kontoklasse = int.Parse(kontonummer[..1]);
        var erGyldig = kontoklasse switch
        {
            1 => kontotype == Kontotype.Eiendel,
            2 => kontotype is Kontotype.Gjeld or Kontotype.Egenkapital,
            3 => kontotype == Kontotype.Inntekt,
            4 or 5 or 6 or 7 => kontotype == Kontotype.Kostnad,
            8 => kontotype is Kontotype.Inntekt or Kontotype.Kostnad,
            _ => false
        };

        if (!erGyldig)
            throw new ArgumentException($"Kontotype {kontotype} er ikke gyldig for kontoklasse {kontoklasse}.");
    }

    /// <summary>
    /// FR-10: Kontotype-til-normalbalanse-mapping.
    /// </summary>
    public static Normalbalanse BestemNormalbalanse(Kontotype kontotype) => kontotype switch
    {
        Kontotype.Eiendel => Normalbalanse.Debet,
        Kontotype.Gjeld => Normalbalanse.Kredit,
        Kontotype.Egenkapital => Normalbalanse.Kredit,
        Kontotype.Inntekt => Normalbalanse.Kredit,
        Kontotype.Kostnad => Normalbalanse.Debet,
        _ => throw new ArgumentOutOfRangeException(nameof(kontotype))
    };
}
