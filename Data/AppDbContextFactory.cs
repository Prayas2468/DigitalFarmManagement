using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DigitalFarmManagement.Data;

/// <summary>
/// Lets the EF Core tools create a context without starting the website.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        options.UseSqlite(config.GetConnectionString("DefaultConnection"));
        return new ApplicationDbContext(options.Options);
    }
}
