using Game.Api.Auth;
using Game.Api.Data;
using Game.Api.Endpoints;
using Game.Api.Errors;
using Game.Api.Hubs;
using Game.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Game")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidIssuer = TokenService.Issuer;
        options.TokenValidationParameters.ValidAudience = TokenService.Issuer;
        options.TokenValidationParameters.IssuerSigningKey = TokenService.GetSigningKey(builder.Configuration);
        options.TokenValidationParameters.NameClaimType = ClaimsPrincipalExtensions.UserIdClaim;
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // WebSockets cannot send an Authorization header; SignalR sends the token as a query parameter.
                var token = context.Request.Query["access_token"].ToString();
                if (token.Length > 0 && context.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemExceptionHandler>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IGameNotifier, GameNotifier>();
builder.Services.AddHealthChecks().AddDbContextCheck<GameDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<GameDbContext>().Database.Migrate();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
AuthEndpoints.Map(app);
MeEndpoints.Map(app);
GameEndpoints.Map(app);
app.MapHub<GameHub>(GameHubContract.Path);

app.Run();
