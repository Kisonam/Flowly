using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flowly.Infrastructure.Migrations
{
    
    public partial class RemoveArchiveEntityTypeDefault : Migration
    {
        
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
            migrationBuilder.Sql("ALTER TABLE \"ArchiveEntries\" ALTER COLUMN \"EntityType\" DROP DEFAULT;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
            migrationBuilder.Sql("ALTER TABLE \"ArchiveEntries\" ALTER COLUMN \"EntityType\" SET DEFAULT '';");
        }
    }
}
