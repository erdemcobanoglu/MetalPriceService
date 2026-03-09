using InflationService.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InflationService.Application.Abstractions
{
    public interface IInflationRepository
    {
        Task<bool> ExistsAsync(
            InflationSourceType source,
            int year,
            int month,
            CancellationToken ct);

        Task UpsertAsync(InflationPoint point, CancellationToken ct);

        Task<InflationPoint?> GetAsync(
            InflationSourceType source,
            int year,
            int month,
            CancellationToken ct);
    }
}
