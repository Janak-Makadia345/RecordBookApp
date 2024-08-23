using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecordBookApp.Migrations
{
    /// <inheritdoc />
    public partial class PartyAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PartyName",
                table: "Records",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartyName",
                table: "Records");
        }
    }
}
