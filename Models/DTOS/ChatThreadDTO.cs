namespace BlogApiPrev.Models.DTOS
{
    public class ChatThreadDTO
    {
        public int Id { get; set; }
        public int HelpPostId { get; set; }
        public int InitiatorUserId { get; set; }
        public int RecipientUserId { get; set; }
        public string InitiatorName { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartedAtUtc { get; set; }
        public DateTime? EndedAtUtc { get; set; }
    }
}
