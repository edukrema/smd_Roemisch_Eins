namespace Game.Domain;

public class ParticipantAttribute
{
    public const int KeyMaxLength = 50;
    public const int ValueMaxLength = 200;

    private ParticipantAttribute()
    {
        Key = "";
    }

    internal ParticipantAttribute(Guid participantId, string key, string? value)
    {
        Id = Guid.NewGuid();
        ParticipantId = participantId;
        Key = key;
        Value = value;
    }

    public Guid Id { get; private set; }

    public Guid ParticipantId { get; private set; }

    public string Key { get; private set; }

    public string? Value { get; private set; }

    internal void Update(string key, string? value)
    {
        Key = key;
        Value = value;
    }

    internal static string NormalizeKey(string? key)
    {
        var trimmed = (key ?? "").Trim();
        if (trimmed.Length is 0 or > KeyMaxLength)
        {
            throw new ValidationException($"An attribute key must be 1 to {KeyMaxLength} characters long.");
        }

        return trimmed;
    }

    internal static string? NormalizeValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > ValueMaxLength)
        {
            throw new ValidationException($"An attribute value must not exceed {ValueMaxLength} characters.");
        }

        return trimmed;
    }
}
