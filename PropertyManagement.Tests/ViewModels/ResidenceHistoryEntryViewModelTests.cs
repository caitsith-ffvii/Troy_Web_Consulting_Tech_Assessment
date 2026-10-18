using System.ComponentModel.DataAnnotations;
using PropertyManagement.Web.ViewModels.RentalApplications;

namespace PropertyManagement.Tests.ViewModels;

public class ResidenceHistoryEntryViewModelTests
{
    [Fact]
    public void Validate_FutureMoveInDate_ReturnsMoveInDateError()
    {
        // Arrange
        var model = ValidEntry();
        model.MoveInDate = DateTime.Today.AddDays(1);

        // Act
        var results = Validate(model);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(model.MoveInDate)));
    }

    [Fact]
    public void Validate_MoveOutBeforeMoveIn_ReturnsMoveOutDateError()
    {
        // Arrange
        var model = ValidEntry();
        model.MoveOutDate = model.MoveInDate.AddDays(-1);

        // Act
        var results = Validate(model);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(model.MoveOutDate)));
    }

    [Fact]
    public void Validate_ValidEntry_ReturnsNoErrors()
    {
        // Act
        var results = Validate(ValidEntry());

        // Assert
        Assert.Empty(results);
    }

    private static ResidenceHistoryEntryViewModel ValidEntry() =>
        new()
        {
            Address = "1 Previous Street",
            LandlordName = "Pat Landlord",
            LandlordPhoneNumber = "555-0100",
            MoveInDate = DateTime.Today.AddYears(-2),
            MoveOutDate = DateTime.Today.AddYears(-1)
        };

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            results,
            validateAllProperties: true);

        return results;
    }
}
