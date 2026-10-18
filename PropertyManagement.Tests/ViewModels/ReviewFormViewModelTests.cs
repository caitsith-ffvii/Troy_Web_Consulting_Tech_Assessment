using System.ComponentModel.DataAnnotations;
using PropertyManagement.Web.ViewModels.RentalApplications;

namespace PropertyManagement.Tests.ViewModels;

public class ReviewFormViewModelTests
{
    [Theory]
    [InlineData(ReviewOutcome.Return)]
    [InlineData(ReviewOutcome.Deny)]
    public void Validate_ReturnOrDenyWithoutComment_ReturnsCommentError(
        ReviewOutcome outcome)
    {
        // Arrange
        var model = new ReviewFormViewModel { Outcome = outcome, Comment = "  " };

        // Act
        var results = Validate(model);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(model.Comment)));
    }

    [Fact]
    public void Validate_ApproveWithoutComment_IsValid()
    {
        // Arrange
        var model = new ReviewFormViewModel { Outcome = ReviewOutcome.Approve };

        // Act
        var results = Validate(model);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_NoOutcome_ReturnsOutcomeError()
    {
        // Arrange
        var model = new ReviewFormViewModel();

        // Act
        var results = Validate(model);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(model.Outcome)));
    }

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
