using Microsoft.EntityFrameworkCore;
using PauperAdvisor.Data;
using PauperAdvisor.RAG.Configuration;
using PauperAdvisor.RAG.Services;
using PauperAdvisor.RAG.Storage;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var ollamaOptions = builder.Configuration
    .GetSection(OllamaOptions.SectionName)
    .Get<OllamaOptions>() ?? new OllamaOptions();

var qdrantOptions = builder.Configuration
    .GetSection(QdrantOptions.SectionName)
    .Get<QdrantOptions>() ?? new QdrantOptions();

builder.Services.AddSingleton(ollamaOptions);
builder.Services.AddSingleton(qdrantOptions);
builder.Services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();
builder.Services.AddSingleton<QdrantStorageService>();
builder.Services.AddSingleton<IngestionStatusService>();
builder.Services.AddScoped<RetrievalService>();
builder.Services.AddScoped<ChatService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=pauper_advisor.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
