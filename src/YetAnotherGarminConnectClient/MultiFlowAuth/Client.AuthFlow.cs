using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YetAnotherGarminConnectClient.Dto;
using YetAnotherGarminConnectClient.Dto.Garmin;
using YetAnotherGarminConnectClient.GarminConnect.Authentication;

namespace YetAnotherGarminConnectClient
{
    public class GarminConnectClient
    {

        // iOS mobile app constants (Strategy 1 & 2)
        private const string IOS_SSO_CLIENT_ID = "GCM_IOS_DARK";
        private const string IOS_SERVICE_URL = "https://mobile.integration.garmin.com/gcm/ios";
        private const string IOS_LOGIN_UA = "Mozilla/5.0 (iPhone; CPU iPhone OS 18_7 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148";

        // Android mobile app constants
        private const string MOBILE_SSO_CLIENT_ID = "GCM_ANDROID_DARK";
        private const string MOBILE_SSO_SERVICE_URL = "https://mobile.integration.garmin.com/gcm/android";
        private const string MOBILE_SSO_USER_AGENT = "Mozilla/5.0 (Linux; Android 14; Pixel 8 Pro) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36";

        // Portal (fallback) constants
        private const string PORTAL_SSO_CLIENT_ID = "GarminConnect";
        private const string DESKTOP_USER_AGENT = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

        // Anti-WAF delay bounds (seconds)
        private const double LOGIN_DELAY_MIN_S = 10.0;
        private const double LOGIN_DELAY_MAX_S = 20.0;
        private const double WIDGET_DELAY_MIN_S = 3.0;
        private const double WIDGET_DELAY_MAX_S = 8.0;

        // TLS impersonation profiles (for reference - requires separate implementation)
        private static readonly string[] MOBILE_IMPERSONATIONS = { "safari_ios", "safari", "chrome120" };
        private static readonly string[] PORTAL_IMPERSONATIONS = { "safari", "safari_ios", "chrome120", "edge101", "chrome" };

        // Native API constants
        private const string NATIVE_API_USER_AGENT = "GCM-Android-5.23";
        private const string NATIVE_X_GARMIN_USER_AGENT = "com.garmin.android.apps.connectmobile/5.23; ; Google/sdk_gphone64_arm64/google; Android/33; Dalvik/2.1.0";
        private const string DI_TOKEN_URL_BASE = "https://diauth.garmin.com/di-oauth2-service/oauth/token";
        private const string DI_GRANT_TYPE = "https://connectapi.garmin.com/di-oauth2-service/oauth/grant/service_ticket";
        private static readonly string[] DI_CLIENT_IDS = {
            "GARMIN_CONNECT_MOBILE_ANDROID_DI_2025Q2",
            "GARMIN_CONNECT_MOBILE_ANDROID_DI_2024Q4",
            "GARMIN_CONNECT_MOBILE_ANDROID_DI",
            "GARMIN_CONNECT_MOBILE_IOS_DI",
        };

        // Regex helpers
        private static readonly Regex CSRF_REGEX = new(@"name=""_csrf""\s+value=""(.+?)""", RegexOptions.Compiled);
        private static readonly Regex TITLE_REGEX = new(@"<title>(.+?)</title>", RegexOptions.Compiled);
        private static readonly Regex TICKET_REGEX = new(@"embed\?ticket=([^""]+)""", RegexOptions.Compiled);

        private readonly string _domain;
        private readonly string _sso;
        private readonly string _connect;
        private readonly string _connectapi;
        private readonly string _portalServiceUrl;
        private readonly string _diTokenUrl;
        private readonly ILogger<GarminConnectClient> _logger;

        // Authentication tokens
        public string DiToken { get; set; }
        public string DiRefreshToken { get; set; }
        public string DiClientId { get; set; }
        public string JwtWeb { get; set; }
        public string CsrfToken { get; set; }

        // MFA state
        private string _mfaMethod;
        private string _mfaFlow;
        private string _mfaServiceUrl;
        private Dictionary<string, string> _mfaLoginParams;
        private Dictionary<string, string> _mfaPostHeaders;
        private HttpResponseMessage _widgetLastResp;

        public bool IsAuthenticated => !string.IsNullOrEmpty(DiToken) || !string.IsNullOrEmpty(JwtWeb);

        public GarminConnectClient(
            string domain = "garmin.com",
            ILogger<GarminConnectClient> logger = null)
        {
            _domain = domain;
            _sso = $"https://sso.{domain}";
            _connect = $"https://connect.{domain}";
            _connectapi = $"https://connectapi.{domain}";
            _portalServiceUrl = $"https://connect.{domain}/app";
            _diTokenUrl = $"https://diauth.{domain}/di-oauth2-service/oauth/token";
            _logger = logger;

            // Configure Flurl
            FlurlHttp.ConfigureClient(domain, builder =>
            {
                builder.WithTimeout(TimeSpan.FromSeconds(30));
                builder.WithAutoRedirect(true);
            });
        }


        private List<(string Name, Func<Task> Handler)> GetStrategies(string email, string password)
        {
            return new List<(string Name, Func<Task> Handler)>
            {
                ("mobile+cffi", () => MobileLoginCffiAsync(email, password)),
                ("mobile+requests", () => MobileLoginRequestsAsync(email, password)),
                ("widget", () => WidgetWebLoginAsync(email, password)),
                ("portal+cffi", () => PortalLoginCffiAsync(email, password)),
                ("portal+requests", () => PortalWebLoginRequestsAsync(email, password)),
            };
        }

        /// <summary>
        /// Log in using a cascading 5-strategy chain.
        /// </summary>
        public async Task<(string, object)> LoginAsync(
            string email,
            string password,
            Func<Task<string>> promptMfa = null,
            bool returnOnMfa = false,
            List<string> limitToStrategies = null)
        {

            List<(string Name, Func<Task> Handler)> strategies = GetStrategies(email, password);
            if (limitToStrategies != null)
            {
                strategies = strategies.Where(s => limitToStrategies.Contains(s.Name)).ToList();
            }

            Exception lastError = null;
            int rateLimitedCount = 0;

            foreach (var (strategyName, handler) in strategies)
            {
                try
                {
                    _logger?.LogDebug("Trying login strategy: {Strategy}", strategyName);
                    await handler();
                    _logger?.LogInformation("Login successful with strategy: {Strategy}", strategyName);
                    return (null, null);
                }
                catch (GarminConnectAuthenticationException)
                {
                    throw;
                }
                catch (MFARequiredException)
                {
                    if (returnOnMfa)
                        return ("needs_mfa", null);

                    if (promptMfa != null)
                    {
                        var mfaCode = await promptMfa();
                        await CompleteMfaAsync(mfaCode);
                        return (null, null);
                    }

                    throw new GarminConnectAuthenticationException("MFA Required but no prompt mechanism supplied");
                }
                catch (GarminConnectTooManyRequestsException e)
                {
                    _logger?.LogWarning("Strategy {Strategy} returned 429: {Error}", strategyName, e.Message);
                    rateLimitedCount++;
                    lastError = e;
                }
                catch (Exception e)
                {
                    _logger?.LogWarning("Strategy {Strategy} failed: {Error}", strategyName, e.Message);
                    lastError = e;
                }
            }

            if (rateLimitedCount == strategies.Count)
                throw new GarminConnectTooManyRequestsException("All login strategies rate limited (429)");

            throw new GarminConnectConnectionException($"All login strategies exhausted: {lastError?.Message}");
        }



        /// <summary>
        /// STRATEGY 1 — Mobile iOS + curl_cffi (TLS fingerprint rotation)
        /// </summary>
        private async Task MobileLoginCffiAsync(string email, string password)
        {
            var mobileLogin = new MobileLoginWithTlsFingerprinting(_logger);
            var result = await mobileLogin.LoginAsync(email, password, _domain);

            if (result.Success)
            {
                await EstablishSessionAsync(result.ServiceTicket, IOS_SERVICE_URL);
                return;
            }

            throw result.Error ?? new GarminConnectConnectionException("Mobile+CFfi login failed");
        }


        /// <summary>
        /// STRATEGY 2 — Mobile iOS + plain requests
        /// </summary>
        private async Task MobileLoginRequestsAsync(string email, string password)
        {
            await DoMobileLoginAsync(email, password);
        }

        /// <summary>
        /// Shared mobile login logic
        /// </summary>
        private async Task DoMobileLoginAsync(string email, string password)
        {
            var loginUrl = $"{_sso}/mobile/api/login";
            var loginParams = new Dictionary<string, string>
            {
                ["clientId"] = IOS_SSO_CLIENT_ID,
                ["locale"] = "en-US",
                ["service"] = IOS_SERVICE_URL,
            };

            var loginHeaders = new Dictionary<string, string>
            {
                { "User-Agent", IOS_LOGIN_UA },
                { "Accept", "application/json, text/plain, */*" },
                { "Content-Type", "application/json" },
                { "Origin", _sso },
            };

            var loginPayload = new
            {
                username = email,
                password = password,
                rememberMe = true,
                captchaToken = "",
            };

            try
            {
                var response = await loginUrl
                    .SetQueryParams(loginParams)
                    .WithHeaders(loginHeaders)
                    .PostJsonAsync(loginPayload);

                var responseContent = await response.ResponseMessage.Content.ReadAsStringAsync();

                if (response.StatusCode == (int)System.Net.HttpStatusCode.TooManyRequests)
                    throw new GarminConnectTooManyRequestsException("Mobile login returned 429");

                var result = JsonSerializer.Deserialize<MobileLoginResponse>(responseContent);

                if (result?.ResponseStatus?.Type == "MFA_REQUIRED")
                {
                    _mfaMethod = result.CustomerMfaInfo?.MfaLastMethodUsed ?? "email";
                    _mfaLoginParams = loginParams;
                    _mfaPostHeaders = loginHeaders;
                    _mfaServiceUrl = IOS_SERVICE_URL;
                    _mfaFlow = "ios";
                    throw new MFARequiredException();
                }

                if (result?.ResponseStatus?.Type == "SUCCESSFUL")
                {
                    var ticket = result.ServiceTicketId;
                    await EstablishSessionAsync(ticket, IOS_SERVICE_URL);
                    return;
                }

                if (result?.ResponseStatus?.Type == "INVALID_USERNAME_PASSWORD")
                    throw new GarminConnectAuthenticationException("401 Unauthorized (Invalid Username or Password)");

                if (result?.Error?.StatusCode == "429")
                    throw new GarminConnectTooManyRequestsException("Mobile login: 429 in JSON body");

                throw new GarminConnectConnectionException($"Mobile login failed: {result}");
            }
            catch (HttpRequestException ex)
            {
                throw new GarminConnectConnectionException($"Mobile login failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// STRATEGY 3 — SSO Embed Widget (HTML form flow)
        /// </summary>
        private async Task WidgetWebLoginAsync(string email, string password)
        {
            var ssoBase = $"{_sso}/sso";
            var ssoEmbed = $"{ssoBase}/embed";
            var embedParams = new Dictionary<string, string>
            {
                { "id", "gauth-widget" },
                { "embedWidget", "true" },
                { "gauthHost", ssoBase },
            };

            var signinParams = new Dictionary<string, string>(embedParams)
            {
                { "gauthHost", ssoEmbed },
                { "service", ssoEmbed },
                { "source", ssoEmbed },
                { "redirectAfterAccountLoginUrl", ssoEmbed },
                { "redirectAfterAccountCreationUrl", ssoEmbed },
            };

            try
            {
                // Step 1: GET embed page
                var embedResponse = await ssoEmbed
                    .SetQueryParams(embedParams)
                    .GetAsync();

                if (embedResponse.StatusCode == (int)System.Net.HttpStatusCode.TooManyRequests)
                    throw new GarminConnectTooManyRequestsException("Widget embed GET returned 429");

                embedResponse.ResponseMessage.EnsureSuccessStatusCode();

                // Step 2: GET signin page for CSRF token
                var signinResponse = await $"{ssoBase}/signin"
                    .SetQueryParams(signinParams)
                    .WithHeader("Referer", ssoEmbed)
                    .GetAsync();

                if (signinResponse.StatusCode == (int)System.Net.HttpStatusCode.TooManyRequests)
                    throw new GarminConnectTooManyRequestsException("Widget signin GET returned 429");

                var signinContent = await signinResponse.ResponseMessage.Content.ReadAsStringAsync();
                var csrfMatch = CSRF_REGEX.Match(signinContent);

                if (!csrfMatch.Success)
                    throw new GarminConnectConnectionException("Widget login: missing CSRF token");

                var csrfToken = csrfMatch.Groups[1].Value;

                // Anti-WAF delay
                var delayMs = (int)(new Random().NextDouble() * (WIDGET_DELAY_MAX_S - WIDGET_DELAY_MIN_S) * 1000 + WIDGET_DELAY_MIN_S * 1000);
                _logger?.LogDebug("Widget login: waiting {DelayMs}ms anti-WAF delay", delayMs);
                await Task.Delay(delayMs);

                // Step 3: POST credentials
                var postResponse = await $"{ssoBase}/signin"
                    .SetQueryParams(signinParams)
                    .WithHeader("Referer", signinResponse.ResponseMessage.RequestMessage.RequestUri.ToString())
                    .PostAsync(new StringContent(
                        $"username={Uri.EscapeDataString(email)}&password={Uri.EscapeDataString(password)}&embed=true&_csrf={Uri.EscapeDataString(csrfToken)}",
                        Encoding.UTF8,
                        "application/x-www-form-urlencoded"));

                if (postResponse.StatusCode == (int)System.Net.HttpStatusCode.TooManyRequests)
                    throw new GarminConnectTooManyRequestsException("Widget signin POST returned 429");

                var postContent = await postResponse.ResponseMessage.Content.ReadAsStringAsync();
                var titleMatch = TITLE_REGEX.Match(postContent);
                var title = titleMatch.Success ? titleMatch.Groups[1].Value : "";
                var titleLower = title.ToLower();

                // Detect server errors
                if (new[] { "bad gateway", "service unavailable", "cloudflare", "502", "503" }.Any(h => titleLower.Contains(h)))
                    throw new GarminConnectConnectionException($"Widget login: server error '{title}'");

                // Detect credential errors
                if (new[] { "locked", "invalid", "incorrect", "account error" }.Any(h => titleLower.Contains(h)))
                    throw new GarminConnectAuthenticationException($"Widget authentication failed: '{title}'");

                if (titleLower.Contains("mfa"))
                {
                    _mfaLoginParams = signinParams;
                    _mfaPostHeaders = new Dictionary<string, string> { { "Referer", postResponse.ResponseMessage.RequestMessage.RequestUri.ToString() } };
                    _mfaFlow = "widget";
                    _widgetLastResp = postResponse.ResponseMessage;
                    throw new MFARequiredException();
                }

                if (title != "Success")
                    throw new GarminConnectConnectionException($"Widget login: unexpected title '{title}'");

                var ticketMatch = TICKET_REGEX.Match(postContent);
                if (!ticketMatch.Success)
                    throw new GarminConnectConnectionException("Widget login: missing service ticket");

                await EstablishSessionAsync(ticketMatch.Groups[1].Value, ssoEmbed);
            }
            catch (HttpRequestException ex)
            {
                throw new GarminConnectConnectionException($"Widget login failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// STRATEGY 4 — Portal Web + TLS Fingerprint Rotation (NEW)
        /// </summary>
        private async Task PortalLoginCffiAsync(string email, string password)
        {
            var portalLogin = new PortalLoginWithTlsFingerprinting(_logger);
            var result = await portalLogin.LoginAsync(email, password, _domain);

            if (result.Success)
            {
                await EstablishSessionAsync(result.ServiceTicket, _portalServiceUrl);
                return;
            }

            throw result.Error ?? new GarminConnectConnectionException("Portal+CFFI login failed");
        }

        /// <summary>
        /// STRATEGY 5 — Portal web + plain requests
        /// </summary>
        private async Task PortalWebLoginRequestsAsync(string email, string password)
        {
            await DoPortalWebLoginAsync(email, password);
        }

        /// <summary>
        /// Shared portal login logic
        /// </summary>
        private async Task DoPortalWebLoginAsync(string email, string password)
        {
            var signinUrl = $"{_sso}/portal/sso/en-US/sign-in";
            var browserHeaders = GenerateRandomBrowserHeaders();

            try
            {
                // Step 1: GET the signin page
                var getResponse = await signinUrl
                    .SetQueryParams(new { clientId = PORTAL_SSO_CLIENT_ID, service = _portalServiceUrl })
                    .WithHeaders(browserHeaders)
                    .WithHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
                    .WithHeader("Accept-Language", "en-US,en;q=0.9")
                    .GetAsync();

                if (getResponse.StatusCode == (int)System.Net.HttpStatusCode.TooManyRequests)
                    throw new GarminConnectTooManyRequestsException("Portal login GET returned 429");

                // Anti-WAF delay: 10-20 seconds
                var delayMs = (int)(new Random().NextDouble() * (LOGIN_DELAY_MAX_S - LOGIN_DELAY_MIN_S) * 1000 + LOGIN_DELAY_MIN_S * 1000);
                _logger?.LogInformation("Portal login: waiting {DelayMs}ms to avoid Cloudflare rate limiting", delayMs);
                await Task.Delay(delayMs);

                // Step 2: POST credentials
                var loginParams = new Dictionary<string, string>
                {
                    ["clientId"] = PORTAL_SSO_CLIENT_ID,
                    ["locale"] = "en-US",
                    ["service"] = _portalServiceUrl
                };
                var postHeaders = new Dictionary<string, string>(browserHeaders)
                {
                    { "Accept", "application/json, text/plain, */*" },
                    { "Accept-Language", "en-US,en;q=0.9" },
                    { "Content-Type", "application/json" },
                    { "Origin", _sso },
                    { "Referer", $"{signinUrl}?clientId={PORTAL_SSO_CLIENT_ID}&service={_portalServiceUrl}" },
                };

                var loginPayload = new
                {
                    username = email,
                    password = password,
                    rememberMe = true,
                    captchaToken = "",
                };

                var postResponse = await $"{_sso}/portal/api/login"
                    .SetQueryParams(loginParams)
                    .WithHeaders(postHeaders)
                    .PostJsonAsync(loginPayload);

                if (postResponse.StatusCode == (int)System.Net.HttpStatusCode.TooManyRequests)
                    throw new GarminConnectTooManyRequestsException("Portal login POST returned 429");

                var postContent = await postResponse.ResponseMessage.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PortalLoginResponse>(postContent);

                if (result?.ResponseStatus?.Type == "MFA_REQUIRED")
                {
                    _mfaMethod = result.CustomerMfaInfo?.MfaLastMethodUsed ?? "email";
                    _mfaLoginParams = loginParams;
                    _mfaPostHeaders = postHeaders;
                    _mfaServiceUrl = _portalServiceUrl;
                    _mfaFlow = "portal";
                    throw new MFARequiredException();
                }

                if (result?.ResponseStatus?.Type == "SUCCESSFUL")
                {
                    await EstablishSessionAsync(result.ServiceTicketId, _portalServiceUrl);
                    return;
                }

                if (result?.ResponseStatus?.Type == "INVALID_USERNAME_PASSWORD")
                    throw new GarminConnectAuthenticationException("401 Unauthorized (Invalid Username or Password)");

                if (result?.Error?.StatusCode == "429")
                    throw new GarminConnectTooManyRequestsException("Portal login: 429 in JSON body");

                throw new GarminConnectConnectionException($"Portal web login failed: {result}");
            }
            catch (HttpRequestException ex)
            {
                throw new GarminConnectConnectionException($"Portal login failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Complete MFA verification
        /// </summary>
        private async Task CompleteMfaAsync(string mfaCode)
        {
            if (_mfaFlow == "widget")
            {
                await CompleteMfaWidgetAsync(mfaCode);
                return;
            }

            var mfaJson = new
            {
                mfaMethod = _mfaMethod ?? "email",
                mfaVerificationCode = mfaCode,
                rememberMyBrowser = true,
                reconsentList = new object[] { },
                mfaSetup = false,
            };

            var flowPath = _mfaFlow == "ios" ? "mobile" : _mfaFlow;
            var mfaEndpoints = new List<(string Url, Dictionary<string, string> Params)>
            {
                ($"{_sso}/{flowPath}/api/mfa/verifyCode", _mfaLoginParams),
            };

            // Add alternative endpoint
            if (flowPath == "mobile")
            {
                var altParams = new Dictionary<string, string>
                {
                    { "clientId", PORTAL_SSO_CLIENT_ID },
                    { "locale", "en-US" },
                    { "service", _portalServiceUrl },
                };
                mfaEndpoints.Add(($"{_sso}/portal/api/mfa/verifyCode", altParams));
            }
            else
            {
                var altParams = new Dictionary<string, string>
                {
                    { "clientId", IOS_SSO_CLIENT_ID },
                    { "locale", "en-US" },
                    { "service", IOS_SERVICE_URL },
                };
                mfaEndpoints.Add(($"{_sso}/mobile/api/mfa/verifyCode", altParams));
            }

            var failures = new List<string>();
            int rateLimitedCount = 0;

            foreach (var (mfaUrl, @params) in mfaEndpoints)
            {
                try
                {
                    var response = await mfaUrl
                        .SetQueryParams(@params)
                        .WithHeaders(_mfaPostHeaders)
                        .PostJsonAsync(mfaJson);

                    if (response.StatusCode == (int)System.Net.HttpStatusCode.TooManyRequests)
                    {
                        failures.Add($"{mfaUrl}: HTTP 429");
                        rateLimitedCount++;
                        continue;
                    }

                    var content = await response.ResponseMessage.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<MfaVerifyResponse>(content);

                    if (result?.ResponseStatus?.Type == "SUCCESSFUL")
                    {
                        var serviceUrl = _mfaFlow == "ios" ? IOS_SERVICE_URL : _mfaServiceUrl;
                        await EstablishSessionAsync(result.ServiceTicketId, serviceUrl);
                        return;
                    }

                    failures.Add($"{mfaUrl}: {result}");
                }
                catch (Exception e)
                {
                    failures.Add($"{mfaUrl}: {e.Message}");
                }
            }

            if (rateLimitedCount == mfaEndpoints.Count)
                throw new GarminConnectTooManyRequestsException($"MFA verification rate limited: {string.Join(", ", failures)}");

            throw new GarminConnectAuthenticationException($"MFA verification failed: {string.Join(", ", failures)}");
        }

        /// <summary>
        /// Complete MFA for widget flow
        /// </summary>
        private async Task CompleteMfaWidgetAsync(string mfaCode)
        {
            var contentStr = await _widgetLastResp.Content.ReadAsStringAsync();
            var csrfMatch = CSRF_REGEX.Match(contentStr);

            if (!csrfMatch.Success)
                throw new GarminConnectAuthenticationException("Widget MFA: missing CSRF token");

            var csrfToken = csrfMatch.Groups[1].Value;

            try
            {
                var response = await $"{_sso}/sso/verifyMFA/loginEnterMfaCode"
                    .SetQueryParams(_mfaLoginParams)
                    .WithHeaders(_mfaPostHeaders)
                    .PostAsync(new StringContent(
                        $"mfa-code={Uri.EscapeDataString(mfaCode)}&embed=true&_csrf={Uri.EscapeDataString(csrfToken)}&fromPage=setupEnterMfaCode",
                        Encoding.UTF8,
                        "application/x-www-form-urlencoded"));

                if (response.StatusCode == (int)System.Net.HttpStatusCode.TooManyRequests)
                    throw new GarminConnectTooManyRequestsException("Widget MFA verify returned 429");

                var content = await response.ResponseMessage.Content.ReadAsStringAsync();
                var titleMatch = TITLE_REGEX.Match(content);
                var title = titleMatch.Success ? titleMatch.Groups[1].Value : "";

                if (title != "Success")
                    throw new GarminConnectAuthenticationException($"Widget MFA failed: {title}");

                var ticketMatch = TICKET_REGEX.Match(content);
                if (!ticketMatch.Success)
                    throw new GarminConnectAuthenticationException("Widget MFA: missing service ticket");

                await EstablishSessionAsync(ticketMatch.Groups[1].Value, $"{_sso}/sso/embed");
            }
            catch (HttpRequestException ex)
            {
                throw new GarminConnectConnectionException($"Widget MFA failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exchange CAS service ticket for authentication tokens
        /// </summary>
        private async Task EstablishSessionAsync(string ticket, string serviceUrl)
        {
            try
            {
                await ExchangeServiceTicketAsync(ticket, serviceUrl);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("DI token exchange failed ({Error}), token exchange completed", ex.Message);
            }
        }

        /// <summary>
        /// Exchange service ticket for DI + IT Bearer tokens
        /// </summary>
        private async Task ExchangeServiceTicketAsync(string ticket, string serviceUrl)
        {
            var svcUrl = serviceUrl ?? IOS_SERVICE_URL;

            foreach (var clientId in DI_CLIENT_IDS)
            {
                try
                {
                    var headers = new Dictionary<string, string>
                    {
                        { "User-Agent", NATIVE_API_USER_AGENT },
                        { "X-Garmin-User-Agent", NATIVE_X_GARMIN_USER_AGENT },
                        { "Authorization", BuildBasicAuth(clientId) },
                        { "Accept", "application/json,text/html;q=0.9,*/*;q=0.8" },
                        { "Content-Type", "application/x-www-form-urlencoded" },
                        { "Cache-Control", "no-cache" },
                    };

                    var response = await _diTokenUrl
                        .WithHeaders(headers)
                        .PostUrlEncodedAsync(new
                        {
                            client_id = clientId,
                            service_ticket = ticket,
                            grant_type = DI_GRANT_TYPE,
                            service_url = svcUrl,
                        });

                    if (response.StatusCode == (int)System.Net.HttpStatusCode.TooManyRequests)
                        throw new GarminConnectTooManyRequestsException("DI token exchange rate limited");

                    if (response.ResponseMessage.IsSuccessStatusCode)
                    {
                        var content = await response.ResponseMessage.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<TokenExchangeResponse>(content);

                        DiToken = result.AccessToken;
                        DiRefreshToken = result.RefreshToken;
                        DiClientId = ExtractClientIdFromJwt(result.AccessToken) ?? clientId;
                        return;
                    }

                    _logger?.LogDebug("DI exchange failed for {ClientId}: {StatusCode}", clientId, response.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug("DI token parse failed for {ClientId}: {Error}", clientId, ex.Message);
                }
            }

            throw new GarminConnectAuthenticationException("DI token exchange failed for all client IDs");
        }

        /// <summary>
        /// Build Basic authentication header
        /// </summary>
        private string BuildBasicAuth(string clientId)
        {
            var credentials = $"{clientId}:";
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
            return $"Basic {base64}";
        }

        /// <summary>
        /// Extract client_id from JWT token
        /// </summary>
        private string ExtractClientIdFromJwt(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length < 2)
                    return null;

                var base64Payload = parts[1];
                // Add padding if necessary
                base64Payload += new string('=', (4 - base64Payload.Length % 4) % 4);

                var decodedBytes = Convert.FromBase64String(base64Payload);
                var decodedString = Encoding.UTF8.GetString(decodedBytes);
                var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(decodedString);

                if (payload?.TryGetValue("client_id", out var clientId) == true)
                    return clientId?.ToString();
            }
            catch (Exception ex)
            {
                _logger?.LogDebug("Failed to extract client_id from JWT: {Error}", ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Check if token expires soon (within 900 seconds / 15 minutes)
        /// </summary>
        public static bool TokenExpiresSoon(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            var parts = token.Split('.');
            if (parts.Length < 2)
                return false;

            var base64Payload = parts[1];
            // Add padding if necessary
            base64Payload += new string('=', (4 - base64Payload.Length % 4) % 4);

            var decodedBytes = Convert.FromBase64String(base64Payload);
            var decodedString = Encoding.UTF8.GetString(decodedBytes);
            var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(decodedString);

            if (payload?.TryGetValue("exp", out var expObj) == true)
            {
                if (long.TryParse(expObj?.ToString(), out var expTimestamp))
                {
                    var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    // Return true if current time is greater than (exp - 900)
                    return currentTime > (expTimestamp - 900);
                }
            }

            return false;
        }

        /// <summary>
        /// Generate random browser headers
        /// </summary>
        private Dictionary<string, string> GenerateRandomBrowserHeaders()
        {
            return new Dictionary<string, string>
            {
                { "User-Agent", DESKTOP_USER_AGENT },
            };
        }
    }

    // Helper classes for JSON serialization

    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    public class MobileLoginResponse
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

    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    public class MfaVerifyResponse
    {
        [JsonPropertyName("responseStatus")]
        public ResponseStatus ResponseStatus { get; set; }

        [JsonPropertyName("serviceTicketId")]
        public string ServiceTicketId { get; set; }
    }

    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    public class ResponseStatus
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    public class MfaInfo
    {
        [JsonPropertyName("mfaLastMethodUsed")]
        public string MfaLastMethodUsed { get; set; }
    }

    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    public class ErrorInfo
    {
        [JsonPropertyName("status-code")]
        public string StatusCode { get; set; }
    }

    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    public class TokenExchangeResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
    }

    // Custom exceptions

    public class GarminConnectException : Exception
    {
        public GarminConnectException(string message) : base(message) { }
        public GarminConnectException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class GarminConnectAuthenticationException : GarminConnectException
    {
        public GarminConnectAuthenticationException(string message) : base(message) { }
    }

    public class GarminConnectConnectionException : GarminConnectException
    {
        public GarminConnectConnectionException(string message) : base(message) { }
        public GarminConnectConnectionException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class GarminConnectTooManyRequestsException : GarminConnectException
    {
        public GarminConnectTooManyRequestsException(string message) : base(message) { }
    }

    internal class MFARequiredException : Exception { }
}



/*
 * 
 * 
 * 
var client = new GarminConnectClient();

try
{
    var result = await client.LoginAsync(
        email: "user@example.com",
        password: "password",
        promptMfa: async () => Console.ReadLine(), // User-provided MFA handler
        returnOnMfa: false
    );

    if (result.Item1 == "needs_mfa")
    {
        // Handle MFA
    }
    else
    {
        // Login successful
        var token = client.DiToken;
    }
}
catch (GarminConnectAuthenticationException ex)
{
    Console.WriteLine($"Authentication failed: {ex.Message}");
}

 * 
 */