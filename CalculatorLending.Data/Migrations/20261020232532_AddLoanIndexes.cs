using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalculatorLending.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Loans_ItemCopyId",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_MemberId",
                table: "Loans");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_ItemCopyId",
                table: "Loans",
                column: "ItemCopyId",
                unique: true,
                filter: "\"ReturnDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_MemberId_ReturnDate",
                table: "Loans",
                columns: new[] { "MemberId", "ReturnDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Loans_ItemCopyId",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_MemberId_ReturnDate",
                table: "Loans");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_ItemCopyId",
                table: "Loans",
                column: "ItemCopyId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_MemberId",
                table: "Loans",
                column: "MemberId");
        }
    }
}
