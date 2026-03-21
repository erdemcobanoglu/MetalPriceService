using CoinMarketCap.Service.Shared.Options;
using CoinMarketCap.Service.Infrastructure.DependencyInjection;
using Serilog;
using CoinMarketCapWorker.Host;

var builder = Host.CreateApplicationBuilder(args);

// Serilog init
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

// 🔥 BURASI ÖNEMLİ
builder.Services.AddSerilog();

builder.Services
    .AddOptions<CoinMarketCapOptions>()
    .Bind(builder.Configuration.GetSection(CoinMarketCapOptions.SectionName))
    .Validate(x => !string.IsNullOrWhiteSpace(x.ApiKey), "CoinMarketCap:ApiKey zorunludur.")
    .Validate(x => x.Symbols is { Length: > 0 }, "CoinMarketCap:Symbols boş olamaz.")
    .Validate(x => !string.IsNullOrWhiteSpace(x.ConvertCurrency), "CoinMarketCap:ConvertCurrency zorunludur.")
    .Validate(x => x.TimeoutSeconds > 0, "CoinMarketCap:TimeoutSeconds 0'dan büyük olmalıdır.")
    .ValidateOnStart();

builder.Services
    .AddOptions<PollingOptions>()
    .Bind(builder.Configuration.GetSection(PollingOptions.SectionName))
    .Validate(x => x.IntervalSeconds > 0, "Polling:IntervalSeconds 0'dan büyük olmalıdır.")
    .ValidateOnStart();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var app = builder.Build();
app.Run();