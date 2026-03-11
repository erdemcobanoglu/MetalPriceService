using InflationService.Application.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace InflationService.Infrastructure.Parsing
{
    public sealed class TuikInflationParser
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

            // 1) Önce download / csv / tablo benzeri içerikleri dene
            var structured = TryParseStructuredContent(content, sourceUrl);
            if (structured is not null)
                return structured;

            // 2) Sonra klasik metin / html parse
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

            return BuildPoint(period.Value.Year, period.Value.Month, monthlyRate, annualRate, indexValue, sourceUrl);
        }

        private static InflationPoint? TryParseStructuredContent(string content, string? sourceUrl)
        {
            var lines = SplitLines(content);
            if (lines.Count == 0)
                return null;

            var delimited = TryParseDelimitedTable(lines, sourceUrl);
            if (delimited is not null)
                return delimited;

            var rowBased = TryParseRowLikeContent(lines, sourceUrl);
            if (rowBased is not null)
                return rowBased;

            return null;
        }

        private static InflationPoint? TryParseDelimitedTable(List<string> lines, string? sourceUrl)
        {
            var delimiter = DetectDelimiter(lines);
            if (delimiter is null)
                return null;

            for (var i = 0; i < Math.Min(lines.Count, 8); i++)
            {
                var rawHeader = lines[i];
                var headerCells = SplitDelimitedLine(rawHeader, delimiter.Value);
                if (headerCells.Length < 2)
                    continue;

                var normalizedHeaders = headerCells
                    .Select(h => Normalize(h, stripHtml: true))
                    .ToArray();

                if (!LooksLikeHeaderRow(normalizedHeaders))
                    continue;

                var periodIndex = FindHeaderIndex(normalizedHeaders, "donem", "tarih", "date", "period");
                var yearIndex = FindHeaderIndex(normalizedHeaders, "yil", "year");
                var monthIndex = FindHeaderIndex(normalizedHeaders, "ay", "month");
                var monthlyIndex = FindHeaderIndex(normalizedHeaders,
                    "aylik",
                    "bir onceki aya gore",
                    "month over month",
                    "monthly");
                var annualIndex = FindHeaderIndex(normalizedHeaders,
                    "yillik",
                    "bir onceki yilin ayni ayina gore",
                    "year over year",
                    "annual");
                var indexValueIndex = FindHeaderIndex(normalizedHeaders,
                    "endeks",
                    "endeks degeri",
                    "tufe",
                    "cpi",
                    "genel");

                var bestRow = default((int Year, int Month, decimal? Monthly, decimal? Annual, decimal? Index)?);

                for (var rowIndex = i + 1; rowIndex < lines.Count; rowIndex++)
                {
                    var rowCells = SplitDelimitedLine(lines[rowIndex], delimiter.Value);
                    if (rowCells.Length == 0)
                        continue;

                    var rowText = Normalize(string.Join(" ", rowCells), stripHtml: true);

                    var period = TryParsePeriodFromCells(rowCells, periodIndex, yearIndex, monthIndex)
                                 ?? TryParsePeriod(rowText);

                    if (period is null)
                        continue;

                    var monthly = GetDecimalCell(rowCells, monthlyIndex);
                    var annual = GetDecimalCell(rowCells, annualIndex);
                    var indexValue = GetDecimalCell(rowCells, indexValueIndex);

                    if (monthly is null && annual is null && indexValue is null)
                    {
                        monthly = TryParseMonthlyRate(rowText);
                        annual = TryParseAnnualRate(rowText);
                        indexValue = TryParseIndexValue(rowText);
                    }

                    if (monthly is null && annual is null && indexValue is null)
                        continue;

                    if (bestRow is null || IsLater(period.Value, (bestRow.Value.Year, bestRow.Value.Month)))
                    {
                        bestRow = (period.Value.Year, period.Value.Month, monthly, annual, indexValue);
                    }
                }

                if (bestRow is not null)
                {
                    return BuildPoint(
                        bestRow.Value.Year,
                        bestRow.Value.Month,
                        bestRow.Value.Monthly,
                        bestRow.Value.Annual,
                        bestRow.Value.Index,
                        sourceUrl);
                }
            }

            return null;
        }

        private static InflationPoint? TryParseRowLikeContent(List<string> lines, string? sourceUrl)
        {
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
                    var numbers = ExtractDecimals(normalizedLine);
                    indexValue = numbers
                        .Where(n => n >= 100m)
                        .OrderByDescending(n => n)
                        .FirstOrDefault();

                    if (indexValue == 0m)
                        indexValue = null;
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

        private static List<string> SplitLines(string content)
        {
            return content
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        private static char? DetectDelimiter(List<string> lines)
        {
            var candidates = new[] { ';', '\t', '|', ',' };

            foreach (var candidate in candidates)
            {
                var score = lines
                    .Take(10)
                    .Count(line => line.Count(ch => ch == candidate) >= 2);

                if (score >= 2)
                    return candidate;
            }

            return null;
        }

        private static string[] SplitDelimitedLine(string line, char delimiter)
        {
            return line
                .Split(delimiter)
                .Select(x => x.Trim().Trim('"'))
                .ToArray();
        }

        private static bool LooksLikeHeaderRow(string[] headers)
        {
            return headers.Any(h =>
                h.Contains("aylik") ||
                h.Contains("yillik") ||
                h.Contains("endeks") ||
                h.Contains("tufe") ||
                h.Contains("donem") ||
                h.Contains("tarih") ||
                h.Contains("year") ||
                h.Contains("month"));
        }

        private static int? FindHeaderIndex(string[] headers, params string[] keywords)
        {
            for (var i = 0; i < headers.Length; i++)
            {
                foreach (var keyword in keywords)
                {
                    if (headers[i].Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }

            return null;
        }

        private static (int Year, int Month)? TryParsePeriodFromCells(
            string[] cells,
            int? periodIndex,
            int? yearIndex,
            int? monthIndex)
        {
            if (periodIndex.HasValue && periodIndex.Value < cells.Length)
            {
                var period = TryParsePeriod(Normalize(cells[periodIndex.Value], stripHtml: true));
                if (period is not null)
                    return period;
            }

            if (yearIndex.HasValue && monthIndex.HasValue &&
                yearIndex.Value < cells.Length &&
                monthIndex.Value < cells.Length)
            {
                var yearCell = Normalize(cells[yearIndex.Value], stripHtml: true);
                var monthCell = Normalize(cells[monthIndex.Value], stripHtml: true);

                if (int.TryParse(yearCell, out var year))
                {
                    if (int.TryParse(monthCell, out var monthNumber) && monthNumber >= 1 && monthNumber <= 12)
                        return (year, monthNumber);

                    if (Months.TryGetValue(monthCell, out var monthNameValue))
                        return (year, monthNameValue);
                }
            }

            return null;
        }

        private static decimal? GetDecimalCell(string[] cells, int? index)
        {
            if (!index.HasValue || index.Value < 0 || index.Value >= cells.Length)
                return null;

            return TryParseDecimal(cells[index.Value]);
        }

        private static bool LooksLikeSpaShell(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return true;

            var shellSignals = new[]
            {
                "javascript gerekli",
                "javascript required",
                "you need to enable javascript",
                "bu web sitesini kullanabilmek icin tarayicinizda javascript",
                "id=\"root\"",
                "div id=\"root\"",
                "/assets/index-",
                "type=\"module\" crossorigin src="
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
                @"tuketici fiyat endeksi[\s,:-]{0,30}(ocak|subat|mart|nisan|mayis|haziran|temmuz|agustos|eylul|ekim|kasim|aralik)\s+(20\d{2})",
                @"tufe[\s,:-]{0,30}(ocak|subat|mart|nisan|mayis|haziran|temmuz|agustos|eylul|ekim|kasim|aralik)\s+(20\d{2})",
                @"\b(20\d{2})\s+(ocak|subat|mart|nisan|mayis|haziran|temmuz|agustos|eylul|ekim|kasim|aralik)\b",
                @"\b(20\d{2})[-/.](0?[1-9]|1[0-2])\b",
                @"\b(0?[1-9]|1[0-2])[-/.](20\d{2})\b"
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
                @"aylik\s*%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)\s*(?:artis|azalis|oldu|olarak)",
                @"aylik\s*degisim[\s:]*%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)",
                @"bir\s*onceki\s*aya\s*gore\s*(?:degisim|artis|azalis)?[\s:]*%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)",
                @"bir\s*onceki\s*aya\s*gore[\s\S]{0,80}?%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)",
                @"month(?:ly)?[\s\S]{0,40}?%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)",
                @"monthly[\s:]*%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)",
                @"aylik[\s\S]{0,50}?%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)"
            };

            foreach (var pattern in patterns)
            {
                var parsed = TryMatchDecimal(text, pattern);
                if (parsed.HasValue)
                    return parsed.Value;
            }

            return null;
        }

        private static decimal? TryParseAnnualRate(string text)
        {
            var patterns = new[]
            {
                @"yillik\s*%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)\s*(?:artis|azalis|oldu|olarak)",
                @"yillik\s*degisim[\s:]*%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)",
                @"bir\s*onceki\s*yilin\s*ayni\s*ayina\s*gore\s*(?:degisim|artis|azalis)?[\s:]*%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)",
                @"bir\s*onceki\s*yilin\s*ayni\s*ayina\s*gore[\s\S]{0,100}?%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)",
                @"year(?:ly)?[\s\S]{0,40}?%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)",
                @"annual[\s:]*%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)",
                @"yillik[\s\S]{0,50}?%?\s*([-+]?\d{1,3}(?:[.,]\d{1,4})?)"
            };

            foreach (var pattern in patterns)
            {
                var parsed = TryMatchDecimal(text, pattern);
                if (parsed.HasValue)
                    return parsed.Value;
            }

            return null;
        }

        private static decimal? TryParseIndexValue(string text)
        {
            var patterns = new[]
            {
                @"endeks\s*(?:degeri|deger|seviyesi)?[\s:]*?(\d{2,6}(?:[.,]\d{1,4})?)",
                @"index\s*(?:value)?[\s:]*?(\d{2,6}(?:[.,]\d{1,4})?)",
                @"tuketici fiyat endeksi[\s\S]{0,120}?(\d{3,6}(?:[.,]\d{1,4})?)",
                @"tufe[\s\S]{0,80}?(\d{3,6}(?:[.,]\d{1,4})?)",
                @"cpi[\s\S]{0,80}?(\d{3,6}(?:[.,]\d{1,4})?)",
                @"genel[\s\S]{0,80}?(\d{3,6}(?:[.,]\d{1,4})?)"
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
                Source: InflationSourceType.Tuik,
                Year: year,
                Month: month,
                MonthlyRate: monthlyRate,
                AnnualRate: annualRate,
                IndexValue: indexValue,
                RetrievedAtUtc: DateTime.UtcNow,
                RawSourceUrl: sourceUrl);
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