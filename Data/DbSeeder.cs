using DigitalFarmManagement.Infrastructure;
using DigitalFarmManagement.Models;
using DigitalFarmManagement.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DigitalFarmManagement.Data;

public static class DbSeeder
{
    public const string AdminEmail = "admin@farmportal.local";
    public const string AdminPassword = "Admin@123";
    public const string FarmerEmail = "farmer@farmportal.local";
    public const string FarmerPassword = "Farmer@123";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var sync = services.GetRequiredService<NotificationSyncService>();

        foreach (var role in new[] { AppRoles.Admin, AppRoles.Farmer })
        {
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));
        }

        var admin = await EnsureUserAsync(users, AdminEmail, AdminPassword, "Portal Administrator", AppRoles.Admin);
        var farmer = await EnsureUserAsync(users, FarmerEmail, FarmerPassword, "Anita Desai", AppRoles.Farmer);

        if (!await db.Farms.AnyAsync())
            await SeedDemoAsync(db, admin, farmer);

        await sync.SyncAsync();
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> users, string email, string password, string fullName, string role)
    {
        var user = await users.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                PhoneNumber = "9876543210"
            };
            var created = await users.CreateAsync(user, password);
            if (!created.Succeeded)
                throw new InvalidOperationException(string.Join(" ", created.Errors.Select(e => e.Description)));
        }

        if (!await users.IsInRoleAsync(user, role))
            await users.AddToRoleAsync(user, role);

        return user;
    }

    private static async Task SeedDemoAsync(ApplicationDbContext db, ApplicationUser admin, ApplicationUser farmer)
    {
        var today = DateTime.Today;

        var greenValley = Farm("Green Valley Dairy", "Anita Desai", "Anand, Gujarat", "9825011122", "anita@greenvalley.example", "Dairy", today.AddYears(-4), farmer.Id);
        var narmada = Farm("Narmada Buffalo Unit", "Anita Desai", "Bharuch, Gujarat", "9825011133", "office@narmada.example", "Buffalo Dairy", today.AddYears(-2), farmer.Id);
        var hill = Farm("Hill Goat Fold", "Anita Desai", "Shimla, Himachal Pradesh", "9817012211", "fold@hillgoat.example", "Small Ruminant", today.AddYears(-1), farmer.Id);
        var layers = Farm("Namakkal Layer Yard", "Portal Administrator", "Namakkal, Tamil Nadu", "9843014455", "yard@namakkal.example", "Poultry", today.AddMonths(-14), admin.Id);
        db.Farms.AddRange(greenValley, narmada, hill, layers);

        var gauri = Animal("CTL-1042", "Gauri", "Cattle", "Gir", "Female", today.AddYears(-4), 412, HealthStatuses.UnderTreatment, greenValley, today.AddYears(-4));
        var nanda = Animal("CTL-1108", "Nanda", "Cattle", "Sahiwal", "Female", today.AddYears(-3), 388, HealthStatuses.Healthy, greenValley, today.AddYears(-3));
        var moti = Animal("BUF-2201", "Moti", "Buffalo", "Murrah", "Female", today.AddYears(-5), 540, HealthStatuses.Recovering, narmada, today.AddYears(-5));
        var heera = Animal("BUF-2214", "Heera", "Buffalo", "Jaffarabadi", "Male", today.AddYears(-2), 610, HealthStatuses.Healthy, narmada, today.AddYears(-2));
        var tara = Animal("GOT-3302", "Tara", "Goat", "Beetal", "Female", today.AddYears(-2), 38, HealthStatuses.Healthy, hill, today.AddMonths(-18));
        var ramu = Animal("SHP-4107", "Ramu", "Sheep", "Nellore", "Male", today.AddMonths(-20), 32, HealthStatuses.Healthy, hill, today.AddMonths(-16));
        var pinky = Animal("PIG-5003", "Pinky", "Pig", "Large White", "Female", today.AddMonths(-14), 96, HealthStatuses.Healthy, layers, today.AddMonths(-12));
        var layer1 = Animal("PLY-6001", "Layer 12", "Poultry", "BV-300", "Female", today.AddMonths(-11), 1.8m, HealthStatuses.Healthy, layers, today.AddMonths(-10));
        var layer2 = Animal("PLY-6008", "Layer 18", "Poultry", "BV-300", "Female", today.AddMonths(-9), 1.7m, HealthStatuses.UnderTreatment, layers, today.AddMonths(-8));
        var calf = Animal("CTL-1180", "Bala", "Cattle", "Gir", "Male", today.AddMonths(-8), 142, HealthStatuses.Critical, greenValley, today.AddMonths(-7));
        db.Livestock.AddRange(gauri, nanda, moti, heera, tara, ramu, pinky, layer1, layer2, calf);

        var oxy = Medicine("Oxyvet LA", "Oxytetracycline", "Tetracycline", "VetCare India", 7);
        var oxyBatch = Batch(oxy, "OV-2401", 450, "ml", today.AddMonths(-2), today.AddMonths(10), 40);
        var amox = Medicine("Amoxi-Care", "Amoxicillin", "Penicillin", "Herd Labs", 4);
        var amoxBatch = Batch(amox, "AM-118", 8, "ml", today.AddMonths(-1), today.AddMonths(8), 20);
        var enro = Medicine("Enro-Flox", "Enrofloxacin", "Fluoroquinolone", "AquaVet", 8);
        var enroBatch = Batch(enro, "EN-077", 180, "ml", today.AddMonths(-3), today.AddMonths(6), 25);
        var bolus = Medicine("Alamycin Bolus", "Oxytetracycline", "Tetracycline", "VetCare India", 10);
        var bolusBatch = Batch(bolus, "AB-552", 86, "bolus", today.AddMonths(-4), today.AddMonths(14), 15);
        var expired = Medicine("Pen-Strep", "Procaine penicillin", "Penicillin", "Old Stock Co", 5);
        var expiredBatch = Batch(expired, "PS-019", 15, "ml", today.AddYears(-1), today.AddDays(-12), 5);
        db.Medicines.AddRange(oxy, amox, enro, bolus, expired);
        db.MedicineInventories.AddRange(oxyBatch, amoxBatch, enroBatch, bolusBatch, expiredBatch);

        db.MrlStandards.AddRange(
            Standard("Milk", "Oxytetracycline", "", 100),
            Standard("Meat", "Oxytetracycline", "", 200),
            Standard("Eggs", "Oxytetracycline", "Poultry", 200),
            Standard("Milk", "Amoxicillin", "", 4),
            Standard("Meat", "Amoxicillin", "", 50),
            Standard("Milk", "Enrofloxacin", "", 100),
            Standard("Meat", "Enrofloxacin", "", 100),
            Standard("Eggs", "Enrofloxacin", "Poultry", 100),
            Standard("Milk", "Procaine penicillin", "", 4),
            Standard("Meat", "Procaine penicillin", "", 50));

        Treat(db, gauri, oxy, oxyBatch, 30, "Intramuscular", today.AddDays(-2), today.AddDays(1), "Dr. Mehta", "Mastitis in the left forequarter", "Recheck milk before the next milking.", farmer.Id);
        Treat(db, moti, oxy, oxyBatch, 40, "Intramuscular", today.AddDays(-12), today.AddDays(-5), "Dr. Mehta", "Respiratory infection", null, farmer.Id);
        Treat(db, tara, bolus, bolusBatch, 2, "Oral", today.AddDays(-30), today.AddDays(-24), "Dr. Kapoor", "Foot rot", "Completed course.", farmer.Id);
        Treat(db, nanda, amox, amoxBatch, 15, "Intramuscular", today.AddMonths(-1), today.AddMonths(-1).AddDays(3), "Dr. Mehta", "Metritis", null, farmer.Id);
        Treat(db, layer2, enro, enroBatch, 2, "Oral", today.AddDays(-1), today.AddDays(2), "Dr. Iyer", "Respiratory signs in the shed", "Isolate the bird.", admin.Id);
        Treat(db, calf, enro, enroBatch, 8, "Subcutaneous", today.AddMonths(-3), today.AddMonths(-3).AddDays(4), "Dr. Mehta", "Calf pneumonia", null, farmer.Id);
        Treat(db, pinky, amox, amoxBatch, 12, "Intramuscular", today.AddMonths(-4), today.AddMonths(-4).AddDays(3), "Dr. Iyer", "Wound infection", null, admin.Id);
        Treat(db, ramu, bolus, bolusBatch, 1, "Oral", today.AddMonths(-2), today.AddMonths(-2).AddDays(2), "Dr. Kapoor", "Enteritis", null, farmer.Id);

        db.MrlTests.AddRange(
            Test(gauri, "Milk", "Oxytetracycline", 42, "µg/kg", 100, "µg/kg", today.AddDays(-20), "Anand Veterinary Lab", false, "Within the sample milk limit."),
            Test(moti, "Milk", "Enrofloxacin", 180, "µg/kg", 100, "µg/kg", today.AddDays(-4), "Bharuch Residue Lab", false, "Above the configured milk limit."),
            Test(nanda, "Milk", "Amoxicillin", 0.003m, "mg/kg", 4, "µg/kg", today.AddDays(-10), "Anand Veterinary Lab", false, "Recorded in mg/kg and compared after converting to µg/kg."),
            Test(layer1, "Eggs", "Enrofloxacin", 40, "µg/kg", 100, "µg/kg", today.AddDays(-6), "Namakkal Poultry Lab", false, null),
            Test(tara, "Meat", "Oxytetracycline", 90, "µg/kg", 200, "µg/kg", today.AddDays(-15), "Shimla Food Lab", true, "Laboratory has not released the final report."));

        db.ProductClearances.Add(new ProductClearance
        {
            Livestock = tara,
            ProductType = "Milk",
            ClearedOn = today.AddDays(-10),
            ClearedById = farmer.Id
        });

        await db.SaveChangesAsync();
    }

    private static Farm Farm(string name, string farmer, string location, string phone, string email, string type, DateTime registered, string ownerId)
        => new()
        {
            FarmName = name,
            FarmerName = farmer,
            Location = location,
            ContactNumber = phone,
            Email = email,
            FarmType = type,
            RegistrationDate = registered,
            OwnerId = ownerId
        };

    private static Livestock Animal(string code, string name, string species, string breed, string gender, DateTime born, decimal weight, string health, Farm farm, DateTime registered)
        => new()
        {
            AnimalCode = code,
            AnimalName = name,
            Species = species,
            Breed = breed,
            Gender = gender,
            DateOfBirth = born,
            Weight = weight,
            HealthStatus = health,
            Farm = farm,
            RegistrationDate = registered
        };

    private static Medicine Medicine(string name, string ingredient, string category, string maker, int withdrawalDays)
        => new()
        {
            MedicineName = name,
            ActiveIngredient = ingredient,
            Category = category,
            Manufacturer = maker,
            WithdrawalPeriodDays = withdrawalDays,
            IsActive = true
        };

    private static MedicineInventory Batch(Medicine medicine, string batch, decimal qty, string unit, DateTime purchased, DateTime expires, decimal low)
        => new()
        {
            Medicine = medicine,
            BatchNumber = batch,
            AvailableQuantity = qty,
            Unit = unit,
            PurchaseDate = purchased,
            ExpiryDate = expires,
            LowStockThreshold = low
        };

    private static MRLStandard Standard(string product, string ingredient, string species, decimal limit)
        => new()
        {
            Country = "India",
            ProductType = product,
            ActiveIngredient = ingredient,
            Species = species,
            LimitValue = limit,
            Unit = "µg/kg",
            RegulatorySource = "Project sample standard — replace with your country's official MRL table"
        };

    private static void Treat(
        ApplicationDbContext db,
        Livestock animal,
        Medicine medicine,
        MedicineInventory batch,
        decimal dose,
        string route,
        DateTime start,
        DateTime end,
        string vet,
        string reason,
        string? remarks,
        string userId)
    {
        var eligible = WithdrawalMath.FirstEligibleDay(end, medicine.WithdrawalPeriodDays);
        var treatment = new AntimicrobialTreatment
        {
            Livestock = animal,
            Medicine = medicine,
            MedicineInventory = batch,
            MedicineName = medicine.MedicineName,
            MedicineCategory = medicine.Category,
            ActiveIngredient = medicine.ActiveIngredient,
            Dosage = dose,
            Unit = batch.Unit,
            AdministrationRoute = route,
            TreatmentStartDate = start,
            TreatmentEndDate = end,
            VeterinarianName = vet,
            ReasonForTreatment = reason,
            WithdrawalPeriodDays = medicine.WithdrawalPeriodDays,
            WithdrawalEndDate = eligible,
            Remarks = remarks,
            RecordedById = userId
        };
        treatment.Withdrawal = new WithdrawalPeriod
        {
            Livestock = animal,
            Treatment = treatment,
            StartDate = end,
            ExpectedCompletionDate = eligible,
            TotalDays = medicine.WithdrawalPeriodDays,
            AffectedProducts = string.Join(", ", SpeciesInfo.ProductsFor(animal.Species))
        };
        db.Treatments.Add(treatment);
    }

    private static MRLTest Test(
        Livestock animal,
        string product,
        string ingredient,
        decimal measured,
        string unit,
        decimal limit,
        string limitUnit,
        DateTime date,
        string lab,
        bool pending,
        string? remarks)
    {
        return new MRLTest
        {
            Livestock = animal,
            ProductType = product,
            ActiveIngredient = ingredient,
            MeasuredResidueLevel = measured,
            Unit = unit,
            ApplicableMrlLimit = limit,
            LimitUnit = limitUnit,
            TestDate = date,
            LaboratoryName = lab,
            TestResult = ResidueUnits.Evaluate(measured, unit, limit, limitUnit, pending, true),
            Remarks = remarks
        };
    }
}
