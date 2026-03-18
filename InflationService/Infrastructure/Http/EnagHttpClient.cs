using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
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
                _logger.LogInformation("Fetching ENAG inflation data from {Url}", url);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
                request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                request.Headers.TryAddWithoutValidation("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
                request.Headers.TryAddWithoutValidation("Cache-Control", "no-cache");
                request.Headers.TryAddWithoutValidation("Pragma", "no-cache");

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "ENAG request failed for {Url}. StatusCode={StatusCode}",
                        url,
                        (int)response.StatusCode);

                    return null;
                }

                var contentType = response.Content.Headers.ContentType?.ToString();
                var body = await response.Content.ReadAsStringAsync(ct);

                _logger.LogInformation(
                    "ENAG response received from {Url}. ContentType={ContentType}, Length={Length}",
                    url,
                    contentType ?? "unknown",
                    body?.Length ?? 0);

                LogContentPreview(url, body);

                return body;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch ENAG inflation data from {Url}", url);
                return null;
            }
        }

        public IReadOnlyList<string> GetCandidateUrls()
        {
            return new[]
            {
                "https://enag.ai",
                "https://enag.ai/robots.txt"
            };
        }

        private void LogContentPreview(string url, string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("ENAG response body is empty for {Url}", url);
                return;
            }

            var preview = content.Length <= 1000
                ? content
                : content[..1000];

            preview = preview
                .Replace("\r", " ")
                .Replace("\n", " ");

            _logger.LogInformation(
                "ENAG response preview for {Url}: {Preview}",
                url,
                preview);
        }
    }
}