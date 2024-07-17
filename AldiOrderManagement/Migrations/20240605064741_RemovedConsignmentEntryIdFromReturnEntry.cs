using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AldiOrderManagement.Migrations
{
    /// <inheritdoc />
    public partial class RemovedConsignmentEntryIdFromReturnEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReturnEntry_ConsignmentEntry_ConsignmentEntryId",
                table: "ReturnEntry");

            migrationBuilder.DropIndex(
                name: "IX_ReturnEntry_ConsignmentEntryId",
                table: "ReturnEntry");

            migrationBuilder.DropColumn(
                name: "ConsignmentEntryId",
                table: "ReturnEntry");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsignmentEntryId",
                table: "ReturnEntry",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnEntry_ConsignmentEntryId",
                table: "ReturnEntry",
                column: "ConsignmentEntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnEntry_ConsignmentEntry_ConsignmentEntryId",
                table: "ReturnEntry",
                column: "ConsignmentEntryId",
                principalTable: "ConsignmentEntry",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
