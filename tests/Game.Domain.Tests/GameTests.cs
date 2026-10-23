namespace Game.Domain.Tests;

public class GameTests
{
    private static readonly Guid GameMasterId = Guid.NewGuid();

    private static UserProfile NewUser(string name = "player")
    {
        var user = new UserProfile(Guid.NewGuid());
        user.ChangeUsername(name);
        return user;
    }

    private static Game NewLobby(int maxPlayers = 4) => Game.Create(GameMasterId, maxPlayers);

    private static Game NewRunningGame(UserProfile player)
    {
        var game = NewLobby();
        game.Join(player);
        game.Start();
        return game;
    }

    [Fact]
    public void Create_StartsInLobbyWithoutPlayers()
    {
        var game = NewLobby(maxPlayers: 3);

        Assert.Equal(GameStatus.Lobby, game.Status);
        Assert.Equal(GameMasterId, game.GameMasterId);
        Assert.Equal(3, game.MaxPlayers);
        Assert.Empty(game.Participants);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public void Create_WithMaxPlayersInRange_Succeeds(int maxPlayers)
    {
        var game = NewLobby(maxPlayers);

        Assert.Equal(maxPlayers, game.MaxPlayers);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    [InlineData(-1)]
    public void Create_WithMaxPlayersOutOfRange_Throws(int maxPlayers)
    {
        Assert.Throws<ValidationException>(() => NewLobby(maxPlayers));
    }

    [Fact]
    public void Join_InLobby_AddsParticipant()
    {
        var game = NewLobby();
        var user = NewUser();

        game.Join(user);

        Assert.True(game.IsParticipant(user.Id));
    }

    [Fact]
    public void Join_WhenLobbyIsFull_Throws()
    {
        var game = NewLobby(maxPlayers: 1);
        game.Join(NewUser("first"));

        Assert.Throws<DomainException>(() => game.Join(NewUser("second")));
    }

    [Fact]
    public void Join_WhenAlreadyParticipant_Throws()
    {
        var game = NewLobby();
        var user = NewUser();
        game.Join(user);

        Assert.Throws<DomainException>(() => game.Join(user));
    }

    [Fact]
    public void Join_AsGameMaster_Throws()
    {
        var game = NewLobby();
        var gameMaster = new UserProfile(GameMasterId);
        gameMaster.ChangeUsername("master");

        Assert.Throws<DomainException>(() => game.Join(gameMaster));
    }

    [Fact]
    public void Join_WithoutUsername_Throws()
    {
        var game = NewLobby();

        Assert.Throws<DomainException>(() => game.Join(new UserProfile(Guid.NewGuid())));
    }

    [Fact]
    public void Join_WhenRunning_Throws()
    {
        var game = NewRunningGame(NewUser());

        Assert.Throws<DomainException>(() => game.Join(NewUser("late")));
    }

    [Fact]
    public void Join_AfterLeavingRunningGame_Throws()
    {
        var player = NewUser();
        var game = NewRunningGame(player);
        game.Leave(player.Id);

        Assert.Throws<DomainException>(() => game.Join(player));
    }

    [Fact]
    public void Join_AfterLeavingLobby_Succeeds()
    {
        var game = NewLobby();
        var player = NewUser();
        game.Join(player);
        game.Leave(player.Id);

        game.Join(player);

        Assert.True(game.IsParticipant(player.Id));
    }

    [Fact]
    public void Leave_InLobby_RemovesParticipant()
    {
        var game = NewLobby();
        var player = NewUser();
        game.Join(player);

        game.Leave(player.Id);

        Assert.False(game.IsParticipant(player.Id));
    }

    [Fact]
    public void Leave_WhenRunning_RemovesParticipantWithAttributes()
    {
        var player = NewUser();
        var game = NewRunningGame(player);
        game.ReplaceAttributes(player.Id, [("hp", "10")]);

        game.Leave(player.Id);

        Assert.Empty(game.Participants);
    }

    [Fact]
    public void Leave_AsGameMaster_Throws()
    {
        var game = NewLobby();

        Assert.Throws<DomainException>(() => game.Leave(GameMasterId));
    }

    [Fact]
    public void Leave_WhenNotParticipant_Throws()
    {
        var game = NewLobby();

        Assert.Throws<DomainException>(() => game.Leave(Guid.NewGuid()));
    }

    [Fact]
    public void Leave_WhenFinished_Throws()
    {
        var player = NewUser();
        var game = NewRunningGame(player);
        game.End();

        Assert.Throws<DomainException>(() => game.Leave(player.Id));
    }

    [Fact]
    public void Leave_WhenAllPlayersLeave_GameKeepsRunning()
    {
        var player = NewUser();
        var game = NewRunningGame(player);

        game.Leave(player.Id);

        Assert.Equal(GameStatus.Running, game.Status);
    }

    [Fact]
    public void Start_WithPlayer_SetsRunning()
    {
        var game = NewRunningGame(NewUser());

        Assert.Equal(GameStatus.Running, game.Status);
    }

    [Fact]
    public void Start_WithoutPlayers_Throws()
    {
        var game = NewLobby();

        Assert.Throws<DomainException>(game.Start);
    }

    [Fact]
    public void Start_WhenAlreadyRunning_Throws()
    {
        var game = NewRunningGame(NewUser());

        Assert.Throws<DomainException>(game.Start);
    }

    [Fact]
    public void End_WhenRunning_SetsFinished()
    {
        var game = NewRunningGame(NewUser());

        game.End();

        Assert.Equal(GameStatus.Finished, game.Status);
    }

    [Fact]
    public void End_InLobby_Throws()
    {
        var game = NewLobby();

        Assert.Throws<DomainException>(game.End);
    }

    [Fact]
    public void End_WhenFinished_Throws()
    {
        var game = NewRunningGame(NewUser());
        game.End();

        Assert.Throws<DomainException>(game.End);
    }

    [Fact]
    public void UpdateSettings_InLobby_ChangesMaxPlayers()
    {
        var game = NewLobby(maxPlayers: 4);

        game.UpdateSettings(10);

        Assert.Equal(10, game.MaxPlayers);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void UpdateSettings_OutOfRange_Throws(int maxPlayers)
    {
        var game = NewLobby();

        Assert.Throws<ValidationException>(() => game.UpdateSettings(maxPlayers));
    }

    [Fact]
    public void UpdateSettings_BelowCurrentPlayerCount_Throws()
    {
        var game = NewLobby(maxPlayers: 4);
        game.Join(NewUser("first"));
        game.Join(NewUser("second"));

        Assert.Throws<DomainException>(() => game.UpdateSettings(1));
    }

    [Fact]
    public void UpdateSettings_WhenRunning_Throws()
    {
        var game = NewRunningGame(NewUser());

        Assert.Throws<DomainException>(() => game.UpdateSettings(10));
    }

    [Theory]
    [InlineData(GameStatus.Lobby)]
    [InlineData(GameStatus.Finished)]
    public void EnsureCanBeDeleted_InLobbyOrFinished_DoesNotThrow(GameStatus status)
    {
        var game = GameIn(status);

        game.EnsureCanBeDeleted();
    }

    [Fact]
    public void EnsureCanBeDeleted_WhenRunning_Throws()
    {
        var game = GameIn(GameStatus.Running);

        Assert.Throws<DomainException>(game.EnsureCanBeDeleted);
    }

    [Theory]
    [InlineData(GameStatus.Lobby)]
    [InlineData(GameStatus.Running)]
    public void ReplaceAttributes_InLobbyOrRunning_SetsAttributes(GameStatus status)
    {
        var player = NewUser();
        var game = GameIn(status, player);

        game.ReplaceAttributes(player.Id, [("hp", "10"), ("class", "Mage")]);

        var attributes = game.Participants.Single().Attributes;
        Assert.Equal(["hp", "class"], attributes.Select(a => a.Key));
        Assert.Equal(["10", "Mage"], attributes.Select(a => a.Value));
    }

    [Fact]
    public void ReplaceAttributes_WhenFinished_Throws()
    {
        var player = NewUser();
        var game = GameIn(GameStatus.Finished, player);

        Assert.Throws<DomainException>(() => game.ReplaceAttributes(player.Id, [("hp", "10")]));
    }

    [Fact]
    public void ReplaceAttributes_ForNonParticipant_Throws()
    {
        var game = NewLobby();

        Assert.Throws<DomainException>(() => game.ReplaceAttributes(Guid.NewGuid(), [("hp", "10")]));
    }

    private static Game GameIn(GameStatus status, UserProfile? player = null)
    {
        var game = NewLobby();
        game.Join(player ?? NewUser());

        if (status is GameStatus.Running or GameStatus.Finished)
        {
            game.Start();
        }

        if (status is GameStatus.Finished)
        {
            game.End();
        }

        return game;
    }
}
