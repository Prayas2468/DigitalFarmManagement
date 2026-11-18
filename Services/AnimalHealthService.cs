using DigitalFarmManagement.Data;
using DigitalFarmManagement.Infrastructure;
using DigitalFarmManagement.Models;
using DigitalFarmManagement.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace DigitalFarmManagement.Services;

public class AnimalHealthService
{
    private readonly ApplicationDbContext _db;
    private readonly FarmAccessService _access;

    public AnimalHealthService(ApplicationDbContext db, FarmAccessService access)
    {
        _db = db;
        _access = access;
    }

    public async Task<(bool Ok, string? Error)> SaveTreatmentAsync(TreatmentFormViewModel form, string userId)
    {
        if (!AdminRoutes.All.Contains(form.AdministrationRoute))
            return (false, "Choose a valid administration route.");
        if (form.Dosage <= 0)
            return (false, "Dosage must be greater than zero.");
        if (form.TreatmentEndDate.Date < form.TreatmentStartDate.Date)
            return (false, "Treatment end date cannot be before the start date.");

        var animal = await (await _access.LivestockAsync()).FirstOrDefaultAsync(a => a.Id == form.LivestockId);
        if (animal == null)
            return (false, "Choose an animal from your farms.");

        var batch = await _db.MedicineInventories.Include(b => b.Medicine).FirstOrDefaultAsync(b => b.Id == form.MedicineInventoryId);
        if (batch == null)
            return (false, "Choose a medicine batch.");
        if (form.Id == 0 && batch.ExpiryDate.Date < DateTime.Today)
            return (false, "This batch is expired. Choose a batch that is still in date.");

        AntimicrobialTreatment? existing = null;
        if (form.Id != 0)
        {
            existing = await (await _access.TreatmentsAsync())
                .Include(t => t.MedicineInventory)
                .Include(t => t.Withdrawal)
                .FirstOrDefaultAsync(t => t.Id == form.Id);
            if (existing == null)
                return (false, "That treatment record was not found.");
        }

        var available = batch.AvailableQuantity;
        if (existing != null && existing.MedicineInventoryId == batch.Id)
            available += existing.Dosage;
        if (available < form.Dosage)
            return (false, $"This batch only has {available:0.###} {batch.Unit} available.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        if (existing != null && existing.MedicineInventoryId != batch.Id)
            existing.MedicineInventory.AvailableQuantity += existing.Dosage;

        batch.AvailableQuantity = existing != null && existing.MedicineInventoryId == batch.Id
            ? available - form.Dosage
            : batch.AvailableQuantity - form.Dosage;

        var treatment = existing ?? new AntimicrobialTreatment();
        treatment.LivestockId = animal.Id;
        treatment.MedicineId = batch.MedicineId;
        treatment.MedicineInventoryId = batch.Id;
        treatment.MedicineName = batch.Medicine.MedicineName;
        treatment.MedicineCategory = batch.Medicine.Category;
        treatment.ActiveIngredient = batch.Medicine.ActiveIngredient;
        treatment.Dosage = form.Dosage;
        treatment.Unit = batch.Unit;
        treatment.AdministrationRoute = form.AdministrationRoute;
        treatment.TreatmentStartDate = form.TreatmentStartDate.Date;
        treatment.TreatmentEndDate = form.TreatmentEndDate.Date;
        treatment.VeterinarianName = form.VeterinarianName.Trim();
        treatment.ReasonForTreatment = form.ReasonForTreatment.Trim();
        treatment.WithdrawalPeriodDays = batch.Medicine.WithdrawalPeriodDays;
        treatment.WithdrawalEndDate = WithdrawalMath.FirstEligibleDay(form.TreatmentEndDate, batch.Medicine.WithdrawalPeriodDays);
        treatment.Remarks = string.IsNullOrWhiteSpace(form.Remarks) ? null : form.Remarks.Trim();
        treatment.RecordedById = userId;

        if (existing == null)
            _db.Treatments.Add(treatment);

        var withdrawal = existing?.Withdrawal ?? new WithdrawalPeriod { Treatment = treatment };
        withdrawal.LivestockId = animal.Id;
        withdrawal.StartDate = form.TreatmentEndDate.Date;
        withdrawal.ExpectedCompletionDate = treatment.WithdrawalEndDate;
        withdrawal.TotalDays = treatment.WithdrawalPeriodDays;
        withdrawal.AffectedProducts = string.Join(", ", SpeciesInfo.ProductsFor(animal.Species));
        if (existing?.Withdrawal == null)
            _db.WithdrawalPeriods.Add(withdrawal);

        await _db.SaveChangesAsync();
        await RefreshHealthAsync(animal.Id);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DeleteTreatmentAsync(int id)
    {
        var treatment = await (await _access.TreatmentsAsync())
            .Include(t => t.MedicineInventory)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (treatment == null)
            return (false, "That treatment record was not found.");

        treatment.MedicineInventory.AvailableQuantity += treatment.Dosage;
        var animalId = treatment.LivestockId;
        _db.Treatments.Remove(treatment);
        await _db.SaveChangesAsync();
        await RefreshHealthAsync(animalId);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> MarkEligibleAsync(int livestockId, string product, string userId)
    {
        var animal = await (await _access.LivestockAsync()).FirstOrDefaultAsync(a => a.Id == livestockId);
        if (animal == null)
            return (false, "Animal not found.");

        var products = SpeciesInfo.ProductsFor(animal.Species);
        if (!products.Contains(product))
            return (false, $"{animal.Species} does not produce {product}.");

        var today = DateTime.Today;
        var open = await _db.WithdrawalPeriods
            .Where(w => w.LivestockId == livestockId && w.ExpectedCompletionDate > today)
            .ToListAsync();
        var blocking = open.FirstOrDefault(w =>
            w.AffectedProducts.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Contains(product));
        if (blocking != null)
        {
            return (false, $"{product} cannot be marked eligible for sale. The withdrawal period runs until {blocking.ExpectedCompletionDate:dd MMM yyyy}.");
        }

        _db.ProductClearances.Add(new ProductClearance
        {
            LivestockId = livestockId,
            ProductType = product,
            ClearedOn = DateTime.Now,
            ClearedById = userId
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private async Task RefreshHealthAsync(int livestockId)
    {
        var animal = await _db.Livestock.FirstAsync(a => a.Id == livestockId);
        var underCare = await _db.Treatments.AnyAsync(t => t.LivestockId == livestockId && t.TreatmentEndDate >= DateTime.Today);
        if (underCare)
            animal.HealthStatus = HealthStatuses.UnderTreatment;
        else if (animal.HealthStatus == HealthStatuses.UnderTreatment)
            animal.HealthStatus = HealthStatuses.Recovering;
    }
}
