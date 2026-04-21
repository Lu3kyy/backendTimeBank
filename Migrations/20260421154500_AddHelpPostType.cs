using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlogApiPrev.Migrations
{
    /// <inheritdoc />
    public partial class AddHelpPostType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PostType",
                table: "HelpPosts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "request");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PostType",
                table: "HelpPosts");
        }
    }
}