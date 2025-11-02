using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YAGCC.Commands.UploadBloodPressure
{
    public sealed class UploadBloodPressureCommandSettings : CommandSettings
    {
        [CommandOption("-h|--heartrate")]
        [Description("Set your heart rate ")]
        public byte? HeartRate { set; get; }

        [CommandOption("-d|--diastolic")]
        [Description("Set your diastolic pressure")]
        public ushort? DiastolicPressure { set; get; }

        [CommandOption("-s|--systolic")]
        [Description("Set your systolic pressure")]
        public ushort? SystolicPressure { set; get; }

        [CommandOption("-t|--unix-timestamp")]
        [Description("Time of measurement")]
        public long? TimeStamp { set; get; }

        [CommandOption("-e|--email")]
        [Description("Email of the Garmin account")]
        public string? Email { set; get; }

        [CommandOption("-p|--password")]
        [Description("Password of the Garmin account")]
        public string? Password { set; get; }

        [CommandOption("-s|--server")]
        [Description("Server of the Gamin API: global or china")]
        public string? Server { set; get; }
    }
}
