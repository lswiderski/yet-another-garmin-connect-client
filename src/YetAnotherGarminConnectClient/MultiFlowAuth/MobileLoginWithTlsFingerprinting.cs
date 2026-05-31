using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using HttpCloak;
using Microsoft.Extensions.Logging;

namespace YetAnotherGarminConnectClient
{
    /// <summary>
    /// STRATEGY 1 — Mobile iOS + TLS Fingerprint Rotation
    /// Uses HttpCloak for JA3 fingerprinting to bypass Cloudflare WAF
    /// </summary>
    public class MobileLoginWithTlsFingerprinting
    {
        // TLS impersonation profiles (JA3 fingerprints)
        private static readonly string[] MOBILE_IMPERSONATIONS = {
            "safari_ios_18.0",      // iPhone iOS 18
            "safari_ios_17.5",      // iPhone iOS 17
            "chrome_120_mac",       // Chrome 120 on macOS (fallback)
            "chrome_120_windows",   // Chrome 120 on Windows
        };

        private const string IOS_SSO_CLIENT_ID = "GCM_IOS_DARK";
        private const string IOS_SERVICE_URL = "https://mobile.integration.garmin.com/gcm/ios";
        private const string IOS_LOGIN_UA = "Mozilla/5.0 (iPhone; CPU iPhone OS 18_7 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148";

        private readonly ILogger _logger;

        public MobileLoginWithTlsFingerprinting(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Mobile login with TLS fingerprint rotation
        /// Different TLS fingerprints land in different Cloudflare rate-limit buckets
        /// </summary>
        public async Task<(bool Success, string ServiceTicket, Exception Error)> LoginAsync(
            string email,
            string password,
            string domain = "garmin.com")
        {
            var sso = $"https://sso.{domain}";
            Exception lastError = null;

            foreach (var impersonation in MOBILE_IMPERSONATIONS)
            {
                try
                {
                    _logger?.LogDebug("mobile+cffi trying impersonation={Impersonation}", impersonation);

                    // Create HttpCloak session with specific TLS fingerprint
                    await using (var session = await CreateHttpCloakSessionAsync(impersonation))
                    {
                        var loginResult = await DoMobileLoginWithTlsAsync(
                            session,
                            email,
                            password,
                            sso
                        );

                        if (loginResult.Success)
                        {
                            _logger?.LogInformation("mobile+cffi successful with {Impersonation}", impersonation);
                            return loginResult;
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
                    _logger?.LogDebug("mobile+cffi({Impersonation}) 429: {Error}", impersonation, ex.Message);
                    lastError = ex;
                    // Continue to next impersonation
                    continue;
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug("mobile+cffi({Impersonation}) failed: {Error}", impersonation, ex.Message);
                    lastError = ex;
                    // Continue to next impersonation
                    continue;
                }
            }

            if (lastError != null)
            {
                return (false, null, lastError);
            }

            return (false, null, new GarminConnectConnectionException("mobile+cffi: no impersonations available"));
        }

        /// <summary>
        /// Create HttpCloak session with JA3 fingerprint
        /// </summary>
        private async Task<HttpCloakSession> CreateHttpCloakSessionAsync(string ja3Profile)
        {
            var session = new HttpCloakSession();

            var sessionConfig = new SessionConfig
            {
                // JA3 fingerprinting - matches specific browser TLS signature
                Ja3 = ja3Profile,

                // Advanced fingerprinting options
                Akamai = null,  // Can add Akamai fingerprinting if needed

                // Network settings
                TlsOnly = false,
                MaxRedirects = 5,
                PreferIpv4 = false,

                // TCP tuning (optional - mimics real browser)
                TcpTtl = null,
                TcpMss = null,
                TcpWindowSize = null,

                // Timeout settings
                ConnectTimeout = 30000,
                Timeout = 30000,

                // Retry settings
                Retry = 1,
                RetryOnStatus = new[] { 429, 503 },  // Retry on rate limit and service unavailable
                RetryWaitMin = 1000,
                RetryWaitMax = 5000,
            };

            // Initialize session with fingerprint config
            await session.InitializeAsync(sessionConfig);
            return session;
        }

        /// <summary>
        /// Perform mobile login with TLS-spoofed session
        /// </summary>
        private async Task<(bool Success, string ServiceTicket, Exception Error)> DoMobileLoginWithTlsAsync(
            HttpCloakSession session,
            string email,
            string password,
            string sso)
        {
            try
            {
                var loginUrl = $"{sso}/mobile/api/login";
                var loginParams = new
                {
                    clientId = IOS_SSO_CLIENT_ID,
                    locale = "en-US",
                    service = IOS_SERVICE_URL,
                };

                var loginHeaders = new Dictionary<string, string>
                {
                    { "User-Agent", IOS_LOGIN_UA },
                    { "Accept", "application/json, text/plain, */*" },
                    { "Content-Type", "application/json" },
                    { "Origin", sso },
                };

                var loginPayload = new
                {
                    username = email,
                    password = password,
                    rememberMe = true,
                    captchaToken = "",
                };

                // Build URL with query parameters
                var fullUrl = loginUrl
                    .SetQueryParam("clientId", IOS_SSO_CLIENT_ID)
                    .SetQueryParam("locale", "en-US")
                    .SetQueryParam("service", IOS_SERVICE_URL);

                // Perform POST with HttpCloak session (TLS fingerprint applied)
                var response = await session.PostAsJsonAsync(
                    fullUrl,
                    null,
                    loginPayload,
                    headers: loginHeaders
                );

                var responseContent = response.Content;

                // Check for 429 rate limiting
                if (response.StatusCode == (int)System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new GarminConnectTooManyRequestsException(
                        "Mobile login returned 429 — IP rate limited by Garmin"
                    );
                }

                // Parse response
                var result = JsonSerializer.Deserialize<MobileLoginResponse>(responseContent);

                // Handle MFA requirement
                if (result?.ResponseStatus?.Type == "MFA_REQUIRED")
                {
                    // This would be handled by the caller
                    throw new MFARequiredException();
                }

                // Successful login
                if (result?.ResponseStatus?.Type == "SUCCESSFUL")
                {
                    var ticket = result.ServiceTicketId;
                    return (true, ticket, null);
                }

                // Invalid credentials
                if (result?.ResponseStatus?.Type == "INVALID_USERNAME_PASSWORD")
                {
                    throw new GarminConnectAuthenticationException(
                        "401 Unauthorized (Invalid Username or Password)"
                    );
                }

                // Check for 429 buried in JSON response
                if (result?.Error?.StatusCode == "429")
                {
                    throw new GarminConnectTooManyRequestsException(
                        "Mobile login: 429 in JSON body"
                    );
                }

                // Unknown error
                throw new GarminConnectConnectionException(
                    $"Mobile login failed: {result}"
                );
            }
            catch (HttpRequestException ex)
            {
                return (false, null, new GarminConnectConnectionException(
                    $"Mobile login request failed: {ex.Message}", ex
                ));
            }
            catch (GarminConnectException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return (false, null, new GarminConnectConnectionException(
                    $"Mobile login failed: {ex.Message}", ex
                ));
            }
        }
    }


}