namespace BlogApiPrev.Models.DTOS
{
    public class ChatMessageDTO
    {
        public int Id { get; set; }
        public int ChatThreadId { get; set; }
        public int SenderUserId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentAtUtc { get; set; }
    }
}
