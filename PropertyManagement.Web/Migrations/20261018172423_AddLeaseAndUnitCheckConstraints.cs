using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaseAndUnitCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Units_Bedrooms_Range",
                table: "Units",
                sql: "[Bedrooms] BETWEEN 0 AND 20");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Units_MonthlyRent_Positive",
                table: "Units",
                sql: "[MonthlyRent] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Leases_AgreedRent_Positive",
                table: "Leases",
                sql: "[AgreedRent] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Leases_EndDate_NotBeforeStartDate",
                table: "Leases",
                sql: "[EndDate] >= [StartDate]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Units_Bedrooms_Range",
                table: "Units");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Units_MonthlyRent_Positive",
                table: "Units");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Leases_AgreedRent_Positive",
                table: "Leases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Leases_EndDate_NotBeforeStartDate",
                table: "Leases");
        }
    }
}
