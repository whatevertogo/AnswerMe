using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnswerMe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWrongQuestionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMastered",
                table: "AttemptDetails",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "MasteredAt",
                table: "AttemptDetails",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttemptDetails_IsMastered",
                table: "AttemptDetails",
                column: "IsMastered");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AttemptDetails_IsMastered",
                table: "AttemptDetails");

            migrationBuilder.DropColumn(
                name: "IsMastered",
                table: "AttemptDetails");

            migrationBuilder.DropColumn(
                name: "MasteredAt",
                table: "AttemptDetails");
        }
    }
}
