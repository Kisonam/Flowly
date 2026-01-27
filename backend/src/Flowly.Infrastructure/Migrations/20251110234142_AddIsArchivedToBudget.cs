using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flowly.Infrastructure.Migrations
{
    
    public partial class AddIsArchivedToBudget : Migration
    {
        
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Budgets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Budgets",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Budgets");
        }
    }
}
