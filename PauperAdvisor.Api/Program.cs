using Microsoft.EntityFrameworkCore;
using PauperAdvisor.Data;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Adiciona os controladores REST
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<PauperAdvisor.RAG.Services.IEmbeddingService, PauperAdvisor.RAG.Services.OllamaEmbeddingService>();
builder.Services.AddSingleton<PauperAdvisor.RAG.Storage.QdrantStorageService>();
builder.Services.AddSingleton<PauperAdvisor.RAG.Services.IngestionStatusService>();

builder.Services.AddScoped<PauperAdvisor.RAG.Services.RetrievalService>();
builder.Services.AddScoped<PauperAdvisor.RAG.Services.ChatService>();

// Configura o SQLite com um banco local ao projeto
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=pauper_advisor.db"));

var app = builder.Build();

// Habilita a interface do Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();