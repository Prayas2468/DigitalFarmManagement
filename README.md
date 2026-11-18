# Supergreen

ASP.NET Core MVC application for farm records, antimicrobial usage (AMU), maximum residue limits (MRL), withdrawal periods, medicine stock, and reports. Data is stored in Microsoft SQL Server through Entity Framework Core.

## What you need

- Visual Studio 2022 (or later) with the **ASP.NET and web development** workload
- The **.NET 10 SDK** (this project targets `net10.0`)
- **SQL Server Express** or **SQL Server Express LocalDB**

This computer did not have LocalDB or a SQL Server service installed, so the database could not be created here. The migration file is already in `Data/Migrations`. The site creates the database and demo data the first time it starts, as long as the connection string can reach SQL Server.

## Connection string

Open `appsettings.json`. The default expects LocalDB:

```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DigitalFarmManagement;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

If you installed SQL Server Express instead, use:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=DigitalFarmManagement;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

Use your own server name if SQL Server is installed under a different instance.

## Run it in Visual Studio

1. Install SQL Server Express or LocalDB if it is not already installed.
2. Open `DigitalFarmManagement.csproj` in Visual Studio.
3. Confirm the connection string in `appsettings.json`.
4. Press **F5** (or choose the `https` profile and run).
5. The first launch applies the migration and inserts demo farms, animals, medicines, treatments, residue tests, and alerts.
6. Sign in with one of the demo accounts below.

From a terminal in this folder you can also run:

```powershell
dotnet run --launch-profile https
```

To create or update the database without starting the site:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update
```

## Demo logins

| Role | Email | Password |
| --- | --- | --- |
| Administrator | admin@farmportal.local | Admin@123 |
| Farmer | farmer@farmportal.local | Farmer@123 |

The administrator sees every farm. The farmer sees only farms linked to that account. Registration creates a new **Farmer** user. New users can register from the home page. The forgot-password page shows a reset link on screen because this project does not send email.

## Modules

- Dashboard statistics and charts come from SQL Server.
- Farms, livestock, treatments, residue tests, and medicine batches support add, edit, delete, search, and filters.
- Recording a treatment deducts stock and calculates the withdrawal end date.
- Residue tests are compared with limits stored in the `MRLStandards` table. Administrators edit those limits under **MRL Monitoring → Configure limits**.
- A product cannot be marked eligible for sale while a withdrawal period is still open.
- Reports can be filtered, printed, and exported as CSV or PDF.
