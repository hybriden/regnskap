namespace Regnskap.Application.Features.Leverandorreskontro;

using Regnskap.Application.Common;
using Regnskap.Domain.Features.Leverandorreskontro;

public class LeverandorService : ILeverandorService
{
    private readonly ILeverandorReskontroRepository _repo;

    public LeverandorService(ILeverandorReskontroRepository repo)
    {
        _repo = repo;
    }

    public async Task<LeverandorDto> OpprettAsync(OpprettLeverandorRequest request, CancellationToken ct = default)
    {
        // Duplikatkontroll leverandornummer
        if (await _repo.LeverandornummerEksistererAsync(request.Leverandornummer, ct))
            throw new LeverandorDuplikatException("leverandornummer", request.Leverandornummer);

        // Duplikatkontroll organisasjonsnummer
        if (request.Organisasjonsnummer != null &&
            await _repo.OrganisasjonsnummerEksistererAsync(request.Organisasjonsnummer, ct))
            throw new LeverandorDuplikatException("organisasjonsnummer", request.Organisasjonsnummer);

        // Valider EgendefinertBetalingsfrist
        if (request.Betalingsbetingelse == Betalingsbetingelse.Egendefinert &&
            (request.EgendefinertBetalingsfrist is null or < 1 or > 365))
            throw new ArgumentException("EgendefinertBetalingsfrist ma vaere mellom 1 og 365 nar Betalingsbetingelse er Egendefinert.");

        var leverandor = new Leverandor
        {
            Id = Guid.NewGuid(),
            Leverandornummer = request.Leverandornummer,
            Navn = request.Navn,
            Organisasjonsnummer = request.Organisasjonsnummer,
            ErMvaRegistrert = request.ErMvaRegistrert,
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
            Bankkontonummer = request.Bankkontonummer,
            Iban = request.Iban,
            Bic = request.Bic,
            StandardKontoId = request.StandardKontoId,
            StandardMvaKode = request.StandardMvaKode
        };

        await _repo.LeggTilLeverandorAsync(leverandor, ct);
        await _repo.LagreEndringerAsync(ct);

        return MapToDto(leverandor);
    }

    public async Task<LeverandorDto> OppdaterAsync(Guid id, OppdaterLeverandorRequest request, CancellationToken ct = default)
    {
        var leverandor = await _repo.HentLeverandorAsync(id, ct)
            ?? throw new LeverandorIkkeFunnetException(id);

        // Duplikatkontroll org.nr (kun hvis endret)
        if (request.Organisasjonsnummer != null &&
            request.Organisasjonsnummer != leverandor.Organisasjonsnummer &&
            await _repo.OrganisasjonsnummerEksistererAsync(request.Organisasjonsnummer, ct))
            throw new LeverandorDuplikatException("organisasjonsnummer", request.Organisasjonsnummer);

        leverandor.Navn = request.Navn;
        leverandor.Organisasjonsnummer = request.Organisasjonsnummer;
        leverandor.ErMvaRegistrert = request.ErMvaRegistrert;
        leverandor.Adresse1 = request.Adresse1;
        leverandor.Adresse2 = request.Adresse2;
        leverandor.Postnummer = request.Postnummer;
        leverandor.Poststed = request.Poststed;
        leverandor.Landkode = request.Landkode;
        leverandor.Kontaktperson = request.Kontaktperson;
        leverandor.Telefon = request.Telefon;
        leverandor.Epost = request.Epost;
        leverandor.Betalingsbetingelse = request.Betalingsbetingelse;
        leverandor.EgendefinertBetalingsfrist = request.EgendefinertBetalingsfrist;
        leverandor.Bankkontonummer = request.Bankkontonummer;
        leverandor.Iban = request.Iban;
        leverandor.Bic = request.Bic;
        leverandor.StandardKontoId = request.StandardKontoId;
        leverandor.StandardMvaKode = request.StandardMvaKode;
        leverandor.ErAktiv = request.ErAktiv;
        leverandor.ErSperret = request.ErSperret;
        leverandor.Notat = request.Notat;

        await _repo.OppdaterLeverandorAsync(leverandor, ct);
        await _repo.LagreEndringerAsync(ct);

        return MapToDto(leverandor);
    }

    public async Task<LeverandorDto> HentAsync(Guid id, CancellationToken ct = default)
    {
        var leverandor = await _repo.HentLeverandorAsync(id, ct)
            ?? throw new LeverandorIkkeFunnetException(id);
        return MapToDto(leverandor);
    }

    public async Task<PagedResult<LeverandorDto>> SokAsync(LeverandorSokRequest request, CancellationToken ct = default)
    {
        var leverandorer = await _repo.SokLeverandorerAsync(request.Query, request.Side, request.Antall, ct);
        var total = await _repo.TellLeverandorerAsync(request.Query, ct);

        return new PagedResult<LeverandorDto>(
            leverandorer.Select(MapToDto).ToList(),
            total,
            request.Side,
            request.Antall);
    }

    public async Task SlettAsync(Guid id, CancellationToken ct = default)
    {
        var leverandor = await _repo.HentLeverandorAsync(id, ct)
            ?? throw new LeverandorIkkeFunnetException(id);

        leverandor.IsDeleted = true;
        await _repo.OppdaterLeverandorAsync(leverandor, ct);
        await _repo.LagreEndringerAsync(ct);
    }

    internal static LeverandorDto MapToDto(Leverandor l) => new(
        l.Id,
        l.Leverandornummer,
        l.Navn,
        l.Organisasjonsnummer,
        l.ErMvaRegistrert,
        l.Adresse1,
        l.Postnummer,
        l.Poststed,
        l.Landkode,
        l.Kontaktperson,
        l.Epost,
        l.Betalingsbetingelse,
        l.Bankkontonummer,
        l.Iban,
        l.ErAktiv,
        l.ErSperret
    );
}
