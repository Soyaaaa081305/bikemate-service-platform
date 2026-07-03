using BikeMate.Core.Constants;

namespace BikeMate.Tests.Core;

public sealed class PaymentStatusesTests
{
    [Theory]
    [InlineData("unpaid")]
    [InlineData("pending")]
    [InlineData("paid")]
    [InlineData("failed")]
    [InlineData("cancelled")]
    [InlineData("refunded")]
    public void StatusConstants_HaveExpectedValues(string expected)
    {
        var allStatuses = new[]
        {
            PaymentStatuses.Unpaid,
            PaymentStatuses.Pending,
            PaymentStatuses.Paid,
            PaymentStatuses.Failed,
            PaymentStatuses.Cancelled,
            PaymentStatuses.Refunded
        };

        Assert.Contains(expected, allStatuses);
    }

    [Fact]
    public void AllStatuses_AreLowercase()
    {
        var allStatuses = new[]
        {
            PaymentStatuses.Unpaid,
            PaymentStatuses.Pending,
            PaymentStatuses.Paid,
            PaymentStatuses.Failed,
            PaymentStatuses.Cancelled,
            PaymentStatuses.Refunded
        };

        foreach (var status in allStatuses)
        {
            Assert.Equal(status, status.ToLowerInvariant());
        }
    }

    [Fact]
    public void StatusCount_IsSix()
    {
        var allStatuses = new[]
        {
            PaymentStatuses.Unpaid,
            PaymentStatuses.Pending,
            PaymentStatuses.Paid,
            PaymentStatuses.Failed,
            PaymentStatuses.Cancelled,
            PaymentStatuses.Refunded
        };

        Assert.Equal(6, allStatuses.Distinct().Count());
    }
}
