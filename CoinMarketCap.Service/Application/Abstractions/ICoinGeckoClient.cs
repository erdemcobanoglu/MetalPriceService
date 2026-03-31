using CoinMarketCap.Service.Application.Models;

namespace CoinMarketCap.Service.Application.Abstractions
{
    public interface ICoinGeckoClient
    {
        Task<IReadOnlyList<CryptoPrice>> GetLatestPricesAsync(CancellationToken cancellationToken = default);
    }
}