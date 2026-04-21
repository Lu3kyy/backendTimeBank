namespace BlogApiPrev.Models.DTOS
{
    public class HelpPostDTO
    {
        public int Id { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public string? CreatorProfilePictureUrl { get; set; }
        public string Category { get; set; } = string.Empty;
        public string PostType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double? DistanceKm { get; set; }
        public bool IsOpen { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
