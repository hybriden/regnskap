using System.Xml;
using System.Xml.Linq;
using Regnskap.Domain.Features.Rapportering;

namespace Regnskap.Application.Features.Rapportering;

public class SaftEksportService : ISaftEksportService
{
    private readonly IRapporteringRepository _repo;
    private readonly ISaftDataProvider _dataProvider;

    private const string SaftNs = "urn:StandardAuditFile-Taxation-Financial:NO";

    public SaftEksportService(IRapporteringRepository repo, ISaftDataProvider dataProvider)
    {
        _repo = repo;
        _dataProvider = dataProvider;
    }

    public async Task<Stream> GenererSaftXmlAsync(
        int ar, int fraPeriode = 1, int tilPeriode = 12,
        string taxAccountingBasis = "A",
        CancellationToken ct = default)
    {
        var konfig = await _repo.HentKonfigurasjonAsync(ct)
            ?? throw new InvalidOperationException("RapportKonfigurasjon mangler. Opprett konfigurasjon for det.");

        var kontoer = await _dataProvider.HentKontoerAsync(ct);
        var kunder = await _dataProvider.HentKunderAsync(ct);
        var leverandorer = await _dataProvider.HentLeverandorerAsync(ct);
        var mvaKoder = await _dataProvider.HentMvaKoderAsync(ct);
        var bilagSerier = await _dataProvider.HentBilagSerierAsync(ct);
        var bilag = await _dataProvider.HentBilagMedPosteringerAsync(ar, fraPeriode, tilPeriode, ct);
        var saldoer = await _repo.HentAggregerteSaldoerAsync(ar, fraPeriode, tilPeriode, ct: ct);

        XNamespace ns = SaftNs;

        var fraDato = new DateOnly(ar, fraPeriode, 1);
        var tilDato = new DateOnly(ar, tilPeriode, DateTime.DaysInMonth(ar, tilPeriode));

        // Build Header
        var header = new XElement(ns + "Header",
            new XElement(ns + "AuditFileVersion", "1.30"),
            new XElement(ns + "AuditFileCountry", "NO"),
            new XElement(ns + "AuditFileDateCreated", DateTime.UtcNow.ToString("yyyy-MM-dd")),
            new XElement(ns + "SoftwareCompanyName", "Regnskap AS"),
            new XElement(ns + "SoftwareID", "Regnskap"),
            new XElement(ns + "SoftwareVersion", "1.0.0"),
            new XElement(ns + "Company",
                new XElement(ns + "RegistrationNumber", konfig.Organisasjonsnummer),
                new XElement(ns + "Name", konfig.Firmanavn),
                new XElement(ns + "Address",
                    new XElement(ns + "StreetName", konfig.Adresse),
                    new XElement(ns + "PostalCode", konfig.Postnummer),
                    new XElement(ns + "City", konfig.Poststed),
                    new XElement(ns + "Country", konfig.Landskode)
                ),
                konfig.ErMvaRegistrert
                    ? new XElement(ns + "TaxRegistration",
                        new XElement(ns + "TaxRegistrationNumber", konfig.Organisasjonsnummer + "MVA"),
                        new XElement(ns + "TaxAuthority", "Skatteetaten"))
                    : null!
            ),
            new XElement(ns + "DefaultCurrencyCode", konfig.Valuta),
            new XElement(ns + "SelectionCriteria",
                new XElement(ns + "SelectionStartDate", fraDato.ToString("yyyy-MM-dd")),
                new XElement(ns + "SelectionEndDate", tilDato.ToString("yyyy-MM-dd"))
            ),
            new XElement(ns + "TaxAccountingBasis", taxAccountingBasis)
        );

        // Build MasterFiles
        var accounts = new XElement(ns + "GeneralLedgerAccounts",
            kontoer.Select(k =>
            {
                var saldo = saldoer.FirstOrDefault(s => s.Kontonummer == k.Kontonummer);
                var ib = saldo?.InngaendeBalanse ?? 0m;
                var ub = saldo?.UtgaendeBalanse ?? 0m;

                var accountEl = new XElement(ns + "Account",
                    new XElement(ns + "AccountID", k.Kontonummer),
                    new XElement(ns + "AccountDescription", k.Navn),
                    new XElement(ns + "StandardAccountID", k.StandardAccountId),
                    new XElement(ns + "AccountType", k.Kontotype.ToString())
                );

                if (k.GrupperingsKategori.HasValue)
                {
                    accountEl.Add(new XElement(ns + "GroupingCategory", k.GrupperingsKategori.Value.ToString()));
                    if (k.GrupperingsKode != null)
                        accountEl.Add(new XElement(ns + "GroupingCode", k.GrupperingsKode));
                }

                // Opening/Closing balances
                if (ib >= 0)
                    accountEl.Add(new XElement(ns + "OpeningDebitBalance", ib.ToString("F2")));
                else
                    accountEl.Add(new XElement(ns + "OpeningCreditBalance", Math.Abs(ib).ToString("F2")));

                if (ub >= 0)
                    accountEl.Add(new XElement(ns + "ClosingDebitBalance", ub.ToString("F2")));
                else
                    accountEl.Add(new XElement(ns + "ClosingCreditBalance", Math.Abs(ub).ToString("F2")));

                return accountEl;
            })
        );

        var customers = new XElement(ns + "Customers",
            kunder.Select(k => new XElement(ns + "Customer",
                new XElement(ns + "CustomerID", k.Kundenummer),
                new XElement(ns + "Name", k.Navn),
                k.Adresse1 != null ? new XElement(ns + "Address",
                    new XElement(ns + "StreetName", k.Adresse1),
                    k.Postnummer != null ? new XElement(ns + "PostalCode", k.Postnummer) : null!,
                    k.Poststed != null ? new XElement(ns + "City", k.Poststed) : null!,
                    new XElement(ns + "Country", k.Landkode)
                ) : null!,
                new XElement(ns + "OpeningDebitBalance", "0.00"),
                new XElement(ns + "ClosingDebitBalance", "0.00")
            ))
        );

        var suppliers = new XElement(ns + "Suppliers",
            leverandorer.Select(l => new XElement(ns + "Supplier",
                new XElement(ns + "SupplierID", l.Leverandornummer),
                new XElement(ns + "Name", l.Navn),
                l.Adresse1 != null ? new XElement(ns + "Address",
                    new XElement(ns + "StreetName", l.Adresse1),
                    l.Postnummer != null ? new XElement(ns + "PostalCode", l.Postnummer) : null!,
                    l.Poststed != null ? new XElement(ns + "City", l.Poststed) : null!,
                    new XElement(ns + "Country", l.Landkode)
                ) : null!,
                new XElement(ns + "OpeningCreditBalance", "0.00"),
                new XElement(ns + "ClosingCreditBalance", "0.00")
            ))
        );

        var taxTable = new XElement(ns + "TaxTable",
            mvaKoder.Select(m => new XElement(ns + "TaxCodeDetails",
                new XElement(ns + "TaxCode", m.Kode),
                new XElement(ns + "Description", m.Beskrivelse),
                new XElement(ns + "TaxPercentage", m.Sats.ToString("F2")),
                new XElement(ns + "StandardTaxCode", m.StandardTaxCode)
            ))
        );

        var masterFiles = new XElement(ns + "MasterFiles",
            accounts, customers, suppliers, taxTable);

        // Build GeneralLedgerEntries
        var totalDebet = 0m;
        var totalKredit = 0m;
        var numberOfEntries = bilag.Count;

        var journalsByType = bilag
            .GroupBy(b => b.SerieKode ?? "GEN")
            .ToList();

        var journals = journalsByType.Select(group =>
        {
            var serie = bilagSerier.FirstOrDefault(s => s.Kode == group.Key);
            var journalEl = new XElement(ns + "Journal",
                new XElement(ns + "JournalID", serie?.SaftJournalId ?? group.Key),
                new XElement(ns + "Description", serie?.Navn ?? "Generell journal"),
                new XElement(ns + "Type", serie?.StandardType.ToString() ?? "Manuelt")
            );

            foreach (var b in group.OrderBy(b => b.Bilagsnummer))
            {
                var transEl = new XElement(ns + "Transaction",
                    new XElement(ns + "TransactionID", b.BilagsId),
                    new XElement(ns + "Period", b.SaftPeriode.ToString()),
                    new XElement(ns + "TransactionDate", b.Bilagsdato.ToString("yyyy-MM-dd")),
                    new XElement(ns + "Description", b.Beskrivelse),
                    new XElement(ns + "SystemEntryDate", b.Registreringsdato.ToString("yyyy-MM-dd")),
                    b.BokfortTidspunkt.HasValue
                        ? new XElement(ns + "GLPostingDate", b.BokfortTidspunkt.Value.ToString("yyyy-MM-dd"))
                        : null!
                );

                foreach (var p in b.Posteringer.OrderBy(p => p.Linjenummer))
                {
                    var lineEl = new XElement(ns + "Line",
                        new XElement(ns + "RecordID", p.Linjenummer.ToString()),
                        new XElement(ns + "AccountID", p.Kontonummer),
                        new XElement(ns + "Description", p.Beskrivelse)
                    );

                    if (p.Side == Domain.Features.Hovedbok.BokforingSide.Debet)
                    {
                        lineEl.Add(new XElement(ns + "DebitAmount",
                            new XElement(ns + "Amount", p.Belop.Verdi.ToString("F2"))));
                        totalDebet += p.Belop.Verdi;
                    }
                    else
                    {
                        lineEl.Add(new XElement(ns + "CreditAmount",
                            new XElement(ns + "Amount", p.Belop.Verdi.ToString("F2"))));
                        totalKredit += p.Belop.Verdi;
                    }

                    // TaxInformation
                    if (p.MvaKode != null)
                    {
                        var taxInfo = new XElement(ns + "TaxInformation",
                            new XElement(ns + "TaxCode", p.MvaKode));
                        if (p.MvaSats.HasValue)
                            taxInfo.Add(new XElement(ns + "TaxPercentage", p.MvaSats.Value.ToString("F2")));
                        if (p.MvaGrunnlag != null)
                            taxInfo.Add(new XElement(ns + "TaxBase", p.MvaGrunnlag.Value.Verdi.ToString("F2")));
                        if (p.MvaBelop != null)
                            taxInfo.Add(new XElement(ns + "TaxAmount",
                                new XElement(ns + "Amount", p.MvaBelop.Value.Verdi.ToString("F2"))));
                        lineEl.Add(taxInfo);
                    }

                    if (p.KundeId.HasValue)
                        lineEl.Add(new XElement(ns + "CustomerID", p.KundeId.Value.ToString()));
                    if (p.LeverandorId.HasValue)
                        lineEl.Add(new XElement(ns + "SupplierID", p.LeverandorId.Value.ToString()));

                    transEl.Add(lineEl);
                }

                journalEl.Add(transEl);
            }

            return journalEl;
        }).ToList(); // Materialize to compute totalDebet/totalKredit

        var generalLedgerEntries = new XElement(ns + "GeneralLedgerEntries",
            new XElement(ns + "NumberOfEntries", numberOfEntries.ToString()),
            new XElement(ns + "TotalDebit", totalDebet.ToString("F2")),
            new XElement(ns + "TotalCredit", totalKredit.ToString("F2")),
            journals
        );

        var auditFile = new XElement(ns + "AuditFile", header, masterFiles, generalLedgerEntries);
        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), auditFile);

        var stream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = System.Text.Encoding.UTF8,
            Async = true
        };
        using (var writer = XmlWriter.Create(stream, settings))
        {
            doc.WriteTo(writer);
        }
        stream.Position = 0;

        // Logg
        var logg = new RapportLogg
        {
            Id = Guid.NewGuid(),
            Type = RapportType.SaftEksport,
            Ar = ar,
            FraPeriode = fraPeriode,
            TilPeriode = tilPeriode,
            GenererTidspunkt = DateTime.UtcNow,
            GenererAv = "system"
        };
        await _repo.LeggTilRapportLoggAsync(logg, ct);
        await _repo.LagreEndringerAsync(ct);

        return stream;
    }

    public Task<List<string>> ValiderSaftXmlAsync(Stream xmlStream, CancellationToken ct = default)
    {
        // TODO: Avklar med arkitekt - XSD-validering krever Skatteetatens XSD-skjema for v1.30
        // For na returnerer vi en tom liste (gyldig).
        var errors = new List<string>();

        try
        {
            xmlStream.Position = 0;
            var doc = XDocument.Load(xmlStream);

            // Basic structural validation
            XNamespace ns = SaftNs;
            var auditFile = doc.Element(ns + "AuditFile");
            if (auditFile == null)
                errors.Add("Mangler AuditFile rotelement.");

            var headerEl = auditFile?.Element(ns + "Header");
            if (headerEl == null)
                errors.Add("Mangler Header-element.");

            var masterFilesEl = auditFile?.Element(ns + "MasterFiles");
            if (masterFilesEl == null)
                errors.Add("Mangler MasterFiles-element.");

            var entriesEl = auditFile?.Element(ns + "GeneralLedgerEntries");
            if (entriesEl == null)
                errors.Add("Mangler GeneralLedgerEntries-element.");

            // Validate totals
            if (entriesEl != null)
            {
                var totalDebit = decimal.Parse(entriesEl.Element(ns + "TotalDebit")?.Value ?? "0");
                var totalCredit = decimal.Parse(entriesEl.Element(ns + "TotalCredit")?.Value ?? "0");

                var calculatedDebit = 0m;
                var calculatedCredit = 0m;

                foreach (var journal in entriesEl.Elements(ns + "Journal"))
                {
                    foreach (var transaction in journal.Elements(ns + "Transaction"))
                    {
                        foreach (var line in transaction.Elements(ns + "Line"))
                        {
                            var debitEl = line.Element(ns + "DebitAmount")?.Element(ns + "Amount");
                            var creditEl = line.Element(ns + "CreditAmount")?.Element(ns + "Amount");

                            if (debitEl != null)
                                calculatedDebit += decimal.Parse(debitEl.Value);
                            if (creditEl != null)
                                calculatedCredit += decimal.Parse(creditEl.Value);
                        }
                    }
                }

                if (Math.Abs(totalDebit - calculatedDebit) > 0.01m)
                    errors.Add($"TotalDebit ({totalDebit}) stemmer ikke med sum av linjer ({calculatedDebit}).");
                if (Math.Abs(totalCredit - calculatedCredit) > 0.01m)
                    errors.Add($"TotalCredit ({totalCredit}) stemmer ikke med sum av linjer ({calculatedCredit}).");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"XML-parsefeil: {ex.Message}");
        }

        return Task.FromResult(errors);
    }
}
