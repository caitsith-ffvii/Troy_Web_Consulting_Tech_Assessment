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