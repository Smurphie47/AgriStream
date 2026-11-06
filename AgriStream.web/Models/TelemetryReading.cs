namespace AgriStream.web.Models
{
    public class TelemetryReading
    {
        public string DeviceId { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public double SoilMoisture { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public DateTimeOffset RecordedAt { get; set; }
    }
}