using PropertyManagement.Domain.Entities;
using PropertyManagement.Web.ViewModels.RentalApplications;

namespace PropertyManagement.Tests.ViewModels;

public class RentalApplicationViewModelFactoryTests
{
    [Fact]
    public void Create_MapsApplicationDetailsAndCompletionFlags()
    {
        // Arrange
        var application = CreateApplication();
        application.Id = 42;
        application.Status = RentalApplicationStatus.Submitted;
        application.ApplicantInformationCompleted = true;
        application.ResidenceHistoryCompleted = false;

        var factory = new RentalApplicationViewModelFactory();

        // Act
        var result = factory.Create(
            application,
            RentalApplicationSection.Summary,
            isReadOnly: true);

        // Assert
        Assert.Equal(42, result.ApplicationId);
        Assert.Equal(RentalApplicationStatus.Submitted, result.Status);
        Assert.True(result.ApplicantInformationCompleted);
        Assert.False(result.ResidenceHistoryCompleted);
    }

    [Fact]
    public void Create_MapsApplicantInformationSnapshot()
    {
        // Arrange
        var application = CreateApplication();
        application.ApplicantInformation = new ApplicantInformationSnapshot
        {
            FirstName = "Jordan",
            LastName = "Taylor",
            PhoneNumber = "555-0100",
            Email = "jordan@example.com",
            CurrentAddress = "123 Main Street"
        };

        var factory = new RentalApplicationViewModelFactory();

        // Act
        var result = factory.Create(
            application,
            RentalApplicationSection.ApplicantInformation,
            isReadOnly: false);

        // Assert
        Assert.NotNull(result.ApplicantInformation);
        Assert.Equal("Jordan", result.ApplicantInformation.FirstName);
        Assert.Equal("Taylor", result.ApplicantInformation.LastName);
        Assert.Equal("555-0100", result.ApplicantInformation.PhoneNumber);
        Assert.Equal("jordan@example.com", result.ApplicantInformation.Email);
        Assert.Equal("123 Main Street", result.ApplicantInformation.CurrentAddress);
    }

    [Fact]
    public void Create_MapsResidenceHistoryIncludingMissingMoveOutDate()
    {
        // Arrange
        var moveInDate = new DateTime(2022, 4, 1);
        var previousMoveOutDate = new DateTime(2024, 6, 30);

        var application = CreateApplication();
        application.ResidenceHistory =
        [
            new ResidenceHistoryEntry
            {
                Address = "123 Previous Street",
                LandlordName = "Alex Morgan",
                LandlordPhoneNumber = "555-0101",
                MoveInDate = moveInDate,
                MoveOutDate = previousMoveOutDate
            },
            new ResidenceHistoryEntry
            {
                Address = "456 Current Avenue",
                LandlordName = "Sam Lee",
                LandlordPhoneNumber = "555-0102",
                MoveInDate = new DateTime(2024, 7, 1),
                MoveOutDate = null
            }
        ];

        var factory = new RentalApplicationViewModelFactory();

        // Act
        var result = factory.Create(
            application,
            RentalApplicationSection.ResidenceHistory,
            isReadOnly: false);

        // Assert
        Assert.Equal(2, result.ResidenceHistory.Count);

        Assert.Equal("123 Previous Street", result.ResidenceHistory[0].Address);
        Assert.Equal("Alex Morgan", result.ResidenceHistory[0].LandlordName);
        Assert.Equal("555-0101", result.ResidenceHistory[0].LandlordPhoneNumber);
        Assert.Equal(moveInDate, result.ResidenceHistory[0].MoveInDate);
        Assert.Equal(previousMoveOutDate, result.ResidenceHistory[0].MoveOutDate);

        Assert.Equal("456 Current Avenue", result.ResidenceHistory[1].Address);
        Assert.Equal("Sam Lee", result.ResidenceHistory[1].LandlordName);
        Assert.Null(result.ResidenceHistory[1].MoveOutDate);
    }

    [Theory]
    [InlineData(RentalApplicationSection.ApplicantInformation, false)]
    [InlineData(RentalApplicationSection.ResidenceHistory, false)]
    [InlineData(RentalApplicationSection.Summary, false)]
    [InlineData(RentalApplicationSection.ApplicantInformation, true)]
    [InlineData(RentalApplicationSection.ResidenceHistory, true)]
    [InlineData(RentalApplicationSection.Summary, true)]
    public void Create_PreservesRequestedSectionAndReadOnlyState(
        RentalApplicationSection section,
        bool isReadOnly)
    {
        // Arrange
        var application = CreateApplication();
        var factory = new RentalApplicationViewModelFactory();

        // Act
        var result = factory.Create(application, section, isReadOnly);

        // Assert
        Assert.Equal(section, result.Section);
        Assert.Equal(isReadOnly, result.IsReadOnly);
    }

    private static RentalApplication CreateApplication()
    {
        return new RentalApplication
        {
            ApplicantInformation = new ApplicantInformationSnapshot()
        };
    }
}