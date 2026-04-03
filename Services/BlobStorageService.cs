using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BlogApiPrev.Models.Configuration;
using Microsoft.Extensions.Options;

namespace BlogApiPrev.Services
{
    public class BlobStorageService
    {
        private readonly BlobStorageOptions _options;

        public BlobStorageService(IOptions<BlobStorageOptions> options)
        {
            _options = options.Value;
        }

        public bool IsConfigured()
        {
            return !string.IsNullOrWhiteSpace(_options.ConnectionString) &&
                   !string.IsNullOrWhiteSpace(_options.ContainerName);
        }

        public async Task<string> UploadProfileImageAsync(IFormFile file, int userId)
        {
            if (!IsConfigured())
            {
                throw new InvalidOperationException("BlobStorage settings are missing.");
            }

            var connectionString = _options.ConnectionString!;
            var containerName = _options.ContainerName.Trim();
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var extension = Path.GetExtension(file.FileName);
            var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".bin" : extension.ToLowerInvariant();
            var blobName = $"profiles/{userId}/{Guid.NewGuid():N}{safeExtension}";
            var blobClient = containerClient.GetBlobClient(blobName);

            await using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType ?? "application/octet-stream" });

            if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            {
                return $"{_options.PublicBaseUrl.TrimEnd('/')}/{blobName}";
            }

            return blobClient.Uri.ToString();
        }
    }
}
