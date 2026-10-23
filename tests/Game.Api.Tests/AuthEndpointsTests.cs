namespace Game.Api.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Game.Api.Tests.Infrastructure;
using Game.Contracts;

public class AuthEndpointsTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Login_WithNewUsername_RegistersAndReturnsToken()
    {
        var response = await LoginAsync("alice", "secret-1");

        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrEmpty(login!.Token));
    }

    [Fact]
    public async Task Login_SameUserTwice_ReturnsTokenForSameAccount()
    {
        var first = await (await LoginAsync("alice", "secret-1")).Content.ReadFromJsonAsync<LoginResponse>();
        var second = await (await LoginAsync("alice", "secret-1")).Content.ReadFromJsonAsync<LoginResponse>();

        Assert.Equal(GetSubject(first!.Token), GetSubject(second!.Token));
    }

    [Fact]
    public async Task Login_DifferentUsers_GetDifferentAccounts()
    {
        var alice = await (await LoginAsync("alice", "secret-1")).Content.ReadFromJsonAsync<LoginResponse>();
        var bob = await (await LoginAsync("bob", "secret-2")).Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotEqual(GetSubject(alice!.Token), GetSubject(bob!.Token));
    }

    [Fact]
    public async Task Login_WithNewUsername_SetsProfileUsername()
    {
        var login = await (await LoginAsync("alice", "secret-1")).Content.ReadFromJsonAsync<LoginResponse>();

        var userId = Guid.Parse(GetSubject(login!.Token));
        var me = await Factory.CreateClientFor(userId).GetFromJsonAsync<MeDto>("/me");

        Assert.Equal("alice", me!.Username);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("has space")]
    public async Task Login_WithInvalidNewUsername_Returns400(string username)
    {
        var response = await LoginAsync(username, "secret");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNewUsernameTakenIgnoringCase_Returns409()
    {
        await LoginAsync("alice", "secret-1");

        var response = await LoginAsync("ALICE", "secret-2");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns409()
    {
        await LoginAsync("alice", "secret-1");

        var response = await LoginAsync("alice", "wrong");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("", "secret")]
    [InlineData("   ", "secret")]
    [InlineData("alice", "")]
    public async Task Login_WithMissingInput_Returns400(string username, string password)
    {
        var response = await LoginAsync(username, password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private Task<HttpResponseMessage> LoginAsync(string username, string password) =>
        Factory.CreateClient().PostAsJsonAsync("/auth/login", new LoginRequest(username, password));

    private static string GetSubject(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        var json = JsonDocument.Parse(Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/')));
        return json.RootElement.GetProperty("sub").GetString()!;
    }
}
