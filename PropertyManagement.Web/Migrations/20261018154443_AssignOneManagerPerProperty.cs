using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AssignOneManagerPerProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PropertyManagerId",
                table: "Properties",
                type: "int",
                nullable: true);

            // Carry each property's existing assignment over before the
            // join table is dropped.
            migrationBuilder.Sql("""
                UPDATE p
                SET p.PropertyManagerId = a.PropertyManagerId
                FROM dbo.Properties p
                JOIN (
                    SELECT PropertiesId, MIN(PropertyManagersId) AS PropertyManagerId
                    FROM dbo.PropertyManagerAssignments
                    GROUP BY PropertiesId
                ) a ON a.PropertiesId = p.Id;
                """);

            // Fails visibly if any property had no assigned manager.
            migrationBuilder.AlterColumn<int>(
                name: "PropertyManagerId",
                table: "Properties",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropTable(
                name: "PropertyManagerAssignments");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Properties_PropertyManagers_PropertyManagerId",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_PropertyManagerId",
                table: "Properties");

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

            migrationBuilder.Sql("""
                INSERT INTO dbo.PropertyManagerAssignments (PropertiesId, PropertyManagersId)
                SELECT Id, PropertyManagerId
                FROM dbo.Properties;
                """);

            migrationBuilder.DropColumn(
                name: "PropertyManagerId",
                table: "Properties");
        }
    }
}
