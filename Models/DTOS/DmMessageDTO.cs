namespace BlogApiPrev.Models.DTOS
{
    public class DmMessageDTO
    {
        public int Id { get; set; }
        public int ChatThreadId { get; set; }
        public int SenderUserId { get; set; }
        public string SenderUsername { get; set; } = string.Empty;
        public string SenderDisplayName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentAtUtc { get; set; }
        public DateTime? ReadAtUtc { get; set; }
        public bool IsMine { get; set; }
    }
}