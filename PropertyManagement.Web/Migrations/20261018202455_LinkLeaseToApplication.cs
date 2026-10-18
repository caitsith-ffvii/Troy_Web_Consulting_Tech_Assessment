using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class LinkLeaseToApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RentalApplicationId",
                table: "Leases",
                type: "int",
                nullable: true);

            // Link existing leases to their approved application (status 3):
            // same unit and applicant, closest approval-to-start date, paired
            // one-to-one so the unique index below holds.
            migrationBuilder.Sql("""
                WITH Candidates AS (
                    SELECT
                        l.Id AS LeaseId,
                        a.Id AS ApplicationId,
                        ROW_NUMBER() OVER (
                            PARTITION BY a.Id
                            ORDER BY ABS(DATEDIFF(day, a.UpdatedAtUtc, l.StartDate)), l.Id) AS ApplicationRank,
                        ROW_NUMBER() OVER (
                            PARTITION BY l.Id
                            ORDER BY ABS(DATEDIFF(day, a.UpdatedAtUtc, l.StartDate)), a.Id) AS LeaseRank
                    FROM dbo.Leases l
                    JOIN dbo.RentalApplications a
                        ON a.UnitId = l.UnitId
                        AND a.ApplicantId = l.ApplicantId
                        AND a.Status = 3
                )
                UPDATE l
                SET l.RentalApplicationId = c.ApplicationId
                FROM dbo.Leases l
                JOIN Candidates c ON c.LeaseId = l.Id
                WHERE c.ApplicationRank = 1 AND c.LeaseRank = 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Leases_RentalApplicationId",
                table: "Leases",
                column: "RentalApplicationId",
                unique: true,
                filter: "[RentalApplicationId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Leases_RentalApplications_RentalApplicationId",
                table: "Leases",
                column: "RentalApplicationId",
                principalTable: "RentalApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leases_RentalApplications_RentalApplicationId",
                table: "Leases");

            migrationBuilder.DropIndex(
                name: "IX_Leases_RentalApplicationId",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "RentalApplicationId",
                table: "Leases");
        }
    }
}
