using Microsoft.EntityFrameworkCore;
using Serilog;
using TcmbRatesWorker.Application.Abstractions;
using TcmbRatesWorker.Infrastructure.Http;
using TcmbRatesWorker.Infrastructure.Persistence;
using TcmbRatesWorker.Shared.Options;
using TcmbRatesWorker.Worker;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.Configure<TcmbOptions>(
    builder.Configuration.GetSection("Tcmb"));

builder.Services.AddDbContext<RatesDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("RatesDb"),
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_TcmbRates", "tcmb")));

builder.Services.AddHttpClient<ITcmbRatesClient, TcmbRatesClient>();
builder.Services.AddHostedService<Worker>();

try
{
    var host = builder.Build();
    host.Run();
}
finally
{ 
    Log.CloseAndFlush();
}