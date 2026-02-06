using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetalPrice.Service.Helper
{
    public static class ScheduleHelper
    {
        public static TimeOnly ParseTimeOnly(string hhmm)
        {
            if (!TimeOnly.TryParseExact(hhmm, "HH:mm", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var t))
                throw new FormatException($"Invalid time format: '{hhmm}'. Use HH:mm (e.g., 09:00).");
            return t;
        }

        public static DateTime GetNextRun(DateTime nowLocal, List<TimeOnly> times)
        {
            if (times == null || times.Count == 0)
                throw new InvalidOperationException("No schedule times defined.");

            var today = nowLocal.Date;

            var candidates = times
                .Select(t => today.Add(t.ToTimeSpan()))
                .OrderBy(x => x);

            var nextToday = candidates.FirstOrDefault(x => x >= nowLocal);

            return nextToday != default
                ? nextToday
                : today.AddDays(1).Add(times.Min().ToTimeSpan());
        }

    }

}
