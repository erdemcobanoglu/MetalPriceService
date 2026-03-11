using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
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
            foreach (var url in GetCandidateUrls())
            {
                var content = await GetContentAsync(url, ct);
                if (!string.IsNullOrWhiteSpace(content))
                    return content;
            }

            return null;
        }

        public async Task<string?> GetContentAsync(string url, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Fetching TÜİK inflation data from {Url}", url);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
                request.Headers.TryAddWithoutValidation("Accept", "*/*");
                request.Headers.TryAddWithoutValidation("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
                request.Headers.TryAddWithoutValidation("Cache-Control", "no-cache");
                request.Headers.TryAddWithoutValidation("Pragma", "no-cache");
                request.Headers.Referrer = new Uri("https://veriportali.tuik.gov.tr/");

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "TÜİK request failed for {Url}. StatusCode={StatusCode}",
                        url,
                        (int)response.StatusCode);

                    return null;
                }

                return await response.Content.ReadAsStringAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch TÜİK inflation data from {Url}", url);
                return null;
            }
        }

        public IReadOnlyList<string> GetCandidateUrls()
        {
            return new[]
            {
                // Resmi veri indirme endpoint'leri (öncelikli)
                "https://veriportali.tuik.gov.tr/api/tr/data/downloads?p=diFoDUZTm7VTAC2F98n4kMYkPjZiWA9W2bGPuyS88QCYCEy2%2F6AMofZpGqyHWN2jZuKthw7ieO6f%2BtZdzn7V%2BVlDFmdHXOs12a%2BF0yMn9h8qVAdkS8ygTxgllHAd4TM5vz%2Bzzxrzgia8OrGCfXUOaXe%2FmSzZuhk1KaDIAsrsmaJaXeLfJEPO7qmgtJ%2BKlnRfx9ezemvIWG13ALEz3UFPjnk3JS1SaIB0KXz3pdlToHg%3D&t=r",
                "https://veriportali.tuik.gov.tr/api/tr/data/downloads?p=UU6%2F4Q8kuktx7jXIyR6IK%2B0QJ8POzwpbRED1w60cL1MIiRI2Rv0td9Nozj8SWZe4zcQtwWUBgMxjEdu2xYloufbgdayQA5doyExxeH%2BtidFpFfqmgGElqqP4GpVFycdfThs4htQDnXxuMiBCFUXovDkMqEN6ZFEuuuKTIPZOXfc%3D&t=r",
                "https://veriportali.tuik.gov.tr/api/tr/data/downloads?p=j6MHQTJFC0NQ3fmvO17gwbjOZywb5rRH9sjX9XP9h7lWqjV6YRRKpQhciYVjiieu88GszJbpISdncc7BKR5cGYTuFqw3JpQuO64Ul0uCpDrhGO0QY8QcFTTi32TzHkHK6do2nhVoJrE2W2PDFTuZTOf%2BcnXEBOx3z73r4a2HsP9o809R1B2TqLwc6%2FbEljyBFspXFcZ3RCRE087iXV3t8w%3D%3D&t=r",

                // Son çare olarak HTML sayfaları
                "https://data.tuik.gov.tr/Bulten/Index?p=Tuketici-Fiyat-Endeksi",
                "https://veriportali.tuik.gov.tr/Bulten/Index?p=Tuketici-Fiyat-Endeksi",
                "https://veriportali.tuik.gov.tr/tr/press/category/18"
            };
        }
    }
}