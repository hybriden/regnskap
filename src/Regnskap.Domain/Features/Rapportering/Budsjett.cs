namespace Regnskap.Domain.Features.Rapportering;

using Regnskap.Domain.Common;

/// <summary>
/// Budsjett for en konto i en periode. Brukes til budsjettsammenligning.
/// </summary>
public class Budsjett : AuditableEntity
{
    public string Kontonummer { get; set; } = default!;
    public int Ar { get; set; }
    public int Periode { get; set; }
    public decimal Belop { get; set; }
    public string Versjon { get; set; } = "Opprinnelig";
    public string? Merknad { get; set; }
}
