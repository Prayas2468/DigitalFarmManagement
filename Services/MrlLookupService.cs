using DigitalFarmManagement.Data;
using DigitalFarmManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace DigitalFarmManagement.Services;

public class MrlLookupService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public MrlLookupService(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public string Country => string.IsNullOrWhiteSpace(_config["Mrl:Country"]) ? "India" : _config["Mrl:Country"]!;

    public async Task<MRLStandard?> FindAsync(string productType, string activeIngredient, string species)
    {
        var country = Country;
        var matches = _db.MrlStandards.Where(s =>
            s.Country == country &&
            s.ProductType == productType &&
            s.ActiveIngredient == activeIngredient);

        var specific = await matches.FirstOrDefaultAsync(s => s.Species == species);
        if (specific != null)
            return specific;

        return await matches.FirstOrDefaultAsync(s => s.Species == "");
    }

    public async Task<List<MRLStandard>> AllForCountryAsync()
    {
        var country = Country;
        return await _db.MrlStandards
            .Where(s => s.Country == country)
            .OrderBy(s => s.ActiveIngredient)
            .ThenBy(s => s.ProductType)
            .ToListAsync();
    }
}
