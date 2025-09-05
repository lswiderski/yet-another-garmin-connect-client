using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YAGCC.Commands.UploadBodyComposition
{
    public sealed class UploadBodyCompositionCommandSettings : CommandSettings
    {
        [CommandOption("-w|--weight")]
        [Description("Set your weight in kilograms")]
        public float Weight { set; get; }

        [CommandOption("-f|--fat")]
        [Description("Set your fat in percent")]
        public float? PercentFat { set; get; }

        [CommandOption("-h|--hydration")]
        [Description("Set your hydration in percent")]
        public float? PercentHydration { set; get; }

        [CommandOption("-b|--bone-mass")]
        [Description("Set your bone mass in kilograms")]
        public float? BoneMass { set; get; }

        [CommandOption("-m|--muscle-mass")]
        [Description("Set your muscle mass in kilograms")]
        public float? MuscleMass { set; get; }

        [CommandOption("--visceral-fat")]
        [Description("Set your visceral fat rating")]
        public float? VisceralFatRating { set; get; }

        [CommandOption("-v|--visceral-fat-mass")]
        [Description("Set your visceral fat mass")]
        public float? VisceralFatMass { set; get; }

        [CommandOption("-r|--physique-rating")]
        [Description("Set your physique rating")]
        public float? PhysiqueRating { set; get; }

        [CommandOption("-a|--metabolic-age")]
        [Description("Set your metabolic age")]
        public float? MetabolicAge { set; get; }

        [CommandOption("-i|--bmi")]
        [Description("Set your BMI - body mass index")]
        public float? BodyMassIndex { get; set; }

        [CommandOption("-t|--unix-timestamp")]
        [Description("Time of measurement")]
        public long? TimeStamp { set; get; }

        [CommandOption("-e|--email")]
        [Description("Email of the Garmin account")]
        public string? Email { set; get; }

        [CommandOption("-p|--password")]
        [Description("Password of the Garmin account")]
        public string? Password { set; get; }
    }
}
