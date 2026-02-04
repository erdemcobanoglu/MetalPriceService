using MetalPrice.Service.Data;
using MetalPrice.Service.Options;
using MetalPrice.Service.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Serilog'u config'den oku ve Host'a entegre et
builder.Services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
);

builder.Services.Configure<MetalPriceOptions>(
    builder.Configuration.GetSection("MetalPrice"));

// Connection string key: "ConnectionStrings:DefaultConnection"
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string bulunamadı. appsettings.json içine 'ConnectionStrings:DefaultConnection' ekleyin.");

builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, sql =>
    {
        sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    });
});

builder.Services.AddHttpClient("metals", c =>
{
    c.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddHostedService<PriceFetchWorker>();

var host = builder.Build();

try
{
    Log.Information("MetalPrice.Service starting up");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MetalPrice.Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
