using System.Globalization;
using System.Text;
using System.Text.Json;
using Regnskap.Domain.Features.Kontoplan;

namespace Regnskap.Application.Features.Kontoplan;

/// <summary>
/// Service for import og eksport av kontoplan.
/// Stotter CSV, JSON og SAF-T (GeneralLedgerAccounts) format.
/// </summary>
public class KontoplanImportExportService : IKontoplanImportExportService
{
    private readonly IKontoplanRepository _repository;
    private readonly IKontoService _kontoService;

    public KontoplanImportExportService(IKontoplanRepository repository, IKontoService kontoService)
    {
        _repository = repository;
        _kontoService = kontoService;
    }

    // --- Eksport ---

    public async Task<byte[]> EksporterCsvAsync(CancellationToken ct = default)
    {
        var kontoer = await _repository.HentKontoerAsync(ct: ct);
        var sb = new StringBuilder();
        sb.AppendLine("Kontonummer;Navn;NavnEn;Kontotype;Normalbalanse;Kontoklasse;Gruppekode;StandardAccountId;GrupperingsKategori;GrupperingsKode;ErAktiv;ErSystemkonto;ErBokforbar;StandardMvaKode;Beskrivelse;KreverAvdeling;KreverProsjekt");

        foreach (var k in kontoer)
        {
            sb.AppendLine(string.Join(";",
                k.Kontonummer,
                EscapeCsv(k.Navn),
                EscapeCsv(k.NavnEn ?? ""),
                k.Kontotype,
                k.Normalbalanse,
                (int)k.Kontoklasse,
                int.Parse(k.Kontonummer[..2]),
                k.StandardAccountId,
                k.GrupperingsKategori?.ToString() ?? "",
                k.GrupperingsKode ?? "",
                k.ErAktiv,
                k.ErSystemkonto,
                k.ErBokforbar,
                k.StandardMvaKode ?? "",
                EscapeCsv(k.Beskrivelse ?? ""),
                k.KreverAvdeling,
                k.KreverProsjekt));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task<byte[]> EksporterJsonAsync(CancellationToken ct = default)
    {
        var kontoer = await _repository.HentKontoerAsync(ct: ct);
        var eksportData = kontoer.Select(k => new KontoEksportDto(
            k.Kontonummer,
            k.Navn,
            k.NavnEn,
            k.Kontotype.ToString(),
            k.Normalbalanse.ToString(),
            (int)k.Kontoklasse,
            int.Parse(k.Kontonummer[..2]),
            k.StandardAccountId,
            k.GrupperingsKategori?.ToString(),
            k.GrupperingsKode,
            k.ErAktiv,
            k.ErSystemkonto,
            k.ErBokforbar,
            k.StandardMvaKode,
            k.Beskrivelse,
            k.KreverAvdeling,
            k.KreverProsjekt
        )).ToList();

        return JsonSerializer.SerializeToUtf8Bytes(eksportData, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public async Task<byte[]> EksporterSaftAsync(CancellationToken ct = default)
    {
        var kontoer = await _repository.HentKontoerAsync(ct: ct);
        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<GeneralLedgerAccounts>");

        foreach (var k in kontoer)
        {
            sb.AppendLine("  <Account>");
            sb.AppendLine($"    <AccountID>{k.Kontonummer}</AccountID>");
            sb.AppendLine($"    <AccountDescription>{EscapeXml(k.Navn)}</AccountDescription>");
            sb.AppendLine($"    <StandardAccountID>{k.StandardAccountId}</StandardAccountID>");
            sb.AppendLine($"    <AccountType>GL</AccountType>");

            if (k.GrupperingsKategori.HasValue)
            {
                sb.AppendLine($"    <GroupingCategory>{k.GrupperingsKategori}</GroupingCategory>");
                if (!string.IsNullOrEmpty(k.GrupperingsKode))
                    sb.AppendLine($"    <GroupingCode>{EscapeXml(k.GrupperingsKode)}</GroupingCode>");
            }

            sb.AppendLine("  </Account>");
        }

        sb.AppendLine("</GeneralLedgerAccounts>");
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    // --- Import ---

    public async Task<ImportResultat> ImporterJsonAsync(Stream data, CancellationToken ct = default)
    {
        var kontoer = await JsonSerializer.DeserializeAsync<List<KontoImportDto>>(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }, ct) ?? throw new ArgumentException("Ugyldig JSON-format.");

        return await ImporterKontoerAsync(kontoer, ct);
    }

    public async Task<ImportResultat> ImporterCsvAsync(Stream data, CancellationToken ct = default)
    {
        using var reader = new StreamReader(data, Encoding.UTF8);
        var header = await reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(header))
            throw new ArgumentException("CSV-filen er tom.");

        var kontoer = new List<KontoImportDto>();
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fields = line.Split(';');
            if (fields.Length < 7)
                throw new ArgumentException($"Ugyldig CSV-linje: {line}");

            kontoer.Add(new KontoImportDto(
                Kontonummer: fields[0].Trim(),
                Navn: fields[1].Trim().Trim('"'),
                NavnEn: string.IsNullOrEmpty(fields[2].Trim().Trim('"')) ? null : fields[2].Trim().Trim('"'),
                Kontotype: fields[3].Trim(),
                Gruppekode: int.Parse(fields[6].Trim()),
                StandardAccountId: fields[7].Trim(),
                GrupperingsKategori: string.IsNullOrEmpty(fields[8].Trim()) ? null : fields[8].Trim(),
                GrupperingsKode: string.IsNullOrEmpty(fields[9].Trim()) ? null : fields[9].Trim(),
                ErBokforbar: fields.Length > 12 && bool.TryParse(fields[12].Trim(), out var bokforbar) && bokforbar,
                StandardMvaKode: fields.Length > 13 ? (string.IsNullOrEmpty(fields[13].Trim()) ? null : fields[13].Trim()) : null,
                Beskrivelse: fields.Length > 14 ? (string.IsNullOrEmpty(fields[14].Trim().Trim('"')) ? null : fields[14].Trim().Trim('"')) : null,
                KreverAvdeling: fields.Length > 15 && bool.TryParse(fields[15].Trim(), out var avd) && avd,
                KreverProsjekt: fields.Length > 16 && bool.TryParse(fields[16].Trim(), out var prj) && prj
            ));
        }

        return await ImporterKontoerAsync(kontoer, ct);
    }

    private async Task<ImportResultat> ImporterKontoerAsync(List<KontoImportDto> kontoer, CancellationToken ct)
    {
        var opprettet = 0;
        var feil = new List<string>();

        foreach (var dto in kontoer)
        {
            try
            {
                if (!Enum.TryParse<Kontotype>(dto.Kontotype, true, out var kontotype))
                {
                    feil.Add($"{dto.Kontonummer}: Ugyldig kontotype '{dto.Kontotype}'.");
                    continue;
                }

                GrupperingsKategori? gk = null;
                if (!string.IsNullOrEmpty(dto.GrupperingsKategori))
                {
                    if (!Enum.TryParse<GrupperingsKategori>(dto.GrupperingsKategori, true, out var parsed))
                    {
                        feil.Add($"{dto.Kontonummer}: Ugyldig GrupperingsKategori '{dto.GrupperingsKategori}'.");
                        continue;
                    }
                    gk = parsed;
                }

                // Sjekk om kontoen allerede finnes
                if (await _repository.KontoFinnesAsync(dto.Kontonummer, ct))
                {
                    feil.Add($"{dto.Kontonummer}: Kontonummer er allerede i bruk. Hopper over.");
                    continue;
                }

                await _kontoService.OpprettKontoAsync(new OpprettKontoRequest(
                    dto.Kontonummer,
                    dto.Navn,
                    dto.NavnEn,
                    kontotype,
                    dto.Gruppekode,
                    dto.StandardAccountId,
                    gk,
                    dto.GrupperingsKode,
                    dto.ErBokforbar,
                    dto.StandardMvaKode,
                    dto.Beskrivelse,
                    null, // OverordnetKontonummer
                    dto.KreverAvdeling,
                    dto.KreverProsjekt
                ), ct);

                opprettet++;
            }
            catch (Exception ex)
            {
                feil.Add($"{dto.Kontonummer}: {ex.Message}");
            }
        }

        return new ImportResultat(kontoer.Count, opprettet, feil);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}

// --- DTOs ---

public record KontoEksportDto(
    string Kontonummer,
    string Navn,
    string? NavnEn,
    string Kontotype,
    string Normalbalanse,
    int Kontoklasse,
    int Gruppekode,
    string StandardAccountId,
    string? GrupperingsKategori,
    string? GrupperingsKode,
    bool ErAktiv,
    bool ErSystemkonto,
    bool ErBokforbar,
    string? StandardMvaKode,
    string? Beskrivelse,
    bool KreverAvdeling,
    bool KreverProsjekt);

public record KontoImportDto(
    string Kontonummer,
    string Navn,
    string? NavnEn,
    string Kontotype,
    int Gruppekode,
    string StandardAccountId,
    string? GrupperingsKategori,
    string? GrupperingsKode,
    bool ErBokforbar,
    string? StandardMvaKode,
    string? Beskrivelse,
    bool KreverAvdeling,
    bool KreverProsjekt);

public record ImportResultat(
    int TotaltAntall,
    int OpprettetAntall,
    List<string> Feil);

/// <summary>
/// Interface for import/eksport av kontoplan.
/// </summary>
public interface IKontoplanImportExportService
{
    Task<byte[]> EksporterCsvAsync(CancellationToken ct = default);
    Task<byte[]> EksporterJsonAsync(CancellationToken ct = default);
    Task<byte[]> EksporterSaftAsync(CancellationToken ct = default);
    Task<ImportResultat> ImporterJsonAsync(Stream data, CancellationToken ct = default);
    Task<ImportResultat> ImporterCsvAsync(Stream data, CancellationToken ct = default);
}
