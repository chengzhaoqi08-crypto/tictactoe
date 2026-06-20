const $ = (id) => document.getElementById(id);

let connection = null;
let myMark = null;
let state = { board: Array(9).fill(null), turn: "X", winner: null, full: false };

// Build the 3x3 grid of clickable cells once.
function buildBoard() {
  const board = $("board");
  board.innerHTML = "";
  for (let i = 0; i < 9; i++) {
    const cell = document.createElement("button");
    cell.className = "cell";
    cell.addEventListener("click", () => onCellClick(i));
    board.appendChild(cell);
  }
}

// Redraw board + status text from the latest server state.
function render() {
  document.querySelectorAll(".cell").forEach((c, i) => {
    const v = state.board[i];
    c.textContent = v || "";
    c.classList.toggle("x", v === "X");
    c.classList.toggle("o", v === "O");
  });

  let msg;
  if (!state.full) msg = "Waiting for opponent…";
  else if (state.winner === "Draw") msg = "It's a draw 🤝";
  else if (state.winner) msg = state.winner === myMark ? "You win! 🎉" : "You lose 😞";
  else msg = state.turn === myMark ? "Your turn" : "Opponent's turn";

  $("status").textContent = msg;
  $("board").classList.toggle(
    "myturn", state.full && !state.winner && state.turn === myMark
  );
}

function onCellClick(i) {
  if (!state.full || state.winner || state.board[i]) return;
  if (state.turn !== myMark) return;                 // not your turn
  connection.invoke("MakeMove", i).catch(console.error);
}

async function join() {
  const code = ($("roomInput").value.trim().toUpperCase()) || "ROOM";

  connection = new signalR.HubConnectionBuilder()
    .withUrl("/gamehub")
    .withAutomaticReconnect()
    .build();

  connection.on("Joined", (info) => {
    myMark = info.mark;
    $("mark").textContent = info.spectator ? "spectator" : info.mark;
    $("roomLabel").textContent = info.room;
  });
  connection.on("State", (s) => { state = s; render(); });
  connection.on("Message", () => {});               // hook for toasts if you want

  try {
    await connection.start();
    await connection.invoke("JoinGame", code);
    $("lobby").classList.add("hidden");
    $("game").classList.remove("hidden");
  } catch (err) {
    console.error(err);
    $("status").textContent = "Connection failed — is the server running?";
  }
}

$("joinBtn").addEventListener("click", join);
$("roomInput").addEventListener("keydown", (e) => { if (e.key === "Enter") join(); });
$("restartBtn").addEventListener("click", () =>
  connection?.invoke("Restart").catch(console.error)
);

buildBoard();
