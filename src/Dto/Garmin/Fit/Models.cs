using Dynastream.Fit;

namespace YetAnotherGarminConnectClient.Dto.Garmin.Fit
{
    public record UserProfileSettings
    {
        /// <summary>
        /// Gender enum as per the FIT SDK
        /// </summary>
        /// <value>Male</value>
        public Gender Gender { set; get; } = Gender.Male;
        public int? Age { set; get; }
        /// <summary>
        /// Height in centimeter
        /// </summary>
        /// <value></value>
        public int? Height { set; get; }
        /// <summary>
        /// Activity class enum that is in reality a byte type with possible values between either 1 and 100 or equal to 128 for professional athletes
        /// </summary>
        /// <returns></returns>
        public ActivityClass ActivityClass { set; get; } = (ActivityClass)90;
    }

    public record GarminWeightScaleData
    {
        public System.DateTime TimeStamp { set; get; }
        public float Weight { set; get; }
        public float? PercentFat { set; get; }
        public float? PercentHydration { set; get; }
        public float? BoneMass { set; get; }
        public float? MuscleMass { set; get; }
        public byte? VisceralFatRating { set; get; }
        public float? VisceralFatMass { set; get; }
        public byte? PhysiqueRating { set; get; }
        public byte? MetabolicAge { set; get; }
        public float? BodyMassIndex { set; get; }
    }

    public record GarminWeightScaleDTO : GarminWeightScaleData
    {
        public string Email { set; get; } = default!;
        public string Password { set; get; } = default!;
    }
}
