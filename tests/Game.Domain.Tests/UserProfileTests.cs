namespace Game.Domain.Tests;

public class UserProfileTests
{
    [Fact]
    public void New_HasNoUsername()
    {
        var user = new UserProfile(Guid.NewGuid());

        Assert.False(user.HasUsername);
        Assert.Null(user.Username);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("Player_One-2")]
    [InlineData("abcdefghijklmnopqrst")]
    public void ChangeUsername_WithValidName_SetsNameAndNormalizedName(string name)
    {
        var user = new UserProfile(Guid.NewGuid());

        user.ChangeUsername(name);

        Assert.Equal(name, user.Username);
        Assert.Equal(name.ToUpperInvariant(), user.UsernameNormalized);
    }

    [Fact]
    public void ChangeUsername_TrimsWhitespace()
    {
        var user = new UserProfile(Guid.NewGuid());

        user.ChangeUsername("  alice  ");

        Assert.Equal("alice", user.Username);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("   ab   ")]
    [InlineData("abcdefghijklmnopqrstu")]
    public void ChangeUsername_WithInvalidLength_Throws(string? name)
    {
        var user = new UserProfile(Guid.NewGuid());

        Assert.Throws<ValidationException>(() => user.ChangeUsername(name));
    }

    [Theory]
    [InlineData("al ice")]
    [InlineData("alice!")]
    [InlineData("jürgen")]
    public void ChangeUsername_WithInvalidCharacters_Throws(string name)
    {
        var user = new UserProfile(Guid.NewGuid());

        Assert.Throws<ValidationException>(() => user.ChangeUsername(name));
    }

    [Fact]
    public void ChangeUsername_CanBeChangedAgain()
    {
        var user = new UserProfile(Guid.NewGuid());
        user.ChangeUsername("first");

        user.ChangeUsername("second");

        Assert.Equal("second", user.Username);
    }
}
