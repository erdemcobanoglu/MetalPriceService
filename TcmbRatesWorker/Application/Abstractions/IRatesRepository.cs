using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcmbRatesWorker.Application.Models;

namespace TcmbRatesWorker.Application.Abstractions
{
    public interface IRatesRepository
    { 
        Task<bool> ExistsAsync(DateOnly date, CancellationToken ct);
         
        Task SaveAsync(RatesSnapshot snapshot, CancellationToken ct);
    }
}
