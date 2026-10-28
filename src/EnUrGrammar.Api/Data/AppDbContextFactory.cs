using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EnUrGrammar.Api.Data;

/// <summary>Design-time factory for <c>dotnet ef migrations</c> without running the host.</summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                 ?? "Server=127.0.0.1,14333;Database=EnUrGrammar;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;Encrypt=False";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(cs)
            .Options;
        return new AppDbContext(options);
    }
}
