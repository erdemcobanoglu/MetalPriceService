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
        Task SaveSnapshotAsync(PriceSnapshot snapshot, CancellationToken cancellationToken = default);
        Task<PriceSnapshot?> GetLatestSnapshotAsync(CancellationToken cancellationToken = default);
    }
}
