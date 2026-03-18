namespace BlogApiPrev.Models.DTOS
{
    public class HelpCategoryDTO
    {
        public string Category { get; set; } = string.Empty;
        public List<string> Subcategories { get; set; } = [];
    }
}
