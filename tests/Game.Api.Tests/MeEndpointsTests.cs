namespace Game.Api.Tests;

using System.Net;
using System.Net.Http.Json;
using Game.Api.Tests.Infrastructure;
using Game.Contracts;
using Microsoft.AspNetCore.Mvc;

public class MeEndpointsTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task GetMe_WithoutLogin_Returns401()
    {
        var response = await Factory.CreateClient().GetAsync("/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_OnFirstCall_CreatesProfileWithoutUsername()
    {
        var userId = Guid.NewGuid();

        var me = await Factory.CreateClientFor(userId).GetFromJsonAsync<MeDto>("/me");

        Assert.Equal(new MeDto(userId, null), me);
    }

    [Fact]
    public async Task GetMe_ParallelFirstCalls_AllSucceed()
    {
        var client = Factory.CreateClientFor(Guid.NewGuid());

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => client.GetAsync("/me")));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
    }

    [Fact]
    public async Task ChangeUsername_WithValidName_IsReturnedByGetMe()
    {
        var user = await CreateUserAsync("alice");

        var me = await user.Client.GetFromJsonAsync<MeDto>("/me");

        Assert.Equal("alice", me!.Username);
    }

    [Fact]
    public async Task ChangeUsername_WithInvalidName_Returns400WithProblemDetails()
    {
        var client = Factory.CreateClientFor(Guid.NewGuid());

        var response = await client.PutAsJsonAsync("/me/username", new ChangeUsernameRequest("a!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(400, problem!.Status);
        Assert.False(string.IsNullOrEmpty(problem.Detail));
    }

    [Fact]
    public async Task ChangeUsername_WhenTakenIgnoringCase_Returns409()
    {
        await CreateUserAsync("alice");
        var client = Factory.CreateClientFor(Guid.NewGuid());

        var response = await client.PutAsJsonAsync("/me/username", new ChangeUsernameRequest("ALICE"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ChangeUsername_ToOwnNameInOtherCase_Succeeds()
    {
        var user = await CreateUserAsync("alice");

        var response = await user.Client.PutAsJsonAsync("/me/username", new ChangeUsernameRequest("Alice"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_WithoutLogin_ReturnsHealthy()
    {
        var response = await Factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
