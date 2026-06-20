namespace TicTacToe.Game;

/// <summary>
/// The authoritative state of a single tic-tac-toe match. All mutation goes
/// through the methods below; the hub serializes access with <see cref="Lock"/>.
/// </summary>
public class GameRoom
{
    public string Code { get; }
    public string?[] Board { get; } = new string?[9];   // null / "X" / "O"
    public string Turn { get; private set; } = "X";
    public string? Winner { get; private set; }          // "X" / "O" / "Draw" / null
    public Dictionary<string, string> Players { get; } = new();  // connectionId -> mark
    public object Lock { get; } = new();

    public GameRoom(string code) => Code = code;

    public bool IsFull => Players.Count >= 2;

    /// <summary>Assigns the connection an "X"/"O" mark, or null if the room is full.</summary>
    public string? AddPlayer(string connectionId)
    {
        if (Players.TryGetValue(connectionId, out var existing)) return existing;
        if (Players.Count >= 2) return null;                       // join as spectator
        var mark = Players.Values.Contains("X") ? "O" : "X";
        Players[connectionId] = mark;
        return mark;
    }

    public void RemovePlayer(string connectionId) => Players.Remove(connectionId);

    /// <summary>Applies a move only if it is legal. Returns true if the board changed.</summary>
    public bool TryMove(string connectionId, int cell)
    {
        if (Winner != null || !IsFull) return false;
        if (cell < 0 || cell >= 9 || Board[cell] != null) return false;
        if (!Players.TryGetValue(connectionId, out var mark) || mark != Turn) return false;

        Board[cell] = mark;
        Winner = CheckWinner();
        if (Winner == null) Turn = mark == "X" ? "O" : "X";
        return true;
    }

    public void Reset()
    {
        for (int i = 0; i < 9; i++) Board[i] = null;
        Turn = "X";
        Winner = null;
    }

    private static readonly int[][] Lines =
    {
        new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 },  // rows
        new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 },  // cols
        new[] { 0, 4, 8 }, new[] { 2, 4, 6 }                      // diagonals
    };

    private string? CheckWinner()
    {
        foreach (var l in Lines)
        {
            var a = Board[l[0]];
            if (a != null && a == Board[l[1]] && a == Board[l[2]]) return a;
        }
        return Board.All(c => c != null) ? "Draw" : null;
    }
}
