namespace DigitalFarmManagement.Infrastructure;

public static class ResidueUnits
{
    public static bool TryToMicrogramsPerKg(decimal value, string unit, out decimal micrograms)
    {
        var normalized = unit.Trim().ToLowerInvariant()
            .Replace(" ", "")
            .Replace("µ", "u")
            .Replace("μ", "u");

        switch (normalized)
        {
            case "ug/kg":
            case "mcg/kg":
                micrograms = value;
                return true;
            case "mg/kg":
                micrograms = value * 1000m;
                return true;
            default:
                micrograms = 0;
                return false;
        }
    }

    public static string Evaluate(decimal measured, string measuredUnit, decimal limit, string limitUnit, bool holdPending, bool standardFound)
    {
        if (holdPending || !standardFound)
            return Models.MrlResults.Pending;

        if (!TryToMicrogramsPerKg(measured, measuredUnit, out var measuredUg))
            return Models.MrlResults.Pending;
        if (!TryToMicrogramsPerKg(limit, limitUnit, out var limitUg))
            return Models.MrlResults.Pending;

        return measuredUg <= limitUg
            ? Models.MrlResults.Compliant
            : Models.MrlResults.NonCompliant;
    }
}
