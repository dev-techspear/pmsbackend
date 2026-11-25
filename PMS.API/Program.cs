using System.Text;
using System.Threading.RateLimiting;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PMS.Application;
using PMS.Domain.Constants;
using PMS.Infrastructure;
using PMS.Infrastructure.Data;
using Techspear.Identity.Adapter;

var builder = WebApplication.CreateBuilder(args);

// 1. Add layers dependency services
builder.Services.AddInfrastructureServices(builder.Configuration);
// Registered before AddApplicationServices so it replaces the no-op default there.
builder.Services.AddSingleton<PMS.API.Hubs.ChatConnectionRegistry>();
builder.Services.AddSingleton<PMS.Application.Common.Interfaces.IChatAccessSync, PMS.API.Hubs.ChatAccessSync>();
builder.Services.AddApplicationServices();

// 2. JWT Authentication.
//
// IMPORTANT: this validates the app's OWN locally-signed session token, not an
// Azure-AD-issued one. The actual identity flow is: the client completes Microsoft SSO,
// POSTs the resulting auth code to /api/v1/auth/redirect (AuthService.HandleMicrosoftCallbackAsync),
// which verifies the user via Microsoft Graph *once* and then mints this API's own HS256
// JWT (see PMS.Infrastructure/Services/TokenService.cs — HMAC-signed with SECRET_KEY, no
// `iss`/`aud` claims). Every request after that is authenticated against that local token,
// exactly like the source FastAPI app (same SECRET_KEY + HS256 pattern in .env.dev).
//
// This used to be wired to `AddMicrosoftIdentityWebApi(...)`, which validates against
// Azure AD's own RS256 JWKS/issuer/audience — a completely different token shape than what
// TokenService actually issues. That mismatch meant [Authorize] rejected every token this
// API's own login endpoint produced; nothing past /auth/redirect could ever have worked.
const string QueryTokenItem = "token_from_query";
var secretKey = builder.Configuration["SECRET_KEY"]
    ?? "0ad7e425b1d48f0cefc36120b071b4b8f078e7d0689b2b0a001b430f2626799a";
builder.Services.AddScoped<PMS.API.Security.SpearIdUserLinker>();
var hubIssuer = builder.Configuration["IdentityHub:Issuer"] ?? SpearIdTokens.DefaultIssuer;
var hubKey = builder.Configuration["IdentityHub:SigningKey"];
var hubAudience = builder.Configuration["IdentityHub:Audience"];
var hubMode = builder.Configuration["IdentityHub:Mode"] ?? "Dual";
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "PmsOrSpear";
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddPolicyScheme("PmsOrSpear", "PMS session or Spear ID", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var header = context.Request.Headers.Authorization.ToString();
            if (string.Equals(hubMode, "Hub", StringComparison.OrdinalIgnoreCase)
                && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return "SpearId";
            }

            if (!string.Equals(hubMode, "Legacy", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(hubKey)
                && SpearIdTokens.IsHubBearer(header, hubIssuer))
            {
                return "SpearId";
            }

            return JwtBearerDefaults.AuthenticationScheme;
        };
    })
    .AddJwtBearer("SpearId", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = hubIssuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(hubAudience),
            ValidAudience = hubAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(hubKey) ? secretKey : hubKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var linker = context.HttpContext.RequestServices.GetRequiredService<PMS.API.Security.SpearIdUserLinker>();
                var identity = SpearIdTokens.Read(context.Request.Headers.Authorization, hubKey ?? "", hubIssuer, hubAudience);
                if (identity is null)
                {
                    context.Fail("Spear ID token was rejected.");
                    return;
                }

                context.Principal = await linker.LinkAsync(identity, context.HttpContext.RequestAborted);
                context.Success();
            },
        };
    })
    .AddJwtBearer(options =>
    {
        // Browsers cannot set an Authorization header on a WebSocket, so SignalR sends the
        // token as ?access_token=... - accepted for the chat hub only.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments(AppConstants.ChatHubPath))
                {
                    context.Token = token;
                    context.HttpContext.Items[QueryTokenItem] = true;
                }
                return System.Threading.Tasks.Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var type = context.Principal?.FindFirst(AppConstants.ChatTicketClaimType)?.Value;
                var onHub = context.HttpContext.Request.Path.StartsWithSegments(AppConstants.ChatHubPath);
                var fromQuery = context.HttpContext.Items.ContainsKey(QueryTokenItem);

                if (type == "refresh")
                    context.Fail("A refresh token cannot be used to call the API.");
                else if (type == AppConstants.ChatTicketClaimValue && !onHub)
                    context.Fail("A chat ticket is only valid for the chat connection.");
                else if (fromQuery && type != AppConstants.ChatTicketClaimValue)
                    context.Fail("Only a short-lived chat ticket may be sent in the URL.");
                return System.Threading.Tasks.Task.CompletedTask;
            },
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

// 2b. Per-client rate limit for the anonymous login/register endpoints. Partitioned by caller IP -
// a plain AddFixedWindowLimiter would be ONE bucket shared by every caller (see api-design.md).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AppConstants.AuthRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = AppConstants.AuthRateLimitPermits,
                Window = TimeSpan.FromMinutes(AppConstants.AuthRateLimitWindowMinutes),
                QueueLimit = 0,
            }));
});

// 2c. Chat posting limit, per signed-in user (falls back to IP) - not one shared bucket.
builder.Services.AddRateLimiter(options =>
{
    // Revealing a stored secret: partitioned per user (never one global bucket).
    options.AddPolicy(AppConstants.CredentialRevealRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = AppConstants.CredentialRevealRateLimitPermits,
                Window = TimeSpan.FromMinutes(AppConstants.CredentialRevealRateLimitWindowMinutes),
                QueueLimit = 0,
            }));
    options.AddPolicy(AppConstants.ChatRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = AppConstants.ChatRateLimitPermits,
                Window = TimeSpan.FromMinutes(AppConstants.ChatRateLimitWindowMinutes),
                QueueLimit = 0,
            }));
});

// Same snake_case JSON as the REST API, so a pushed message has the same shape as a fetched one.
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    });

// 3. Add Controllers and force JSON responses to utilize snake_case
builder.Services.AddControllers(options =>
    {
        options.Filters.Add<PMS.API.Security.AccessScopeFilter>();
        options.Filters.Add<PMS.API.Security.AccessDeniedExceptionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
    });

// 4. Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        var configuredOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>()
            ?? builder.Configuration["CorsOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? Array.Empty<string>();

        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;
            try
            {
                var uri = new Uri(origin);
                return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.EndsWith("techspear.in", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.EndsWith("azurestaticapps.net", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.EndsWith("azurewebsites.net", StringComparison.OrdinalIgnoreCase)
                    || configuredOrigins.Any(o => string.Equals(o, origin, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// 5. Configure Swagger with JWT Bearer Authorize support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "TechSpear PMS API", Version = "v1" });
    // ChatController.Post / PostWithFiles share one route, disambiguated at runtime by
    // Content-Type ([Consumes("application/json")] vs "multipart/form-data") — a legitimate
    // ASP.NET Core pattern, but Swashbuckle can't represent it as two OpenAPI operations on
    // the same path/verb and throws instead of just picking one. Without this, the whole
    // swagger.json endpoint 500s, taking Swagger UI down with it (API routing itself is
    // unaffected either way).
    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\n\nExample: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    // OpenAPI 2.x: requirements reference the scheme registered above by name.
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TechSpear PMS API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

// After authentication so the chat limiter can partition by user.
app.UseRateLimiter();

app.MapControllers();
app.MapHub<PMS.API.Hubs.ChatHub>("/hubs/chat");

// Apply any pending EF Core migrations and seed reference/lookup data
// (roles, statuses, master_lookups, skills) — idempotent, so safe to run on startup.
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await DbInitializer.SeedAsync(db);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Failed to migrate or seed database during startup: {Message}", ex.Message);
}

app.Run();

// Lets integration tests (WebApplicationFactory<Program>) start the real app.
public partial class Program { }
