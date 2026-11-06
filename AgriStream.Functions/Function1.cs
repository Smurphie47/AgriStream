using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions
{
    public class GetLatestTelemetry
    {
        private readonly TableClient _table;
        private readonly ILogger<GetLatestTelemetry> _logger;

        public GetLatestTelemetry(
            IConfiguration configuration,
            ILogger<GetLatestTelemetry> logger)
        {
            var connectionString = configuration["FarmStorage"]
                ?? throw new InvalidOperationException(
                    "FarmStorage is missing.");

            _table = new TableClient(connectionString, "Telemetry");
            _logger = logger;
        }

        [Function("GetLatestTelemetry")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get",
                Route = "telemetry/latest")] HttpRequest request)
        {
            var deviceIds = new[]
            {
                "SENSOR-001",
                "SENSOR-002",
                "SENSOR-003"
            };

            var tasks = deviceIds.Select(
                id => GetLatestReadingAsync(
                    id, request.HttpContext.RequestAborted));

            var results = await Task.WhenAll(tasks);

            var readings = results
                .Where(reading => reading != null)
                .ToArray();

            _logger.LogInformation(
                "Retrieved latest telemetry for {DeviceCount} devices.",
                readings.Length);

            return new OkObjectResult(readings);
        }

        private async Task<object?> GetLatestReadingAsync(
            string deviceId,
            CancellationToken cancellationToken)
        {
            var filter = TableClient.CreateQueryFilter(
                $"PartitionKey eq {deviceId}");

            // Our reverse-timestamp RowKeys put the newest reading first.
            await foreach (var entity in _table.QueryAsync<TableEntity>(
                filter: filter,
                maxPerPage: 1,
                cancellationToken: cancellationToken))
            {
                return new
                {
                    DeviceId = entity.PartitionKey,
                    FieldName = entity.GetString("FieldName"),
                    SoilMoisture = entity.GetDouble("SoilMoisture"),
                    Temperature = entity.GetDouble("Temperature"),
                    Humidity = entity.GetDouble("Humidity"),
                    RecordedAt = entity.GetDateTimeOffset("RecordedAt")
                };
            }

            return null;
        }
    }
}