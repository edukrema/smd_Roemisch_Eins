namespace Game.Contracts;

/// <summary>The logged-in user. Username is null until it has been set.</summary>
public record MeDto(Guid Id, string? Username);

public record ChangeUsernameRequest(string Username);
