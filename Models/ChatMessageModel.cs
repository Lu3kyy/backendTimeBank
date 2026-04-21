namespace BlogApiPrev.Models
{
    public class ChatMessageModel
    {
        public int Id { get; set; }
        public int ChatThreadId { get; set; }
        public int SenderUserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAtUtc { get; set; }
    }
}
