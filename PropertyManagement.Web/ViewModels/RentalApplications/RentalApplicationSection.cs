namespace PropertyManagement.Web.ViewModels.RentalApplications;

public enum RentalApplicationSection
{
    ApplicantInformation,
    ResidenceHistory,
    Summary
}

public static class RentalApplicationSectionExtensions
{
    public static string DisplayName(this RentalApplicationSection section) =>
        section switch
        {
            RentalApplicationSection.ApplicantInformation => "Applicant Information",
            RentalApplicationSection.ResidenceHistory => "Residence History",
            RentalApplicationSection.Summary => "Summary",
            _ => section.ToString()
        };

    // 1-based position for "Step X of Y".
    public static int StepNumber(this RentalApplicationSection section) =>
        (int)section + 1;

    public static int StepCount => Enum.GetValues<RentalApplicationSection>().Length;
}
