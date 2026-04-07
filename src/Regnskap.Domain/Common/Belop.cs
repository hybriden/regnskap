namespace Regnskap.Domain.Common;

/// <summary>
/// Value object for monetary amounts. Uses decimal to avoid floating-point errors.
/// </summary>
public readonly record struct Belop(decimal Verdi)
{
    public static readonly Belop Null = new(0m);

    public static Belop operator +(Belop a, Belop b) => new(a.Verdi + b.Verdi);
    public static Belop operator -(Belop a, Belop b) => new(a.Verdi - b.Verdi);
    public static Belop operator -(Belop a) => new(-a.Verdi);
    public static bool operator >(Belop a, Belop b) => a.Verdi > b.Verdi;
    public static bool operator <(Belop a, Belop b) => a.Verdi < b.Verdi;
    public static bool operator >=(Belop a, Belop b) => a.Verdi >= b.Verdi;
    public static bool operator <=(Belop a, Belop b) => a.Verdi <= b.Verdi;

    public Belop Abs() => new(Math.Abs(Verdi));

    public override string ToString() => Verdi.ToString("N2");
}
