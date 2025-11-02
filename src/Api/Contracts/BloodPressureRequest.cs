using System.ComponentModel;

namespace Api.Contracts
{
    public class BloodPressureRequest
    {
        public byte? HeartRate { set; get; }
        public ushort? DiastolicPressure { set; get; }
        public ushort? SystolicPressure { set; get; }
        public long? TimeStamp { set; get; }
        public string Email { set; get; } = default!;
        public string Password { set; get; } = default!;
        public string? MFACode { get; set; }
        public string? ClientID { get; set; }
        public string? Server { get; set; }
    }
}
