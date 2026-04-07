namespace Regnskap.Application.Features.Kundereskontro;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Kundereskontro;

public class KundeService : IKundeService
{
    private readonly IKundeReskontroRepository _repo;

    public KundeService(IKundeReskontroRepository repo)
    {
        _repo = repo;
    }

    public async Task<KundeDto> OpprettAsync(OpprettKundeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Kundenummer))
            throw new ArgumentException("Kundenummer er pakreves.");
        if (string.IsNullOrWhiteSpace(request.Navn))
            throw new ArgumentException("Navn er pakreves.");
        if (request.Kundenummer.Length > 20)
            throw new ArgumentException("Kundenummer kan maks vaere 20 tegn.");

        if (await _repo.KundenummerEksistererAsync(request.Kundenummer, ct))
            throw new KundenummerEksistererException(request.Kundenummer);

        if (request.ErBedrift && request.Landkode == "NO" && string.IsNullOrWhiteSpace(request.Organisasjonsnummer))
            throw new ArgumentException("Organisasjonsnummer er pakreves for norske bedriftskunder.");

        if (request.Organisasjonsnummer is not null && !System.Text.RegularExpressions.Regex.IsMatch(request.Organisasjonsnummer, @"^\d{9}$"))
            throw new ArgumentException("Organisasjonsnummer ma vaere 9 siffer.");

        if (request.Fodselsnummer is not null && !System.Text.RegularExpressions.Regex.IsMatch(request.Fodselsnummer, @"^\d{11}$"))
            throw new ArgumentException("Fodselsnummer ma vaere 11 siffer.");

        if (request.Betalingsbetingelse == KundeBetalingsbetingelse.Egendefinert &&
            (request.EgendefinertBetalingsfrist is null || request.EgendefinertBetalingsfrist < 1 || request.EgendefinertBetalingsfrist > 365))
            throw new ArgumentException("EgendefinertBetalingsfrist ma vaere mellom 1 og 365.");

        if (request.Kredittgrense.HasValue && request.Kredittgrense.Value < 0)
            throw new ArgumentException("Kredittgrense ma vaere >= 0.");

        var kunde = new Kunde
        {
            Id = Guid.NewGuid(),
            Kundenummer = request.Kundenummer,
            Navn = request.Navn,
            ErBedrift = request.ErBedrift,
            Organisasjonsnummer = request.Organisasjonsnummer,
            Fodselsnummer = request.Fodselsnummer,
            Adresse1 = request.Adresse1,
            Adresse2 = request.Adresse2,
            Postnummer = request.Postnummer,
            Poststed = request.Poststed,
            Landkode = request.Landkode,
            Kontaktperson = request.Kontaktperson,
            Telefon = request.Telefon,
            Epost = request.Epost,
            Betalingsbetingelse = request.Betalingsbetingelse,
            EgendefinertBetalingsfrist = request.EgendefinertBetalingsfrist,
            StandardKontoId = request.StandardKontoId,
            StandardMvaKode = request.StandardMvaKode,
            Kredittgrense = new Belop(request.Kredittgrense ?? 0m),
            PeppolId = request.PeppolId,
            KanMottaEhf = request.KanMottaEhf,
        };

        await _repo.LeggTilKundeAsync(kunde, ct);
        await _repo.LagreEndringerAsync(ct);

        return MapToDto(kunde);
    }

    public async Task<KundeDto> OppdaterAsync(Guid id, OppdaterKundeRequest request, CancellationToken ct = default)
    {
        var kunde = await _repo.HentKundeAsync(id, ct)
            ?? throw new KundeIkkeFunnetException(id);

        kunde.Navn = request.Navn;
        kunde.Organisasjonsnummer = request.Organisasjonsnummer;
        kunde.Fodselsnummer = request.Fodselsnummer;
        kunde.Adresse1 = request.Adresse1;
        kunde.Adresse2 = request.Adresse2;
        kunde.Postnummer = request.Postnummer;
        kunde.Poststed = request.Poststed;
        kunde.Landkode = request.Landkode;
        kunde.Kontaktperson = request.Kontaktperson;
        kunde.Telefon = request.Telefon;
        kunde.Epost = request.Epost;
        kunde.Betalingsbetingelse = request.Betalingsbetingelse;
        kunde.EgendefinertBetalingsfrist = request.EgendefinertBetalingsfrist;
        kunde.StandardKontoId = request.StandardKontoId;
        kunde.StandardMvaKode = request.StandardMvaKode;
        kunde.Kredittgrense = new Belop(request.Kredittgrense ?? 0m);
        kunde.PeppolId = request.PeppolId;
        kunde.KanMottaEhf = request.KanMottaEhf;
        kunde.ErAktiv = request.ErAktiv;
        kunde.ErSperret = request.ErSperret;

        await _repo.OppdaterKundeAsync(kunde, ct);
        await _repo.LagreEndringerAsync(ct);

        return MapToDto(kunde);
    }

    public async Task<KundeDto> HentAsync(Guid id, CancellationToken ct = default)
    {
        var kunde = await _repo.HentKundeAsync(id, ct)
            ?? throw new KundeIkkeFunnetException(id);
        return MapToDto(kunde);
    }

    public async Task<(List<KundeDto> Data, int TotaltAntall)> SokAsync(KundeSokRequest request, CancellationToken ct = default)
    {
        var (data, totalt) = await _repo.SokKunderAsync(request.Query, request.Side, request.Antall, ct);
        return (data.Select(MapToDto).ToList(), totalt);
    }

    public async Task SlettAsync(Guid id, CancellationToken ct = default)
    {
        var kunde = await _repo.HentKundeAsync(id, ct)
            ?? throw new KundeIkkeFunnetException(id);

        kunde.IsDeleted = true;
        await _repo.OppdaterKundeAsync(kunde, ct);
        await _repo.LagreEndringerAsync(ct);
    }

    public async Task<decimal> HentSaldoAsync(Guid id, CancellationToken ct = default)
    {
        var kunde = await _repo.HentKundeAsync(id, ct)
            ?? throw new KundeIkkeFunnetException(id);

        var apnePoster = await _repo.HentApnePosterAsync(null, ct);
        return apnePoster
            .Where(f => f.KundeId == id)
            .Sum(f => f.GjenstaendeBelop.Verdi);
    }

    internal static KundeDto MapToDto(Kunde k) => new(
        k.Id,
        k.Kundenummer,
        k.Navn,
        k.ErBedrift,
        k.Organisasjonsnummer,
        k.Fodselsnummer,
        k.Adresse1,
        k.Adresse2,
        k.Postnummer,
        k.Poststed,
        k.Landkode,
        k.Kontaktperson,
        k.Telefon,
        k.Epost,
        k.Betalingsbetingelse,
        k.EgendefinertBetalingsfrist,
        k.StandardKontoId,
        k.StandardMvaKode,
        k.Kredittgrense.Verdi,
        k.PeppolId,
        k.KanMottaEhf,
        k.ErAktiv,
        k.ErSperret);
}
