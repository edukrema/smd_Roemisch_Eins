namespace Game.Domain;

public class Participant
{
    public const int MaxAttributes = 20;

    private readonly List<ParticipantAttribute> _attributes = [];

    private Participant()
    {
    }

    internal Participant(Guid gameId, Guid userId)
    {
        Id = Guid.NewGuid();
        GameId = gameId;
        UserId = userId;
        JoinedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid GameId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime JoinedAt { get; private set; }

    public IReadOnlyList<ParticipantAttribute> Attributes => _attributes;

    internal void ReplaceAttributes(IReadOnlyCollection<(string Key, string? Value)> attributes)
    {
        if (attributes.Count > MaxAttributes)
        {
            throw new ValidationException($"A player may have at most {MaxAttributes} attributes.");
        }

        var normalized = attributes
            .Select(a => (Key: ParticipantAttribute.NormalizeKey(a.Key), Value: ParticipantAttribute.NormalizeValue(a.Value)))
            .ToList();

        if (normalized.DistinctBy(a => a.Key, StringComparer.OrdinalIgnoreCase).Count() != normalized.Count)
        {
            throw new ValidationException("Attribute keys must be unique per player.");
        }

        // Existing rows are updated instead of recreated so the unique index
        // (ParticipantId, Key) is never briefly duplicated while saving.
        _attributes.RemoveAll(existing => !normalized.Any(a => IsSameKey(a.Key, existing.Key)));

        foreach (var (key, value) in normalized)
        {
            var existing = _attributes.FirstOrDefault(a => IsSameKey(a.Key, key));
            if (existing is null)
            {
                _attributes.Add(new ParticipantAttribute(Id, key, value));
            }
            else
            {
                existing.Update(key, value);
            }
        }
    }

    private static bool IsSameKey(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
