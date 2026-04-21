namespace BlogApiPrev.Models.DTOS
{
    public class HelpPostCreateDTO
    {
        public string Category { get; set; } = string.Empty;
        public string PostType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
