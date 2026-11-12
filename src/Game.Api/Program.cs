using Game.Api.Auth;
using Game.Api.Data;
using Game.Api.Endpoints;
using Game.Api.Errors;
using Game.Api.Hubs;
using Game.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<GameDbContext>(options =>
{
    // Data in transit: never talk to the database without TLS, whatever the connection string says.
    var connection = new NpgsqlConnectionStringBuilder(builder.Configuration.GetConnectionString("Game"));
    if (connection.SslMode < SslMode.Require)
    {
        connection.SslMode = SslMode.Require;
    }

    options.UseNpgsql(connection.ConnectionString);
});

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

// API description at /openapi/v1.json; the Bearer scheme enables the "Authorize" button in Swagger UI.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Game Master API",
            Version = "v1",
            Description = "Game masters create games, players join, and the game master manages the players' attributes."
        };
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" }
        };
        return Task.CompletedTask;
    });
    // Only routes without AllowAnonymous need the token.
    options.AddOperationTransformer((operation, context, _) =>
    {
        if (!context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            operation.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = [] }];
        }

        return Task.CompletedTask;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<GameDbContext>().Database.Migrate();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

// Data in transit: HTTPS only. HSTS is skipped in Development (and for localhost anyway).
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    // Before UseAuthorization, otherwise the fallback policy would block the UI.
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Game API"));
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

AuthEndpoints.Map(app);
MeEndpoints.Map(app);
GameEndpoints.Map(app);
app.MapHub<GameHub>(GameHubContract.Path);

app.Run();
