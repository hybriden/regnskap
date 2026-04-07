namespace Regnskap.Application.Features.Mva;

using Regnskap.Domain.Features.Mva;

public class MvaTerminService : IMvaTerminService
{
    private readonly IMvaRepository _repo;

    public MvaTerminService(IMvaRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<MvaTerminDto>> HentTerminerAsync(int ar, CancellationToken ct = default)
    {
        var terminer = await _repo.HentTerminerForArAsync(ar, ct);
        var dtos = new List<MvaTerminDto>();

        foreach (var t in terminer)
        {
            var oppgjor = await _repo.HentOppgjorForTerminAsync(t.Id, ct);
            dtos.Add(MapTermin(t, oppgjor != null));
        }

        return dtos;
    }

    public async Task<MvaTerminDto> HentTerminAsync(Guid id, CancellationToken ct = default)
    {
        var termin = await _repo.HentTerminAsync(id, ct)
            ?? throw new MvaTerminIkkeFunnetException(id);

        var oppgjor = await _repo.HentOppgjorForTerminAsync(id, ct);
        return MapTermin(termin, oppgjor != null);
    }

    public async Task<List<MvaTerminDto>> GenererTerminerAsync(int ar, MvaTerminType type, CancellationToken ct = default)
    {
        if (ar < 2000 || ar > 2099)
            throw new ArgumentException("Ar ma vaere mellom 2000 og 2099.", nameof(ar));

        if (await _repo.TerminerFinnesForArAsync(ar, ct))
            throw new MvaTerminerFinnesException(ar);

        var terminer = type == MvaTerminType.Arlig
            ? GenererArstermin(ar)
            : GenererTomaanedersTerminer(ar);

        await _repo.LeggTilTerminerAsync(terminer, ct);
        await _repo.LagreEndringerAsync(ct);

        return terminer.Select(t => MapTermin(t, false)).ToList();
    }

    private static List<MvaTermin> GenererTomaanedersTerminer(int ar)
    {
        var erSkuddar = DateTime.IsLeapYear(ar);
        var febSiste = erSkuddar ? 29 : 28;

        return new List<MvaTermin>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Ar = ar, Termin = 1,
                Type = MvaTerminType.Tomaaneders,
                FraDato = new DateOnly(ar, 1, 1),
                TilDato = new DateOnly(ar, 2, febSiste),
                Frist = new DateOnly(ar, 4, 10),
                Status = MvaTerminStatus.Apen
            },
            new()
            {
                Id = Guid.NewGuid(),
                Ar = ar, Termin = 2,
                Type = MvaTerminType.Tomaaneders,
                FraDato = new DateOnly(ar, 3, 1),
                TilDato = new DateOnly(ar, 4, 30),
                Frist = new DateOnly(ar, 6, 10),
                Status = MvaTerminStatus.Apen
            },
            new()
            {
                Id = Guid.NewGuid(),
                Ar = ar, Termin = 3,
                Type = MvaTerminType.Tomaaneders,
                FraDato = new DateOnly(ar, 5, 1),
                TilDato = new DateOnly(ar, 6, 30),
                Frist = new DateOnly(ar, 8, 31), // Forlenget frist, sommerferie
                Status = MvaTerminStatus.Apen
            },
            new()
            {
                Id = Guid.NewGuid(),
                Ar = ar, Termin = 4,
                Type = MvaTerminType.Tomaaneders,
                FraDato = new DateOnly(ar, 7, 1),
                TilDato = new DateOnly(ar, 8, 31),
                Frist = new DateOnly(ar, 10, 10),
                Status = MvaTerminStatus.Apen
            },
            new()
            {
                Id = Guid.NewGuid(),
                Ar = ar, Termin = 5,
                Type = MvaTerminType.Tomaaneders,
                FraDato = new DateOnly(ar, 9, 1),
                TilDato = new DateOnly(ar, 10, 31),
                Frist = new DateOnly(ar, 12, 10),
                Status = MvaTerminStatus.Apen
            },
            new()
            {
                Id = Guid.NewGuid(),
                Ar = ar, Termin = 6,
                Type = MvaTerminType.Tomaaneders,
                FraDato = new DateOnly(ar, 11, 1),
                TilDato = new DateOnly(ar, 12, 31),
                Frist = new DateOnly(ar + 1, 2, 10), // Neste ar
                Status = MvaTerminStatus.Apen
            }
        };
    }

    private static List<MvaTermin> GenererArstermin(int ar)
    {
        return new List<MvaTermin>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Ar = ar, Termin = 1,
                Type = MvaTerminType.Arlig,
                FraDato = new DateOnly(ar, 1, 1),
                TilDato = new DateOnly(ar, 12, 31),
                Frist = new DateOnly(ar + 1, 3, 10), // Neste ar
                Status = MvaTerminStatus.Apen
            }
        };
    }

    private static MvaTerminDto MapTermin(MvaTermin t, bool harOppgjor)
    {
        var erForfalt = t.Frist < DateOnly.FromDateTime(DateTime.UtcNow)
            && t.Status != MvaTerminStatus.Innsendt
            && t.Status != MvaTerminStatus.Betalt;

        return new MvaTerminDto(
            t.Id, t.Ar, t.Termin,
            t.Type.ToString(),
            t.FraDato, t.TilDato, t.Frist,
            t.Status.ToString(),
            t.Terminnavn,
            t.AvsluttetTidspunkt, t.AvsluttetAv,
            t.OppgjorsBilagId,
            harOppgjor,
            erForfalt
        );
    }
}
