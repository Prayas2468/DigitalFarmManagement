namespace DigitalFarmManagement.Infrastructure;

public static class SpeciesInfo
{
    public static IReadOnlyList<string> ProductsFor(string species) => species switch
    {
        "Pig" => ["Meat"],
        "Poultry" => ["Meat", "Eggs"],
        _ => ["Milk", "Meat"]
    };
}
