namespace TicTacToe.Data;

public static class Db
{
    /// <summary>
    /// Accepts a ready Npgsql connection string, or a `postgres://user:pass@host:port/db`
    /// URL (the form Neon / Render / Railway hand out) and converts it.
    /// </summary>
    public static string ToNpgsql(string url)
    {
        if (!url.StartsWith("postgres://") && !url.StartsWith("postgresql://"))
            return url;   // already a keyword connection string

        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);
        var database = uri.AbsolutePath.TrimStart('/');
        var port = uri.Port > 0 ? uri.Port : 5432;
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";

        return $"Host={uri.Host};Port={port};Username={Uri.UnescapeDataString(userInfo[0])};"
             + $"Password={password};Database={database};SSL Mode=Require;Trust Server Certificate=true";
    }
}
