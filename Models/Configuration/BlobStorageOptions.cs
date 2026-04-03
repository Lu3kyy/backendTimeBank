namespace BlogApiPrev.Models.Configuration
{
    public class BlobStorageOptions
    {
        public const string SectionName = "BlobStorage";

        public string? ConnectionString { get; set; }
        public string ContainerName { get; set; } = "profile-images";
        public string? PublicBaseUrl { get; set; }
    }
}
