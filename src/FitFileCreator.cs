using Dynastream.Fit;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherGarminConnectClient.Dto.Garmin.Fit;

namespace YetAnotherGarminConnectClient
{
    public class FitFileCreator
    {
        private static readonly string _tmpDir = Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp")).FullName;
        private ILogger _logger => NLog.LogManager.GetLogger("FitFileCreator");


        public static string CreateWeightBodyCompositionFitFile(GarminWeightScaleData garminWeightScaleData, UserProfileSettings userProfileSettings)
        {
            var timeCreated = new Dynastream.Fit.DateTime(System.DateTime.UtcNow);
            var outputFilePath = Path.Combine(_tmpDir, $"{timeCreated.GetTimeStamp()}_WEIGHT_SCALE.fit");

            var stream = new FileStream(outputFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            var encoder = new Encode(ProtocolVersion.V20);

            encoder.Open(stream);

            var fileIdMesg = new FileIdMesg();
            fileIdMesg.SetType(Dynastream.Fit.File.Weight);
            fileIdMesg.SetManufacturer(Manufacturer.Garmin);
            fileIdMesg.SetGarminProduct(2429);
            fileIdMesg.SetSerialNumber(1234);
            fileIdMesg.SetTimeCreated(timeCreated);
            encoder.Write(fileIdMesg);

            var myUserProfile = new UserProfileMesg();
            myUserProfile.SetGender(userProfileSettings.Gender);
            myUserProfile.SetAge(Convert.ToByte(userProfileSettings.Age));
            myUserProfile.SetWeight(garminWeightScaleData.Weight);
            myUserProfile.SetHeight(userProfileSettings.Height / 100);
            myUserProfile.SetActivityClass(userProfileSettings.ActivityClass);
            myUserProfile.SetMessageIndex(0);
            myUserProfile.SetLocalId(0);

            encoder.Write(myUserProfile);

            var weightMesg = new WeightScaleMesg();

            weightMesg.SetTimestamp(new Dynastream.Fit.DateTime(garminWeightScaleData.TimeStamp.ToUniversalTime()));
            weightMesg.SetUserProfileIndex(0);
            weightMesg.SetWeight(garminWeightScaleData.Weight);

            if (garminWeightScaleData.PercentFat is not null)
                weightMesg.SetPercentFat(garminWeightScaleData.PercentFat);
            if (garminWeightScaleData.PercentHydration is not null)
                weightMesg.SetPercentHydration(garminWeightScaleData.PercentHydration);
            if (garminWeightScaleData.MuscleMass is not null)
                weightMesg.SetMuscleMass(garminWeightScaleData.MuscleMass);
            if (garminWeightScaleData.BoneMass is not null)
                weightMesg.SetBoneMass(garminWeightScaleData.BoneMass);
            if (garminWeightScaleData.MetabolicAge is not null)
                weightMesg.SetMetabolicAge(garminWeightScaleData.MetabolicAge);
            if (garminWeightScaleData.PhysiqueRating is not null)
                weightMesg.SetPhysiqueRating(garminWeightScaleData.PhysiqueRating);
            if (garminWeightScaleData.VisceralFatMass is not null)
                weightMesg.SetVisceralFatMass(garminWeightScaleData.VisceralFatMass);
            if (garminWeightScaleData.VisceralFatRating is not null)
                weightMesg.SetVisceralFatRating(garminWeightScaleData.VisceralFatRating);


            if (garminWeightScaleData.Weight > 0 && userProfileSettings.Height > 0 || garminWeightScaleData.BodyMassIndex is not null)
                weightMesg.SetBmi((float)Math.Round(garminWeightScaleData.BodyMassIndex ?? (garminWeightScaleData.Weight / Math.Pow((float)userProfileSettings.Height! / 100, 2)), 1));

            encoder.Write(weightMesg);

            var deviceInfoMesg = new DeviceInfoMesg();
            deviceInfoMesg.SetTimestamp(timeCreated);
            deviceInfoMesg.SetBatteryVoltage(384);
            deviceInfoMesg.SetCumOperatingTime(45126);

            encoder.Close();
            stream.Close();

            return outputFilePath;
        }
    }
}
