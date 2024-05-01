using Flurl.Http;
using NLog;
using YetAnotherGarminConnectClient.Dto;
using YetAnotherGarminConnectClient.Dto.Garmin;

namespace YetAnotherGarminConnectClient
{
    internal partial class Client : IClient
    {
        private readonly string _consumerKey;
        private readonly string _consumerSecret;
        private AuthStatus _authStatus;
        private string _mfaCsrfToken = string.Empty;
        CookieJar _cookieJar = null;

        private static readonly object _commonQueryParams = new
        {
            id = "gauth-widget",
            embedWidget = "true",
            gauthHost = URLs.SSO_EMBED_URL,
            redirectAfterAccountCreationUrl = URLs.SSO_EMBED_URL,
            redirectAfterAccountLoginUrl = URLs.SSO_EMBED_URL,
            service = URLs.SSO_EMBED_URL,
            source = URLs.SSO_EMBED_URL,
        };

        private ILogger _logger => NLog.LogManager.GetLogger("Client");
        public OAuth2Token OAuth2Token { get; private set; }
        public DateTime _oAuth2TokenValidUntil { get; private set; }

        private string _oAuth1AccessToken = string.Empty; 
        private string _oAuth1TokenSecret = string.Empty;


        private Client() { }
        internal Client(string consumerKey, string consumerSecret)
        {

            _consumerKey = consumerKey;
            _consumerSecret = consumerSecret;

        }

        public bool IsOAuthValid
        {
            get
            {
                if (this.OAuth2Token == null)
                {
                    return false;
                }

                return DateTime.UtcNow < _oAuth2TokenValidUntil;
            }
        }

        public async Task<UploadResponse> UploadActivity(string format, byte[] file)
        {
            UploadResponse response = null;

            try
            {
                using (var stream = new MemoryStream(file))
                {
                    response = await $"{URLs.UPLOAD_URL}/{format}"
                 .WithOAuthBearerToken(OAuth2Token.Access_Token)
                 .WithHeader("NK", "NT")
                 .WithHeader("origin", URLs.ORIGIN)
                 .WithHeader("User-Agent", MagicStrings.USER_AGENT)
                 .AllowHttpStatus("2xx,409")
                 .PostMultipartAsync((data) =>
                 {
                     var fileName = $"{DateTime.UtcNow.ToShortDateString()}_YAGCC.fit";
                     data.AddFile("\"file\"", stream, contentType: "application/octet-stream", fileName: $"\"{fileName}\"");

                 })
                 .ReceiveJson<UploadResponse>();
                }
            }


            catch (FlurlHttpException ex)
            {
                this._logger.Error(ex, "Failed to upload activity to Garmin. Flur Exception.");
            }
            catch (Exception ex)
            {
                this._logger.Error(ex, "Failed to upload activity to Garmin.");
            }
            finally
            {
                if (response != null)
                {
                    var result = response.DetailedImportResult;

                    if (result.failures.Any())
                    {
                        foreach (var failure in result.failures)
                        {
                            if (failure.Messages.Any())
                            {
                                foreach (var message in failure.Messages)
                                {
                                    if (message.Code == 202)
                                    {
                                        _logger.Info("Activity already uploaded", result.fileName);
                                    }
                                    else
                                    {
                                        _logger.Error("Failed to upload activity to Garmin. Message:", message);
                                    }
                                }
                            }
                        }
                    }
                }

            }
            return response;
        }

        private async Task<UploadResult> TryToAuthenticate(string email, string password, string? mfaCode = "")
        {
            var result = new UploadResult();
            try
            {
                if (!IsOAuthValid)
                {

                    var authResult = string.IsNullOrEmpty(mfaCode) 
                        ? await this.Authenticate(email, password) 
                        : await this.CompleteMFAAuthAsync(mfaCode);

                    result.AccessToken = this._oAuth1AccessToken;
                    result.TokenSecret = this._oAuth1TokenSecret;

                    if (!authResult.IsSuccess)
                    {
                        result.MFACodeRequested = authResult.MFACodeRequested;
                        result.AuthStatus = _authStatus;
                        result.Logs = Logger.GetLogs();
                        result.ErrorLogs = Logger.GetErrorLogs();
                        return result;
                    }
                }
            }
            catch (GarminClientException ex)
            {
                result.AuthStatus = _authStatus;
                _logger.Error(ex, ex.Message);
            }
            catch (Exception ex)
            {
                result.AuthStatus = _authStatus;
                _logger.Error(ex, ex.Message);
            }

            return result;
        }

        private async Task<UploadResult> TryToUploadActivity(UploadResult result, byte[] file)
        {
            if (IsOAuthValid)
            {
                try
                {
                    if (file == null)
                    {
                        _logger.Error("Problem with creating fit file. File empty");
                    }
                    else
                    {
                        var response = await UploadActivity(".fit", file);
                        if (response != null && response.DetailedImportResult != null)
                        {
                            result.UploadId = response.DetailedImportResult.uploadId;
                            result.IsSuccess = true;
                        }
                    }

                }
                catch (GarminClientException ex)
                {
                    result.AuthStatus = _authStatus;
                    _logger.Error(ex, ex.Message);
                }
                catch (Exception ex)
                {
                    result.AuthStatus = _authStatus;
                    _logger.Error(ex, ex.Message);
                }
            }

            result.AuthStatus = _authStatus;
            result.Logs = Logger.GetLogs();
            result.ErrorLogs = Logger.GetErrorLogs();

            return result;
        }
    }
}
