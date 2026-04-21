using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlogApiPrev.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectMessageInboxReadTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAtUtc",
                table: "ChatMessages",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReadAtUtc",
                table: "ChatMessages");
        }
    }
}
