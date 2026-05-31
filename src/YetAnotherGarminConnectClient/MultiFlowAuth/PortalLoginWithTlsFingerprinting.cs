using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Logging;

namespace YetAnotherGarminConnectClient
{
    /// <summary>
    /// STRATEGY 4 — Portal Web + TLS Fingerprint Rotation
    /// Uses HttpCloak for JA3 fingerprinting + 10-20s anti-WAF delay
    /// 
    /// Key differences from Strategy 1 (Mobile):
    /// - Desktop browser fingerprints (more varied options)
    /// - Longer anti-WAF delay (10-20s vs no delay)
    /// - Random browser User-Agent generation
    /// - Portal API endpoint instead of mobile endpoint
    /// </summary>
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    namespace GarminConnect.Authentication
    {
        /// <summary>
        /// STRATEGY 4 — Portal Web + TLS Fingerprint Rotation
        /// Uses HttpCloak for JA3 fingerprinting + 10-20s anti-WAF delay
        /// 
        /// Key differences from Strategy 1 (Mobile):
        /// - Desktop browser fingerprints (more varied options)
        /// - Longer anti-WAF delay (10-20s vs no delay)
        /// - Random browser User-Agent generation
        /// - Portal API endpoint instead of mobile endpoint
        /// </summary>
        public class PortalLoginWithTlsFingerprinting
        {
            // Portal-specific TLS impersonation profiles (more desktop-like)
            private static readonly string[] PORTAL_IMPERSONATIONS = {
            "safari",
            "safari_ios",
            "chrome120",
            "edge101",
            "chrome",
        };

            // Portal constants
            private const string PORTAL_SSO_CLIENT_ID = "GarminConnect";

            // Anti-WAF delays (seconds) - Portal requires much longer delay than mobile
            private const double LOGIN_DELAY_MIN_S = 10.0;
            private const double LOGIN_DELAY_MAX_S = 20.0;

            private readonly ILogger _logger;
            private readonly Random _random = new();

            public PortalLoginWithTlsFingerprinting(ILogger logger = null)
            {
                _logger = logger;
            }

            /// <summary>
            /// Portal login with TLS fingerprint rotation + anti-WAF delay
            /// 
            /// Strategy:
            /// 1. Try multiple TLS fingerprints (desktop profiles)
            /// 2. For each fingerprint:
            ///    a. GET signin page (establish cookies)
            ///    b. Sleep 10-20s (anti-WAF mimics real user reading)
            ///    c. POST credentials
            /// 3. Different TLS fingerprints land in different rate-limit buckets
            /// </summary>
            public async Task<(bool Success, string ServiceTicket, Exception Error)> LoginAsync(
                string email,
                string password,
                string domain = "garmin.com")
            {
                var sso = $"https://sso.{domain}";
                var portalServiceUrl = $"https://connect.{domain}/app";
                Exception lastError = null;

                foreach (var impersonation in PORTAL_IMPERSONATIONS)
                {
                    try
                    {
                        _logger?.LogDebug("portal+cffi trying impersonation={Impersonation}", impersonation);

                        // Create HttpCloak session with specific TLS fingerprint
                        await using (var session = await CreateHttpCloakSessionAsync(impersonation))
                        {
                            var loginResult = await DoPortalWebLoginWithTlsAsync(
                                session,
                                email,
                                password,
                                sso,
                                portalServiceUrl
                            );

                            if (loginResult.Success)
                            {
                                _logger?.LogInformation("portal+cffi successful with {Impersonation}", impersonation);
                                return loginResult;
                            }

                            // If not successful but no error, continue to next impersonation
                            if (loginResult.Error != null)
                            {
                                lastError = loginResult.Error;
                            }
                        }
                    }
                    catch (GarminConnectAuthenticationException)
                    {
                        // Auth errors stop the chain immediately
                        throw;
                    }
                    catch (GarminConnectTooManyRequestsException ex)
                    {
                        _logger?.LogDebug("portal+cffi({Impersonation}) 429: {Error}", impersonation, ex.Message);
                        lastError = ex;
                        // Continue to next impersonation
                        continue;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogDebug("portal+cffi({Impersonation}) failed: {Error}", impersonation, ex.Message);
                        lastError = ex;
                        // Continue to next impersonation
                        continue;
                    }
                }

                if (lastError != null)
                {
                    return (false, null, lastError);
                }

                return (false, null, new GarminConnectConnectionException("portal+cffi: no impersonations available"));
            }

            /// <summary>
            /// Create HttpCloak session with JA3 fingerprint for portal login
            /// </summary>
            private async Task<HttpCloakSession> CreateHttpCloakSessionAsync(string ja3Profile)
            {
                var session = new HttpCloakSession();

                var sessionConfig = new SessionConfig
                {
                    // JA3 fingerprinting - desktop browser profiles
                    Ja3 = ja3Profile,

                    // Network settings
                    TlsOnly = false,
                    MaxRedirects = 5,
                    PreferIpv4 = false,

                    // TCP tuning (optional)
                    TcpTtl = null,
                    TcpMss = null,

                    // Timeout settings
                    ConnectTimeout = 30000,
                    Timeout = 30000,

                    // Retry settings (important for rate limiting)
                    Retry = 2,  // More retries for portal
                    RetryOnStatus = new[] { 429, 503 },
                    RetryWaitMin = 2000,
                    RetryWaitMax = 8000,
                };

                await session.InitializeAsync(sessionConfig);
                return session;
            }

            /// <summary>
            /// Perform portal login with TLS-spoofed session + anti-WAF delay
            /// 
            /// Portal flow:
            /// 1. GET signin page to grab cookies
            /// 2. Wait 10-20s (mimic real user behavior)
            /// 3. POST credentials
            /// </summary>
            private async Task<(bool Success, string ServiceTicket, Exception Error)> DoPortalWebLoginWithTlsAsync(
                HttpCloakSession session,
                string email,
                string password,
                string sso,
                string portalServiceUrl)
            {
                try
                {
                    var signinUrl = $"{sso}/portal/sso/en-US/sign-in";

                    // Generate random browser headers (mimics real user)
                    var browserHeaders = GenerateRandomBrowserHeaders();

                    // Step 1: GET the signin page to establish session/cookies
                    _logger?.LogDebug("Portal: Step 1 - GET signin page");
                    var getQueryParams = new Dictionary<string, string>
                {
                    { "clientId", PORTAL_SSO_CLIENT_ID },
                    { "service", portalServiceUrl },
                };

                    var getHeaders = new Dictionary<string, string>
                {
                    { "User-Agent", browserHeaders["User-Agent"] },
                    { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8" },
                    { "Accept-Language", "en-US,en;q=0.9" },
                    { "Cache-Control", "no-cache" },
                    { "Pragma", "no-cache" },
                };

                    var getResponse = await session.GetAsync(signinUrl, getQueryParams, getHeaders);

                    if (getResponse.StatusCode == 429)
                    {
                        var error = new GarminConnectTooManyRequestsException(
                            "Portal login GET returned 429 — Cloudflare blocking this request."
                        );
                        return (false, null, error);
                    }

                    if (getResponse.StatusCode == 403)
                    {
                        try
                        {
                            var tmp = Path.GetTempFileName();
                            File.WriteAllText(tmp, getResponse.Content ?? string.Empty);
                            _logger?.LogWarning("Portal+cffi GET returned 403 — body saved to {Tmp}", tmp);
                            return (false, null, new GarminConnectConnectionException($"Portal login GET returned 403 — body saved to {tmp}"));
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning("Failed to save Portal+cffi GET 403 body: {Error}", ex.Message);
                            return (false, null, new GarminConnectConnectionException("Portal login GET returned 403"));
                        }
                    }

                    if (getResponse.StatusCode >= 400)
                    {
                        var error = new GarminConnectConnectionException(
                            $"Portal login GET failed with status {getResponse.StatusCode}: { (getResponse.Content?.Length > 200 ? getResponse.Content.Substring(0,200) : getResponse.Content) }"
                        );
                        return (false, null, error);
                    }

                    // Step 2: Anti-WAF delay (10-20 seconds)
                    // Cloudflare flags rapid GET→POST sequences as bot-like
                    var delayMs = (int)(_random.NextDouble() * (LOGIN_DELAY_MAX_S - LOGIN_DELAY_MIN_S) * 1000 + LOGIN_DELAY_MIN_S * 1000);
                    _logger?.LogInformation(
                        "Portal: Step 2 - Anti-WAF delay: waiting {DelayMs}ms to avoid Cloudflare rate limiting...",
                        delayMs
                    );
                    await Task.Delay(delayMs);

                    // Step 3: POST credentials with portal API
                    _logger?.LogDebug("Portal: Step 3 - POST credentials");

                    var postHeaders = new Dictionary<string, string>
                {
                    { "User-Agent", browserHeaders["User-Agent"] },
                    { "Accept", "application/json, text/plain, */*" },
                    { "Accept-Language", "en-US,en;q=0.9" },
                    { "Content-Type", "application/json" },
                    { "Origin", sso },
                    { "Referer", $"{signinUrl}?clientId={PORTAL_SSO_CLIENT_ID}&service={portalServiceUrl}" },
                    { "Cache-Control", "no-cache" },
                };

                    var loginPayload = new
                    {
                        username = email,
                        password = password,
                        rememberMe = true,
                        captchaToken = "",
                    };

                    var loginParams = new Dictionary<string, string>
                {
                    { "clientId", PORTAL_SSO_CLIENT_ID },
                    { "locale", "en-US" },
                    { "service", portalServiceUrl },
                };

                    var postResponse = await session.PostAsJsonAsync(
                        $"{sso}/portal/api/login",
                        loginParams,
                        loginPayload,
                        postHeaders
                    );

                    if (postResponse.StatusCode == 429)
                    {
                        var error = new GarminConnectTooManyRequestsException(
                            "Portal login POST returned 429 — Cloudflare blocking this request."
                        );
                        return (false, null, error);
                    }

                    // Parse response
                    try
                    {
                        var result = JsonSerializer.Deserialize<PortalLoginResponse>(postResponse.Content);

                        // Handle MFA requirement
                        if (result?.ResponseStatus?.Type == "MFA_REQUIRED")
                        {
                            var error = new MFARequiredException();
                            return (false, null, error);
                        }

                        // Successful login
                        if (result?.ResponseStatus?.Type == "SUCCESSFUL")
                        {
                            var ticket = result.ServiceTicketId;
                            _logger?.LogDebug("Portal: Login successful, got service ticket");
                            return (true, ticket, null);
                        }

                        // Invalid credentials
                        if (result?.ResponseStatus?.Type == "INVALID_USERNAME_PASSWORD")
                        {
                            var error = new GarminConnectAuthenticationException(
                                "401 Unauthorized (Invalid Username or Password)"
                            );
                            return (false, null, error);
                        }

                        // Check for 429 buried in JSON response
                        if (result?.Error?.StatusCode == "429")
                        {
                            var error = new GarminConnectTooManyRequestsException(
                                "Portal login: 429 in JSON body"
                            );
                            return (false, null, error);
                        }

                        // Unknown error
                        var unknownError = new GarminConnectConnectionException(
                            $"Portal login failed: {result}"
                        );
                        return (false, null, unknownError);
                    }
                    catch (JsonException ex)
                    {
                        var error = new GarminConnectConnectionException(
                            $"Portal login failed (non-JSON response): HTTP {postResponse.StatusCode}",
                            ex
                        );
                        return (false, null, error);
                    }
                }
                catch (GarminConnectException ex)
                {
                    return (false, null, ex);
                }
                catch (Exception ex)
                {
                    var error = new GarminConnectConnectionException(
                        $"Portal login failed: {ex.Message}",
                        ex
                    );
                    return (false, null, error);
                }
            }

            /// <summary>
            /// Generate random browser headers to avoid detection
            /// </summary>
            private Dictionary<string, string> GenerateRandomBrowserHeaders()
            {
                var userAgents = new[]
                {
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:122.0) Gecko/20100101 Firefox/122.0",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0",
            };

                var selectedUA = userAgents[_random.Next(userAgents.Length)];

                // Include sec-ch and Sec-Fetch headers commonly seen in real browsers to reduce bot detection.
                return new Dictionary<string, string>
            {
                { "User-Agent", selectedUA },
                { "sec-ch-ua", "\"Chromium\";v=\"131\", \"Google Chrome\";v=\"131\", \"Not:A-Brand\";v=\"99\"" },
                { "sec-ch-ua-platform", "\"Windows\"" },
                { "sec-ch-ua-mobile", "?0" },
                { "Upgrade-Insecure-Requests", "1" },
                { "Sec-Fetch-Site", "same-site" },
                { "Sec-Fetch-Mode", "navigate" },
                { "Sec-Fetch-User", "?1" },
                { "Sec-Fetch-Dest", "document" },
                { "Connection", "keep-alive" },
            };
            }
        }

        /// <summary>
        /// Portal login response structure
        /// </summary>
        [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
        public class PortalLoginResponse
        {
            [JsonPropertyName("responseStatus")]
            public ResponseStatus ResponseStatus { get; set; }

            [JsonPropertyName("serviceTicketId")]
            public string ServiceTicketId { get; set; }

            [JsonPropertyName("customerMfaInfo")]
            public MfaInfo CustomerMfaInfo { get; set; }

            [JsonPropertyName("error")]
            public ErrorInfo Error { get; set; }
        }
    }
}