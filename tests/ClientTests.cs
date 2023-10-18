using NLog.Targets;
using NLog;
using NUnit.Framework;
using YetAnotherGarminConnectClient.Dto.Garmin.Fit;
using YetAnotherGarminConnectClient.Dto;

namespace YetAnotherGarminConnectClient.Tests
{
    public class Tests
    {
        private string _consumerKey = "";
        private string _consumerSecret = "";
        private string _email = "";
        private string _password = "";

        private UserProfileSettings _userProfileSettings;
        private GarminWeightScaleDTO _garminWeightScaleDTO;

        private IClient _client;

        [SetUp]
        public async Task Setup()
        {
            _client = await ClientFactory.Create();
            _userProfileSettings = new UserProfileSettings
            {
                Age = 40,
                Height = 180,
            };
            _garminWeightScaleDTO = new GarminWeightScaleDTO
            {
                TimeStamp = DateTime.UtcNow,
                Weight = 82.1f,
                PercentFat = 10.1f,
                PercentHydration = 53.3f,
                BoneMass = 5.8f,
                MuscleMass = 32f,
                VisceralFatRating = 9,
                VisceralFatMass = 10f,
                PhysiqueRating = 9,
                MetabolicAge = 28,
                Email = "",
                Password = "",

            };
        }

        [Test]
        public async Task ShouldReceiveOAuth2Token()
        {
            try
            {
                var accessToken = "";
                var tokenSecret = "";
                await _client.SetOAuth2Token(accessToken, tokenSecret);

            }
            catch (Exception ex)
            {
                var logs = Logger.GetLogs();
                var errorLogs = Logger.GetErrorLogs();
            }

            Assert.IsNotNull(_client.OAuth2Token);
        }


        [Test]
        public async Task ShouldAuthenticate()
        {
            bool isSuccess = false;
            try
            {
                var result = await _client.Authenticate(_email, _password);
                isSuccess = result.IsSuccess;

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
        public async Task ShouldSuccessfullUploadWeight()
        {
            bool isSuccess = false;
            try
            {
                var result = await _client.UploadWeight(_garminWeightScaleDTO, _userProfileSettings);
                isSuccess = result.IsSuccess;

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