using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcmbRatesWorker.Application.Abstractions;
using TcmbRatesWorker.Application.Models;
using TcmbRatesWorker.Infrastructure.Persistence.Entities;

namespace TcmbRatesWorker.Infrastructure.Persistence
{
    public sealed class RatesRepository : IRatesRepository
    {
        private readonly RatesDbContext _db;

        public RatesRepository(RatesDbContext db)
        {
            _db = db;
        }

        public Task<bool> ExistsAsync(DateOnly date, CancellationToken ct)
        {
            return _db.Rates.AnyAsync(x => x.RateDate == date, ct);
        }

        public async Task SaveAsync(RatesSnapshot snapshot, CancellationToken ct)
        {
            var createdAtUtc = DateTime.UtcNow;

            var entities = snapshot.Rates.Select(r => new RateEntity
            {
                RateDate = snapshot.Date,
                CurrencyCode = r.Code,
                Unit = r.Unit,
                ForexBuying = r.ForexBuying,
                ForexSelling = r.ForexSelling,
                BanknoteBuying = r.BanknoteBuying,
                BanknoteSelling = r.BanknoteSelling,
                CreatedAtUtc = createdAtUtc
            }).ToList();

            _db.Rates.AddRange(entities);
            await _db.SaveChangesAsync(ct);
        }
    }
}
