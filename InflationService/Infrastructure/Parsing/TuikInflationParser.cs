using InflationService.Application.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InflationService.Infrastructure.Parsing
{
    public sealed class TuikInflationParser
    {
        private static readonly CultureInfo TrCulture = new("tr-TR");

        public InflationPoint? Parse(string? content, string? sourceUrl = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            var normalized = Normalize(content);

            var period = TryParsePeriod(normalized);
            if (period is null)
                return null;

            var monthlyRate = TryParsePercentageAfterLabels(normalized,
                "bir onceki aya gore degisim",
                "aylik degisim",
                "aylik");

            var annualRate = TryParsePercentageAfterLabels(normalized,
                "bir onceki yilin ayni ayina gore degisim",
                "yillik degisim",
                "yillik");

            var indexValue = TryParseIndexValue(normalized);

            return new InflationPoint(
                Source: InflationSourceType.Tuik,
                Year: period.Value.Year,
                Month: period.Value.Month,
                MonthlyRate: monthlyRate,
                AnnualRate: annualRate,
                IndexValue: indexValue,
                RetrievedAtUtc: DateTime.UtcNow,
                RawSourceUrl: sourceUrl);
        }

        private static (int Year, int Month)? TryParsePeriod(string text)
        {
            // Örnek: "subat 2026", "ocak 2025", "mart 2024"
            var months = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["ocak"] = 1,
                ["subat"] = 2,
                ["mart"] = 3,
                ["nisan"] = 4,
                ["mayis"] = 5,
                ["haziran"] = 6,
                ["temmuz"] = 7,
                ["agustos"] = 8,
                ["eylul"] = 9,
                ["ekim"] = 10,
                ["kasim"] = 11,
                ["aralik"] = 12
            };

            var match = Regex.Match(
                text,
                @"\b(ocak|subat|mart|nisan|mayis|haziran|temmuz|agustos|eylul|ekim|kasim|aralik)\s+(20\d{2})\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (!match.Success)
                return null;

            var monthName = match.Groups[1].Value;
            var yearText = match.Groups[2].Value;

            if (!months.TryGetValue(monthName, out var month))
                return null;

            if (!int.TryParse(yearText, out var year))
                return null;

            return (year, month);
        }

        private static decimal? TryParsePercentageAfterLabels(string text, params string[] labels)
        {
            foreach (var label in labels)
            {
                // Label'dan sonra ilk yüzde/ondalıklı değeri bul
                var pattern = Regex.Escape(label) + @"[\s\S]{0,120}?([-+]?\d{1,3}(?:[.,]\d{1,4})?)";
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                if (!match.Success)
                    continue;

                var parsed = TryParseDecimal(match.Groups[1].Value);
                if (parsed.HasValue)
                    return parsed.Value;
            }

            return null;
        }

        private static decimal? TryParseIndexValue(string text)
        {
            // Esnek yaklaşım: "endeks", "tufe", "genel" gibi alanlardan sonra sayı ara
            var patterns = new[]
            {
            @"endeks[\s\S]{0,80}?(\d{2,6}(?:[.,]\d{1,4})?)",
            @"tuketici fiyat endeksi[\s\S]{0,80}?(\d{2,6}(?:[.,]\d{1,4})?)",
            @"genel[\s\S]{0,80}?(\d{2,6}(?:[.,]\d{1,4})?)"
        };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (!match.Success)
                    continue;

                var parsed = TryParseDecimal(match.Groups[1].Value);
                if (parsed.HasValue)
                    return parsed.Value;
            }

            return null;
        }

        private static decimal? TryParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            value = value.Trim().Replace("%", string.Empty);

            // Türkçe format: 2,96
            if (decimal.TryParse(value, NumberStyles.Any, TrCulture, out var tr))
                return tr;

            // Invariant fallback: 2.96
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var inv))
                return inv;

            // Binlik ayıracı vs normalize
            var normalized = value.Replace(".", string.Empty).Replace(",", ".");
            if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var normalizedValue))
                return normalizedValue;

            return null;
        }

        private static string Normalize(string content)
        {
            var text = content.ToLowerInvariant();

            text = text.Replace("&nbsp;", " ");
            text = text.Replace("ş", "s")
                       .Replace("ğ", "g")
                       .Replace("ü", "u")
                       .Replace("ı", "i")
                       .Replace("ö", "o")
                       .Replace("ç", "c");

            text = Regex.Replace(text, "<.*?>", " ");
            text = Regex.Replace(text, @"\s+", " ");

            return text.Trim();
        }
    }
}
