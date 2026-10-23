namespace Game.App.ViewModels;

using System.Collections.ObjectModel;
using Game.Contracts;

/// <summary>A player with their attribute table. Only the game master can edit it.</summary>
public class ParticipantViewModel(ParticipantDto participant, bool canEdit)
{
    public Guid UserId { get; } = participant.UserId;

    public string Username { get; } = participant.Username;

    public bool CanEdit { get; } = canEdit;

    public ObservableCollection<AttributeRowViewModel> Attributes { get; } =
        new(participant.Attributes.Select(a => new AttributeRowViewModel(a.Key, a.Value, !canEdit)));

    public void AddEmptyRow() => Attributes.Add(new AttributeRowViewModel("", null, !CanEdit));

    /// <summary>Rows without a key count as deleted and are not sent.</summary>
    public List<AttributeDto> ToAttributes() =>
        Attributes
            .Where(a => !string.IsNullOrWhiteSpace(a.Key))
            .Select(a => new AttributeDto(a.Key, a.Value))
            .ToList();
}

public class AttributeRowViewModel(string key, string? value, bool isReadOnly)
{
    public string Key { get; set; } = key;

    public string? Value { get; set; } = value;

    public bool IsReadOnly { get; } = isReadOnly;
}
