using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Azure.Storage.Blobs;
using Azure.Data.Tables;
using Azure.Storage.Queues;

namespace AgriStream.web.Controllers
{
    public class DiagnosticsController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public DiagnosticsController(
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Database()
        {
            // This diagnostic is available only during local development.
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            var connectionString = _configuration
                .GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return Content("DefaultConnection is missing from User Secrets.");
            }

            await using var connection =
                new SqlConnection(connectionString);

            await connection.OpenAsync();

            await using var command = new SqlCommand(
                "SELECT COUNT(*) FROM dbo.Devices;",
                connection);

            var deviceCount = Convert.ToInt32(
                await command.ExecuteScalarAsync());

            return Content(
                $"Azure SQL connection successful! Devices found: {deviceCount}");
        }

        [HttpGet]
        public async Task<IActionResult> Storage()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            var connectionString =
                _configuration["Storage:ConnectionString"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return Content(
                    "Storage:ConnectionString is missing from User Secrets.");
            }

            var container = new BlobContainerClient(
                connectionString, "drone-images");

            var telemetry = new TableClient(
                connectionString, "Telemetry");

            var audit = new TableClient(
                connectionString, "UploadAudit");

            var queue = new QueueClient(
                connectionString, "irrigation-commands");

            var containerExists = (await container.ExistsAsync()).Value;
            var queueExists = (await queue.ExistsAsync()).Value;

            // Read up to one entity to verify access, even if tables are empty.
            await foreach (var entity in telemetry.QueryAsync<TableEntity>(
                maxPerPage: 1))
            {
                break;
            }

            await foreach (var entity in audit.QueryAsync<TableEntity>(
                maxPerPage: 1))
            {
                break;
            }

            return Content(
                $"Blob container found: {containerExists}\n" +
                "Telemetry table connection: Successful\n" +
                "UploadAudit table connection: Successful\n" +
                $"Irrigation queue found: {queueExists}");
        }
    }
}