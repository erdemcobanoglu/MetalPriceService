using InflationService.Application.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace InflationService.Infrastructure.Parsing
{
    public sealed class EnagInflationParser
    {
        private static readonly CultureInfo TrCulture = new("tr-TR");

        private static readonly Dictionary<string, int> Months = new(StringComparer.OrdinalIgnoreCase)
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

        public InflationPoint? Parse(string? content, string? sourceUrl = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            // 1) Önce satır / tablo / structured benzeri içerikleri dene
            var structured = TryParseStructuredContent(content, sourceUrl);
            if (structured is not null)
                return structured;

            // 2) Sonra normal html/text parse
            var rawNormalized = Normalize(content, stripHtml: false);
            var visibleNormalized = Normalize(content, stripHtml: true);
            var searchable = $"{rawNormalized} {visibleNormalized}".Trim();

            if (LooksLikeSpaShell(searchable))
                return null;

            var period = TryParsePeriod(searchable);
            if (period is null)
                return null;

            var monthlyRate = TryParseMonthlyRate(searchable);
            var annualRate = TryParseAnnualRate(searchable);
            var indexValue = TryParseIndexValue(searchable);

            if (monthlyRate is null && annualRate is null && indexValue is null)
                return null;

            return BuildPoint(
                period.Value.Year,
                period.Value.Month,
                monthlyRate,
                annualRate,
                indexValue,
                sourceUrl);
        }

        private static InflationPoint? TryParseStructuredContent(string content, string? sourceUrl)
        {
            var lines = SplitLines(content);
            if (lines.Count == 0)
                return null;

            var best = default((int Year, int Month, decimal? Monthly, decimal? Annual, decimal? Index)?);

            foreach (var line in lines)
            {
                var normalizedLine = Normalize(line, stripHtml: true);
                if (string.IsNullOrWhiteSpace(normalizedLine))
                    continue;

                var period = TryParsePeriod(normalizedLine);
                if (period is null)
                    continue;

                var monthly = TryParseMonthlyRate(normalizedLine);
                var annual = TryParseAnnualRate(normalizedLine);
                var indexValue = TryParseIndexValue(normalizedLine);

                if (monthly is null && annual is null && indexValue is null)
                {
                    var numbers = ExtractDecimals(normalizedLine).ToList();

                    if (numbers.Count > 0)
                    {
                        if (monthly is null)
                            monthly = numbers.FirstOrDefault(x => x > -50m && x < 50m);

                        if (annual is null)
                            annual = numbers.FirstOrDefault(x => x > 0m && x < 500m);

                        if (indexValue is null)
                        {
                            var candidate = numbers
                                .Where(x => x >= 100m)
                                .OrderByDescending(x => x)
                                .FirstOrDefault();

                            if (candidate != 0m)
                                indexValue = candidate;
                        }
                    }
                }

                if (monthly is null && annual is null && indexValue is null)
                    continue;

                if (best is null || IsLater(period.Value, (best.Value.Year, best.Value.Month)))
                {
                    best = (period.Value.Year, period.Value.Month, monthly, annual, indexValue);
                }
            }

            if (best is null)
                return null;

            return BuildPoint(
                best.Value.Year,
                best.Value.Month,
                best.Value.Monthly,
                best.Value.Annual,
                best.Value.Index,
                sourceUrl);
        }

        private static bool LooksLikeSpaShell(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return true;

            var shellSignals = new[]
            {
                "id=\"root\"",
                "div id=\"root\"",
                "type=\"module\" crossorigin src=",
                "/assets/index-",
                "lovable-badge",
                "edit with lovable",
                "independent economic intelligence",
                "financial ratings",
                "strategic consulting"
            };

            foreach (var signal in shellSignals)
            {
                if (text.Contains(signal, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static (int Year, int Month)? TryParsePeriod(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var patterns = new[]
            {
                @"\b(ocak|subat|mart|nisan|mayis|haziran|temmuz|agustos|eylul|ekim|kasim|aralik)\s+(20\d{2})\b",
                @"\b(20\d{2})\s+(ocak|subat|mart|nisan|mayis|haziran|temmuz|agustos|eylul|ekim|kasim|aralik)\b",
                @"\b(20\d{2})[-/.](0?[1-9]|1[0-2])\b",
                @"\b(0?[1-9]|1[0-2])[-/.](20\d{2})\b",
                @"e[-\s]?tufe[\s,:-]{0,30}(ocak|subat|mart|nisan|mayis|haziran|temmuz|agustos|eylul|ekim|kasim|aralik)\s+(20\d{2})",
                @"enagrup[\s\S]{0,40}?(ocak|subat|mart|nisan|mayis|haziran|temmuz|agustos|eylul|ekim|kasim|aralik)\s+(20\d{2})"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (!match.Success)
                    continue;

                if (pattern.Contains(@"(20\d{2})[-/.]"))
                {
                    if (int.TryParse(match.Groups[1].Value, out var year1) &&
                        int.TryParse(match.Groups[2].Value, out var month1) &&
                        month1 >= 1 && month1 <= 12)
                    {
                        return (year1, month1);
                    }

                    continue;
                }

                if (pattern.Contains(@"\b(0?[1-9]|1[0-2])[-/.](20\d{2})\b"))
                {
                    if (int.TryParse(match.Groups[1].Value, out var month2) &&
                        int.TryParse(match.Groups[2].Value, out var year2) &&
                        month2 >= 1 && month2 <= 12)
                    {
                        return (year2, month2);
                    }

                    continue;
                }

                string monthName;
                string yearText;

                if (Regex.IsMatch(match.Groups[1].Value, @"20\d{2}"))
                {
                    yearText = match.Groups[1].Value;
                    monthName = match.Groups[2].Value;
                }
                else
                {
                    monthName = match.Groups[1].Value;
                    yearText = match.Groups[2].Value;
                }

                if (!Months.TryGetValue(monthName, out var month))
                    continue;

                if (!int.TryParse(yearText, out var year))
                    continue;

                return (year, month);
            }

            return null;
        }

        private static decimal? TryParseMonthlyRate(string text)
        {
            var patterns = new[]
            {
                @"e[-\s]?tufe\s*aylik[\s:]*%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)",
                @"enagrup\s*tuketici\s*fiyat\s*endeksi\s*aylik[\s:]*%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)",
                @"aylik\s*%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)\s*(?:artis|azalis|oldu|olarak)?",
                @"month(?:ly)?[\s:]*%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)",
                @"monthly[\s:]*%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)",
                @"e[-\s]?tufe[\s\S]{0,80}?aylik[\s\S]{0,40}?%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)"
            };

            foreach (var pattern in patterns)
            {
                var parsed = TryMatchDecimal(text, pattern);
                if (parsed.HasValue)
                    return parsed.Value;
            }

            return TryParsePercentageAfterLabels(text,
                "aylik",
                "e-tufe aylik",
                "e tufe aylik",
                "enagrup tuketici fiyat endeksi aylik",
                "monthly");
        }

        private static decimal? TryParseAnnualRate(string text)
        {
            var patterns = new[]
            {
                @"e[-\s]?tufe\s*yillik[\s:]*%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)",
                @"son\s*on\s*iki\s*aylik[\s:]*%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)",
                @"yillik\s*%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)\s*(?:artis|azalis|oldu|olarak)?",
                @"year(?:ly)?[\s:]*%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)",
                @"annual[\s:]*%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)",
                @"e[-\s]?tufe[\s\S]{0,100}?yillik[\s\S]{0,40}?%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)"
            };

            foreach (var pattern in patterns)
            {
                var parsed = TryMatchDecimal(text, pattern);
                if (parsed.HasValue)
                    return parsed.Value;
            }

            return TryParsePercentageAfterLabels(text,
                "yillik",
                "e-tufe yillik",
                "e tufe yillik",
                "son on iki aylik",
                "annual",
                "yearly");
        }

        private static decimal? TryParsePercentageAfterLabels(string text, params string[] labels)
        {
            foreach (var label in labels)
            {
                var pattern = Regex.Escape(label) + @"[\s\S]{0,160}?%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)";
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
            var patterns = new[]
            {
                @"e[-\s]?tufe[\s\S]{0,80}?endeks[\s\S]{0,40}?(\d{2,6}(?:[.,]\d{1,4})?)",
                @"e[-\s]?tufe[\s\S]{0,80}?(\d{3,6}(?:[.,]\d{1,4})?)",
                @"endeks[\s\S]{0,80}?(\d{2,6}(?:[.,]\d{1,4})?)",
                @"index[\s\S]{0,80}?(\d{2,6}(?:[.,]\d{1,4})?)"
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

        private static IEnumerable<decimal> ExtractDecimals(string text)
        {
            var matches = Regex.Matches(text, @"[-+]?\d{1,6}(?:[.,]\d{1,4})?");
            foreach (Match match in matches)
            {
                var parsed = TryParseDecimal(match.Value);
                if (parsed.HasValue)
                    yield return parsed.Value;
            }
        }

        private static decimal? TryMatchDecimal(string text, string pattern)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success)
                return null;

            return TryParseDecimal(match.Groups[1].Value);
        }

        private static decimal? TryParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            value = value.Trim().Replace("%", string.Empty);

            if (decimal.TryParse(value, NumberStyles.Any, TrCulture, out var tr))
                return tr;

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var inv))
                return inv;

            var normalized = value.Replace(".", string.Empty).Replace(",", ".");
            if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var normalizedValue))
                return normalizedValue;

            return null;
        }

        private static bool IsLater((int Year, int Month) current, (int Year, int Month) other)
        {
            return current.Year > other.Year ||
                   (current.Year == other.Year && current.Month > other.Month);
        }

        private static InflationPoint BuildPoint(
            int year,
            int month,
            decimal? monthlyRate,
            decimal? annualRate,
            decimal? indexValue,
            string? sourceUrl)
        {
            return new InflationPoint(
                Source: InflationSourceType.Enag,
                Year: year,
                Month: month,
                MonthlyRate: monthlyRate,
                AnnualRate: annualRate,
                IndexValue: indexValue,
                RetrievedAtUtc: DateTime.UtcNow,
                RawSourceUrl: sourceUrl);
        }

        private static List<string> SplitLines(string content)
        {
            return content
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        private static string Normalize(string? content, bool stripHtml)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            var text = WebUtility.HtmlDecode(content).ToLowerInvariant();

            if (stripHtml)
            {
                text = Regex.Replace(text, "<script\\b[^>]*>[\\s\\S]*?</script>", " ", RegexOptions.IgnoreCase);
                text = Regex.Replace(text, "<style\\b[^>]*>[\\s\\S]*?</style>", " ", RegexOptions.IgnoreCase);
                text = Regex.Replace(text, "<.*?>", " ");
            }

            text = text.Replace("&nbsp;", " ")
                       .Replace("ş", "s")
                       .Replace("ğ", "g")
                       .Replace("ü", "u")
                       .Replace("ı", "i")
                       .Replace("ö", "o")
                       .Replace("ç", "c")
                       .Replace("\\n", " ")
                       .Replace("\\r", " ")
                       .Replace("\\t", " ")
                       .Replace("\\/", "/");

            text = Regex.Replace(text, @"\s+", " ");
            return text.Trim();
        }
    }
}