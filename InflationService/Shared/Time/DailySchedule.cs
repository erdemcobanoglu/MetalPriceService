using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InflationService.Shared.Time
{
    public static class DailySchedule
    {
        public static DateTimeOffset GetNextRunUtc(
            DateTimeOffset nowUtc,
            TimeZoneInfo timeZone,
            int runHour,
            int runMinute)
        {
            var localNow = TimeZoneInfo.ConvertTime(nowUtc, timeZone);

            var nextLocal = new DateTimeOffset(
                localNow.Year,
                localNow.Month,
                localNow.Day,
                runHour,
                runMinute,
                0,
                localNow.Offset);

            if (localNow >= nextLocal)
            {
                nextLocal = nextLocal.AddDays(1);
            }

            return nextLocal.ToUniversalTime();
        }

        public static DateOnly GetTodayLocalDate(DateTimeOffset nowUtc, TimeZoneInfo timeZone)
        {
            var localNow = TimeZoneInfo.ConvertTime(nowUtc, timeZone);
            return DateOnly.FromDateTime(localNow.DateTime);
        }
    }
}
