using BikeMate.Core.Services;

namespace BikeMate.Tests.Core;

public sealed class RepairConcernMatcherTests
{
    [Theory]
    [InlineData("Gear Shifting Issue", "Drivetrain & Gear Service", "Gear Tuning and Cable Adjustment")]
    [InlineData("Chain Maintenance", "Chain & Sprocket Service", "Chain Cleaning and Tensioning")]
    [InlineData("Accessory Installation", "Accessories & Electrical Installation", "Accessory Fitment")]
    [InlineData("General Tune-up", "Preventive Maintenance & Tune-up", "Complete Motorcycle Tune-up")]
    public void Matches_ReturnsTrueForRelatedService(string concern, string category, string service)
    {
        Assert.True(RepairConcernMatcher.Matches(concern, category, service));
    }

    [Theory]
    [InlineData("Gear Shifting Issue", "Tire Service", "Flat Tire Rescue")]
    [InlineData("Chain Maintenance", "Oil Change", "Basic Oil Change")]
    [InlineData("Accessory Installation", "Emergency Roadside Assistance", "Emergency Roadside Help")]
    public void Matches_ReturnsFalseForUnrelatedService(string concern, string category, string service)
    {
        Assert.False(RepairConcernMatcher.Matches(concern, category, service));
    }
}
