using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebDataProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixedCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Classes_Id",
                table: "Students");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Classes_Id",
                table: "Students",
                column: "Id",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Classes_Id",
                table: "Students");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Classes_Id",
                table: "Students",
                column: "Id",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
