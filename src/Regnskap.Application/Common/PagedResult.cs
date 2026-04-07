namespace Regnskap.Application.Common;

/// <summary>
/// Paginated result wrapper.
/// </summary>
public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Side,
    int Antall
)
{
    public int TotalSider => (int)Math.Ceiling((double)TotalCount / Antall);
    public bool HarNesteSide => Side < TotalSider;
}
