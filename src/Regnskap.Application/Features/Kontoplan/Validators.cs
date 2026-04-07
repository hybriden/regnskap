using FluentValidation;
using Regnskap.Domain.Features.Kontoplan;

namespace Regnskap.Application.Features.Kontoplan;

public class OpprettKontoRequestValidator : AbstractValidator<OpprettKontoRequest>
{
    public OpprettKontoRequestValidator()
    {
        RuleFor(x => x.Kontonummer)
            .NotEmpty().WithMessage("Kontonummer er pakreved.")
            .Matches(@"^[1-8]\d{3,5}$").WithMessage("Kontonummer ma vaere 4-6 siffer og starte med 1-8.");

        RuleFor(x => x.Navn)
            .NotEmpty().WithMessage("Navn er pakreved.")
            .MaximumLength(200).WithMessage("Navn kan vaere maks 200 tegn.");

        RuleFor(x => x.Kontotype)
            .IsInEnum().WithMessage("Ugyldig kontotype.");

        RuleFor(x => x.Gruppekode)
            .InclusiveBetween(10, 89).WithMessage("Gruppekode ma vaere mellom 10 og 89.");

        RuleFor(x => x.StandardAccountId)
            .NotEmpty().WithMessage("StandardAccountId er pakreved.")
            .Matches(@"^\d{4}$").WithMessage("StandardAccountId ma vaere 4 siffer.");

        RuleFor(x => x.NavnEn)
            .MaximumLength(200).When(x => x.NavnEn != null);

        RuleFor(x => x.Beskrivelse)
            .MaximumLength(1000).When(x => x.Beskrivelse != null);

        RuleFor(x => x.GrupperingsKode)
            .MaximumLength(50).When(x => x.GrupperingsKode != null);
    }
}

public class OppdaterKontoRequestValidator : AbstractValidator<OppdaterKontoRequest>
{
    public OppdaterKontoRequestValidator()
    {
        RuleFor(x => x.Navn)
            .NotEmpty().WithMessage("Navn er pakreved.")
            .MaximumLength(200).WithMessage("Navn kan vaere maks 200 tegn.");

        RuleFor(x => x.NavnEn)
            .MaximumLength(200).When(x => x.NavnEn != null);

        RuleFor(x => x.Beskrivelse)
            .MaximumLength(1000).When(x => x.Beskrivelse != null);

        RuleFor(x => x.GrupperingsKode)
            .MaximumLength(50).When(x => x.GrupperingsKode != null);
    }
}

public class OpprettMvaKodeRequestValidator : AbstractValidator<OpprettMvaKodeRequest>
{
    public OpprettMvaKodeRequestValidator()
    {
        RuleFor(x => x.Kode)
            .NotEmpty().WithMessage("Kode er pakreved.")
            .MaximumLength(10).WithMessage("Kode kan vaere maks 10 tegn.");

        RuleFor(x => x.Beskrivelse)
            .NotEmpty().WithMessage("Beskrivelse er pakreved.")
            .MaximumLength(300).WithMessage("Beskrivelse kan vaere maks 300 tegn.");

        RuleFor(x => x.StandardTaxCode)
            .NotEmpty().WithMessage("StandardTaxCode er pakreved.")
            .MaximumLength(10).WithMessage("StandardTaxCode kan vaere maks 10 tegn.");

        RuleFor(x => x.Sats)
            .GreaterThanOrEqualTo(0).WithMessage("Sats ma vaere 0 eller hoyere.")
            .LessThanOrEqualTo(100).WithMessage("Sats kan vaere maks 100%.");

        RuleFor(x => x.Retning)
            .IsInEnum().WithMessage("Ugyldig MVA-retning.");
    }
}
