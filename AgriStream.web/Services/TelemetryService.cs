using System.Globalization;
using AgriStream.web.Models;
using Azure.Data.Tables;

namespace AgriStream.web.Services
{
    public class TelemetryService
    {
        private readonly TableClient _table;

        public TelemetryService(IConfiguration configuration)
        {
            var connectionString =
                configuration["Storage:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "Storage connection string is missing.");

            _table = new TableClient(connectionString, "Telemetry");
        }

        public async Task<int> GenerateSampleReadingsAsync()
        {
            var devices = new[]
            {
                (Id: "SENSOR-001", Field: "North Field"),
                (Id: "SENSOR-002", Field: "South Field"),
                (Id: "SENSOR-003", Field: "East Field")
            };

            foreach (var device in devices)
            {
                var recordedAt = DateTimeOffset.UtcNow;

                var reverseTicks = (
                    DateTime.MaxValue.Ticks -
                    recordedAt.UtcDateTime.Ticks)
                    .ToString("D19", CultureInfo.InvariantCulture);

                var reading = new TelemetryEntity
                {
                    PartitionKey = device.Id,
                    RowKey = $"{reverseTicks}_{Guid.NewGuid():N}",
                    FieldName = device.Field,
                    SoilMoisture = Math.Round(
                        15 + Random.Shared.NextDouble() * 65, 1),
                    Temperature = Math.Round(
                        18 + Random.Shared.NextDouble() * 20, 1),
                    Humidity = Math.Round(
                        30 + Random.Shared.NextDouble() * 60, 1),
                    RecordedAt = recordedAt
                };

                await _table.AddEntityAsync(reading);
            }

            return devices.Length;
        }
    }
}