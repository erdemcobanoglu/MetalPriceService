using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcmbRatesWorker.Application.Abstractions;
using TcmbRatesWorker.Application.Models;
using TcmbRatesWorker.Infrastructure.Parsing;

namespace TcmbRatesWorker.Infrastructure.Http
{
    public sealed class TcmbRatesClient : ITcmbRatesClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _todayUrl;

        public TcmbRatesClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
             
            _todayUrl = configuration["Tcmb:TodayUrl"]
                ?? "https://www.tcmb.gov.tr/kurlar/today.xml";
        }

        public async Task<RatesSnapshot> GetTodayAsync(CancellationToken ct)
        {
            var xml = await _httpClient.GetStringAsync(_todayUrl, ct);
            return TcmbXmlParser.Parse(xml);
        }
    }
}
