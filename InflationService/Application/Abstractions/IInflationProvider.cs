using InflationService.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InflationService.Application.Abstractions
{
    public interface IInflationProvider
    {
        InflationSourceType Source { get; }

        Task<InflationPoint?> GetLatestAsync(CancellationToken ct);

        Task<InflationPoint?> GetByPeriodAsync(int year, int month, CancellationToken ct);
    }
}
