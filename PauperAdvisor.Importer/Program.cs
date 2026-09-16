using Microsoft.EntityFrameworkCore;
using PauperAdvisor.Data;
using PauperAdvisor.Data.Seeding;

static string? GetOption(string[] arguments, string optionName)
{
    var index = Array.FindIndex(arguments, arg =>
        string.Equals(arg, optionName, StringComparison.OrdinalIgnoreCase));

    return index >= 0 && index + 1 < arguments.Length
        ? arguments[index + 1]
        : null;
}

var cardsPath = GetOption(args, "--cards")
    ?? Environment.GetEnvironmentVariable("PAUPER_CARDS_PATH");

var rulingsPath = GetOption(args, "--rulings")
    ?? Environment.GetEnvironmentVariable("PAUPER_RULINGS_PATH");

var connectionString = GetOption(args, "--connection")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? "Data Source=pauper_advisor.db";

if (string.IsNullOrWhiteSpace(cardsPath) || string.IsNullOrWhiteSpace(rulingsPath))
{
    Console.Error.WriteLine("Uso:");
    Console.Error.WriteLine("  dotnet run --project PauperAdvisor.Importer -- --cards <cards.json> --rulings <rulings.jsonl>");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Opcional:");
    Console.Error.WriteLine("  --connection \"Data Source=pauper_advisor.db\"");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Também é possível usar PAUPER_CARDS_PATH, PAUPER_RULINGS_PATH e ConnectionStrings__DefaultConnection.");
    return 1;
}

cardsPath = Path.GetFullPath(cardsPath);
rulingsPath = Path.GetFullPath(rulingsPath);

if (!File.Exists(cardsPath))
{
    Console.Error.WriteLine($"Arquivo de cartas não encontrado: {cardsPath}");
    return 2;
}

if (!File.Exists(rulingsPath))
{
    Console.Error.WriteLine($"Arquivo de rulings não encontrado: {rulingsPath}");
    return 3;
}

var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseSqlite(connectionString);

await using var context = new ApplicationDbContext(optionsBuilder.Options);
var seeder = new DatabaseSeeder(context);

await seeder.SeedAsync(cardsPath, rulingsPath);
return 0;
