using Microsoft.AspNetCore.SignalR;

namespace TicTacToe.Game;

/// <summary>
/// Real-time game endpoint. Each browser opens one connection; players are
/// grouped by room code so a move broadcasts only to that match.
/// </summary>
public class GameHub : Hub
{
    private const string RoomKey = "room";
    private readonly GameManager _manager;

    public GameHub(GameManager manager) => _manager = manager;

    public async Task JoinGame(string roomCode)
    {
        roomCode = string.IsNullOrWhiteSpace(roomCode) ? "ROOM" : roomCode.Trim().ToUpperInvariant();
        var room = _manager.GetOrCreate(roomCode);

        string? mark;
        lock (room.Lock) { mark = room.AddPlayer(Context.ConnectionId); }

        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
        Context.Items[RoomKey] = roomCode;

        await Clients.Caller.SendAsync("Joined",
            new { mark, spectator = mark is null, room = roomCode });
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
        lock (room.Lock) { changed = room.TryMove(Context.ConnectionId, cell); }
        if (changed) await BroadcastState(room);
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
                if (remaining == 0) _manager.Remove(roomCode);   // free empty room
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Snapshots state under the lock, then pushes it to the whole room.</summary>
    private Task BroadcastState(GameRoom room)
    {
        object state;
        lock (room.Lock)
        {
            state = new
            {
                board = room.Board.ToArray(),
                turn = room.Turn,
                winner = room.Winner,
                full = room.IsFull
            };
        }
        return Clients.Group(room.Code).SendAsync("State", state);
    }
}
