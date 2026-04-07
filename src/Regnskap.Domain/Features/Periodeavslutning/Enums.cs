namespace Regnskap.Domain.Features.Periodeavslutning;

public enum PeriodiseringsType
{
    /// <summary>Forskuddsbetalt kostnad (prepaid expense). Balanse -> Resultat.</summary>
    ForskuddsbetaltKostnad,

    /// <summary>Palopte kostnader (accrued expense). Resultat -> Balanse (gjeld).</summary>
    PaloptKostnad,

    /// <summary>Forskuddsbetalt inntekt (deferred revenue). Balanse (gjeld) -> Resultat.</summary>
    ForskuddsbetaltInntekt,

    /// <summary>Opptjent, ikke fakturert inntekt (accrued revenue). Resultat -> Balanse (fordring).</summary>
    OpptjentInntekt
}

public enum PeriodeLukkingSteg
{
    AvskrivningerBeregnet,
    PeriodiseringerBokfort,
    AvstemmingKjort,
    SaldokontrollBestatt,
    BilagsnummerKontrollert,
    MvaAvstemt,
    PeriodeLukket
}

public enum ArsavslutningFase
{
    IkkeStartet,
    AllePerioderLukket,
    ArsoppgjorBilagOpprettet,
    ResultatDisponert,
    ResultatkontoerNullstilt,
    ApningsbalanseOpprettet,
    Fullfort
}
