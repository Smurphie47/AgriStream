using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AgriStream.web.Services
{
    public class DroneImageService
    {
        public const long MaxFileSize = 20 * 1024 * 1024;

        private readonly BlobContainerClient _container;

        public DroneImageService(IConfiguration configuration)
        {
            var connectionString =
                configuration["Storage:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "Storage connection string is missing.");

            _container = new BlobContainerClient(
                connectionString, "drone-images");
        }

        public async Task<string> UploadAsync(
            IFormFile image,
            CancellationToken cancellationToken = default)
        {
            if (image.Length == 0 || image.Length > MaxFileSize)
            {
                throw new ArgumentException(
                    "Select an image between 1 byte and 20 MB.");
            }

            var extension = Path.GetExtension(image.FileName)
                .ToLowerInvariant();

            if (extension is not (".jpg" or ".jpeg" or ".png"))
            {
                throw new ArgumentException(
                    "Only JPG and PNG images are allowed.");
            }

            await using var stream = image.OpenReadStream();

            var header = new byte[8];
            var bytesRead = 0;

            while (bytesRead < header.Length)
            {
                var read = await stream.ReadAsync(
                    header.AsMemory(bytesRead),
                    cancellationToken);

                if (read == 0)
                {
                    break;
                }

                bytesRead += read;
            }

            var isJpeg = bytesRead >= 3 &&
                         header[0] == 0xFF &&
                         header[1] == 0xD8 &&
                         header[2] == 0xFF;

            var isPng = bytesRead == 8 &&
                        header[0] == 0x89 &&
                        header[1] == 0x50 &&
                        header[2] == 0x4E &&
                        header[3] == 0x47 &&
                        header[4] == 0x0D &&
                        header[5] == 0x0A &&
                        header[6] == 0x1A &&
                        header[7] == 0x0A;

            var matchesExtension = extension == ".png"
                ? isPng
                : isJpeg;

            if (!matchesExtension)
            {
                throw new ArgumentException(
                    "The file signature does not match its image extension.");
            }

            // Use a fresh stream to upload the complete file.
            await using var uploadStream = image.OpenReadStream();

            var blobName =
                $"{DateTime.UtcNow:yyyyMMdd}/{Guid.NewGuid():N}{extension}";

            var blob = _container.GetBlobClient(blobName);

            await blob.UploadAsync(
                uploadStream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = isPng
                            ? "image/png"
                            : "image/jpeg"
                    }
                },
                cancellationToken);

            return blobName;
        }
    }
}