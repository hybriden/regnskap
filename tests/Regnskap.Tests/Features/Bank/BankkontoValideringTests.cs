using FluentAssertions;
using Regnskap.Api.Features.Bank;

namespace Regnskap.Tests.Features.Bank;

public class BankkontoValideringTests
{
    [Theory]
    [InlineData("86011117947", true)]   // Gyldig DNB-konto
    [InlineData("12345678903", true)]   // Gyldig testkonto
    [InlineData("00000000000", true)]   // MOD11: sum=0, rest=0, kontrollsiffer=0
    [InlineData("12345678901", false)]  // Feil kontrollsiffer
    [InlineData("1234567890", false)]   // For kort
    [InlineData("123456789012", false)] // For lang
    [InlineData("1234567890A", false)]  // Ikke-numerisk
    [InlineData("", false)]
    public void ValiderBankkontonummer_Scenarier(string kontonummer, bool forventet)
    {
        BankkontoController.ValiderBankkontonummer(kontonummer).Should().Be(forventet);
    }

    [Fact]
    public void ValiderBankkontonummer_MedPunktum_FjernerFormattering()
    {
        // "8601.11.17947" should be valid when dots are removed
        BankkontoController.ValiderBankkontonummer("8601.11.17947").Should().BeTrue();
    }
}
