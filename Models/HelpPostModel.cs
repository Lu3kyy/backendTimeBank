namespace BlogApiPrev.Models
{
    public class HelpPostModel
    {
        public int Id { get; set; }
        public int CreatedByUserId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string PostType { get; set; } = string.Empty;
        public string Subcategory { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsOpen { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAtUtc { get; set; }
    }
}
