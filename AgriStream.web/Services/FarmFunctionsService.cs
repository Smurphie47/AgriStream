using System.Net.Http.Json;
using AgriStream.web.Models;

namespace AgriStream.web.Services
{
    public class FarmFunctionsService
    {
        private readonly HttpClient _http;

        public FarmFunctionsService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<TelemetryReading>> GetLatestAsync(
            CancellationToken cancellationToken = default)
        {
            return await _http.GetFromJsonAsync<List<TelemetryReading>>(
                "api/telemetry/latest",
                cancellationToken)
                ?? new List<TelemetryReading>();
        }

        public async Task TriggerIrrigationAsync(
            string deviceId,
            CancellationToken cancellationToken = default)
        {
            using var response = await _http.PostAsJsonAsync(
                "api/irrigation",
                new { deviceId },
                cancellationToken);

            response.EnsureSuccessStatusCode();
        }
    }
}