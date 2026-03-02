using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcmbRatesWorker.Application.Models
{
    public class RatesSnapshot
    {
        public DateOnly Date { get; init; }
        public IReadOnlyList<MoneyRate> Rates { get; init; } = Array.Empty<MoneyRate>();
    }
}
