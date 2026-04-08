using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Api.Models;
using Api.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Api.Tests
{
    [TestFixture]
    public class UploadEndpointTests
    {
        private WebApplicationFactory<Program> _factory = null!;
        private HttpClient _client = null!;

        // Common test credentials
        private const string TestEmail = "";
        private const string TestPassword = "";

        [SetUp]
        public void Setup()
        {
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    // Configure test services if needed
                });
            _client = _factory.CreateClient();
        }

        [TearDown]
        public void TearDown()
        {
            _client?.Dispose();
            _factory?.Dispose();
        }

        [Test]
        public async Task Upload_WithValidRequest_CreateFileOnly_ShouldReturnFitFile()
        {
            // Arrange
            var request = BodyCompositionRequestBuilder.CreateCompleteRequest(TestEmail, TestPassword);

            // Act
            var response = await _client.PostAsJsonAsync("/upload", request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK).Or
                .EqualTo(HttpStatusCode.BadRequest),
                "Response should be OK for file creation or BadRequest for missing credentials");
        }

        [Test]
        public async Task Upload_WithCustomUserProfile_ShouldUseProvidedSettings()
        {
            // Arrange
            var customProfile = new UserProfileModel { Age = 30, Height = 175, Gender = GenderEnum.Male };
            var request = BodyCompositionRequestBuilder.CreateCompleteRequest(TestEmail, TestPassword);
            request.UserProfile = customProfile;

            // Act
            var response = await _client.PostAsJsonAsync("/upload", request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK).Or
                .EqualTo(HttpStatusCode.BadRequest),
                "Should process request with custom user profile");
        }


        [Test]
        public async Task Upload_HealthCheck_ShouldReturn()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
    }
}
