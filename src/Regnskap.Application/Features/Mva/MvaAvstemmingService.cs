namespace Regnskap.Application.Features.Mva;

using Regnskap.Domain.Features.Mva;

public class MvaAvstemmingService : IMvaAvstemmingService
{
    private readonly IMvaRepository _repo;

    public MvaAvstemmingService(IMvaRepository repo)
    {
        _repo = repo;
    }

    public async Task<MvaAvstemmingDto> KjorAvstemmingAsync(Guid terminId, CancellationToken ct = default)
    {
        var termin = await _repo.HentTerminAsync(terminId, ct)
            ?? throw new MvaTerminIkkeFunnetException(terminId);

        // Bestem regnskapsperiodene som inngaar i terminen
        int fraPeriode = termin.FraDato.Month;
        int tilPeriode = termin.TilDato.Month;

        // Hent saldo for MVA-kontoer
        var kontoSaldoer = await _repo.HentMvaKontoSaldoerAsync(
            termin.Ar, fraPeriode, tilPeriode, ct);

        // Hent beregnede MVA-belop per kontonummer fra auto-genererte posteringer
        var beregnetPerKonto = await _repo.HentBeregnetMvaPerKontoAsync(
            termin.FraDato, termin.TilDato, ct);
        var beregnetMap = beregnetPerKonto.ToDictionary(b => b.Kontonummer);

        // Bygg avstemmingslinjer: sammenlign KontoSaldo med beregnet fra posteringer
        var linjer = new List<MvaAvstemmingLinje>();

        foreach (var ks in kontoSaldoer)
        {
            // Saldo ifg. hovedbok: netto endring i perioden (debet - kredit for balanse)
            var saldoHovedbok = ks.UtgaendeBalanse;

            // Beregnet fra auto-genererte MVA-posteringer pa denne kontoen
            // NettoEndring = SumDebet - SumKredit (same sign convention as UtgaendeBalanse)
            var beregnet = beregnetMap.TryGetValue(ks.Kontonummer, out var b)
                ? b.NettoEndring
                : 0m;

            var avvik = saldoHovedbok - beregnet;

            linjer.Add(new MvaAvstemmingLinje
            {
                Id = Guid.NewGuid(),
                Kontonummer = ks.Kontonummer,
                Kontonavn = ks.Kontonavn,
                SaldoIflgHovedbok = saldoHovedbok,
                BeregnetFraPosteringer = beregnet,
                Avvik = avvik
            });
        }

        var avstemming = new MvaAvstemming
        {
            Id = Guid.NewGuid(),
            MvaTerminId = terminId,
            AvstemmingTidspunkt = DateTime.UtcNow,
            AvstemmingAv = "system", // TODO: Hent fra HTTP-kontekst
            ErGodkjent = false,
            Linjer = linjer
        };

        await _repo.LeggTilAvstemmingAsync(avstemming, ct);
        await _repo.LagreEndringerAsync(ct);

        return MapAvstemming(avstemming, termin.Terminnavn);
    }

    public async Task<MvaAvstemmingDto> HentAvstemmingAsync(Guid terminId, CancellationToken ct = default)
    {
        var termin = await _repo.HentTerminAsync(terminId, ct)
            ?? throw new MvaTerminIkkeFunnetException(terminId);

        var avstemming = await _repo.HentSisteAvstemmingForTerminAsync(terminId, ct)
            ?? throw new MvaAvstemmingIkkeFunnetException(terminId);

        return MapAvstemming(avstemming, termin.Terminnavn);
    }

    public async Task<List<MvaAvstemmingDto>> HentAvstemmingshistorikkAsync(Guid terminId, CancellationToken ct = default)
    {
        var termin = await _repo.HentTerminAsync(terminId, ct)
            ?? throw new MvaTerminIkkeFunnetException(terminId);

        var historikk = await _repo.HentAvstemmingshistorikkAsync(terminId, ct);
        return historikk.Select(a => MapAvstemming(a, termin.Terminnavn)).ToList();
    }

    public async Task<MvaAvstemmingDto> GodkjennAvstemmingAsync(
        Guid terminId, Guid avstemmingId, string? merknad, CancellationToken ct = default)
    {
        var termin = await _repo.HentTerminAsync(terminId, ct)
            ?? throw new MvaTerminIkkeFunnetException(terminId);

        var avstemming = await _repo.HentSisteAvstemmingForTerminAsync(terminId, ct)
            ?? throw new MvaAvstemmingIkkeFunnetException(terminId);

        if (avstemming.Id != avstemmingId)
            throw new MvaAvstemmingIkkeFunnetException(terminId);

        avstemming.ErGodkjent = true;
        avstemming.Merknad = merknad;

        // Oppdater terminstatus til Avstemt
        if (termin.Status == MvaTerminStatus.Beregnet)
            termin.Status = MvaTerminStatus.Avstemt;

        await _repo.LagreEndringerAsync(ct);

        return MapAvstemming(avstemming, termin.Terminnavn);
    }

    private static MvaAvstemmingDto MapAvstemming(MvaAvstemming a, string terminnavn)
    {
        var linjerDto = a.Linjer.Select(l => new MvaAvstemmingLinjeDto(
            l.Kontonummer, l.Kontonavn,
            l.SaldoIflgHovedbok, l.BeregnetFraPosteringer,
            l.Avvik, l.HarAvvik
        )).ToList();

        var harAvvik = linjerDto.Any(l => l.HarAvvik);
        var totaltAvvik = linjerDto.Sum(l => Math.Abs(l.Avvik));

        return new MvaAvstemmingDto(
            a.Id, a.MvaTerminId, terminnavn,
            a.AvstemmingTidspunkt, a.AvstemmingAv,
            a.ErGodkjent, a.Merknad,
            harAvvik, totaltAvvik, linjerDto
        );
    }
}
