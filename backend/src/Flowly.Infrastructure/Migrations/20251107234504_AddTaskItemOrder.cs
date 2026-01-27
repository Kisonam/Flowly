using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flowly.Infrastructure.Migrations
{
    
    public partial class AddTaskItemOrder : Migration
    {
        
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Tasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_TaskThemeId_Order",
                table: "Tasks",
                columns: new[] { "TaskThemeId", "Order" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_TaskThemeId_Order",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Tasks");
        }
    }
}
