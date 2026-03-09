using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InflationService.Application.Models
{
    public sealed record InflationPoint(
    InflationSourceType Source,
    int Year,
    int Month,
    decimal? MonthlyRate,
    decimal? AnnualRate,
    decimal? IndexValue,
    DateTime RetrievedAtUtc,
    string? RawSourceUrl);
}
