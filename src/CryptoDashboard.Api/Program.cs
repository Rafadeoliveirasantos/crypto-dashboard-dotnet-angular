using Serilog;
using CryptoDashboard.IoC.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// --- IN�CIO DA CONFIGURA��O DO CORS ---
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
// --- FIM DA CONFIGURA��O DO CORS ---


// 1. Limpa os loggers padr�o.
builder.Logging.ClearProviders();

// 2. Configura o Serilog para ler as configura��es do appsettings.json.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
);

// --- IN�CIO DA ADI��O DO SERVI�O CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          // URL do seu frontend Angular
                          policy.WithOrigins("http://localhost:4200")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});
// --- FIM DA ADI��O DO SERVI�O CORS ---


// Adiciona o IHttpClientFactory e o cliente nomeado.
builder.Services.AddHttpClient("CoinGecko", client =>
{
    client.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
    client.DefaultRequestHeaders.Add("User-Agent", "CryptoDashboard-App-by-Rafadeoliveirasantos");
});

// Registra as outras depend�ncias.
builder.Services.AddDashboardDependencies();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure o pipeline de requisi��es HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- IN�CIO DO USO DO MIDDLEWARE CORS (A ORDEM � IMPORTANTE) ---
app.UseCors(MyAllowSpecificOrigins);
// --- FIM DO USO DO MIDDLEWARE CORS ---

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();