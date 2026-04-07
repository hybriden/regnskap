namespace Regnskap.Application.Features.Kundereskontro;

using Regnskap.Domain.Features.Kundereskontro;

public class KidService : IKidService
{
    public string Generer(string kundenummer, int fakturanummer, KidAlgoritme algoritme)
    {
        return KidGenerator.Generer(kundenummer, fakturanummer, algoritme);
    }

    public bool Valider(string kid, KidAlgoritme algoritme)
    {
        return KidGenerator.Valider(kid, algoritme);
    }
}
