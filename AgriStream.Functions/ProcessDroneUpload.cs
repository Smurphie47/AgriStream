using System.Security.Cryptography;
using System.Text;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions
{
    public class ProcessDroneUpload
    {
        private readonly TableClient _audit;
        private readonly ILogger<ProcessDroneUpload> _logger;

        public ProcessDroneUpload(
            IConfiguration configuration,
            ILogger<ProcessDroneUpload> logger)
        {
            var connectionString = configuration["FarmStorage"]
                ?? throw new InvalidOperationException(
                    "FarmStorage is missing.");

            _audit = new TableClient(
                connectionString, "UploadAudit");

            _logger = logger;
        }

        [Function("ProcessDroneUpload")]
        public async Task Run(
            [BlobTrigger(
                "drone-images/{name}",
                Connection = "FarmStorage")] Stream image,
            string name)
        {
            _logger.LogInformation(
                "Drone image upload detected: {BlobName}.", name);

            // A stable key avoids duplicate audit rows on retries.
            var rowKey = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(name)));

            var entry = new TableEntity("DroneUploads", rowKey)
            {
                ["BlobName"] = name,
                ["ContainerName"] = "drone-images",
                ["ProcessingStatus"] = "UploadLogged",
                ["ProcessedAt"] = DateTimeOffset.UtcNow
            };

            await _audit.UpsertEntityAsync(
                entry, TableUpdateMode.Replace);

            _logger.LogInformation(
                "Upload audit saved for {BlobName}. Status: UploadLogged.",
                name);
        }
    }
}