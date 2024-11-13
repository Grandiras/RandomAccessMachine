using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebDataProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixedCascadeAgainAgain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Teachers_Id",
                table: "Classes");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Teachers_Id",
                table: "Classes",
                column: "Id",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Teachers_Id",
                table: "Classes");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Teachers_Id",
                table: "Classes",
                column: "Id",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
