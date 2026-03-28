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
    .Validate(x => !string.IsNullOrWhiteSpace(x.ConvertCurrency), "CoinMarketCap:ConvertCurrency zorunludur.")
    .Validate(x => x.Start > 0, "CoinMarketCap:Start 0'dan büyük olmalıdır.")
    .Validate(x => x.Limit > 0, "CoinMarketCap:Limit 0'dan büyük olmalıdır.")
    .Validate(x => x.TimeoutSeconds > 0, "CoinMarketCap:TimeoutSeconds 0'dan büyük olmalıdır.")
    .ValidateOnStart();

builder.Services
    .AddOptions<PollingOptions>()
    .Bind(builder.Configuration.GetSection(PollingOptions.SectionName))
    .Validate(x => x.DailyRunTimeUtc >= TimeSpan.Zero && x.DailyRunTimeUtc < TimeSpan.FromDays(1),
    "Polling:DailyRunTimeUtc 00:00:00 ile 23:59:59 arasında olmalıdır.")
    .ValidateOnStart();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var app = builder.Build();
app.Run();