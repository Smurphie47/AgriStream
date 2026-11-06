using System.Data;
using AgriStream.web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace AgriStream.web.Services
{
    public class UserService
    {
        private readonly string _connectionString;
        private readonly PasswordHasher<AppUser> _hasher = new();

        public UserService(IConfiguration configuration)
        {
            _connectionString = configuration
                .GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "DefaultConnection is missing.");
        }

        public async Task<AppUser?> ValidateLoginAsync(
            string username, string password)
        {
            await using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            const string sql = """
                SELECT UserId, Username, PasswordHash, Role, CreatedAt
                FROM dbo.Users
                WHERE Username = @Username;
                """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@Username", SqlDbType.NVarChar, 100)
                .Value = username.Trim();

            AppUser user;

            await using (var reader = await command.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    return null;
                }

                user = new AppUser
                {
                    UserId = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    PasswordHash = reader.GetString(2),
                    Role = reader.GetString(3),
                    CreatedAt = reader.GetDateTime(4)
                };
            }

            var result = _hasher.VerifyHashedPassword(
                user, user.PasswordHash, password);

            if (result == PasswordVerificationResult.Failed)
            {
                return null;
            }

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                const string updateSql = """
                    UPDATE dbo.Users
                    SET PasswordHash = @PasswordHash
                    WHERE UserId = @UserId;
                    """;

                await using var update =
                    new SqlCommand(updateSql, connection);

                update.Parameters.Add(
                    "@PasswordHash", SqlDbType.NVarChar, 500)
                    .Value = _hasher.HashPassword(user, password);

                update.Parameters.Add("@UserId", SqlDbType.Int)
                    .Value = user.UserId;

                await update.ExecuteNonQueryAsync();
            }

            return user;
        }

        public async Task CreateUserIfMissingAsync(
            string username, string password, string role)
        {
            if (role is not ("Farmer" or "Agronomist" or "Admin"))
            {
                throw new ArgumentException("Invalid system role.");
            }

            var user = new AppUser
            {
                Username = username.Trim(),
                Role = role
            };

            user.PasswordHash = _hasher.HashPassword(user, password);

            await using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            const string sql = """
                INSERT INTO dbo.Users (Username, PasswordHash, Role)
                SELECT @Username, @PasswordHash, @Role
                WHERE NOT EXISTS
                (
                    SELECT 1 FROM dbo.Users
                    WHERE Username = @Username
                );
                """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@Username", SqlDbType.NVarChar, 100)
                .Value = user.Username;

            command.Parameters.Add(
                "@PasswordHash", SqlDbType.NVarChar, 500)
                .Value = user.PasswordHash;

            command.Parameters.Add(
                "@Role", SqlDbType.NVarChar, 20)
                .Value = user.Role;

            await command.ExecuteNonQueryAsync();
        }
    }
}