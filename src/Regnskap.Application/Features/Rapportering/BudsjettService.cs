using Regnskap.Domain.Features.Rapportering;

namespace Regnskap.Application.Features.Rapportering;

public class BudsjettService : IBudsjettService
{
    private readonly IRapporteringRepository _repo;

    public BudsjettService(IRapporteringRepository repo)
    {
        _repo = repo;
    }

    public async Task<BudsjettDto> OpprettBudsjettLinjeAsync(
        OpprettBudsjettRequest request, CancellationToken ct = default)
    {
        // Sjekk om linjen allerede eksisterer (upsert)
        var existing = await _repo.HentBudsjettLinjeAsync(
            request.Kontonummer, request.Ar, request.Periode, request.Versjon, ct);

        if (existing != null)
        {
            existing.Belop = request.Belop;
            existing.Merknad = request.Merknad;
            await _repo.LagreEndringerAsync(ct);
            return MapToDto(existing);
        }

        var budsjett = new Budsjett
        {
            Id = Guid.NewGuid(),
            Kontonummer = request.Kontonummer,
            Ar = request.Ar,
            Periode = request.Periode,
            Belop = request.Belop,
            Versjon = request.Versjon,
            Merknad = request.Merknad
        };

        await _repo.LeggTilBudsjettAsync(budsjett, ct);
        await _repo.LagreEndringerAsync(ct);

        return MapToDto(budsjett);
    }

    public async Task<List<BudsjettDto>> BulkImportAsync(
        BudsjettBulkRequest request, CancellationToken ct = default)
    {
        var result = new List<BudsjettDto>();

        foreach (var linje in request.Linjer)
        {
            var dto = await OpprettBudsjettLinjeAsync(new OpprettBudsjettRequest(
                Kontonummer: linje.Kontonummer,
                Ar: request.Ar,
                Periode: linje.Periode,
                Belop: linje.Belop,
                Versjon: request.Versjon
            ), ct);

            result.Add(dto);
        }

        return result;
    }

    public async Task<List<BudsjettDto>> HentBudsjettAsync(
        int ar, string versjon = "Opprinnelig", CancellationToken ct = default)
    {
        var budsjetter = await _repo.HentBudsjettForArAsync(ar, versjon, ct);
        return budsjetter.Select(MapToDto).ToList();
    }

    public async Task SlettBudsjettAsync(int ar, string versjon, CancellationToken ct = default)
    {
        await _repo.SlettBudsjettForArAsync(ar, versjon, ct);
        await _repo.LagreEndringerAsync(ct);
    }

    private static BudsjettDto MapToDto(Budsjett b) => new(
        Id: b.Id,
        Kontonummer: b.Kontonummer,
        Ar: b.Ar,
        Periode: b.Periode,
        Belop: b.Belop,
        Versjon: b.Versjon,
        Merknad: b.Merknad
    );
}
