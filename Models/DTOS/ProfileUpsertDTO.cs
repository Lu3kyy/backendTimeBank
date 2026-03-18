namespace BlogApiPrev.Models.DTOS
{
    public class ProfileUpsertDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string? Description { get; set; }
    }
}
