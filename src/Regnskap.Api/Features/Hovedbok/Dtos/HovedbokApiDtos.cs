using Regnskap.Application.Features.Hovedbok;

namespace Regnskap.Api.Features.Hovedbok.Dtos;

public record OpprettPerioderApiRequest
{
    public int Ar { get; init; }
}

public record EndrePeriodeStatusApiRequest
{
    public string NyStatus { get; init; } = default!;
    public string? Merknad { get; init; }
}

public record PerioderListeResponse(
    int Ar,
    List<RegnskapsperiodeDto> Perioder);

public record BilagListeResponse(
    List<BilagDto> Data,
    int TotaltAntall,
    int Side,
    int Antall);
