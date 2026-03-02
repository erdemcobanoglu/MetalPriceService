using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcmbRatesWorker.Application.Models
{
    public sealed class MoneyRate
    { 
        public string Code { get; init; } = "";
         
        public int Unit { get; init; }

        public decimal? ForexBuying { get; init; }
        public decimal? ForexSelling { get; init; }

        public decimal? BanknoteBuying { get; init; }
        public decimal? BanknoteSelling { get; init; }
    }
}
