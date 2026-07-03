using BikeMate.Core.Constants;

namespace BikeMate.Tests.Core;

public sealed class RequestStatusesTests
{
    [Theory]
    [InlineData("pending")]
    [InlineData("accepted")]
    [InlineData("rejected")]
    [InlineData("en_route")]
    [InlineData("arrived")]
    [InlineData("in_progress")]
    [InlineData("completed")]
    [InlineData("payment_pending")]
    [InlineData("paid")]
    [InlineData("cancelled")]
    public void StatusConstants_HaveExpectedLowercaseValues(string expected)
    {
        var allStatuses = new[]
        {
            RequestStatuses.Pending,
            RequestStatuses.Accepted,
            RequestStatuses.Rejected,
            RequestStatuses.EnRoute,
            RequestStatuses.Arrived,
            RequestStatuses.InProgress,
            RequestStatuses.Completed,
            RequestStatuses.PaymentPending,
            RequestStatuses.Paid,
            RequestStatuses.Cancelled
        };

        Assert.Contains(expected, allStatuses);
    }

    [Fact]
    public void AllStatuses_AreLowercase()
    {
        var allStatuses = new[]
        {
            RequestStatuses.Pending,
            RequestStatuses.Accepted,
            RequestStatuses.Rejected,
            RequestStatuses.EnRoute,
            RequestStatuses.Arrived,
            RequestStatuses.InProgress,
            RequestStatuses.Completed,
            RequestStatuses.PaymentPending,
            RequestStatuses.Paid,
            RequestStatuses.Cancelled
        };

        foreach (var status in allStatuses)
        {
            Assert.Equal(status, status.ToLowerInvariant());
        }
    }
}
