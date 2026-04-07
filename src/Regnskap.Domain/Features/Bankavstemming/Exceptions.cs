namespace Regnskap.Domain.Features.Bankavstemming;

public class BankkontoFinnesException : Exception
{
    public BankkontoFinnesException(string kontonummer)
        : base($"BANK_KONTO_FINNES: Bankkonto med kontonummer '{kontonummer}' er allerede registrert.") { }
}

public class UgyldigBankkontonummerException : Exception
{
    public UgyldigBankkontonummerException(string kontonummer)
        : base($"BANK_UGYLDIG_KONTONUMMER: Bankkontonummer '{kontonummer}' er ugyldig (MOD11-kontroll feilet).") { }
}

public class UgyldigHovedbokkontoException : Exception
{
    public UgyldigHovedbokkontoException(string kontonummer)
        : base($"BANK_UGYLDIG_HOVEDBOK: Hovedbokkonto '{kontonummer}' maa vaere i 19xx-serien (bank/kasse).") { }
}

public class ImportDuplikatException : Exception
{
    public ImportDuplikatException(string meldingsId)
        : base($"IMPORT_DUPLIKAT: Kontoutskrift med MeldingsId '{meldingsId}' er allerede importert.") { }
}

public class ImportFeilKontoException : Exception
{
    public ImportFeilKontoException()
        : base("IMPORT_FEIL_KONTO: IBAN/kontonummer i filen matcher ikke valgt bankkonto.") { }
}

public class ImportUgyldigXmlException : Exception
{
    public ImportUgyldigXmlException(string detaljer)
        : base($"IMPORT_UGYLDIG_XML: Filen er ikke gyldig CAMT.053 XML. {detaljer}") { }
}

public class MatchAlleredeMatchetException : Exception
{
    public MatchAlleredeMatchetException()
        : base("MATCH_ALLEREDE_MATCHET: Bevegelsen er allerede matchet.") { }
}

public class MatchBelopMismatchException : Exception
{
    public MatchBelopMismatchException(decimal bevegelsebelop, decimal postbelop)
        : base($"MATCH_BELOP_MISMATCH: Bevegelsebelop ({bevegelsebelop:N2}) matcher ikke den aapne posten ({postbelop:N2}).") { }
}

public class SplittSumFeilException : Exception
{
    public SplittSumFeilException(decimal sum, decimal belop)
        : base($"SPLITT_SUM_FEIL: Sum av delbelop ({sum:N2}) stemmer ikke med bevegelsebelop ({belop:N2}).") { }
}

public class AvstemmingUforklartException : Exception
{
    public AvstemmingUforklartException()
        : base("AVSTEMMING_UFORKLART: Kan ikke godkjenne avstemming med uforklart differanse.") { }
}
