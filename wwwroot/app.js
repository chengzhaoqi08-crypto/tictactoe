const $ = (id) => document.getElementById(id);

let token = localStorage.getItem("token");
let username = localStorage.getItem("username");
let mode = "login";                 // "login" | "register"
let connection = null;
let myMark = null;
let prevWinner = null;
let state = { board: Array(9).fill(null), turn: "X", winner: null, full: false, xUser: null, oUser: null };

// ---------------- auth ----------------
function setMode(m) {
  mode = m;
  $("tabLogin").classList.toggle("active", m === "login");
  $("tabRegister").classList.toggle("active", m === "register");
  $("authSubmit").textContent = m === "login" ? "Log in" : "Create account";
  $("authErr").textContent = "";
}
$("tabLogin").onclick = () => setMode("login");
$("tabRegister").onclick = () => setMode("register");
$("authSubmit").onclick = submitAuth;
$("authPass").addEventListener("keydown", (e) => { if (e.key === "Enter") submitAuth(); });

async function submitAuth() {
  const u = $("authUser").value.trim();
  const p = $("authPass").value;
  $("authErr").textContent = "";
  try {
    const res = await fetch(`/api/auth/${mode}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username: u, password: p }),
    });
    const data = await res.json();
    if (!res.ok) { $("authErr").textContent = data.error || "Failed."; return; }
    token = data.token;
    username = data.username;
    localStorage.setItem("token", token);
    localStorage.setItem("username", username);
    enterApp();
  } catch {
    $("authErr").textContent = "Network error.";
  }
}

function logout() {
  localStorage.removeItem("token");
  localStorage.removeItem("username");
  token = username = null;
  if (connection) { connection.stop(); connection = null; }
  $("appView").classList.add("hidden");
  $("authView").classList.remove("hidden");
}
$("logout").onclick = logout;

function enterApp() {
  $("meName").textContent = username;
  $("authView").classList.add("hidden");
  $("appView").classList.remove("hidden");
  $("game").classList.add("hidden");
  $("lobby").classList.remove("hidden");
  loadLeaderboard();
}

// ---------------- leaderboard ----------------
async function loadLeaderboard() {
  try {
    const rows = await (await fetch("/api/leaderboard")).json();
    const tb = $("lbBody");
    tb.innerHTML = "";
    if (!rows.length) {
      tb.innerHTML = `<tr><td colspan="5" class="empty">No games yet — be the first!</td></tr>`;
      return;
    }
    rows.forEach((r, i) => {
      const tr = document.createElement("tr");
      if (r.username === username) tr.className = "me";
      tr.innerHTML =
        `<td>${i + 1}</td><td>${escapeHtml(r.username)}</td>` +
        `<td>${r.wins}</td><td>${r.losses}</td><td>${r.draws}</td>`;
      tb.appendChild(tr);
    });
  } catch { /* leaderboard is best-effort */ }
}
$("lbRefresh").onclick = loadLeaderboard;

function escapeHtml(s) {
  return s.replace(/[&<>"']/g, (c) =>
    ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
}

// ---------------- game ----------------
function buildBoard() {
  const board = $("board");
  board.innerHTML = "";
  for (let i = 0; i < 9; i++) {
    const cell = document.createElement("button");
    cell.className = "cell";
    cell.onclick = () => onCell(i);
    board.appendChild(cell);
  }
}

function render() {
  document.querySelectorAll(".cell").forEach((c, i) => {
    const v = state.board[i];
    c.textContent = v || "";
    c.classList.toggle("x", v === "X");
    c.classList.toggle("o", v === "O");
  });

  $("opp").textContent = (myMark === "X" ? state.oUser : state.xUser) || "—";

  let msg;
  if (!state.full) msg = "Waiting for opponent…";
  else if (state.winner === "Draw") msg = "It's a draw 🤝";
  else if (state.winner) msg = state.winner === myMark ? "You win! 🎉" : "You lose 😞";
  else msg = state.turn === myMark ? "Your turn" : "Opponent's turn";
  $("status").textContent = msg;

  $("board").classList.toggle("myturn", state.full && !state.winner && state.turn === myMark);
}

function onCell(i) {
  if (!state.full || state.winner || state.board[i]) return;
  if (state.turn !== myMark) return;
  connection.invoke("MakeMove", i).catch(console.error);
}

async function join() {
  const code = $("roomInput").value.trim().toUpperCase() || "ROOM";
  prevWinner = null;

  connection = new signalR.HubConnectionBuilder()
    .withUrl("/gamehub", { accessTokenFactory: () => token })
    .withAutomaticReconnect()
    .build();

  connection.on("Joined", (info) => {
    myMark = info.mark;
    $("mark").textContent = info.spectator ? "spectator" : info.mark;
    $("roomLabel").textContent = info.room;
  });
  connection.on("State", (s) => {
    state = s;
    render();
    if (s.winner && s.winner !== prevWinner) loadLeaderboard();  // a game just ended
    prevWinner = s.winner;
  });
  connection.on("Message", () => {});

  try {
    await connection.start();
    await connection.invoke("JoinGame", code);
    $("lobby").classList.add("hidden");
    $("game").classList.remove("hidden");
  } catch (err) {
    console.error(err);
    $("status").textContent = "Connection failed — try logging in again.";
  }
}
$("joinBtn").onclick = join;
$("roomInput").addEventListener("keydown", (e) => { if (e.key === "Enter") join(); });
$("restartBtn").onclick = () => connection?.invoke("Restart").catch(console.error);
$("leaveBtn").onclick = () => {
  if (connection) { connection.stop(); connection = null; }
  $("game").classList.add("hidden");
  $("lobby").classList.remove("hidden");
  loadLeaderboard();
};

// ---------------- boot ----------------
buildBoard();
setMode("login");
if (token && username) enterApp();
