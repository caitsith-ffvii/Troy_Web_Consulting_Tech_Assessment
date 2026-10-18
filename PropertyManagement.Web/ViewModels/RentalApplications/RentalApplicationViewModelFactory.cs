using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Web.ViewModels.RentalApplications;

public class RentalApplicationViewModelFactory
{
    public RentalApplicationViewModel Create(
        RentalApplication application,
        RentalApplicationSection section,
        bool isReadOnly)
    {
        ArgumentNullException.ThrowIfNull(application);

        // Only Returned and Denied carry a comment the applicant needs.
        var review = application.Status is
                RentalApplicationStatus.Returned or RentalApplicationStatus.Denied
            ? application.StatusHistory
                .Where(h => h.NewStatus == application.Status)
                .OrderByDescending(h => h.ChangedAtUtc)
                .FirstOrDefault()
            : null;

        return new RentalApplicationViewModel
        {
            ApplicationId = application.Id,
            Status = application.Status,
            ApplicantInformationCompleted = application.ApplicantInformationCompleted,
            ResidenceHistoryCompleted = application.ResidenceHistoryCompleted,
            Section = section,
            IsReadOnly = isReadOnly,
            ReviewComment = review?.Comment,
            ReviewedAtUtc = review?.ChangedAtUtc,

            ApplicantInformation = new ApplicantInformationViewModel
            {
                FirstName = application.ApplicantInformation.FirstName,
                LastName = application.ApplicantInformation.LastName,
                PhoneNumber = application.ApplicantInformation.PhoneNumber,
                Email = application.ApplicantInformation.Email,
                CurrentAddress = application.ApplicantInformation.CurrentAddress
            },

            ResidenceHistory = application.ResidenceHistory
                .Select(entry => new ResidenceHistoryEntryViewModel
                {
                    Id = entry.Id,
                    Address = entry.Address,
                    LandlordName = entry.LandlordName,
                    LandlordPhoneNumber = entry.LandlordPhoneNumber,
                    MoveInDate = entry.MoveInDate,
                    MoveOutDate = entry.MoveOutDate
                }).ToList()
        };
    }
}
