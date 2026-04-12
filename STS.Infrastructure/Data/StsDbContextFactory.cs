using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sts.Infrastructure.Data;

/// <summary>
/// Factory utilisée par les outils EF Core (migrations) en dehors du runtime ASP.NET.
/// Génère la BDD dans le répertoire courant lors des commandes <c>dotnet ef</c>.
/// </summary>
public sealed class StsDbContextFactory : IDesignTimeDbContextFactory<StsDbContext>
{
    public StsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<StsDbContext>()
            .UseSqlite("Data Source=sts-design.db")
            .Options;

        return new StsDbContext(options);
    }
}
