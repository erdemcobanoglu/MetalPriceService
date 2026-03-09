using InflationService.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InflationService.Application.Abstractions
{
    public interface IInflationIngestionService
    {
        Task<InflationIngestionResult> IngestAsync(
            InflationSourceType source,
            CancellationToken ct);
    }
}
