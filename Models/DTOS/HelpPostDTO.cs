namespace BlogApiPrev.Models.DTOS
{
    public class HelpPostDTO
    {
        public int Id { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatorUsername { get; set; } = string.Empty;
        public string? CreatorName { get; set; }
        public string? CreatorDescription { get; set; }
        public string? CreatorProfilePictureUrl { get; set; }
        public int CreatorCredits { get; set; }
        public string Category { get; set; } = string.Empty;
        public string PostType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? DistanceKm { get; set; }
        public bool IsOpen { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
