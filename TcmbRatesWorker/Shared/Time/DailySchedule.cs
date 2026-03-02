using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcmbRatesWorker.Shared.Time
{
    public static class DailySchedule
    {
        public static DateTimeOffset GetNextRunUtc(
            DateTimeOffset nowUtc,
            TimeZoneInfo timeZone,
            int hour,
            int minute)
        {
            var localNow = TimeZoneInfo.ConvertTime(nowUtc, timeZone);

            var localTarget = new DateTimeOffset(
                localNow.Year,
                localNow.Month,
                localNow.Day,
                hour,
                minute,
                0,
                localNow.Offset);

            if (localNow >= localTarget)
                localTarget = localTarget.AddDays(1);

            // DateTimeOffset → UTC dönüşümü (doğru yol)
            return localTarget.ToUniversalTime();
        }

        public static DateOnly GetTodayLocalDate(
            DateTimeOffset nowUtc,
            TimeZoneInfo timeZone)
        {
            var localNow = TimeZoneInfo.ConvertTime(nowUtc, timeZone);
            return DateOnly.FromDateTime(localNow.DateTime);
        }
    }
}