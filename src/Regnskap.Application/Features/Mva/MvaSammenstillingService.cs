namespace Regnskap.Application.Features.Mva;

using Regnskap.Domain.Features.Kontoplan;
using Regnskap.Domain.Features.Mva;

/// <summary>
/// MVA-konto sammenstilling: oversikt over alle MVA-posteringer per kode og periode.
/// Lovgrunnlag: Bokforingsloven §5 nr. 5 (MVA-spesifikasjon).
/// </summary>
public class MvaSammenstillingService : IMvaSammenstillingService
{
    private readonly IMvaRepository _repo;
    private readonly IKontoplanRepository _kontoplanRepo;

    public MvaSammenstillingService(IMvaRepository repo, IKontoplanRepository kontoplanRepo)
    {
        _repo = repo;
        _kontoplanRepo = kontoplanRepo;
    }

    public async Task<MvaSammenstillingDto> HentSammenstillingAsync(int ar, int termin, CancellationToken ct = default)
    {
        var mvaTermin = await _repo.HentTerminAsync(ar, termin, ct)
            ?? throw new MvaTerminIkkeFunnetException(Guid.Empty);

        var aggregeringer = await _repo.HentMvaAggregertForPeriodeAsync(
            mvaTermin.FraDato, mvaTermin.TilDato, ct);

        var mvaKoder = await _kontoplanRepo.HentAlleMvaKoderAsync(erAktiv: true, ct: ct);
        var kodeMap = mvaKoder.ToDictionary(k => k.Kode);

        var grupper = aggregeringer.Select(agg =>
        {
            var beskrivelse = kodeMap.TryGetValue(agg.MvaKode, out var mk)
                ? mk.Beskrivelse
                : agg.MvaKode;

            return new MvaSammenstillingGruppeDto(
                agg.MvaKode,
                beskrivelse,
                agg.StandardTaxCode,
                agg.Sats,
                agg.Retning.ToString(),
                agg.SumGrunnlag,
                agg.SumMvaBelop,
                agg.AntallPosteringer
            );
        }).ToList();

        return new MvaSammenstillingDto(
            ar, termin,
            mvaTermin.FraDato, mvaTermin.TilDato,
            grupper,
            grupper.Sum(g => g.SumGrunnlag),
            grupper.Sum(g => g.SumMvaBelop)
        );
    }

    public async Task<MvaSammenstillingDetaljDto> HentSammenstillingDetaljAsync(
        int ar, int termin, string mvaKode, CancellationToken ct = default)
    {
        var mvaTermin = await _repo.HentTerminAsync(ar, termin, ct)
            ?? throw new MvaTerminIkkeFunnetException(Guid.Empty);

        var posteringer = await _repo.HentMvaPosteringerForPeriodeAsync(
            mvaTermin.FraDato, mvaTermin.TilDato, mvaKode, ct);

        var mk = await _kontoplanRepo.HentMvaKodeAsync(mvaKode, ct);
        var beskrivelse = mk?.Beskrivelse ?? mvaKode;

        var posteringDtos = posteringer.Select(p => new MvaPosteringDetaljDto(
            p.PosteringId,
            p.BilagId,
            p.Bilagsnummer,
            p.Bilagsdato,
            p.Kontonummer,
            p.Beskrivelse,
            p.Side.ToString(),
            p.Belop,
            p.MvaGrunnlag,
            p.MvaBelop,
            p.MvaSats,
            p.ErAutoGenerertMva
        )).ToList();

        return new MvaSammenstillingDetaljDto(
            mvaKode,
            beskrivelse,
            posteringDtos,
            posteringDtos.Sum(p => p.MvaGrunnlag),
            posteringDtos.Sum(p => p.MvaBelop),
            posteringDtos.Count
        );
    }
}
