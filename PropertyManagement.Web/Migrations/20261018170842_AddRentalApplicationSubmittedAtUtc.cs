using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRentalApplicationSubmittedAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAtUtc",
                table: "RentalApplications",
                type: "datetime2",
                nullable: true);

            // Backfill from status history: the latest transition into
            // Submitted (status value 1).
            migrationBuilder.Sql("""
                UPDATE a
                SET a.SubmittedAtUtc = h.SubmittedAtUtc
                FROM dbo.RentalApplications a
                JOIN (
                    SELECT RentalApplicationId, MAX(ChangedAtUtc) AS SubmittedAtUtc
                    FROM dbo.ApplicationStatusHistories
                    WHERE NewStatus = 1
                    GROUP BY RentalApplicationId
                ) h ON h.RentalApplicationId = a.Id;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_RentalApplications_Status_SubmittedAtUtc",
                table: "RentalApplications",
                columns: new[] { "Status", "SubmittedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RentalApplications_Status_SubmittedAtUtc",
                table: "RentalApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedAtUtc",
                table: "RentalApplications");
        }
    }
}
