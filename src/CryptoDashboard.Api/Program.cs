using Serilog;
using CryptoDashboard.IoC.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// 1. Limpa os loggers padrão.
builder.Logging.ClearProviders();

// 2. Configura o Serilog para ler as configurações do appsettings.json.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
);

// Adiciona o IHttpClientFactory e o cliente nomeado.
builder.Services.AddHttpClient("CoinGecko", client =>
{
    client.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
    client.DefaultRequestHeaders.Add("User-Agent", "CryptoDashboard-App-by-Rafadeoliveirasantos");
});

// Registra as outras dependências.
builder.Services.AddDashboardDependencies();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();