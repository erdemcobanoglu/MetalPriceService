using MetalPrice.Service.Data;
using MetalPrice.Service.Options;
using MetalPrice.Service.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

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
        // Docker / network / ilk bağlantı anındaki geçici hatalara karşı
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

await builder.Build().RunAsync();
