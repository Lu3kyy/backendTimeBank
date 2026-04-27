namespace BlogApiPrev.Models.DTOS
{
    public class HelpPostUpdateDTO
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
