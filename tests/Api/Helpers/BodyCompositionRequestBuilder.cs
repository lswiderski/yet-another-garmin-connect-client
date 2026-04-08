using Api.Contracts;
using Api.Models;

namespace Api.Tests.Helpers
{
    /// <summary>
    /// Factory for creating test BodyCompositionRequest instances
    /// </summary>
    public static class BodyCompositionRequestBuilder
    {
        /// <summary>
        /// Creates a minimal valid request with only required fields
        /// </summary>
        public static BodyCompositionRequest CreateMinimalRequest(string email = "test@example.com", string password = "testPassword")
        {
            return new BodyCompositionRequest
            {
                Email = email,
                Password = password,
                Weight = 75.0f,
                CreateOnlyFile = true
            };
        }

        /// <summary>
        /// Creates a complete request with all fields populated
        /// </summary>
        public static BodyCompositionRequest CreateCompleteRequest(string email = "test@example.com", string password = "testPassword")
        {
            return new BodyCompositionRequest
            {
                Email = email,
                Password = password,
                Weight = 75.5f,
                PercentFat = 20.5f,
                PercentHydration = 55.0f,
                BoneMass = 3.2f,
                MuscleMass = 30.0f,
                VisceralFatRating = 5,
                VisceralFatMass = 2.5f,
                PhysiqueRating = 3,
                MetabolicAge = 35,
                bodyMassIndex = 24.5f,
                TimeStamp = null,
                CreateOnlyFile = false
            };
        }
   
    }
 
}
