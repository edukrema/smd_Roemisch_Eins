namespace Game.Domain;

public class Game
{
    // REQ08
    public const int MaxPlayersLowerLimit = 1;
    public const int MaxPlayersUpperLimit = 50;

    /// <summary>New lobbies start with this limit; the game master adjusts it in the lobby settings.</summary>
    public const int DefaultMaxPlayers = 4;

    private readonly List<Participant> _participants = [];

    private Game()
    {
    }

    public Guid Id { get; private set; }

    public Guid GameMasterId { get; private set; }

    public int MaxPlayers { get; private set; }

    // REQ07: Lobby -> Running -> Finished (Start, End, EnsureCanBeDeleted)
    public GameStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<Participant> Participants => _participants;

    public bool IsFull => _participants.Count >= MaxPlayers;

    public static Game Create(Guid gameMasterId, int maxPlayers)
    {
        EnsureValidMaxPlayers(maxPlayers);

        return new Game
        {
            Id = Guid.NewGuid(),
            GameMasterId = gameMasterId,
            MaxPlayers = maxPlayers,
            Status = GameStatus.Lobby,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsGameMaster(Guid userId) => GameMasterId == userId;

    public bool IsParticipant(Guid userId) => _participants.Any(p => p.UserId == userId);

    public void Join(UserProfile user)
    {
        EnsureStatus("Joining is only possible in the lobby.", GameStatus.Lobby);

        if (!user.HasUsername)
        {
            throw new DomainException("Please set a username first.");
        }

        if (IsGameMaster(user.Id))
        {
            throw new DomainException("The game master cannot join their own game.");
        }

        if (IsParticipant(user.Id))
        {
            throw new DomainException("You are already in this game.");
        }

        if (IsFull)
        {
            throw new DomainException("The lobby is full.");
        }

        _participants.Add(new Participant(Id, user.Id));
    }

    public void Leave(Guid userId)
    {
        if (IsGameMaster(userId))
        {
            throw new DomainException("The game master cannot leave the game, only end or delete it.");
        }

        EnsureStatus("A finished game cannot be left.", GameStatus.Lobby, GameStatus.Running);

        _participants.Remove(GetParticipant(userId));
    }

    public void Start()
    {
        EnsureStatus("Only a game in the lobby can be started.", GameStatus.Lobby);

        if (_participants.Count == 0)
        {
            throw new DomainException("At least one player is required to start.");
        }

        Status = GameStatus.Running;
    }

    public void End()
    {
        EnsureStatus("Only a running game can be ended.", GameStatus.Running);

        Status = GameStatus.Finished;
    }

    public void UpdateSettings(int maxPlayers)
    {
        EnsureStatus("Settings can only be changed in the lobby.", GameStatus.Lobby);
        EnsureValidMaxPlayers(maxPlayers);

        if (maxPlayers < _participants.Count)
        {
            throw new DomainException("The maximum number of players must not be below the current number of players.");
        }

        MaxPlayers = maxPlayers;
    }

    public void ReplaceAttributes(Guid userId, IReadOnlyCollection<(string Key, string? Value)> attributes)
    {
        EnsureStatus("Attributes can no longer be changed in a finished game.",
            GameStatus.Lobby, GameStatus.Running);

        GetParticipant(userId).ReplaceAttributes(attributes);
    }

    public void EnsureCanBeDeleted()
    {
        EnsureStatus("A running game cannot be deleted.", GameStatus.Lobby, GameStatus.Finished);
    }

    private Participant GetParticipant(Guid userId) =>
        _participants.FirstOrDefault(p => p.UserId == userId)
        ?? throw new DomainException("This user is not a player in this game.");

    private void EnsureStatus(string message, params GameStatus[] allowed)
    {
        if (!allowed.Contains(Status))
        {
            throw new DomainException(message);
        }
    }

    private static void EnsureValidMaxPlayers(int maxPlayers)
    {
        if (maxPlayers is < MaxPlayersLowerLimit or > MaxPlayersUpperLimit)
        {
            throw new ValidationException(
                $"The maximum number of players must be between {MaxPlayersLowerLimit} and {MaxPlayersUpperLimit}.");
        }
    }
}
