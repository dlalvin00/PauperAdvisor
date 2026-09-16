using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PauperAdvisor.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Ensina o EF Core onde o banco será criado ao rodar os comandos CLI
        optionsBuilder.UseSqlite("Data Source=pauper_advisor.db");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}