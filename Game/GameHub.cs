using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TicTacToe.Data;

namespace TicTacToe.Game;

/// <summary>
/// Real-time game endpoint. Requires a valid JWT; players are grouped by room
/// code so a move broadcasts only to that match. Game results are persisted.
/// </summary>
[Authorize]
public class GameHub : Hub
{
    private const string RoomKey = "room";
    private readonly GameManager _manager;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public GameHub(GameManager manager, IDbContextFactory<AppDbContext> dbFactory)
    {
        _manager = manager;
        _dbFactory = dbFactory;
    }

    private string Username => Context.User?.Identity?.Name ?? "?";

    public async Task JoinGame(string roomCode)
    {
        roomCode = string.IsNullOrWhiteSpace(roomCode) ? "ROOM" : roomCode.Trim().ToUpperInvariant();
        var room = _manager.GetOrCreate(roomCode);

        string? mark;
        lock (room.Lock) { mark = room.AddPlayer(Context.ConnectionId, Username); }

        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
        Context.Items[RoomKey] = roomCode;

        await Clients.Caller.SendAsync("Joined", new { mark, spectator = mark is null, room = roomCode });
        await BroadcastState(room);
        await Clients.Group(roomCode).SendAsync("Message",
            room.IsFull ? "Both players are in — game on!" : "Waiting for an opponent…");
    }

    public async Task MakeMove(int cell)
    {
        if (Context.Items[RoomKey] is not string roomCode) return;
        var room = _manager.Get(roomCode);
        if (room is null) return;

        bool changed;
        string? winner = null;
        (string? x, string? o) users = default;
        lock (room.Lock)
        {
            changed = room.TryMove(Context.ConnectionId, cell);
            if (changed)
            {
                winner = room.Winner;
                if (winner != null) users = room.UsersByMark();
            }
        }

        if (!changed) return;
        // Persist BEFORE broadcasting, so a client refetching the leaderboard
        // on game-over already sees the updated stats.
        if (winner != null) await RecordResultAsync(winner, users.x, users.o);
        await BroadcastState(room);
    }

    public async Task Restart()
    {
        if (Context.Items[RoomKey] is not string roomCode) return;
        var room = _manager.Get(roomCode);
        if (room is null) return;

        lock (room.Lock) { room.Reset(); }
        await BroadcastState(room);
        await Clients.Group(roomCode).SendAsync("Message", "New round — X to move.");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items[RoomKey] is string roomCode)
        {
            var room = _manager.Get(roomCode);
            if (room is not null)
            {
                int remaining;
                lock (room.Lock)
                {
                    room.RemovePlayer(Context.ConnectionId);
                    remaining = room.Players.Count;
                }
                await Clients.Group(roomCode).SendAsync("Message", "A player left the game.");
                await BroadcastState(room);
                if (remaining == 0) _manager.Remove(roomCode);
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Updates win/loss/draw counts for both players in the database.</summary>
    private async Task RecordResultAsync(string winnerMark, string? xUser, string? oUser)
    {
        if (xUser is null || oUser is null || xUser == oUser) return;   // need two distinct accounts

        await using var db = await _dbFactory.CreateDbContextAsync();
        var x = await db.Users.FirstOrDefaultAsync(u => u.Username == xUser);
        var o = await db.Users.FirstOrDefaultAsync(u => u.Username == oUser);
        if (x is null || o is null) return;

        if (winnerMark == "Draw") { x.Draws++; o.Draws++; }
        else if (winnerMark == "X") { x.Wins++; o.Losses++; }
        else { o.Wins++; x.Losses++; }

        await db.SaveChangesAsync();
    }

    private Task BroadcastState(GameRoom room)
    {
        object state;
        lock (room.Lock)
        {
            var (xUser, oUser) = room.UsersByMark();
            state = new
            {
                board = room.Board.ToArray(),
                turn = room.Turn,
                winner = room.Winner,
                full = room.IsFull,
                xUser,
                oUser
            };
        }
        return Clients.Group(room.Code).SendAsync("State", state);
    }
}
