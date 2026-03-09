using InflationService.Application.Abstractions;
using InflationService.Application.Services;
using InflationService.Infrastructure.Http;
using InflationService.Infrastructure.Parsing;
using InflationService.Infrastructure.Persistence;
using InflationService.Infrastructure.Providers;
using InflationService.Shared.Options;
using InflationService.Worker;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();

builder.Services.Configure<InflationSourcesOptions>(
    builder.Configuration.GetSection("InflationSources"));

builder.Services.AddDbContext<InflationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql =>
        {
            sql.MigrationsAssembly(typeof(InflationDbContext).Assembly.FullName);
            sql.MigrationsHistoryTable("__InflationMigrationsHistory", "inflation");
        }));

builder.Services.AddHttpClient<TuikHttpClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(90);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
});

builder.Services.AddHttpClient<EnagHttpClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(90);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
});

builder.Services.AddSingleton<TuikInflationParser>();
builder.Services.AddSingleton<EnagInflationParser>();

builder.Services.AddScoped<IInflationProvider, TuikInflationProvider>();
builder.Services.AddScoped<IInflationProvider, EnagInflationProvider>();

builder.Services.AddScoped<IInflationIngestionService, InflationIngestionService>();
builder.Services.AddScoped<IInflationRepository, SqlInflationRepository>();

builder.Services.AddHostedService<InflationWorker>();

var host = builder.Build();
await host.RunAsync();