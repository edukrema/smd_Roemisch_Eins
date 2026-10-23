namespace Game.Contracts;

/// <summary>Signs in; unknown usernames are registered automatically.</summary>
public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token);
