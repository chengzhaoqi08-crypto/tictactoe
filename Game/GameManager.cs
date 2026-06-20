using System.Collections.Concurrent;

namespace TicTacToe.Game;

/// <summary>Thread-safe registry of active rooms, keyed by room code.</summary>
public class GameManager
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();

    public GameRoom GetOrCreate(string code) => _rooms.GetOrAdd(code, c => new GameRoom(c));
    public GameRoom? Get(string code) => _rooms.TryGetValue(code, out var r) ? r : null;
    public void Remove(string code) => _rooms.TryRemove(code, out _);
}
