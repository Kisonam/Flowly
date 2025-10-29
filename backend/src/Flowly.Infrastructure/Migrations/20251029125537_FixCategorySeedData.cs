using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Flowly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCategorySeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("0239502c-e983-49f1-a554-3ea1d55e7041"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("1346525b-83b1-4085-9289-c12d118d2ef6"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("2baa6ecb-b24c-4b6a-9095-0e438dbfab51"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("5b0b51f3-e0ea-46c9-a5df-d690b8885608"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("827992c7-0617-434f-b25f-207979536ec9"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("9152637d-2d4f-4d5d-b81b-f86402a1db6b"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("bcdc067d-be4f-4c6e-8854-bdec2e5b22ab"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("c8d4d6cb-27f6-40fa-8654-43d71405e2b9"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("d8de97bc-f50a-4bd1-935a-73b8dd15fa4c"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("db56ea0a-3003-4a1a-a2eb-92c245db4d15"));

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name", "UserId" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), "Food & Drinks", null },
                    { new Guid("00000000-0000-0000-0000-000000000002"), "Transport", null },
                    { new Guid("00000000-0000-0000-0000-000000000003"), "Shopping", null },
                    { new Guid("00000000-0000-0000-0000-000000000004"), "Entertainment", null },
                    { new Guid("00000000-0000-0000-0000-000000000005"), "Health", null },
                    { new Guid("00000000-0000-0000-0000-000000000006"), "Education", null },
                    { new Guid("00000000-0000-0000-0000-000000000007"), "Utilities", null },
                    { new Guid("00000000-0000-0000-0000-000000000008"), "Salary", null },
                    { new Guid("00000000-0000-0000-0000-000000000009"), "Freelance", null },
                    { new Guid("00000000-0000-0000-0000-00000000000a"), "Other", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-00000000000a"));

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name", "UserId" },
                values: new object[,]
                {
                    { new Guid("0239502c-e983-49f1-a554-3ea1d55e7041"), "Transport", null },
                    { new Guid("1346525b-83b1-4085-9289-c12d118d2ef6"), "Food & Drinks", null },
                    { new Guid("2baa6ecb-b24c-4b6a-9095-0e438dbfab51"), "Other", null },
                    { new Guid("5b0b51f3-e0ea-46c9-a5df-d690b8885608"), "Freelance", null },
                    { new Guid("827992c7-0617-434f-b25f-207979536ec9"), "Salary", null },
                    { new Guid("9152637d-2d4f-4d5d-b81b-f86402a1db6b"), "Utilities", null },
                    { new Guid("bcdc067d-be4f-4c6e-8854-bdec2e5b22ab"), "Education", null },
                    { new Guid("c8d4d6cb-27f6-40fa-8654-43d71405e2b9"), "Shopping", null },
                    { new Guid("d8de97bc-f50a-4bd1-935a-73b8dd15fa4c"), "Health", null },
                    { new Guid("db56ea0a-3003-4a1a-a2eb-92c245db4d15"), "Entertainment", null }
                });
        }
    }
}
