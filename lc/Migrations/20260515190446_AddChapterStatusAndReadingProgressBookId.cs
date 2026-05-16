using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lc.Migrations
{
    /// <inheritdoc />
    public partial class AddChapterStatusAndReadingProgressBookId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReadingProgress_Chapters_ChapterId",
                table: "ReadingProgress");

            migrationBuilder.AddColumn<int>(
                name: "BookId",
                table: "ReadingProgress",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Chapters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgress_BookId_ChapterId",
                table: "ReadingProgress",
                columns: new[] { "BookId", "ChapterId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ReadingProgress_Books_BookId",
                table: "ReadingProgress",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "BookId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReadingProgress_Chapters_ChapterId",
                table: "ReadingProgress",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "ChapterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReadingProgress_Books_BookId",
                table: "ReadingProgress");

            migrationBuilder.DropForeignKey(
                name: "FK_ReadingProgress_Chapters_ChapterId",
                table: "ReadingProgress");

            migrationBuilder.DropIndex(
                name: "IX_ReadingProgress_BookId_ChapterId",
                table: "ReadingProgress");

            migrationBuilder.DropColumn(
                name: "BookId",
                table: "ReadingProgress");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Chapters");

            migrationBuilder.AddForeignKey(
                name: "FK_ReadingProgress_Chapters_ChapterId",
                table: "ReadingProgress",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "ChapterId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
