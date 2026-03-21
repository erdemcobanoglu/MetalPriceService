using CoinMarketCap.Service.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinMarketCap.Service.Application.Abstractions
{
    public interface IPriceRepository
    {
        Task SaveSnapshotAsync(CryptoPriceSnapshot snapshot, CancellationToken cancellationToken = default);
        Task<CryptoPriceSnapshot?> GetLatestSnapshotAsync(CancellationToken cancellationToken = default);
    }
}
