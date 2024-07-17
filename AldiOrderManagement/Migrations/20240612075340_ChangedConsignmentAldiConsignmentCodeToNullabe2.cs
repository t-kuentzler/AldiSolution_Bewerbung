using Microsoft.EntityFrameworkCore.Migrations;

namespace AldiOrderManagement.Migrations
{
    public partial class ChangedConsignmentAldiConsignmentCodeToNullabe2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "VendorConsignmentCode",
                table: "Consignment",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true, // Setzen Sie nullable auf true
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "VendorConsignmentCode",
                table: "Consignment",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false, // Hier könnten Sie nullable auf false setzen, wenn das notwendig ist
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}