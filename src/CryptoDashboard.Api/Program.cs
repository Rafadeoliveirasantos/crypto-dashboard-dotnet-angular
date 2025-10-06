using Serilog;
using CryptoDashboard.IoC.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// --- INÍCIO DA CONFIGURAÇÃO DO CORS ---
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
// --- FIM DA CONFIGURAÇÃO DO CORS ---


// 1. Limpa os loggers padrão.
builder.Logging.ClearProviders();

// 2. Configura o Serilog para ler as configurações do appsettings.json.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
);

// --- INÍCIO DA ADIÇÃO DO SERVIÇO CORS ---
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
// --- FIM DA ADIÇÃO DO SERVIÇO CORS ---


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

// Configure o pipeline de requisições HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- INÍCIO DO USO DO MIDDLEWARE CORS (A ORDEM É IMPORTANTE) ---
app.UseCors(MyAllowSpecificOrigins);
// --- FIM DO USO DO MIDDLEWARE CORS ---

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();