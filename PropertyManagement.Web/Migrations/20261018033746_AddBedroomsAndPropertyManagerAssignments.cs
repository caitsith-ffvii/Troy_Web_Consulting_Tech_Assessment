using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBedroomsAndPropertyManagerAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Properties_PropertyManagers_PropertyManagerId",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_PropertyManagerId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "PropertyManagerId",
                table: "Properties");

            migrationBuilder.AddColumn<int>(
                name: "Bedrooms",
                table: "Units",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PropertyManagerAssignments",
                columns: table => new
                {
                    PropertiesId = table.Column<int>(type: "int", nullable: false),
                    PropertyManagersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyManagerAssignments", x => new { x.PropertiesId, x.PropertyManagersId });
                    table.ForeignKey(
                        name: "FK_PropertyManagerAssignments_Properties_PropertiesId",
                        column: x => x.PropertiesId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PropertyManagerAssignments_PropertyManagers_PropertyManagersId",
                        column: x => x.PropertyManagersId,
                        principalTable: "PropertyManagers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyManagerAssignments_PropertyManagersId",
                table: "PropertyManagerAssignments",
                column: "PropertyManagersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropertyManagerAssignments");

            migrationBuilder.DropColumn(
                name: "Bedrooms",
                table: "Units");

            migrationBuilder.AddColumn<int>(
                name: "PropertyManagerId",
                table: "Properties",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Properties_PropertyManagerId",
                table: "Properties",
                column: "PropertyManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_PropertyManagers_PropertyManagerId",
                table: "Properties",
                column: "PropertyManagerId",
                principalTable: "PropertyManagers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
