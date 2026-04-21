namespace BlogApiPrev.Models.DTOS
{
    public class DmInboxItemDTO
    {
        public int ThreadId { get; set; }
        public int OtherUserId { get; set; }
        public string OtherUsername { get; set; } = string.Empty;
        public string OtherDisplayName { get; set; } = string.Empty;
        public string? OtherProfilePictureUrl { get; set; }
        public string LastMessagePreview { get; set; } = string.Empty;
        public DateTime LastMessageAtUtc { get; set; }
        public string LastMessageFromUsername { get; set; } = string.Empty;
        public int UnreadCount { get; set; }
    }
}