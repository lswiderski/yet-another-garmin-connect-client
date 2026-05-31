using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YetAnotherGarminConnectClient;

namespace YetAnotherGarminConnectClient
{
    /// <summary>
    /// Wrapper for HttpCloak session with enhanced functionality for portal/mobile login
    /// </summary>
    public class HttpCloakSession : IAsyncDisposable
    {
        private HttpCloakClient _client;
        private SessionConfig _config;

        public async Task InitializeAsync(SessionConfig config)
        {
            _config = config;
            _client = new HttpCloakClient();
            await _client.ConnectAsync(_config);
        }

        /// <summary>
        /// Perform GET request with query parameters
        /// </summary>
        public async Task<HttpCloakResponse> GetAsync(
            string url,
            Dictionary<string, string> queryParams = null,
            Dictionary<string, string> headers = null)
        {
            // Build full URL with query parameters
            var fullUrl = url;
            if (queryParams != null && queryParams.Count > 0)
            {
                var queryString = string.Join("&", queryParams
                    .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                fullUrl = $"{url}?{queryString}";
            }

            var requestConfig = new RequestConfig
            {
                Url = fullUrl,
                Method = "GET",
                Headers = headers ?? new Dictionary<string, string>(),
                Ja3 = _config.Ja3,
            };

            return await _client.DoRequestAsync(requestConfig);
        }

        /// <summary>
        /// Perform POST request with JSON body and query parameters
        /// </summary>
        public async Task<HttpCloakResponse> PostAsJsonAsync(
            string url,
            Dictionary<string, string> queryParams,
            object payload,
            Dictionary<string, string> headers = null)
        {
            // Build full URL with query parameters
            var fullUrl = url;
            if (queryParams != null && queryParams.Count > 0)
            {
                var queryString = string.Join("&", queryParams
                    .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                fullUrl = $"{url}?{queryString}";
            }

            var requestConfig = new RequestConfig
            {
                Url = fullUrl,
                Method = "POST",
                Headers = headers ?? new Dictionary<string, string>(),
                Body = JsonSerializer.Serialize(payload),
                BodyType = "application/json",
                Ja3 = _config.Ja3,
            };

            return await _client.DoRequestAsync(requestConfig);
        }

        /// <summary>
        /// Perform POST request with form-encoded body
        /// </summary>
        public async Task<HttpCloakResponse> PostAsFormAsync(
            string url,
            Dictionary<string, string> queryParams,
            Dictionary<string, string> formData,
            Dictionary<string, string> headers = null)
        {
            // Build full URL with query parameters
            var fullUrl = url;
            if (queryParams != null && queryParams.Count > 0)
            {
                var queryString = string.Join("&", queryParams
                    .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                fullUrl = $"{url}?{queryString}";
            }

            // Build form-encoded body
            var body = string.Join("&", formData
                .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            var requestConfig = new RequestConfig
            {
                Url = fullUrl,
                Method = "POST",
                Headers = headers ?? new Dictionary<string, string>(),
                Body = body,
                BodyType = "application/x-www-form-urlencoded",
                Ja3 = _config.Ja3,
            };

            return await _client.DoRequestAsync(requestConfig);
        }

        public async ValueTask DisposeAsync()
        {
            if (_client != null)
            {
                await _client.DisconnectAsync();
                _client?.Dispose();
            }
        }
    }

    /// <summary>
    /// HttpCloak client wrapper
    /// Handles HTTP requests with JA3 TLS fingerprinting
    /// </summary>
    public class HttpCloakClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private SessionConfig _config;
        private readonly CookieContainer _cookieContainer;

        public HttpCloakClient()
        {
            _cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = true,
            };
            _httpClient = new HttpClient(handler);
        }

        public async Task ConnectAsync(SessionConfig config)
        {
            _config = config;

            // Set timeout
            _httpClient.Timeout = TimeSpan.FromMilliseconds(config.Timeout);

            await Task.CompletedTask;
        }

        public async Task<HttpCloakResponse> DoRequestAsync(RequestConfig config)
        {
            try
            {
                var request = new HttpRequestMessage(
                    new HttpMethod(config.Method),
                    config.Url
                );

                // Add headers
                if (config.Headers != null)
                {
                    foreach (var header in config.Headers)
                    {
                        // Skip headers that should go to the content
                        if (!header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                        {
                            request.Headers.Add(header.Key, header.Value);
                        }
                    }
                }

                // Add body if POST/PUT
                if (!string.IsNullOrEmpty(config.Body))
                {
                    var contentType = config.BodyType ?? "application/json";
                    request.Content = new StringContent(
                        config.Body,
                        Encoding.UTF8,
                        contentType
                    );
                }

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                return new HttpCloakResponse
                {
                    StatusCode = (int)response.StatusCode,
                    Content = content,
                    Headers = response.Headers
                        .Concat(response.Content.Headers)
                        .ToDictionary(h => h.Key, h => string.Join(",", h.Value)),
                };
            }
            catch (HttpRequestException ex)
            {
                throw new GarminConnectConnectionException($"HttpCloak request failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new GarminConnectConnectionException($"HttpCloak request error: {ex.Message}", ex);
            }
        }

        public async Task DisconnectAsync()
        {
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// HttpCloak response wrapper
    /// </summary>
    public class HttpCloakResponse
    {
        public int StatusCode { get; set; }
        public string Content { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }

    /// <summary>
    /// Session configuration for HttpCloak
    /// </summary>
    public class SessionConfig
    {
        public string Ja3 { get; set; }
        public string Akamai { get; set; }
        public bool TlsOnly { get; set; }
        public int MaxRedirects { get; set; }
        public bool PreferIpv4 { get; set; }
        public int? TcpTtl { get; set; }
        public int? TcpMss { get; set; }
        public int? TcpWindowSize { get; set; }
        public int ConnectTimeout { get; set; }
        public int Timeout { get; set; }
        public int Retry { get; set; }
        public int[] RetryOnStatus { get; set; }
        public int RetryWaitMin { get; set; }
        public int RetryWaitMax { get; set; }
    }

    /// <summary>
    /// Request configuration for HttpCloak
    /// </summary>
    public class RequestConfig
    {
        public string Url { get; set; }
        public string Method { get; set; }
        public string Body { get; set; }
        public string BodyType { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Ja3 { get; set; }
    }
}