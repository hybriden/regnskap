using Regnskap.Domain.Features.Kontoplan;

namespace Regnskap.Infrastructure.Features.Kontoplan;

/// <summary>
/// NS 4102 standard kontogrupper og kontoer for seed.
/// </summary>
public static class KontoplanSeedData
{
    public static List<Kontogruppe> HentKontogrupper()
    {
        return new List<Kontogruppe>
        {
            Gruppe(10, "Immaterielle eiendeler", "Intangible assets", Kontotype.Eiendel, Normalbalanse.Debet),
            Gruppe(11, "Tomter, bygninger og annen fast eiendom", "Land, buildings and other real estate", Kontotype.Eiendel, Normalbalanse.Debet),
            Gruppe(12, "Transportmidler, maskiner, inventar", "Vehicles, machinery, fixtures", Kontotype.Eiendel, Normalbalanse.Debet),
            Gruppe(13, "Finansielle anleggsmidler", "Long-term financial assets", Kontotype.Eiendel, Normalbalanse.Debet),
            Gruppe(14, "Varelager og forskudd til leverandorer", "Inventory and prepayments to suppliers", Kontotype.Eiendel, Normalbalanse.Debet),
            Gruppe(15, "Kortsiktige fordringer", "Short-term receivables", Kontotype.Eiendel, Normalbalanse.Debet),
            Gruppe(16, "Merverdiavgift, opptjente inntekter", "VAT, accrued revenue", Kontotype.Eiendel, Normalbalanse.Debet),
            Gruppe(17, "Forskuddsbetalt kostnad, pabegynt arbeid", "Prepaid expenses, work in progress", Kontotype.Eiendel, Normalbalanse.Debet),
            Gruppe(18, "Kortsiktige finansinvesteringer", "Short-term financial investments", Kontotype.Eiendel, Normalbalanse.Debet),
            Gruppe(19, "Bankinnskudd, kontanter og lignende", "Bank deposits, cash and equivalents", Kontotype.Eiendel, Normalbalanse.Debet),
            Gruppe(20, "Innskutt egenkapital", "Contributed equity", Kontotype.Egenkapital, Normalbalanse.Kredit),
            Gruppe(21, "Opptjent egenkapital", "Retained earnings", Kontotype.Egenkapital, Normalbalanse.Kredit),
            Gruppe(22, "Langsiktig gjeld", "Long-term liabilities", Kontotype.Gjeld, Normalbalanse.Kredit),
            Gruppe(23, "Annen langsiktig gjeld", "Other long-term liabilities", Kontotype.Gjeld, Normalbalanse.Kredit),
            Gruppe(24, "Leverandorgjeld", "Accounts payable", Kontotype.Gjeld, Normalbalanse.Kredit),
            Gruppe(25, "Skattetrekk og offentlige avgifter", "Tax withholding and public charges", Kontotype.Gjeld, Normalbalanse.Kredit),
            Gruppe(26, "Skyldig merverdiavgift", "VAT payable", Kontotype.Gjeld, Normalbalanse.Kredit),
            Gruppe(27, "Skyldig arbeidsgiveravgift, lonnsrelatert gjeld", "Employer tax, payroll liabilities", Kontotype.Gjeld, Normalbalanse.Kredit),
            Gruppe(28, "Annen kortsiktig gjeld", "Other short-term liabilities", Kontotype.Gjeld, Normalbalanse.Kredit),
            Gruppe(29, "Annen gjeld og egenkapitalposter", "Other liabilities and equity items", Kontotype.Gjeld, Normalbalanse.Kredit),
            Gruppe(30, "Salgsinntekt, avgiftspliktig", "Sales revenue, VAT liable", Kontotype.Inntekt, Normalbalanse.Kredit),
            Gruppe(31, "Salgsinntekt, avgiftsfri", "Sales revenue, VAT exempt", Kontotype.Inntekt, Normalbalanse.Kredit),
            Gruppe(32, "Ovrige salgsinntekter", "Other sales revenue", Kontotype.Inntekt, Normalbalanse.Kredit),
            Gruppe(36, "Leieinntekt", "Rental income", Kontotype.Inntekt, Normalbalanse.Kredit),
            Gruppe(39, "Annen driftsinntekt", "Other operating income", Kontotype.Inntekt, Normalbalanse.Kredit),
            Gruppe(40, "Varekjop", "Purchases", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(43, "Innkjop for viderefakturering", "Purchases for resale", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(50, "Lonn til ansatte", "Salaries", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(51, "Feriepenger", "Holiday pay", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(54, "Arbeidsgiveravgift", "Employer tax", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(59, "Annen personalkostnad", "Other personnel cost", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(60, "Avskrivning", "Depreciation", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(61, "Frakt og transport", "Freight and transport", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(63, "Leie lokaler", "Rent premises", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(64, "Leie maskiner og inventar", "Lease machinery and fixtures", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(65, "Verktoy, inventar uten aktiveringsplikt", "Tools, non-capitalized fixtures", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(66, "Reparasjon og vedlikehold", "Repair and maintenance", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(67, "Fremmedtjenester", "External services", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(68, "Kontorrekvisita", "Office supplies", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(69, "Telefon, porto, data", "Telephone, postage, IT", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(70, "Reisekostnad", "Travel expenses", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(71, "Bilkostnad", "Vehicle expenses", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(73, "Salgskostnad, reklame", "Sales and marketing", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(74, "Kontingenter", "Subscriptions and memberships", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(75, "Forsikringspremie", "Insurance premiums", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(77, "Annen kostnad", "Other expenses", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(78, "Tap pa fordringer", "Bad debts", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(80, "Finansinntekt", "Financial income", Kontotype.Inntekt, Normalbalanse.Kredit),
            Gruppe(84, "Finanskostnad", "Financial expenses", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(85, "Annen finanskostnad", "Other financial expenses", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(88, "Arsresultat", "Annual result", Kontotype.Kostnad, Normalbalanse.Debet),
            Gruppe(89, "Skattekostnad", "Tax expense", Kontotype.Kostnad, Normalbalanse.Debet),
        };
    }

    public static List<(string Kontonummer, string Navn, string? NavnEn, Kontotype Kontotype, string? StandardMvaKode, int Gruppekode, bool ErSystemkonto)> HentStandardKontoer()
    {
        return new()
        {
            ("1000", "Forskning og utvikling", "Research and development", Kontotype.Eiendel, null, 10, true),
            ("1070", "Utsatt skattefordel", "Deferred tax asset", Kontotype.Eiendel, null, 10, true),
            ("1100", "Bygninger", "Buildings", Kontotype.Eiendel, null, 11, true),
            ("1200", "Maskiner og anlegg", "Machinery and equipment", Kontotype.Eiendel, null, 12, true),
            ("1250", "Inventar", "Fixtures", Kontotype.Eiendel, null, 12, true),
            ("1280", "Kontormaskiner", "Office machines", Kontotype.Eiendel, null, 12, true),
            ("1300", "Investeringer i datterselskap", "Investments in subsidiaries", Kontotype.Eiendel, null, 13, true),
            ("1350", "Investeringer i aksjer og andeler", "Investments in shares", Kontotype.Eiendel, null, 13, true),
            ("1400", "Varelager", "Inventory", Kontotype.Eiendel, null, 14, true),
            ("1500", "Kundefordringer", "Accounts receivable", Kontotype.Eiendel, null, 15, true),
            ("1570", "Andre kortsiktige fordringer", "Other short-term receivables", Kontotype.Eiendel, null, 15, true),
            ("1600", "Inngaende merverdiavgift (mellomkonto)", "Input VAT (interim account)", Kontotype.Eiendel, null, 16, true),
            ("1700", "Forskuddsbetalte kostnader", "Prepaid expenses", Kontotype.Eiendel, null, 17, true),
            ("1900", "Kasse", "Cash", Kontotype.Eiendel, null, 19, true),
            ("1920", "Bankinnskudd", "Bank deposits", Kontotype.Eiendel, null, 19, true),
            ("1930", "Bankinnskudd skattetrekk", "Bank deposits tax withholding", Kontotype.Eiendel, null, 19, true),
            ("2000", "Aksjekapital", "Share capital", Kontotype.Egenkapital, null, 20, true),
            ("2020", "Overkurs", "Share premium", Kontotype.Egenkapital, null, 20, true),
            ("2050", "Annen innskutt egenkapital", "Other contributed equity", Kontotype.Egenkapital, null, 20, true),
            ("2080", "Udekket tap", "Uncovered loss", Kontotype.Egenkapital, null, 20, true),
            ("2100", "Fond", "Reserves", Kontotype.Egenkapital, null, 21, true),
            ("2120", "Annen egenkapital", "Other equity", Kontotype.Egenkapital, null, 21, true),
            ("2200", "Langsiktige lan", "Long-term loans", Kontotype.Gjeld, null, 22, true),
            ("2400", "Leverandorgjeld", "Accounts payable", Kontotype.Gjeld, null, 24, true),
            ("2500", "Skattetrekk", "Tax withholding", Kontotype.Gjeld, null, 25, true),
            ("2600", "Utgaende merverdiavgift", "Output VAT", Kontotype.Gjeld, null, 26, true),
            ("2610", "Inngaende merverdiavgift", "Input VAT", Kontotype.Gjeld, null, 26, true),
            ("2700", "Oppgjorskonto merverdiavgift", "VAT settlement account", Kontotype.Gjeld, null, 27, true),
            ("2701", "Inngaende merverdiavgift, alminnelig sats", "Input VAT, standard rate", Kontotype.Gjeld, null, 27, true),
            ("2702", "Inngaende merverdiavgift, middels sats", "Input VAT, medium rate", Kontotype.Gjeld, null, 27, true),
            ("2703", "Inngaende merverdiavgift, lav sats", "Input VAT, low rate", Kontotype.Gjeld, null, 27, true),
            ("2710", "Utgaende merverdiavgift, alminnelig sats", "Output VAT, standard rate", Kontotype.Gjeld, null, 27, true),
            ("2711", "Utgaende merverdiavgift, middels sats", "Output VAT, medium rate", Kontotype.Gjeld, null, 27, true),
            ("2712", "Utgaende merverdiavgift, lav sats", "Output VAT, low rate", Kontotype.Gjeld, null, 27, true),
            ("2770", "Skyldig arbeidsgiveravgift av feriepenger", "Employer tax on holiday pay", Kontotype.Gjeld, null, 27, true),
            ("2780", "Palopte feriepenger", "Accrued holiday pay", Kontotype.Gjeld, null, 27, true),
            ("2800", "Avsatt utbytte", "Dividend payable", Kontotype.Gjeld, null, 28, true),
            ("2900", "Annen kortsiktig gjeld", "Other short-term liabilities", Kontotype.Gjeld, null, 29, true),
            ("3000", "Salgsinntekt, avgiftspliktig", "Sales revenue, VAT liable", Kontotype.Inntekt, "3", 30, true),
            ("3100", "Salgsinntekt, avgiftsfri", "Sales revenue, VAT exempt", Kontotype.Inntekt, "5", 31, true),
            ("3200", "Salgsinntekt, utenfor avgiftsomradet", "Sales revenue, outside VAT scope", Kontotype.Inntekt, "6", 32, true),
            ("3600", "Leieinntekt", "Rental income", Kontotype.Inntekt, "3", 36, true),
            ("3900", "Annen driftsinntekt", "Other operating income", Kontotype.Inntekt, null, 39, true),
            ("4000", "Varekjop", "Purchases", Kontotype.Kostnad, "1", 40, true),
            ("4300", "Innkjop for viderefakturering", "Purchases for resale", Kontotype.Kostnad, "1", 43, true),
            ("5000", "Lonn til ansatte", "Salaries", Kontotype.Kostnad, null, 50, true),
            ("5100", "Feriepenger", "Holiday pay", Kontotype.Kostnad, null, 51, true),
            ("5400", "Arbeidsgiveravgift", "Employer tax", Kontotype.Kostnad, null, 54, true),
            ("5420", "Arbeidsgiveravgift av feriepenger", "Employer tax on holiday pay", Kontotype.Kostnad, null, 54, true),
            ("5900", "Annen personalkostnad", "Other personnel cost", Kontotype.Kostnad, null, 59, true),
            ("6000", "Avskrivning", "Depreciation", Kontotype.Kostnad, null, 60, true),
            ("6100", "Frakt og transport", "Freight and transport", Kontotype.Kostnad, "1", 61, true),
            ("6300", "Leie lokaler", "Rent premises", Kontotype.Kostnad, "1", 63, true),
            ("6400", "Leie maskiner og inventar", "Lease machinery and fixtures", Kontotype.Kostnad, "1", 64, true),
            ("6500", "Verktoy, inventar uten aktiveringsplikt", "Tools, non-capitalized fixtures", Kontotype.Kostnad, "1", 65, true),
            ("6600", "Reparasjon og vedlikehold", "Repair and maintenance", Kontotype.Kostnad, "1", 66, true),
            ("6700", "Fremmedtjenester (revisor, advokat)", "External services (auditor, lawyer)", Kontotype.Kostnad, "1", 67, true),
            ("6800", "Kontorrekvisita", "Office supplies", Kontotype.Kostnad, "1", 68, true),
            ("6900", "Telefon, porto, data", "Telephone, postage, IT", Kontotype.Kostnad, "1", 69, true),
            ("7000", "Reisekostnad", "Travel expenses", Kontotype.Kostnad, null, 70, true),
            ("7100", "Bilkostnad", "Vehicle expenses", Kontotype.Kostnad, "1", 71, true),
            ("7300", "Salgskostnad, reklame", "Sales and marketing", Kontotype.Kostnad, "1", 73, true),
            ("7400", "Kontingenter", "Subscriptions and memberships", Kontotype.Kostnad, null, 74, true),
            ("7500", "Forsikringspremie", "Insurance premiums", Kontotype.Kostnad, null, 75, true),
            ("7700", "Annen kostnad", "Other expenses", Kontotype.Kostnad, "1", 77, true),
            ("7800", "Tap pa fordringer", "Bad debts", Kontotype.Kostnad, null, 78, true),
            ("8000", "Finansinntekt", "Financial income", Kontotype.Inntekt, null, 80, true),
            ("8050", "Renteinntekt", "Interest income", Kontotype.Inntekt, null, 80, true),
            ("8100", "Annen finansinntekt", "Other financial income", Kontotype.Inntekt, null, 80, false),
            ("8150", "Gevinst ved salg av aksjer", "Gain on sale of shares", Kontotype.Inntekt, null, 80, false),
            ("8400", "Finanskostnad", "Financial expenses", Kontotype.Kostnad, null, 84, true),
            ("8450", "Rentekostnad", "Interest expense", Kontotype.Kostnad, null, 84, true),
            ("8500", "Annen finanskostnad", "Other financial expenses", Kontotype.Kostnad, null, 85, true),
            ("8800", "Arsresultat", "Annual result", Kontotype.Kostnad, null, 88, true),
            ("8900", "Skattekostnad", "Tax expense", Kontotype.Kostnad, null, 89, true),
            ("8960", "Endring utsatt skatt", "Change in deferred tax", Kontotype.Kostnad, null, 89, true),
        };
    }

    public static List<(string Kode, string Beskrivelse, string? BeskrivelseEn, string StandardTaxCode, decimal Sats, MvaRetning Retning)> HentStandardMvaKoder()
    {
        return new()
        {
            ("0", "Ingen MVA-behandling", "No VAT", "0", 0m, MvaRetning.Ingen),
            ("1", "Inngaende MVA 25%", "Input VAT 25%", "1", 25m, MvaRetning.Inngaende),
            ("11", "Inngaende MVA 15%", "Input VAT 15%", "11", 15m, MvaRetning.Inngaende),
            ("13", "Inngaende MVA 12%", "Input VAT 12%", "13", 12m, MvaRetning.Inngaende),
            ("3", "Utgaende MVA 25%", "Output VAT 25%", "3", 25m, MvaRetning.Utgaende),
            ("31", "Utgaende MVA 15%", "Output VAT 15%", "31", 15m, MvaRetning.Utgaende),
            ("33", "Utgaende MVA 12%", "Output VAT 12%", "33", 12m, MvaRetning.Utgaende),
            ("5", "Utgaende MVA 0% (utforsel)", "Output VAT 0% (export)", "5", 0m, MvaRetning.Utgaende),
            ("6", "Utenfor MVA-omradet", "Outside VAT scope", "6", 0m, MvaRetning.Ingen),
            ("14", "Inngaende MVA innforsel 25%", "Input VAT import 25%", "14", 25m, MvaRetning.Inngaende),
            ("15", "Inngaende MVA innforsel 15%", "Input VAT import 15%", "15", 15m, MvaRetning.Inngaende),
        };
    }

    public static void Seed(Regnskap.Infrastructure.Persistence.RegnskapDbContext db)
    {
        if (db.Kontogrupper.Any()) return;

        var grupper = HentKontogrupper();
        db.Kontogrupper.AddRange(grupper);
        db.SaveChanges();

        var standardKontoer = HentStandardKontoer();
        foreach (var k in standardKontoer)
        {
            var gruppe = grupper.First(g => g.Gruppekode == k.Gruppekode);
            db.Kontoer.Add(new Konto
            {
                Id = Guid.NewGuid(),
                Kontonummer = k.Kontonummer,
                Navn = k.Navn,
                NavnEn = k.NavnEn,
                Kontotype = k.Kontotype,
                KontogruppeId = gruppe.Id,
                StandardAccountId = k.Kontonummer,
                StandardMvaKode = k.StandardMvaKode,
                ErSystemkonto = k.ErSystemkonto,
                ErAktiv = true
            });
        }
        db.SaveChanges();

        var mvaKoder = HentStandardMvaKoder();
        foreach (var m in mvaKoder)
        {
            db.MvaKoder.Add(new MvaKode
            {
                Id = Guid.NewGuid(),
                Kode = m.Kode,
                Beskrivelse = m.Beskrivelse,
                BeskrivelseEn = m.BeskrivelseEn,
                StandardTaxCode = m.StandardTaxCode,
                Sats = m.Sats,
                Retning = m.Retning,
                ErAktiv = true
            });
        }
        db.SaveChanges();
    }

    private static Kontogruppe Gruppe(int kode, string navn, string navnEn, Kontotype type, Normalbalanse balanse)
    {
        return new Kontogruppe
        {
            Id = Guid.NewGuid(),
            Gruppekode = kode,
            Navn = navn,
            NavnEn = navnEn,
            Kontotype = type,
            Normalbalanse = balanse,
            ErSystemgruppe = true
        };
    }
}
