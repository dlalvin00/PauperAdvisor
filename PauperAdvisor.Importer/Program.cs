using Microsoft.EntityFrameworkCore;
using PauperAdvisor.Data;
using PauperAdvisor.Data.Seeding;

var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseSqlite("Data Source=pauper_advisor.db");

using var context = new ApplicationDbContext(optionsBuilder.Options);
var seeder = new DatabaseSeeder(context);

string cardsPath = Path.Combine("data", "pauper_cards.json");
string rulingsPath = Path.Combine("data", "rulings.json");

// await seeder.SeedAsync(cardsPath, rulingsPath);

Console.WriteLine("Pressione ENTER para sair...");
Console.ReadLine();