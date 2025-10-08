using Serilog;
using CryptoDashboard.IoC.DependencyInjection;

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  CRYPTODASHBOARD API - PROGRAM.CS                                         ║
// ║  Autor: Rafael de Oliveira Santos (@Rafadeoliveirasantos)                ║
// ║  Data: 2025-10-08 19:30:00 UTC                                           ║
// ║  Descrição: Configuração principal da API de gerenciamento de cryptos    ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

var builder = WebApplication.CreateBuilder(args);

// ═════════════════════════════════════════════════════════════════════════════
// CONFIGURAÇÃO DO CORS
// ═════════════════════════════════════════════════════════════════════════════
var corsPolicy = "_cryptoDashboardCorsPolicy";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicy, policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",      // Angular Dev
                "https://localhost:4200"      // Angular Dev HTTPS
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition"); // Para downloads (CSV)
    });
});

// ═════════════════════════════════════════════════════════════════════════════
// CONFIGURAÇÃO DE LOGGING (SERILOG)
// ═════════════════════════════════════════════════════════════════════════════
builder.Logging.ClearProviders();

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "CryptoDashboard")
    .Enrich.WithProperty("Version", "1.0.0")
    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
    .Enrich.WithProperty("Developer", "Rafadeoliveirasantos")
    .Enrich.WithProperty("MachineName", Environment.MachineName)
);

// ═════════════════════════════════════════════════════════════════════════════
// CONFIGURAÇÃO DO MEMORY CACHE
// ═════════════════════════════════════════════════════════════════════════════
builder.Services.AddMemoryCache(options =>
{
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
    options.CompactionPercentage = 0.25;
});

// ═════════════════════════════════════════════════════════════════════════════
// CONFIGURAÇÃO DO HTTPCLIENT FACTORY
// ═════════════════════════════════════════════════════════════════════════════
builder.Services.AddHttpClient("CoinGecko", client =>
{
    client.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
    client.DefaultRequestHeaders.Add("User-Agent", "CryptoDashboard-v1.0-by-Rafadeoliveirasantos");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    MaxConnectionsPerServer = 10,
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
    UseProxy = false,
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5));

// ═════════════════════════════════════════════════════════════════════════════
// REGISTRO DE DEPENDÊNCIAS (DI)
// ═════════════════════════════════════════════════════════════════════════════
builder.Services.AddDashboardDependencies();

// ═════════════════════════════════════════════════════════════════════════════
// CONTROLLERS E API
// ═════════════════════════════════════════════════════════════════════════════
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = false;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

// ═════════════════════════════════════════════════════════════════════════════
// SWAGGER / OPENAPI
// ═════════════════════════════════════════════════════════════════════════════
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CryptoDashboard API",
        Version = "v1.0.0",
        Description = @"
🚀 **API RESTful para gerenciamento de criptomoedas**

Funcionalidades:
- 📊 Listagem de top 10 criptomoedas
- ⭐ Sistema de favoritos
- 🔔 Alertas de preço
- 📈 Gráficos de histórico
- 💱 Conversão de moedas (USD/BRL)
- 📥 Exportação de dados (CSV/JSON)
- ⚙️ Gerenciamento de configurações
- 🔄 Atualização automática em background

Desenvolvido por Rafael de Oliveira Santos (@Rafadeoliveirasantos)
Tecnologias: ASP.NET Core 8, AutoMapper, Serilog, Memory Cache
        ",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Rafael de Oliveira Santos",
            Email = "rafadeoliveirasantos@example.com",
            Url = new Uri("https://github.com/Rafadeoliveirasantos")
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // ✅ REMOVIDO: options.EnableAnnotations(); (causava erro)
    options.OrderActionsBy(apiDesc => $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.HttpMethod}");
});

// ═════════════════════════════════════════════════════════════════════════════
// HEALTH CHECKS
// ═════════════════════════════════════════════════════════════════════════════
builder.Services.AddHealthChecks()
    .AddCheck("api-health", () =>
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
            "✅ API está online e funcionando"
        ))
    .AddCheck("memory-check", () =>
    {
        var allocated = GC.GetTotalMemory(forceFullCollection: false);
        var allocatedMB = allocated / 1024 / 1024;
        var threshold = 1024;

        if (allocatedMB < threshold)
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                $"✅ Memória OK: {allocatedMB} MB alocados"
            );

        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
            $"⚠️ Memória alta: {allocatedMB} MB alocados (limite: {threshold} MB)"
        );
    })
    .AddCheck("uptime-check", () =>
    {
        var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
            $"✅ Uptime: {uptime:dd\\:hh\\:mm\\:ss}"
        );
    });

// ═════════════════════════════════════════════════════════════════════════════
// BUILD DA APLICAÇÃO
// ═════════════════════════════════════════════════════════════════════════════
var app = builder.Build();

// ═════════════════════════════════════════════════════════════════════════════
// LOGS DE INICIALIZAÇÃO
// ═════════════════════════════════════════════════════════════════════════════
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("╔═══════════════════════════════════════════════════════════════════════════╗");
logger.LogInformation("║  🚀 CRYPTODASHBOARD API - INICIANDO                                      ║");
logger.LogInformation("╚═══════════════════════════════════════════════════════════════════════════╝");
logger.LogInformation("👨‍💻 Desenvolvedor: Rafael de Oliveira Santos (@Rafadeoliveirasantos)");
logger.LogInformation("📅 Data/Hora: {DateTime}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
logger.LogInformation("🌍 Ambiente: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("💻 Máquina: {MachineName}", Environment.MachineName);
logger.LogInformation("🔧 .NET Version: {Version}", Environment.Version);
logger.LogInformation("📍 CORS: http://localhost:4200");

// ═════════════════════════════════════════════════════════════════════════════
// MIDDLEWARE PIPELINE
// ═════════════════════════════════════════════════════════════════════════════

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CryptoDashboard API v1.0");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "CryptoDashboard API - Documentação";
        options.DefaultModelsExpandDepth(2);
        options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Example);
        options.DisplayRequestDuration();
        options.EnableDeepLinking();
        options.EnableFilter();
        options.ShowExtensions();
    });

    logger.LogInformation("📖 Swagger: https://localhost:7215/swagger");
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Request logging (Serilog) - ✅ CORRIGIDO
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "🌐 HTTP {RequestMethod} {RequestPath} → {StatusCode} em {Elapsed:0.0000}ms";

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        // ✅ CORRIGIDO: Adicionado ?? "localhost"
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "localhost");
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown");
        diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
        diagnosticContext.Set("Protocol", httpContext.Request.Protocol ?? "HTTP/1.1");
        diagnosticContext.Set("Scheme", httpContext.Request.Scheme ?? "https");
    };

    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (httpContext.Request.Path.StartsWithSegments("/health"))
            return Serilog.Events.LogEventLevel.Verbose;

        if (ex != null || httpContext.Response.StatusCode >= 500)
            return Serilog.Events.LogEventLevel.Error;

        if (httpContext.Response.StatusCode >= 400)
            return Serilog.Events.LogEventLevel.Warning;

        if (elapsed > 1000)
            return Serilog.Events.LogEventLevel.Warning;

        return Serilog.Events.LogEventLevel.Information;
    };
});

app.UseHttpsRedirection();
app.UseCors(corsPolicy);
app.UseAuthorization();

// ═════════════════════════════════════════════════════════════════════════════
// MAPEAMENTO DE ROTAS
// ═════════════════════════════════════════════════════════════════════════════
app.MapControllers();

// Health Check Endpoint
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = $"{e.Value.Duration.TotalMilliseconds:F2}ms"
            }),
            totalDuration = $"{report.TotalDuration.TotalMilliseconds:F2}ms"
        }, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        await context.Response.WriteAsync(result);
    }
});

// Root Endpoint
app.MapGet("/", () => Results.Ok(new
{
    application = "CryptoDashboard API",
    version = "1.0.0",
    status = "✅ Running",
    developer = "Rafael de Oliveira Santos (@Rafadeoliveirasantos)",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    endpoints = new
    {
        swagger = "/swagger",
        health = "/health",
        cryptos = "/api/cryptos",
        settings = "/api/settings",
        export = "/api/export"
    },
    features = new[]
    {
        "📊 Top 10 Criptomoedas",
        "⭐ Sistema de Favoritos",
        "🔔 Alertas de Preço",
        "📈 Gráficos Históricos",
        "💱 Conversão USD/BRL",
        "📥 Exportação CSV/JSON",
        "⚙️ Configurações Dinâmicas",
        "🔄 Auto-refresh (5 min)"
    }
}))
.WithName("Root")
.WithTags("Info")
.Produces(200);

// Error Endpoint
app.MapGet("/error", () => Results.Problem(
    title: "Erro Interno",
    detail: "Ocorreu um erro inesperado no servidor. Verifique os logs.",
    statusCode: 500
))
.ExcludeFromDescription();

// ═════════════════════════════════════════════════════════════════════════════
// LOGS DE FINALIZAÇÃO
// ═════════════════════════════════════════════════════════════════════════════
logger.LogInformation("✅ CryptoDashboard API configurada e pronta!");
logger.LogInformation("🌐 Escutando em: https://localhost:7215");
logger.LogInformation("📖 Swagger: https://localhost:7215/swagger");
logger.LogInformation("❤️ Health: https://localhost:7215/health");
logger.LogInformation("╔═══════════════════════════════════════════════════════════════════════════╗");
logger.LogInformation("║  🎯 PRONTO PARA RECEBER REQUISIÇÕES!                                     ║");
logger.LogInformation("╚═══════════════════════════════════════════════════════════════════════════╝");

// ═════════════════════════════════════════════════════════════════════════════
// EXECUÇÃO DA APLICAÇÃO
// ═════════════════════════════════════════════════════════════════════════════
try
{
    logger.LogInformation("🚀 Iniciando servidor HTTP...");
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "💥 FALHA CRÍTICA: Aplicação não pôde ser iniciada!");
    logger.LogCritical("❌ Motivo: {Message}", ex.Message);
    throw;
}
finally
{
    logger.LogInformation("🛑 CryptoDashboard API finalizando...");
    logger.LogInformation("👋 Até logo, Rafadeoliveirasantos!");
    Log.CloseAndFlush();
}