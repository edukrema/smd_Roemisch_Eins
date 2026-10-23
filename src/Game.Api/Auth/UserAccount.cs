namespace Game.Api.Auth;

public class UserAccount
{
    public const int UsernameMaxLength = 50;

    private UserAccount()
    {
        Username = "";
        PasswordHash = "";
    }

    public Guid Id { get; private set; }

    public string Username { get; private set; }

    public string PasswordHash { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static UserAccount Create(string username, string password) => new()
    {
        Id = Guid.NewGuid(),
        Username = username,
        PasswordHash = PasswordHashing.Hash(password),
        CreatedAt = DateTime.UtcNow
    };

    public bool VerifyPassword(string password) => PasswordHashing.Verify(password, PasswordHash);
}
