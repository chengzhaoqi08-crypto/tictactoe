using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TicTacToe.Api;
using TicTacToe.Auth;
using TicTacToe.Data;
using TicTacToe.Game;

var builder = WebApplication.CreateBuilder(args);

// ---- Database: SQLite for local dev; Postgres when DATABASE_URL is provided ----
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
builder.Services.AddDbContextFactory<AppDbContext>(opt =>
{
    if (!string.IsNullOrWhiteSpace(databaseUrl))
        opt.UseNpgsql(Db.ToNpgsql(databaseUrl));
    else
        opt.UseSqlite($"Data Source={Path.Combine(builder.Environment.ContentRootPath, "app.db")}");
});

// ---- Auth: JWT bearer ----
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
             ?? builder.Configuration["Jwt:Key"]
             ?? "dev-only-insecure-key-change-me-please-32+chars";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey.PadRight(32));
builder.Services.AddSingleton(new TokenService(keyBytes));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            NameClaimType = "username",
        };
        // The SignalR JS client can only send the token via the query string on
        // the WebSocket; lift it into the auth pipeline for the hub path.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/gamehub"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddSignalR();
builder.Services.AddSingleton<GameManager>();

// Cloud hosts inject the port via PORT; locally we keep launchSettings' 5000.
if (!builder.Environment.IsDevelopment())
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

// Create the schema on first run (simple model -> no migrations needed).
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = factory.CreateDbContext();
    db.Database.EnsureCreated();
}

app.UseDefaultFiles();   // serve wwwroot/index.html at "/"
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<GameHub>("/gamehub");
AuthApi.Map(app);
LeaderboardApi.Map(app);

app.Run();
