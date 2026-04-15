namespace BlogApiPrev.Models
{
    public class UserModel
    {
        public int Id {get; set;}
        public string Username {get; set;} = string.Empty;
        public string Salt {get; set;} = string.Empty;
        public string Hash {get; set;} = string.Empty;
        public string? Name {get; set;}
        public string? ProfilePictureUrl {get; set;}
        public string? Description {get; set;}
        public int Credits {get; set;} = 10;
        public DateTime CreatedAtUtc {get; set;} = DateTime.UtcNow;
        public DateTime UpdatedAtUtc {get; set;} = DateTime.UtcNow;
    }
}