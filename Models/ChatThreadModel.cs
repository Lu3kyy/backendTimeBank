namespace BlogApiPrev.Models
{
    public class ChatThreadModel
    {
        public int Id { get; set; }
        public int HelpPostId { get; set; }
        public int InitiatorUserId { get; set; }
        public int RecipientUserId { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAtUtc { get; set; }
    }
}
