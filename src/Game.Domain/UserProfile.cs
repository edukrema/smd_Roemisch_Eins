namespace Game.Domain;

public class UserProfile
{
    public const int UsernameMinLength = 3;
    public const int UsernameMaxLength = 20;

    private UserProfile()
    {
    }

    public UserProfile(Guid id)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>The account ID (sub claim of the token).</summary>
    public Guid Id { get; private set; }

    public string? Username { get; private set; }

    /// <summary>Uppercased username so the unique index applies case-insensitively.</summary>
    public string? UsernameNormalized { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public bool HasUsername => Username is not null;

    public void ChangeUsername(string? username)
    {
        var trimmed = (username ?? "").Trim();

        if (trimmed.Length is < UsernameMinLength or > UsernameMaxLength)
        {
            throw new ValidationException(
                $"The username must be {UsernameMinLength} to {UsernameMaxLength} characters long.");
        }

        if (!trimmed.All(IsAllowedCharacter))
        {
            throw new ValidationException("The username may only contain letters, digits, _ and -.");
        }

        Username = trimmed;
        UsernameNormalized = trimmed.ToUpperInvariant();
    }

    private static bool IsAllowedCharacter(char c) => char.IsAsciiLetterOrDigit(c) || c is '_' or '-';
}
