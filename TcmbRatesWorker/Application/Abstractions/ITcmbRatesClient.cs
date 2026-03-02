using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcmbRatesWorker.Application.Models;

namespace TcmbRatesWorker.Application.Abstractions
{
    public interface ITcmbRatesClient
    {
        Task<RatesSnapshot> GetTodayAsync(CancellationToken ct);
    }
}
