using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TicTacToe.Auth;
using TicTacToe.Data;

namespace TicTacToe.Api;

public static class AuthApi
{
    public record Credentials(string Username, string Password);

    public static void Map(WebApplication app)
    {
        app.MapPost("/api/auth/register", async (Credentials c, IDbContextFactory<AppDbContext> factory, TokenService tokens) =>
        {
            var username = (c.Username ?? "").Trim();
            if (!Regex.IsMatch(username, "^[A-Za-z0-9_]{3,20}$"))
                return Results.BadRequest(new { error = "Username must be 3–20 chars (letters, digits, underscore)." });
            if ((c.Password ?? "").Length < 4)
                return Results.BadRequest(new { error = "Password must be at least 4 characters." });

            await using var db = await factory.CreateDbContextAsync();
            if (await db.Users.AnyAsync(u => u.Username == username))
                return Results.BadRequest(new { error = "Username already taken." });

            var user = new User { Username = username, PasswordHash = PasswordHashing.Hash(c.Password!) };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return Results.Ok(new { token = tokens.Create(user), username = user.Username });
        });

        app.MapPost("/api/auth/login", async (Credentials c, IDbContextFactory<AppDbContext> factory, TokenService tokens) =>
        {
            var username = (c.Username ?? "").Trim();
            await using var db = await factory.CreateDbContextAsync();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user is null || !PasswordHashing.Verify(c.Password ?? "", user.PasswordHash))
                return Results.Json(new { error = "Invalid username or password." }, statusCode: 401);
            return Results.Ok(new { token = tokens.Create(user), username = user.Username });
        });
    }
}
