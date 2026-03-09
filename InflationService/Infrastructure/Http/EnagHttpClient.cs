using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InflationService.Infrastructure.Http
{
    public sealed class EnagHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EnagHttpClient> _logger;

        public EnagHttpClient(HttpClient httpClient, ILogger<EnagHttpClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string?> GetLatestInflationContentAsync(CancellationToken ct)
        {
            var url = "https://enag.ai";

            try
            {
                _logger.LogInformation("Fetching ENAG inflation data from {Url}", url);

                using var response = await _httpClient.GetAsync(url, ct);

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch ENAG inflation data.");
                return null;
            }
        }
    }
}
