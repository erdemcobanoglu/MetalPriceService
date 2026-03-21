using CoinMarketCap.Service.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinMarketCap.Service.Application.Abstractions
{
    public interface ICoinMarketCapClient
    {
        Task<IReadOnlyCollection<CryptoPrice>> GetLatestPricesAsync(
            IReadOnlyCollection<string> symbols,
            string convertCurrency,
            CancellationToken cancellationToken = default);
    }
}
