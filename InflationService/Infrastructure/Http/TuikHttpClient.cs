using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InflationService.Infrastructure.Http
{
    public sealed class TuikHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TuikHttpClient> _logger;

        public TuikHttpClient(HttpClient httpClient, ILogger<TuikHttpClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string?> GetLatestInflationContentAsync(CancellationToken ct)
        {
            // TÜİK CPI bülten sayfası
            var url = "https://data.tuik.gov.tr/Bulten/Index?p=Tuketici-Fiyat-Endeksi";

            try
            {
                _logger.LogInformation("Fetching TÜİK inflation data from {Url}", url);

                using var response = await _httpClient.GetAsync(url, ct);

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch TÜİK inflation data.");
                return null;
            }
        }
    }
}
