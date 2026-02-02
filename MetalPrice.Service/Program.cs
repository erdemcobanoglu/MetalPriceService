using MetalPrice.Service.Data;
using MetalPrice.Service.Options;
using MetalPrice.Service.Worker;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MetalPriceOptions>(
    builder.Configuration.GetSection("MetalPrice"));

builder.Services.AddDbContextFactory<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

builder.Services.AddHttpClient("metals", c =>
{
    c.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddHostedService<PriceFetchWorker>();

await builder.Build().RunAsync();