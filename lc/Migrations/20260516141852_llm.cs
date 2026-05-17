using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lc.Migrations
{
    /// <inheritdoc />
    public partial class llm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReadingHistory_Books_BookId",
                table: "ReadingHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_ReadingHistory_Users_UserId",
                table: "ReadingHistory");

            migrationBuilder.DropTable(
                name: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_UserLibraryLists_UserId",
                table: "UserLibraryLists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReadingHistory",
                table: "ReadingHistory");

            migrationBuilder.DropIndex(
                name: "IX_ReadingHistory_UserId",
                table: "ReadingHistory");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserLibraryListBooks");

            migrationBuilder.RenameTable(
                name: "ReadingHistory",
                newName: "ReadingHistories");

            migrationBuilder.RenameIndex(
                name: "IX_ReadingHistory_BookId",
                table: "ReadingHistories",
                newName: "IX_ReadingHistories_BookId");

            migrationBuilder.AlterColumn<string>(
                name: "PreferredTheme",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Dark",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Light");

            migrationBuilder.AlterColumn<string>(
                name: "AvatarPath",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReadingHistories",
                table: "ReadingHistories",
                column: "HistoryId");

            migrationBuilder.CreateTable(
                name: "AuthorRequests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewerId = table.Column<int>(type: "int", nullable: true),
                    ReviewComment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorRequests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_AuthorRequests_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthorRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserLibraryLists_UserId_Name",
                table: "UserLibraryLists",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReadingHistories_UserId_BookId",
                table: "ReadingHistories",
                columns: new[] { "UserId", "BookId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthorRequests_ReviewerId",
                table: "AuthorRequests",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorRequests_Status",
                table: "AuthorRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorRequests_UserId_Status",
                table: "AuthorRequests",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_ReadingHistories_Books_BookId",
                table: "ReadingHistories",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "BookId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReadingHistories_Users_UserId",
                table: "ReadingHistories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReadingHistories_Books_BookId",
                table: "ReadingHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_ReadingHistories_Users_UserId",
                table: "ReadingHistories");

            migrationBuilder.DropTable(
                name: "AuthorRequests");

            migrationBuilder.DropIndex(
                name: "IX_UserLibraryLists_UserId_Name",
                table: "UserLibraryLists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReadingHistories",
                table: "ReadingHistories");

            migrationBuilder.DropIndex(
                name: "IX_ReadingHistories_UserId_BookId",
                table: "ReadingHistories");

            migrationBuilder.RenameTable(
                name: "ReadingHistories",
                newName: "ReadingHistory");

            migrationBuilder.RenameIndex(
                name: "IX_ReadingHistories_BookId",
                table: "ReadingHistory",
                newName: "IX_ReadingHistory_BookId");

            migrationBuilder.AlterColumn<string>(
                name: "PreferredTheme",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Light",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Dark");

            migrationBuilder.AlterColumn<string>(
                name: "AvatarPath",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "UserLibraryListBooks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReadingHistory",
                table: "ReadingHistory",
                column: "HistoryId");

            migrationBuilder.CreateTable(
                name: "Favorites",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    BookId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favorites", x => new { x.UserId, x.BookId });
                    table.ForeignKey(
                        name: "FK_Favorites_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "BookId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Favorites_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserLibraryLists_UserId",
                table: "UserLibraryLists",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingHistory_UserId",
                table: "ReadingHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_BookId",
                table: "Favorites",
                column: "BookId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReadingHistory_Books_BookId",
                table: "ReadingHistory",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "BookId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReadingHistory_Users_UserId",
                table: "ReadingHistory",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
