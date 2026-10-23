using System.Text.Json.Serialization;

namespace Game.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter<GameStatusDto>))]
public enum GameStatusDto
{
    Lobby,
    Running,
    Finished
}

public record UpdateSettingsRequest(int MaxPlayers);

/// <summary>An entry in the lobby list.</summary>
public record GameSummaryDto(Guid Id, string GameMasterName, GameStatusDto Status, int PlayerCount, int MaxPlayers);

/// <summary>The complete game state including all players and attributes.</summary>
public record GameDto(
    Guid Id,
    Guid GameMasterId,
    string GameMasterName,
    GameStatusDto Status,
    int MaxPlayers,
    IReadOnlyList<ParticipantDto> Participants);

public record ParticipantDto(Guid UserId, string Username, IReadOnlyList<AttributeDto> Attributes);

public record AttributeDto(string Key, string? Value);
