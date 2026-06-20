# Realtime Tic-Tac-Toe

🎮 **Live demo:** <https://tictactoe-z95h.onrender.com> — open it in two tabs (or
send a friend the room code) and play. *(Free tier; first load after idle wakes in ~30s.)*

A two-player online tic-tac-toe game. **One ASP.NET Core project** serves the
web frontend *and* hosts the real-time game server over **SignalR** — frontend,
backend, and deployment all in a single unit.

Open it in two browsers (or send a friend the room code) and moves sync
instantly. The **server is authoritative**: it owns the board, enforces turns,
and detects wins, so clients can't cheat.

## Tech
- **Backend:** C# / ASP.NET Core 8, SignalR (WebSockets)
- **Frontend:** vanilla JS + the `@microsoft/signalr` browser client (CDN), no build step
- **Deploy:** single container (`Dockerfile`) → Azure App Service / Render / Fly.io

## Architecture
```
Browser (index.html + app.js)
        │  WebSocket  /gamehub
        ▼
GameHub ──▶ GameManager ──▶ GameRoom   (board, turn, winner, players)
  (per-room broadcast)        ^ all moves validated here
```
- `Game/GameRoom.cs` — authoritative game state + win detection
- `Game/GameManager.cs` — thread-safe registry of active rooms
- `Game/GameHub.cs` — SignalR endpoint: `JoinGame`, `MakeMove`, `Restart`
- `wwwroot/` — the entire frontend

## Run locally
Requires the **.NET 8 SDK** (`dotnet --list-sdks` should show an 8.x).

```bash
dotnet run
```
Then open <http://localhost:5000> in **two tabs**, type the same room code in
both, and play.

## Run with Docker (no SDK needed)
```bash
docker build -t tictactoe .
docker run -p 8080:8080 tictactoe
```
Open <http://localhost:8080>.

## Deploy (pick one, all have free tiers)
- **Render / Railway / Fly.io** — point them at this repo; they build the
  `Dockerfile` and give you an HTTPS URL. WebSockets work out of the box.
- **Azure App Service** — `az webapp up` or push the container; SignalR is
  first-class on Azure.

> Add a GitHub Actions workflow that builds the image and deploys on push — the
> CI/CD line is itself a résumé bullet.

## Résumé bullet
> Built and deployed a real-time multiplayer game with ASP.NET Core + SignalR
> (authoritative server, room-based matchmaking, live state sync over
> WebSockets); single containerized deploy with CI/CD.

## Ideas to extend
- Score tracking across rounds · rematch button · spectator count
- Reconnect-to-your-seat after a refresh
- A "play vs computer" mode (minimax — a nice algorithms talking point)
