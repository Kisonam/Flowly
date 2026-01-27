using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flowly.Infrastructure.Migrations
{
    
    public partial class AddNoteGroups : Migration
    {
        
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "NoteGroupId",
                table: "Notes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NoteGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteGroups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notes_NoteGroupId",
                table: "Notes",
                column: "NoteGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_NoteGroups_UserId_Order",
                table: "NoteGroups",
                columns: new[] { "UserId", "Order" });

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_NoteGroups_NoteGroupId",
                table: "Notes",
                column: "NoteGroupId",
                principalTable: "NoteGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notes_NoteGroups_NoteGroupId",
                table: "Notes");

            migrationBuilder.DropTable(
                name: "NoteGroups");

            migrationBuilder.DropIndex(
                name: "IX_Notes_NoteGroupId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "NoteGroupId",
                table: "Notes");
        }
    }
}
