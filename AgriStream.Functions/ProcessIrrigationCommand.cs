using System.Data;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions
{
    public class ProcessIrrigationCommand
    {
        private readonly string _connectionString;
        private readonly ILogger<ProcessIrrigationCommand> _logger;

        public ProcessIrrigationCommand(
            IConfiguration configuration,
            ILogger<ProcessIrrigationCommand> logger)
        {
            _connectionString = configuration["SqlConnectionString"]
                ?? throw new InvalidOperationException(
                    "SqlConnectionString is missing.");

            _logger = logger;
        }

        [Function("ProcessIrrigationCommand")]
        public async Task Run(
            [QueueTrigger(
                "irrigation-commands",
                Connection = "FarmStorage")] string message)
        {
            var command = JsonSerializer.Deserialize<IrrigationCommand>(
                message,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (command == null ||
                string.IsNullOrWhiteSpace(command.CommandId) ||
                string.IsNullOrWhiteSpace(command.DeviceId) ||
                command.Action != "StartIrrigation")
            {
                throw new InvalidOperationException(
                    "Invalid irrigation command.");
            }

            await using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            const string sql = """
                UPDATE dbo.Devices
                SET OperationalStatus = @Status,
                    LastCommandId = @CommandId,
                    LastUpdated = SYSUTCDATETIME()
                WHERE DeviceId = @DeviceId
                  AND (
                      LastCommandId IS NULL
                      OR LastCommandId <> @CommandId
                  );
                """;

            await using var update = new SqlCommand(sql, connection);

            update.Parameters.Add(
                "@Status", SqlDbType.NVarChar, 50)
                .Value = "Irrigating";

            update.Parameters.Add(
                "@CommandId", SqlDbType.NVarChar, 100)
                .Value = command.CommandId;

            update.Parameters.Add(
                "@DeviceId", SqlDbType.NVarChar, 50)
                .Value = command.DeviceId;

            var updated = await update.ExecuteNonQueryAsync();

            if (updated == 0)
            {
                const string checkSql = """
                    SELECT COUNT(*)
                    FROM dbo.Devices
                    WHERE DeviceId = @DeviceId;
                    """;

                await using var check =
                    new SqlCommand(checkSql, connection);

                check.Parameters.Add(
                    "@DeviceId", SqlDbType.NVarChar, 50)
                    .Value = command.DeviceId;

                var exists = Convert.ToInt32(
                    await check.ExecuteScalarAsync()) > 0;

                if (!exists)
                {
                    throw new InvalidOperationException(
                        $"Device {command.DeviceId} was not found.");
                }

                _logger.LogInformation(
                    "Command {CommandId} already applied; skipped.",
                    command.CommandId);

                return;
            }

            _logger.LogInformation(
                "Processed command {CommandId}. " +
                "Device {DeviceId} status updated to Irrigating.",
                command.CommandId,
                command.DeviceId);
        }

        public class IrrigationCommand
        {
            public string CommandId { get; set; } = string.Empty;
            public string DeviceId { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public DateTimeOffset RequestedAt { get; set; }
        }
    }
}