namespace Game.Domain.Tests;

public class ParticipantAttributeTests
{
    private readonly UserProfile _player;
    private readonly Game _game;

    public ParticipantAttributeTests()
    {
        _player = new UserProfile(Guid.NewGuid());
        _player.ChangeUsername("player");
        _game = Game.Create(Guid.NewGuid(), maxPlayers: 4);
        _game.Join(_player);
    }

    private IReadOnlyList<ParticipantAttribute> Attributes => _game.Participants.Single().Attributes;

    private void Replace(params (string Key, string? Value)[] attributes) =>
        _game.ReplaceAttributes(_player.Id, attributes);

    [Fact]
    public void Replace_RemovesAttributesThatAreNotInTheNewList()
    {
        Replace(("hp", "10"), ("mana", "5"));

        Replace(("hp", "8"));

        var attribute = Assert.Single(Attributes);
        Assert.Equal("hp", attribute.Key);
        Assert.Equal("8", attribute.Value);
    }

    [Fact]
    public void Replace_WithSameKeyInOtherCase_UpdatesExistingAttribute()
    {
        Replace(("hp", "10"));
        var original = Attributes.Single();

        Replace(("HP", "12"));

        var attribute = Assert.Single(Attributes);
        Assert.Same(original, attribute);
        Assert.Equal("HP", attribute.Key);
        Assert.Equal("12", attribute.Value);
    }

    [Fact]
    public void Replace_WithEmptyList_RemovesAll()
    {
        Replace(("hp", "10"));

        Replace();

        Assert.Empty(Attributes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Replace_WithBlankValue_StoresNull(string? value)
    {
        Replace(("note", value));

        Assert.Null(Attributes.Single().Value);
    }

    [Fact]
    public void Replace_TrimsKeyAndValue()
    {
        Replace(("  hp ", " 10 "));

        Assert.Equal("hp", Attributes.Single().Key);
        Assert.Equal("10", Attributes.Single().Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Replace_WithBlankKey_Throws(string key)
    {
        Assert.Throws<ValidationException>(() => Replace((key, "x")));
    }

    [Fact]
    public void Replace_WithKeyOf50Characters_Succeeds()
    {
        Replace((new string('k', 50), "x"));

        Assert.Single(Attributes);
    }

    [Fact]
    public void Replace_WithKeyLongerThan50Characters_Throws()
    {
        Assert.Throws<ValidationException>(() => Replace((new string('k', 51), "x")));
    }

    [Fact]
    public void Replace_WithValueLongerThan200Characters_Throws()
    {
        Assert.Throws<ValidationException>(() => Replace(("note", new string('v', 201))));
    }

    [Fact]
    public void Replace_WithDuplicateKeysIgnoringCase_Throws()
    {
        Assert.Throws<ValidationException>(() => Replace(("hp", "1"), ("HP", "2")));
    }

    [Fact]
    public void Replace_With20Attributes_Succeeds()
    {
        Replace(Enumerable.Range(1, 20).Select(i => ($"key{i}", (string?)"x")).ToArray());

        Assert.Equal(20, Attributes.Count);
    }

    [Fact]
    public void Replace_WithMoreThan20Attributes_Throws()
    {
        var attributes = Enumerable.Range(1, 21).Select(i => ($"key{i}", (string?)"x")).ToArray();

        Assert.Throws<ValidationException>(() => Replace(attributes));
    }

    [Fact]
    public void Replace_WhenInvalid_KeepsPreviousAttributes()
    {
        Replace(("hp", "10"));

        Assert.Throws<ValidationException>(() => Replace(("", "x")));

        Assert.Equal("10", Attributes.Single().Value);
    }
}
