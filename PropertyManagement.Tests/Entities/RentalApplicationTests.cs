using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Tests.Entities;

public class RentalApplicationTests
{
    [Theory]
    [InlineData(RentalApplicationStatus.Draft, RentalApplicationStatus.Submitted)]
    [InlineData(RentalApplicationStatus.Draft, RentalApplicationStatus.Withdrawn)]
    [InlineData(RentalApplicationStatus.Submitted, RentalApplicationStatus.Returned)]
    [InlineData(RentalApplicationStatus.Submitted, RentalApplicationStatus.Approved)]
    [InlineData(RentalApplicationStatus.Submitted, RentalApplicationStatus.Denied)]
    [InlineData(RentalApplicationStatus.Submitted, RentalApplicationStatus.Withdrawn)]
    [InlineData(RentalApplicationStatus.Returned, RentalApplicationStatus.Submitted)]
    [InlineData(RentalApplicationStatus.Returned, RentalApplicationStatus.Withdrawn)]
    public void TransitionTo_AllowedTransition_UpdatesStatus(
        RentalApplicationStatus currentStatus,
        RentalApplicationStatus newStatus)
    {
        var application = new RentalApplication
        {
            Status = currentStatus
        };

        application.TransitionTo(newStatus);

        Assert.Equal(newStatus, application.Status);
    }

    // Every status pair: exactly the documented transitions are allowed.
    [Fact]
    public void CanTransitionTo_FullMatrix_AllowsOnlyDocumentedTransitions()
    {
        var allowed = new HashSet<(RentalApplicationStatus, RentalApplicationStatus)>
        {
            (RentalApplicationStatus.Draft, RentalApplicationStatus.Submitted),
            (RentalApplicationStatus.Draft, RentalApplicationStatus.Withdrawn),
            (RentalApplicationStatus.Submitted, RentalApplicationStatus.Approved),
            (RentalApplicationStatus.Submitted, RentalApplicationStatus.Returned),
            (RentalApplicationStatus.Submitted, RentalApplicationStatus.Denied),
            (RentalApplicationStatus.Submitted, RentalApplicationStatus.Withdrawn),
            (RentalApplicationStatus.Returned, RentalApplicationStatus.Submitted),
            (RentalApplicationStatus.Returned, RentalApplicationStatus.Withdrawn)
        };

        foreach (var from in Enum.GetValues<RentalApplicationStatus>())
        {
            foreach (var to in Enum.GetValues<RentalApplicationStatus>())
            {
                var application = new RentalApplication { Status = from };

                Assert.True(
                    allowed.Contains((from, to)) == application.CanTransitionTo(to),
                    $"{from} -> {to} should be {(allowed.Contains((from, to)) ? "allowed" : "rejected")}.");
            }
        }
    }

    [Theory]
    [InlineData(RentalApplicationStatus.Draft, RentalApplicationStatus.Approved)]
    [InlineData(RentalApplicationStatus.Draft, RentalApplicationStatus.Denied)]
    [InlineData(RentalApplicationStatus.Submitted, RentalApplicationStatus.Draft)]
    [InlineData(RentalApplicationStatus.Returned, RentalApplicationStatus.Approved)]
    [InlineData(RentalApplicationStatus.Approved, RentalApplicationStatus.Submitted)]
    [InlineData(RentalApplicationStatus.Denied, RentalApplicationStatus.Submitted)]
    [InlineData(RentalApplicationStatus.Withdrawn, RentalApplicationStatus.Submitted)]
    public void TransitionTo_DisallowedTransition_ThrowsAndPreservesStatus(
        RentalApplicationStatus currentStatus,
        RentalApplicationStatus newStatus)
    {
        var application = new RentalApplication
        {
            Status = currentStatus
        };

        Assert.Throws<InvalidOperationException>(
            () => application.TransitionTo(newStatus));

        Assert.Equal(currentStatus, application.Status);
    }
}