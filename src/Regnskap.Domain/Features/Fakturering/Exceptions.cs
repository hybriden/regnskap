namespace Regnskap.Domain.Features.Fakturering;

public class FakturaException : Exception
{
    public string Kode { get; }
    public FakturaException(string kode, string melding) : base(melding) => Kode = kode;
}

public class FakturaIkkeUtkastException : FakturaException
{
    public FakturaIkkeUtkastException()
        : base("FAKTURA_IKKE_UTKAST", "Kun utkast kan redigeres/kanselleres.") { }
}

public class FakturaAlleredeUtstedtException : FakturaException
{
    public FakturaAlleredeUtstedtException()
        : base("FAKTURA_ALLEREDE_UTSTEDT", "Faktura er allerede utstedt.") { }
}

public class FakturaKundeIkkeFunnetException : FakturaException
{
    public FakturaKundeIkkeFunnetException(Guid kundeId)
        : base("FAKTURA_KUNDE_IKKE_FUNNET", $"Kunde med ID '{kundeId}' finnes ikke.") { }
}

public class FakturaKundeSperretException : FakturaException
{
    public FakturaKundeSperretException(string kundenummer)
        : base("FAKTURA_KUNDE_SPERRET", $"Kunden '{kundenummer}' er sperret for fakturering.") { }
}

public class FakturaIngenLinjerException : FakturaException
{
    public FakturaIngenLinjerException()
        : base("FAKTURA_INGEN_LINJER", "Faktura maa ha minimum en linje.") { }
}

public class FakturaUgyldigMvaKodeException : FakturaException
{
    public FakturaUgyldigMvaKodeException(string kode)
        : base("FAKTURA_UGYLDIG_MVA_KODE", $"MVA-kode '{kode}' finnes ikke.") { }
}

public class FakturaUgyldigKontoException : FakturaException
{
    public FakturaUgyldigKontoException(string kontonummer)
        : base("FAKTURA_UGYLDIG_KONTO", $"Konto '{kontonummer}' er ikke en gyldig inntektskonto.") { }
}

public class FakturaPeriodeLukketException : FakturaException
{
    public FakturaPeriodeLukketException()
        : base("FAKTURA_PERIODE_LUKKET", "Regnskapsperioden er lukket.") { }
}

public class KreditnotaOriginalIkkeFunnetException : FakturaException
{
    public KreditnotaOriginalIkkeFunnetException(Guid fakturaId)
        : base("KREDITNOTA_ORIGINAL_IKKE_FUNNET", $"Opprinnelig faktura '{fakturaId}' finnes ikke.") { }
}

public class KreditnotaAlleredeKreditertException : FakturaException
{
    public KreditnotaAlleredeKreditertException()
        : base("KREDITNOTA_ALLEREDE_KREDITERT", "Fakturaen er allerede fullt kreditert.") { }
}

public class KreditnotaBelopOverskriderException : FakturaException
{
    public KreditnotaBelopOverskriderException()
        : base("KREDITNOTA_BELOP_OVERSTIGER", "Kreditert belop overstiger gjenstaende.") { }
}

public class EhfManglerKjopersRefException : FakturaException
{
    public EhfManglerKjopersRefException()
        : base("EHF_MANGLER_KJOPERS_REF", "EHF krever BuyerReference eller OrderReference.") { }
}

public class EhfKundeUtenPeppolException : FakturaException
{
    public EhfKundeUtenPeppolException()
        : base("EHF_KUNDE_UTEN_PEPPOL", "Kunden har ikke PEPPOL-ID.") { }
}
