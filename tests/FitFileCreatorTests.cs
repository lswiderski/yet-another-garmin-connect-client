using Dynastream.Fit;
using NUnit.Framework;
using YetAnotherGarminConnectClient.Dto.Garmin.Fit;

namespace YetAnotherGarminConnectClient.Tests;

[TestFixture]
public class FitFileCreatorTests
{
    [Test]
    public void WeightFitFileRoundTripsBasalMetAndSkeletalMuscleMass()
    {
        var data = new GarminWeightScaleData
        {
            TimeStamp = new System.DateTime(2026, 7, 12, 8, 30, 0, DateTimeKind.Utc),
            Weight = 74.2f,
            SkeletalMuscleMass = 40.0f,
            BasalMet = 1650f,
        };
        var profile = new UserProfileSettings { Age = 29, Height = 182 };

        var bytes = FitFileCreator.CreateWeightBodyCompositionFitFile(data, profile);
        var decode = new Decode();
        var broadcaster = new MesgBroadcaster();
        WeightScaleMesg? decoded = null;

        decode.MesgEvent += broadcaster.OnMesg;
        broadcaster.WeightScaleMesgEvent += (_, args) => decoded = new WeightScaleMesg(args.mesg);

        using var stream = new MemoryStream(bytes);
        Assert.That(decode.Read(stream), Is.True);
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded!.GetBasalMet(), Is.EqualTo(1650f).Within(0.25f));
        Assert.That(decoded.GetMuscleMass(), Is.EqualTo(40.0f).Within(0.01f));
    }

    [Test]
    public void WeightFitFileOmitsBasalMetWhenItIsNull()
    {
        var data = new GarminWeightScaleData
        {
            TimeStamp = new System.DateTime(2026, 7, 12, 8, 30, 0, DateTimeKind.Utc),
            Weight = 74.2f,
            BasalMet = null,
        };
        var profile = new UserProfileSettings { Age = 29, Height = 182 };

        var bytes = FitFileCreator.CreateWeightBodyCompositionFitFile(data, profile);
        var decode = new Decode();
        var broadcaster = new MesgBroadcaster();
        WeightScaleMesg? decoded = null;

        decode.MesgEvent += broadcaster.OnMesg;
        broadcaster.WeightScaleMesgEvent += (_, args) => decoded = new WeightScaleMesg(args.mesg);

        using var stream = new MemoryStream(bytes);
        Assert.That(decode.Read(stream), Is.True);
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded!.GetBasalMet(), Is.Null);
    }

    [Test]
    public void LegacyMuscleMassAliasPopulatesSkeletalMuscleMass()
    {
#pragma warning disable CS0618
        var data = new GarminWeightScaleData { MuscleMass = 39.5f };
#pragma warning restore CS0618

        Assert.That(data.SkeletalMuscleMass, Is.EqualTo(39.5f));
    }
}
