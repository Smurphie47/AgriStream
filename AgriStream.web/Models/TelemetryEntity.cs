using Azure;
using Azure.Data.Tables;

namespace AgriStream.web.Models
{
    public class TelemetryEntity : ITableEntity
    {
        // Groups readings by device.
        public string PartitionKey { get; set; } = string.Empty;

        // Reverse timestamp allows newest readings to appear first.
        public string RowKey { get; set; } = string.Empty;

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public string FieldName { get; set; } = string.Empty;

        public double SoilMoisture { get; set; }

        public double Temperature { get; set; }

        public double Humidity { get; set; }

        public DateTimeOffset RecordedAt { get; set; }
    }
}