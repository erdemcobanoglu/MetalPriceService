using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcmbRatesWorker.Infrastructure.Persistence.Entities
{
    public sealed class RateEntity
    {
        public int Id { get; set; }

        // TCMB actuel tarihi
        public DateOnly RateDate { get; set; }

        // USD, EUR vs.
        public string CurrencyCode { get; set; } = null!;

        public int Unit { get; set; }

        public decimal? ForexBuying { get; set; }
        public decimal? ForexSelling { get; set; }

        public decimal? BanknoteBuying { get; set; }
        public decimal? BanknoteSelling { get; set; }

        // audit
        public DateTime CreatedAtUtc { get; set; }
    }
}
