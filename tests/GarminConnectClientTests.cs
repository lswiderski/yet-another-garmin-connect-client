using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YetAnotherGarminConnectClient.Dto;
using YetAnotherGarminConnectClient.Dto.Garmin.Fit;

namespace YetAnotherGarminConnectClient.Tests
{
    public class GarminConnectClientTests
    {

        private string _consumerKey = "";
        private string _consumerSecret = "";
        private string _email = "";
        private string _password = "";

        private UserProfileSettings _userProfileSettings;
        private GarminWeightScaleDTO _garminWeightScaleDTO;
        private CredentialsData _credentials;
        private GarminConnectClient _client;
        private ILogger<GarminConnectClient> logger;

        [SetUp]
        public async Task Setup()
        {
            _client = new GarminConnectClient("garmin.com", logger);
            _userProfileSettings = new UserProfileSettings
            {
                Age = 40,
                Height = 180,
            };
            _garminWeightScaleDTO = new GarminWeightScaleDTO
            {
                TimeStamp = DateTime.UtcNow,
                Weight = 81.1f,
                PercentFat = 10.1f,
                PercentHydration = 53.3f,
                BoneMass = 5.8f,
                MuscleMass = 32f,
                VisceralFatRating = 9,
                VisceralFatMass = 10f,
                PhysiqueRating = 9,
                MetabolicAge = 28,
            };

            _credentials = new CredentialsData
            {
                Email = _email,
                Password = _password,
            };
        }

        [Test]
        public async Task ShouldAuthenticate()
        {
            bool isSuccess = false;
            try
            {
                var result = await _client.LoginAsync(_email, _password);
                isSuccess = _client.IsAuthenticated;

            }
            catch (GarminClientException ex)
            {
                var logs = Logger.GetLogs();
                var errorLogs = Logger.GetErrorLogs();
            }
            catch (Exception ex)
            {
                var logs = Logger.GetLogs();
                var errorLogs = Logger.GetErrorLogs();
            }

            Assert.IsTrue(isSuccess);
        }

        public Func<Task<string>> GetMFACode()
        {
            return async () =>
            {
                // DO BREAKPOINT HERE
                // refplace mfaCode with proper value
                Thread.Sleep(1000);
                return "";
            };
        }

        [Test]
        public async Task ShouldAuthenticateViaScenarioMobileAndCFFI()
        {
            bool isSuccess = false;
            try
            {
                var result = await _client.LoginAsync(_email, _password, GetMFACode(), false, new List<string> { "mobile+cffi" });

                isSuccess = _client.IsAuthenticated;
               
            }
            catch (GarminClientException ex)
            {
                var logs = Logger.GetLogs();
                var errorLogs = Logger.GetErrorLogs();
            }
            catch (Exception ex)
            {
                var logs = Logger.GetLogs();
                var errorLogs = Logger.GetErrorLogs();
            }

            Assert.IsTrue(isSuccess);
        }


        [Test]
        public async Task ShouldAuthenticateViaScenarioMobileAndRequests()
        {
            bool isSuccess = false;
            try
            {
                var result = await _client.LoginAsync(_email, _password, GetMFACode(), false, new List<string> { "mobile+requests" });

                isSuccess = _client.IsAuthenticated;

            }
            catch (GarminClientException ex)
            {
                var logs = Logger.GetLogs();
                var errorLogs = Logger.GetErrorLogs();
            }
            catch (Exception ex)
            {
                var logs = Logger.GetLogs();
                var errorLogs = Logger.GetErrorLogs();
            }

            Assert.IsTrue(isSuccess);
        }

        [Test]
        public async Task ShouldAuthenticateViaScenarioWidget()
        {
            bool isSuccess = false;
            try
            {
                var result = await _client.LoginAsync(_email, _password, GetMFACode(), false, new List<string> { "widget" });

                isSuccess = _client.IsAuthenticated;

            }
            catch (GarminClientException ex)
            {
                var logs = Logger.GetLogs();
                var errorLogs = Logger.GetErrorLogs();
            }
            catch (Exception ex)
            {
                var logs = Logger.GetLogs();
                var errorLogs = Logger.GetErrorLogs();
            }

            Assert.IsTrue(isSuccess);
        }

        [Test]
        public async Task ShouldAuthenticateViaScenarioPortalAndCFFI()
        {
            bool isSuccess = false;
            try
            {
                var result = await _client.LoginAsync(_email, _password, GetMFACode(), false, new List<string> { "portal+cffi" });

                isSuccess = _client.IsAuthenticated;

            }
            catch (GarminClientException ex)
            {
                var logs = Logger.GetLogs();
                var errorLogs = Logger.GetErrorLogs();
            }
            catch (Exception ex)
            {
                var logs = Logger.GetLogs();
                var errorLogs = Logger.GetErrorLogs();
            }

            Assert.IsTrue(isSuccess);
        }

        [Test]
        public async Task ShouldAuthenticateViaScenarioPortalAndRequests()
        {
            bool isSuccess = false;
            try
            {
                var result = await _client.LoginAsync(_email, _password, GetMFACode(), false, new List<string> { "portal+requests" });

                isSuccess = _client.IsAuthenticated;

            }
            catch (GarminClientException ex)
            {
                var logs = Logger.GetLogs();
                var errorLogs = Logger.GetErrorLogs();
            }
            catch (Exception ex)
            {
                var logs = Logger.GetLogs();
                var errorLogs = Logger.GetErrorLogs();
            }

            Assert.IsTrue(isSuccess);
        }

        [Test]
        public async Task TokenShouldNotExpireSoon()
        {
            bool isSuccess = false;
            try
            {
                string token = "";
                var result = GarminConnectClient.TokenExpiresSoon(token);

                isSuccess = !result;

            }
            catch (GarminClientException ex)
            {
                var logs = Logger.GetLogs();
                var errorLogs = Logger.GetErrorLogs();
            }
            catch (Exception ex)
            {
                var logs = Logger.GetLogs();
                var errorLogs = Logger.GetErrorLogs();
            }

            Assert.IsTrue(isSuccess);
        }

    }
}
