using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InflationService.Infrastructure.Persistence.Entities
{
    public sealed class InflationSeriesEntity
    {
        public long Id { get; set; }

        public int Source { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public decimal? MonthlyRate { get; set; }

        public decimal? AnnualRate { get; set; }

        public decimal? IndexValue { get; set; }

        public DateTime RetrievedAtUtc { get; set; }

        public string? RawSourceUrl { get; set; }
    }
}
