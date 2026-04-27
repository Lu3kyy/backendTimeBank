using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlogApiPrev.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSubcategoryFromHelpPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Subcategory",
                table: "HelpPosts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Subcategory",
                table: "HelpPosts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
