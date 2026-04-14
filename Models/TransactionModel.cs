using System.ComponentModel.DataAnnotations;

namespace BlogApiPrev.Models
{
    public class TransactionModel
    {
        [Key]
        public int TransactionId { get; set; }
        public int SenderId { get; set; }
        public string? SenderUser { get; set; }
        public int ReceiverId { get; set; }
        public string? ReceiverUser { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}