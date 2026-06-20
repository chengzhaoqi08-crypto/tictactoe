using TicTacToe.Game;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<GameManager>();   // one shared store of active games

// Cloud hosts (Render / Fly / Railway / Azure) inject the port to listen on via
// the PORT env var. Locally we keep launchSettings' port (5000) untouched.
if (!builder.Environment.IsDevelopment())
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

app.UseDefaultFiles();   // serve wwwroot/index.html at "/"
app.UseStaticFiles();    // serve app.js, style.css
app.MapHub<GameHub>("/gamehub");

app.Run();
