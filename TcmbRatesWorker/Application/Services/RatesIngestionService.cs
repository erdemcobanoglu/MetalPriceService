using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcmbRatesWorker.Application.Abstractions;
using TcmbRatesWorker.Application.Models;

namespace TcmbRatesWorker.Application.Services
{
    public sealed class RatesIngestionService
    {
        private readonly ITcmbRatesClient _client;
        private readonly IRatesRepository _repo;
        private readonly ILogger<RatesIngestionService> _logger;

        public RatesIngestionService(
            ITcmbRatesClient client,
            IRatesRepository repo,
            ILogger<RatesIngestionService> logger)
        {
            _client = client;
            _repo = repo;
            _logger = logger;
        }

        public async Task IngestTodayAsync(DateOnly expectedDate, CancellationToken ct)
        { 
            if (await _repo.ExistsAsync(expectedDate, ct))
            {
                _logger.LogInformation("Rates already exist for {Date}. Skipping.", expectedDate);
                return;
            }

            RatesSnapshot snapshot = await _client.GetTodayAsync(ct);
             
            if (snapshot.Date != expectedDate)
            {
                _logger.LogWarning(
                    "Snapshot date {SnapshotDate} != expected date {ExpectedDate}. Not saving.",
                    snapshot.Date, expectedDate);
                return;
            }

            await _repo.SaveAsync(snapshot, ct);

            _logger.LogInformation("Saved {Count} rates for {Date}.",
                snapshot.Rates.Count, snapshot.Date);
        }
    }
}
