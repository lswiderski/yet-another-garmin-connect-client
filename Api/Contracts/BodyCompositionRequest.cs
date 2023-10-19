namespace Api.Contracts
{
    public class BodyCompositionRequest
    {
        public long TimeStamp { set; get; }
        public float Weight { set; get; }
        public float? PercentFat { set; get; }
        public float? PercentHydration { set; get; }
        public float? BoneMass { set; get; }
        public float? MuscleMass { set; get; }
        public byte? VisceralFatRating { set; get; }
        public float? VisceralFatMass { set; get; }
        public byte? PhysiqueRating { set; get; }
        public byte? MetabolicAge { set; get; }
        public string Email { set; get; } = default!;
        public string Password { set; get; } = default!;
    }
}
