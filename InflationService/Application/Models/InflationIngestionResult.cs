using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InflationService.Application.Models
{
    public sealed record InflationIngestionResult(
     InflationSourceType Source,
     int? Year,
     int? Month,
     bool Success,
     bool DataFound,
     bool InsertedOrUpdated,
     string Message);
}
