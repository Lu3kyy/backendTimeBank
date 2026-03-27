namespace BlogApiPrev.Models.DTOS
{
    public class PasswordDTO
    {
        public string Salt {get; set;} = string.Empty;
        public string Hash {get; set;} = string.Empty;
    }
}