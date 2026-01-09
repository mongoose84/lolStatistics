using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public static class LoginDto
{
    public record LoginRequest(
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("password")] string Password
    );

    public record LoginResponse(
        [property: JsonPropertyName("userId")] int UserId,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("message")] string Message
    );
}
