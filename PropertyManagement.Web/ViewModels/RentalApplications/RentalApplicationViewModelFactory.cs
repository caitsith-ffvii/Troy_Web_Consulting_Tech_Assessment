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

        return new RentalApplicationViewModel
        {
            ApplicationId = application.Id,
            Status = application.Status,
            ApplicantInformationCompleted = application.ApplicantInformationCompleted,
            ResidenceHistoryCompleted = application.ResidenceHistoryCompleted,
            Section = section,
            IsReadOnly = isReadOnly,

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
                    Address = entry.Address,
                    LandlordName = entry.LandlordName,
                    LandlordPhoneNumber = entry.LandlordPhoneNumber,
                    MoveInDate = entry.MoveInDate,
                    MoveOutDate = entry.MoveOutDate
                }).ToList()
        };
    }
}
