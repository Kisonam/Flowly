using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flowly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveArchiveEntityTypeDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove the default value from EntityType column
            migrationBuilder.Sql("ALTER TABLE \"ArchiveEntries\" ALTER COLUMN \"EntityType\" DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the default value
            migrationBuilder.Sql("ALTER TABLE \"ArchiveEntries\" ALTER COLUMN \"EntityType\" SET DEFAULT '';");
        }
    }
}
