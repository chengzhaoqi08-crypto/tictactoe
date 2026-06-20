using Microsoft.EntityFrameworkCore;
using TicTacToe.Data;

namespace TicTacToe.Api;

public static class LeaderboardApi
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/leaderboard", async (IDbContextFactory<AppDbContext> factory) =>
        {
            await using var db = await factory.CreateDbContextAsync();
            var top = await db.Users
                .Where(u => u.Wins + u.Losses + u.Draws > 0)   // only players who've played
                .OrderByDescending(u => u.Wins)
                .ThenBy(u => u.Losses)
                .Take(20)
                .Select(u => new
                {
                    u.Username,
                    u.Wins,
                    u.Losses,
                    u.Draws,
                    Played = u.Wins + u.Losses + u.Draws
                })
                .ToListAsync();
            return Results.Ok(top);
        });
    }
}
