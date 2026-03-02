using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TcmbRatesWorker.Application.Models;

namespace TcmbRatesWorker.Infrastructure.Parsing
{
    public static class TcmbXmlParser
    {
        public static RatesSnapshot Parse(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                throw new ArgumentException("XML content is empty.", nameof(xml));

            var doc = XDocument.Parse(xml);
            var root = doc.Root ?? throw new InvalidOperationException("Invalid TCMB XML: root element missing.");

            // <Tarih_Date Tarih="dd.MM.yyyy" ...>
            var tarih = root.Attribute("Tarih")?.Value;
            if (!DateTime.TryParseExact(
                    tarih,
                    "dd.MM.yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var dt))
            {
                throw new InvalidOperationException("Invalid TCMB XML: Tarih attribute is missing or not in dd.MM.yyyy format.");
            }

            var date = DateOnly.FromDateTime(dt);

            static decimal? ParseDecimal(string? s)
            {
                if (string.IsNullOrWhiteSpace(s)) return null;

                s = s.Trim();

                // Önce invariant (1.2345), sonra tr-TR (1,2345)
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var inv))
                    return inv;

                if (decimal.TryParse(s, NumberStyles.Any, new CultureInfo("tr-TR"), out var tr))
                    return tr;

                return null;
            }

            var rates = doc.Descendants("Currency")
                .Select(c =>
                {
                    var code = (string?)c.Attribute("CurrencyCode");
                    if (string.IsNullOrWhiteSpace(code))
                        return null;

                    var unitText = c.Element("Unit")?.Value;
                    _ = int.TryParse(unitText, out var unit);

                    return new MoneyRate
                    {
                        Code = code,
                        Unit = unit,
                        ForexBuying = ParseDecimal(c.Element("ForexBuying")?.Value),
                        ForexSelling = ParseDecimal(c.Element("ForexSelling")?.Value),
                        BanknoteBuying = ParseDecimal(c.Element("BanknoteBuying")?.Value),
                        BanknoteSelling = ParseDecimal(c.Element("BanknoteSelling")?.Value),
                    };
                })
                .Where(x => x is not null)
                .Cast<MoneyRate>()
                .ToList();

            return new RatesSnapshot
            {
                Date = date,
                Rates = rates
            };
        }
    }
}
