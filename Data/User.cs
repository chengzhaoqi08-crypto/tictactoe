namespace TicTacToe.Data;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";   // PBKDF2: "salt.hash" (base64)
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
