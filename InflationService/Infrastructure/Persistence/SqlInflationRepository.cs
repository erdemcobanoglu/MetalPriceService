using InflationService.Application.Abstractions;
using InflationService.Application.Models;
using InflationService.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InflationService.Infrastructure.Persistence
{
    public sealed class SqlInflationRepository : IInflationRepository
    {
        private readonly InflationDbContext _dbContext;

        public SqlInflationRepository(InflationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> ExistsAsync(
            InflationSourceType source,
            int year,
            int month,
            CancellationToken ct)
        {
            return await _dbContext.InflationSeries
                .AsNoTracking()
                .AnyAsync(
                    x => x.Source == (int)source &&
                         x.Year == year &&
                         x.Month == month,
                    ct);
        }

        public async Task<InflationPoint?> GetAsync(
            InflationSourceType source,
            int year,
            int month,
            CancellationToken ct)
        {
            var entity = await _dbContext.InflationSeries
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Source == (int)source &&
                         x.Year == year &&
                         x.Month == month,
                    ct);

            return entity is null ? null : Map(entity);
        }

        public async Task UpsertAsync(InflationPoint point, CancellationToken ct)
        {
            var entity = await _dbContext.InflationSeries
                .FirstOrDefaultAsync(
                    x => x.Source == (int)point.Source &&
                         x.Year == point.Year &&
                         x.Month == point.Month,
                    ct);

            if (entity is null)
            {
                entity = new InflationSeriesEntity
                {
                    Source = (int)point.Source,
                    Year = point.Year,
                    Month = point.Month,
                    MonthlyRate = point.MonthlyRate,
                    AnnualRate = point.AnnualRate,
                    IndexValue = point.IndexValue,
                    RetrievedAtUtc = point.RetrievedAtUtc,
                    RawSourceUrl = point.RawSourceUrl
                };

                _dbContext.InflationSeries.Add(entity);
            }
            else
            {
                entity.MonthlyRate = point.MonthlyRate;
                entity.AnnualRate = point.AnnualRate;
                entity.IndexValue = point.IndexValue;
                entity.RetrievedAtUtc = point.RetrievedAtUtc;
                entity.RawSourceUrl = point.RawSourceUrl;
            }

            await _dbContext.SaveChangesAsync(ct);
        }

        private static InflationPoint Map(InflationSeriesEntity entity)
        {
            return new InflationPoint(
                Source: (InflationSourceType)entity.Source,
                Year: entity.Year,
                Month: entity.Month,
                MonthlyRate: entity.MonthlyRate,
                AnnualRate: entity.AnnualRate,
                IndexValue: entity.IndexValue,
                RetrievedAtUtc: DateTime.SpecifyKind(entity.RetrievedAtUtc, DateTimeKind.Utc),
                RawSourceUrl: entity.RawSourceUrl);
        }
    }
}
