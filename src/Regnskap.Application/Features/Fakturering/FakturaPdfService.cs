namespace Regnskap.Application.Features.Fakturering;

using System.Globalization;
using System.Text;
using Regnskap.Domain.Features.Fakturering;

/// <summary>
/// Genererer faktura-PDF via HTML-layout.
/// Returnerer HTML som byte-array (UTF-8).
/// I produksjon kan dette utvides med en HTML-til-PDF konverterer.
/// </summary>
public class FakturaPdfService : IFakturaPdfService
{
    private readonly IFakturaRepository _fakturaRepo;
    private static readonly CultureInfo No = new("nb-NO");

    public FakturaPdfService(IFakturaRepository fakturaRepo)
    {
        _fakturaRepo = fakturaRepo;
    }

    public async Task<byte[]> GenererPdfAsync(Guid fakturaId, CancellationToken ct = default)
    {
        var faktura = await _fakturaRepo.HentMedLinjerAsync(fakturaId, ct)
            ?? throw new FakturaKundeIkkeFunnetException(fakturaId);

        var selskapsinfo = await _fakturaRepo.HentSelskapsinfoAsync(ct);

        var html = GenererHtml(faktura, selskapsinfo);

        faktura.PdfFilsti = $"fakturaer/{faktura.FakturaId ?? faktura.Id.ToString()}.html";
        await _fakturaRepo.LagreEndringerAsync(ct);

        return Encoding.UTF8.GetBytes(html);
    }

    private static string GenererHtml(Faktura faktura, Selskapsinfo? selskap)
    {
        var sb = new StringBuilder();
        var erKreditnota = faktura.Dokumenttype == FakturaDokumenttype.Kreditnota;
        var dokumentNavn = erKreditnota ? "KREDITNOTA" : "FAKTURA";

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='no'><head><meta charset='UTF-8'>");
        sb.AppendLine($"<title>{dokumentNavn} {faktura.FakturaId}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; font-size: 12px; margin: 40px; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
        sb.AppendLine("th, td { padding: 6px 8px; text-align: left; border-bottom: 1px solid #ddd; }");
        sb.AppendLine("th { background-color: #f5f5f5; font-weight: bold; }");
        sb.AppendLine(".right { text-align: right; }");
        sb.AppendLine(".header { display: flex; justify-content: space-between; }");
        sb.AppendLine(".total-section { margin-top: 20px; }");
        sb.AppendLine(".total-section td { border: none; }");
        sb.AppendLine(".payment-info { margin-top: 30px; padding: 15px; background: #f9f9f9; }");
        sb.AppendLine("</style></head><body>");

        // Header
        sb.AppendLine("<div class='header'>");
        sb.AppendLine("<div>");
        if (selskap != null)
        {
            sb.AppendLine($"<h2>{selskap.Navn}</h2>");
            sb.AppendLine($"<p>{selskap.Adresse1}<br>{selskap.Postnummer} {selskap.Poststed}</p>");
            sb.AppendLine($"<p>Org.nr: {selskap.Organisasjonsnummer}{(selskap.ErMvaRegistrert ? " MVA" : "")}</p>");
            if (!string.IsNullOrWhiteSpace(selskap.Telefon))
                sb.AppendLine($"<p>Tlf: {selskap.Telefon}</p>");
            if (!string.IsNullOrWhiteSpace(selskap.Epost))
                sb.AppendLine($"<p>E-post: {selskap.Epost}</p>");
        }
        sb.AppendLine("</div>");

        sb.AppendLine("<div>");
        sb.AppendLine($"<h1>{dokumentNavn}</h1>");
        sb.AppendLine($"<p><strong>Nr:</strong> {faktura.FakturaId}</p>");
        sb.AppendLine($"<p><strong>Dato:</strong> {faktura.Fakturadato?.ToString("dd.MM.yyyy")}</p>");
        sb.AppendLine($"<p><strong>Forfallsdato:</strong> {faktura.Forfallsdato?.ToString("dd.MM.yyyy")}</p>");
        if (faktura.Leveringsdato.HasValue)
            sb.AppendLine($"<p><strong>Leveringsdato:</strong> {faktura.Leveringsdato.Value:dd.MM.yyyy}</p>");
        sb.AppendLine("</div></div>");

        // Kundeinfo
        var kunde = faktura.Kunde;
        if (kunde != null)
        {
            sb.AppendLine("<div style='margin-top:20px'>");
            sb.AppendLine($"<p><strong>{kunde.Navn}</strong></p>");
            if (!string.IsNullOrWhiteSpace(kunde.Adresse1))
                sb.AppendLine($"<p>{kunde.Adresse1}</p>");
            if (!string.IsNullOrWhiteSpace(kunde.Postnummer))
                sb.AppendLine($"<p>{kunde.Postnummer} {kunde.Poststed}</p>");
            sb.AppendLine($"<p>Kundenr: {kunde.Kundenummer}</p>");
            sb.AppendLine("</div>");
        }

        // Referanser
        if (!string.IsNullOrWhiteSpace(faktura.Bestillingsnummer))
            sb.AppendLine($"<p><strong>Bestillingsnr:</strong> {faktura.Bestillingsnummer}</p>");
        if (!string.IsNullOrWhiteSpace(faktura.KjopersReferanse))
            sb.AppendLine($"<p><strong>Deres ref:</strong> {faktura.KjopersReferanse}</p>");
        if (!string.IsNullOrWhiteSpace(faktura.VaarReferanse))
            sb.AppendLine($"<p><strong>Vaar ref:</strong> {faktura.VaarReferanse}</p>");

        if (erKreditnota && faktura.KreditertFaktura != null)
            sb.AppendLine($"<p><strong>Krediterer faktura:</strong> {faktura.KreditertFaktura.FakturaId}</p>");

        // Linjetabell
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine("<th>Linje</th><th>Beskrivelse</th><th class='right'>Antall</th>");
        sb.AppendLine("<th>Enhet</th><th class='right'>Pris</th><th class='right'>Rabatt</th>");
        sb.AppendLine("<th class='right'>Netto</th><th class='right'>MVA%</th><th class='right'>MVA</th>");
        sb.AppendLine("</tr></thead><tbody>");

        foreach (var linje in faktura.Linjer.OrderBy(l => l.Linjenummer))
        {
            var rabatt = linje.RabattBelop.HasValue ? linje.RabattBelop.Value.Verdi.ToString("N2", No) : "";
            sb.AppendLine($"<tr>");
            sb.AppendLine($"<td>{linje.Linjenummer}</td>");
            sb.AppendLine($"<td>{linje.Beskrivelse}</td>");
            sb.AppendLine($"<td class='right'>{linje.Antall:N2}</td>");
            sb.AppendLine($"<td>{linje.Enhet}</td>");
            sb.AppendLine($"<td class='right'>{linje.Enhetspris.Verdi.ToString("N2", No)}</td>");
            sb.AppendLine($"<td class='right'>{rabatt}</td>");
            sb.AppendLine($"<td class='right'>{linje.Nettobelop.Verdi.ToString("N2", No)}</td>");
            sb.AppendLine($"<td class='right'>{linje.MvaSats:N0}%</td>");
            sb.AppendLine($"<td class='right'>{linje.MvaBelop.Verdi.ToString("N2", No)}</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</tbody></table>");

        // MVA-spesifikasjon
        if (faktura.MvaLinjer.Any())
        {
            sb.AppendLine("<div class='total-section'>");
            sb.AppendLine("<h3>MVA-spesifikasjon</h3>");
            sb.AppendLine("<table style='width:50%'>");
            sb.AppendLine("<tr><th>MVA-sats</th><th class='right'>Grunnlag</th><th class='right'>MVA</th></tr>");
            foreach (var ml in faktura.MvaLinjer)
            {
                sb.AppendLine($"<tr><td>{ml.MvaSats:N0}%</td>");
                sb.AppendLine($"<td class='right'>{ml.Grunnlag.Verdi.ToString("N2", No)}</td>");
                sb.AppendLine($"<td class='right'>{ml.MvaBelop.Verdi.ToString("N2", No)}</td></tr>");
            }
            sb.AppendLine("</table></div>");
        }

        // Totaler
        sb.AppendLine("<div class='total-section'>");
        sb.AppendLine("<table style='width:50%; margin-left:auto'>");
        sb.AppendLine($"<tr><td><strong>Sum ekskl. MVA</strong></td><td class='right'>{faktura.BelopEksMva.Verdi.ToString("N2", No)}</td></tr>");
        sb.AppendLine($"<tr><td><strong>MVA</strong></td><td class='right'>{faktura.MvaBelop.Verdi.ToString("N2", No)}</td></tr>");
        sb.AppendLine($"<tr><td><strong>Sum inkl. MVA</strong></td><td class='right'><strong>{faktura.BelopInklMva.Verdi.ToString("N2", No)}</strong></td></tr>");
        sb.AppendLine("</table></div>");

        // Betalingsinfo
        if (!erKreditnota)
        {
            sb.AppendLine("<div class='payment-info'>");
            sb.AppendLine("<h3>Betalingsinformasjon</h3>");
            if (!string.IsNullOrWhiteSpace(faktura.Bankkontonummer))
                sb.AppendLine($"<p><strong>Konto:</strong> {faktura.Bankkontonummer}</p>");
            if (!string.IsNullOrWhiteSpace(faktura.KidNummer))
                sb.AppendLine($"<p><strong>KID:</strong> {faktura.KidNummer}</p>");
            sb.AppendLine($"<p><strong>Forfallsdato:</strong> {faktura.Forfallsdato?.ToString("dd.MM.yyyy")}</p>");
            sb.AppendLine($"<p><strong>A betale:</strong> {faktura.BelopInklMva.Verdi.ToString("N2", No)}</p>");
            sb.AppendLine("</div>");
        }

        // Merknad
        if (!string.IsNullOrWhiteSpace(faktura.Merknad))
            sb.AppendLine($"<p style='margin-top:20px'><em>{faktura.Merknad}</em></p>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}
