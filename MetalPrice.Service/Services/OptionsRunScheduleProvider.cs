using MetalPrice.Service.Abstractions;
using MetalPrice.Service.Helper;
using MetalPrice.Service.Options;
using Microsoft.Extensions.Options;

namespace MetalPrice.Service.Services
{
    public sealed class OptionsRunScheduleProvider : IRunScheduleProvider
    {
        private readonly IOptionsMonitor<MetalPriceOptions> _opt;

        public OptionsRunScheduleProvider(IOptionsMonitor<MetalPriceOptions> opt)
        {
            _opt = opt;
        }

        public Task<IReadOnlyList<TimeOnly>> GetTimesAsync(CancellationToken ct)
        {
            var rawTimes = _opt.CurrentValue.Times ?? Array.Empty<string>();

            var parsed = rawTimes
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(ScheduleHelper.ParseTimeOnly)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            if (parsed.Count == 0)
            {
                parsed = new List<TimeOnly>
                {
                    new TimeOnly(9, 0),
                    new TimeOnly(18, 0)
                };
            }

            return Task.FromResult<IReadOnlyList<TimeOnly>>(parsed);
        }
    }
}
