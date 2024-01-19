namespace Api.Contracts
{
    public class BodyCompositionRequest
    {
        public long? TimeStamp { set; get; }
        public float Weight { set; get; }
        public float? PercentFat { set; get; }
        public float? PercentHydration { set; get; }
        public float? BoneMass { set; get; }
        public float? MuscleMass { set; get; }
        public float? VisceralFatRating { set; get; }
        public float? VisceralFatMass { set; get; }
        public float? PhysiqueRating { set; get; }
        public float? MetabolicAge { set; get; }
        public float? bodyMassIndex { get; set; }
        public string Email { set; get; } = default!;
        public string Password { set; get; } = default!;
        public string? MFACode { get; set; }
        public string? ClientID { get; set; }
        public bool CreateOnlyFile { get; set; } = false;
    }
}
