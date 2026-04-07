namespace Regnskap.Application.Features.Kundereskontro;

using Regnskap.Domain.Features.Kundereskontro;

public interface IKidService
{
    string Generer(string kundenummer, int fakturanummer, KidAlgoritme algoritme);
    bool Valider(string kid, KidAlgoritme algoritme);
}
