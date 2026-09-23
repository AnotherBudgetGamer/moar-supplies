using MoarSupplies.Models;
using SPTarkov.DI.Annotations;
using System.Security.Cryptography;
using System.Text;

namespace MoarSupplies.Services;

/// <summary>
/// Produces stable SPT identifiers without exposing them in user configuration.
/// </summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed class StimIdService
{
    private const string ItemIdNamespace = "com.anotherbudgetgamer.moarsupplies:item:";
    private const string DrinkItemIdNamespace = "com.anotherbudgetgamer.moarsupplies:drink:item:";
    private const string MedicalPackItemIdNamespace = "com.anotherbudgetgamer.moarsupplies:medical-pack:item:";
    private const string TraderAssortIdNamespace = "com.anotherbudgetgamer.moarsupplies:assort:";
    private const string BuffKeyPrefix = "MoarSupplies_";

    public StimRegistrationIds Create(string stimId)
    {
        string itemTemplateId = CreateMongoId(ItemIdNamespace + stimId);
        string traderAssortId = CreateMongoId(TraderAssortIdNamespace + stimId);
        string buffKey = BuffKeyPrefix + stimId;

        return new StimRegistrationIds(itemTemplateId, buffKey, traderAssortId);
    }

    public StimRegistrationIds CreateDrink(string drinkId)
    {
        string itemTemplateId = CreateMongoId(DrinkItemIdNamespace + drinkId);
        string traderAssortId = CreateMongoId(TraderAssortIdNamespace + "drink:" + drinkId);
        string buffKey = BuffKeyPrefix + "Drink_" + drinkId;

        return new StimRegistrationIds(itemTemplateId, buffKey, traderAssortId);
    }

    public StimRegistrationIds CreateMedicalPack(string medicalPackId)
    {
        string itemTemplateId = CreateMongoId(MedicalPackItemIdNamespace + medicalPackId);
        string traderAssortId = CreateMongoId(TraderAssortIdNamespace + "medical-pack:" + medicalPackId);
        return new StimRegistrationIds(itemTemplateId, string.Empty, traderAssortId);
    }

    private static string CreateMongoId(string source)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return Convert.ToHexString(hash).ToLowerInvariant()[..24];
    }
}
