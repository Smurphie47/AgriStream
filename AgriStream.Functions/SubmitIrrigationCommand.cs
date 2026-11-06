using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions
{
    public class SubmitIrrigationCommand
    {
        private readonly QueueClient _queue;
        private readonly ILogger<SubmitIrrigationCommand> _logger;

        public SubmitIrrigationCommand(
            IConfiguration configuration,
            ILogger<SubmitIrrigationCommand> logger)
        {
            var connectionString = configuration["FarmStorage"]
                ?? throw new InvalidOperationException(
                    "FarmStorage is missing.");

            _queue = new QueueClient(
                connectionString,
                "irrigation-commands",
                new QueueClientOptions
                {
                    MessageEncoding = QueueMessageEncoding.Base64
                });

            _logger = logger;
        }

        [Function("SubmitIrrigationCommand")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = "irrigation")] HttpRequest request)
        {
            IrrigationRequest? input;

            try
            {
                input = await JsonSerializer
                    .DeserializeAsync<IrrigationRequest>(
                        request.Body,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        },
                        request.HttpContext.RequestAborted);
            }
            catch (JsonException)
            {
                return new BadRequestObjectResult(
                    new { error = "Send a valid JSON request." });
            }

            var allowedDevices = new[]
            {
                "SENSOR-001", "SENSOR-002", "SENSOR-003"
            };

            if (input == null ||
                !allowedDevices.Contains(input.DeviceId))
            {
                return new BadRequestObjectResult(
                    new { error = "A valid DeviceId is required." });
            }

            var command = new
            {
                CommandId = Guid.NewGuid().ToString("N"),
                DeviceId = input.DeviceId,
                Action = "StartIrrigation",
                RequestedAt = DateTimeOffset.UtcNow
            };

            await _queue.SendMessageAsync(
                JsonSerializer.Serialize(command),
                cancellationToken: request.HttpContext.RequestAborted);

            _logger.LogInformation(
                "Queued irrigation command {CommandId} for {DeviceId}.",
                command.CommandId,
                command.DeviceId);

            return new ObjectResult(new
            {
                command.CommandId,
                command.DeviceId,
                Status = "Queued"
            })
            {
                StatusCode = StatusCodes.Status202Accepted
            };
        }

        public class IrrigationRequest
        {
            public string DeviceId { get; set; } = string.Empty;
        }
    }
}