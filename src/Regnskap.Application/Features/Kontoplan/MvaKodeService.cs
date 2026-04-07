using Regnskap.Domain.Features.Kontoplan;

namespace Regnskap.Application.Features.Kontoplan;

public class MvaKodeService : IMvaKodeService
{
    private readonly IKontoplanRepository _repository;

    public MvaKodeService(IKontoplanRepository repository)
    {
        _repository = repository;
    }

    public async Task<MvaKode?> HentMvaKodeAsync(string kode, CancellationToken ct = default)
    {
        return await _repository.HentMvaKodeAsync(kode, ct);
    }

    public async Task<MvaKode> HentMvaKodeEllerKastAsync(string kode, CancellationToken ct = default)
    {
        return await _repository.HentMvaKodeAsync(kode, ct)
            ?? throw new MvaKodeIkkeFunnetException(kode);
    }

    public async Task<IReadOnlyList<MvaKode>> HentAlleMvaKoderAsync(bool? erAktiv = null, MvaRetning? retning = null, CancellationToken ct = default)
    {
        return await _repository.HentAlleMvaKoderAsync(erAktiv, retning, ct);
    }

    public async Task<string?> HentStandardMvaKodeForKontoAsync(string kontonummer, CancellationToken ct = default)
    {
        var konto = await _repository.HentKontoAsync(kontonummer, ct);
        return konto?.StandardMvaKode;
    }

    public async Task<MvaKode> OpprettMvaKodeAsync(OpprettMvaKodeRequest request, CancellationToken ct = default)
    {
        if (await _repository.MvaKodeFinnesAsync(request.Kode, ct))
            throw new ArgumentException($"MVA-kode {request.Kode} er allerede i bruk.");

        Guid? utgaendeKontoId = null;
        if (!string.IsNullOrEmpty(request.UtgaendeKontonummer))
        {
            var konto = await _repository.HentKontoAsync(request.UtgaendeKontonummer, ct)
                ?? throw new KontoIkkeFunnetException(request.UtgaendeKontonummer);
            utgaendeKontoId = konto.Id;
        }

        Guid? inngaendeKontoId = null;
        if (!string.IsNullOrEmpty(request.InngaendeKontonummer))
        {
            var konto = await _repository.HentKontoAsync(request.InngaendeKontonummer, ct)
                ?? throw new KontoIkkeFunnetException(request.InngaendeKontonummer);
            inngaendeKontoId = konto.Id;
        }

        // FR-17: MVA-kontokoblinger
        ValidateMvaKontokoblinger(request.Retning, utgaendeKontoId, inngaendeKontoId, request.Sats);

        var mvaKode = new MvaKode
        {
            Id = Guid.NewGuid(),
            Kode = request.Kode,
            Beskrivelse = request.Beskrivelse,
            BeskrivelseEn = request.BeskrivelseEn,
            StandardTaxCode = request.StandardTaxCode,
            Sats = request.Sats,
            Retning = request.Retning,
            UtgaendeKontoId = utgaendeKontoId,
            InngaendeKontoId = inngaendeKontoId,
            ErAktiv = true,
            ErSystemkode = false
        };

        await _repository.LeggTilMvaKodeAsync(mvaKode, ct);
        await _repository.LagreEndringerAsync(ct);
        return mvaKode;
    }

    public async Task<MvaKode> OppdaterMvaKodeAsync(string kode, OppdaterMvaKodeRequest request, CancellationToken ct = default)
    {
        var mvaKode = await _repository.HentMvaKodeAsync(kode, ct)
            ?? throw new MvaKodeIkkeFunnetException(kode);

        Guid? utgaendeKontoId = mvaKode.UtgaendeKontoId;
        if (request.UtgaendeKontonummer is not null)
        {
            if (string.IsNullOrEmpty(request.UtgaendeKontonummer))
            {
                utgaendeKontoId = null;
            }
            else
            {
                var kontoUtg = await _repository.HentKontoAsync(request.UtgaendeKontonummer, ct)
                    ?? throw new KontoIkkeFunnetException(request.UtgaendeKontonummer);
                utgaendeKontoId = kontoUtg.Id;
            }
        }

        Guid? inngaendeKontoId = mvaKode.InngaendeKontoId;
        if (request.InngaendeKontonummer is not null)
        {
            if (string.IsNullOrEmpty(request.InngaendeKontonummer))
            {
                inngaendeKontoId = null;
            }
            else
            {
                var kontoInn = await _repository.HentKontoAsync(request.InngaendeKontonummer, ct)
                    ?? throw new KontoIkkeFunnetException(request.InngaendeKontonummer);
                inngaendeKontoId = kontoInn.Id;
            }
        }

        mvaKode.Beskrivelse = request.Beskrivelse;
        mvaKode.BeskrivelseEn = request.BeskrivelseEn;
        mvaKode.Sats = request.Sats;
        mvaKode.ErAktiv = request.ErAktiv;
        mvaKode.UtgaendeKontoId = utgaendeKontoId;
        mvaKode.InngaendeKontoId = inngaendeKontoId;

        await _repository.LagreEndringerAsync(ct);
        return mvaKode;
    }

    private static void ValidateMvaKontokoblinger(MvaRetning retning, Guid? utgaendeKontoId, Guid? inngaendeKontoId, decimal sats = -1)
    {
        // FR-17: MVA-kontokoblinger
        // Unntak for koder med sats 0% (f.eks. kode 5 eksport 0%, kode 6 fritatt 0%)
        var erNullsats = sats == 0m;

        switch (retning)
        {
            case MvaRetning.Utgaende when utgaendeKontoId is null && !erNullsats:
                throw new ArgumentException("Utgaende MVA-koder med sats > 0% ma ha UtgaendeKontoId satt.");
            case MvaRetning.Inngaende when inngaendeKontoId is null && !erNullsats:
                throw new ArgumentException("Inngaende MVA-koder med sats > 0% ma ha InngaendeKontoId satt.");
            case MvaRetning.SnuddAvregning when utgaendeKontoId is null || inngaendeKontoId is null:
                throw new ArgumentException("Reverse charge MVA-koder ma ha bade utgaende og inngaende konto.");
        }
    }
}
