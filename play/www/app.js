const $ = (sel, el = document) => el.querySelector(sel);
const app = $("#app");

const state = {
  cards: [],
  session: null,
  draft: { suspect: -1, type: 0, time: -1, room: -1, other: -1 },
  viewTime: 0,
  flash: null,
  hintIndex: 0,
  hintsOn: true
};

const Q = [
  { type: 1, label: "Ku ishe në orën X?", cost: 1, needs: ["time"] },
  { type: 2, label: "A ke qenë ndonjëherë në dhomën R?", cost: 1, needs: ["room"] },
  { type: 3, label: "A e ke parë personin Y në orën X?", cost: 1, needs: ["other", "time"] },
  { type: 4, label: "Kush ishte me ty në orën X?", cost: 2, needs: ["time"] },
  { type: 5, label: "Në cilën dhomë ishe para se të kaloje në R?", cost: 2, needs: ["room"] }
];

async function api(path, opts) {
  const res = await fetch(path, {
    headers: { "Content-Type": "application/json" },
    ...opts
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

function esc(s) {
  return String(s ?? "").replace(/[&<>"']/g, c => ({ "&":"&amp;","<":"&lt;",">":"&gt;","\"":"&quot;","'":"&#39;" }[c]));
}

async function boot() {
  state.cards = await api("/api/cards");
  renderHome();
}

function renderHome() {
  app.innerHTML = `
    <section class="hero">
      <div class="hero-inner">
        <p class="kicker">Lojë misteri deduktiv · CSP · AC-3</p>
        <h1 class="brand">ALIBI</h1>
        <p class="lede">Çdo dëshmi është një kufizim. Fajtori gënjen — por gënjeshtra del sapo dy deklarata nuk mund të jenë të vërteta njëkohësisht. Niveli i ri: Karta 4 · Kalimi i fshehtë.</p>
        <div class="cards">
          ${state.cards.map(c => `
            <button class="card-pick" data-card="${c.card}">
              <div class="meta">${esc(c.kicker)}</div>
              <h3>Karta ${c.card}</h3>
              <p>${esc(c.objective)}</p>
              <p style="margin-top:10px;color:var(--gold-soft);font-size:0.8rem">${c.suspects} dyshuar · ${c.rooms} dhoma · ${c.hours} orë · buxhet ${c.budget}</p>
            </button>
          `).join("")}
        </div>
      </div>
    </section>`;
  app.querySelectorAll(".card-pick").forEach(btn => {
    btn.addEventListener("click", () => startCard(+btn.dataset.card));
  });
}

async function startCard(card) {
  state.flash = null;
  state.draft = { suspect: -1, type: 0, time: -1, room: -1, other: -1 };
  state.hintIndex = 0;
  state.hintsOn = true;
  state.session = await api("/api/session", { method: "POST", body: JSON.stringify({ card }) });
  state.viewTime = state.session.crimeTime;
  renderGame();
}

function currentHint() {
  const tut = state.session?.tutorial || [];
  if (!state.hintsOn || !tut.length || state.hintIndex >= tut.length) return null;
  return tut[state.hintIndex];
}

function advanceHintIfMatched(fill) {
  const h = currentHint();
  if (!h?.fill || !fill) return;
  const f = h.fill;
  if (f.suspect === fill.suspect && f.type === fill.type &&
      (f.time < 0 || f.time === fill.time) &&
      (f.room < 0 || f.room === fill.room) &&
      (f.other < 0 || f.other === fill.other)) {
    state.hintIndex++;
  }
}

function draftReady() {
  const d = state.draft;
  if (d.suspect < 0 || !d.type) return { ok: false, msg: "Zgjidh të dyshuarin dhe pyetjen." };
  const meta = Q.find(q => q.type === d.type);
  if (meta.needs.includes("time") && d.time < 0) return { ok: false, msg: "Zgjidh orën." };
  if (meta.needs.includes("room") && d.room < 0) return { ok: false, msg: "Zgjidh dhomën." };
  if (meta.needs.includes("other") && d.other < 0) return { ok: false, msg: "Zgjidh personin Y." };
  if (meta.cost > state.session.remaining) return { ok: false, msg: "Buxhet i pamjaftueshëm." };
  return { ok: true, msg: "Gati." };
}

function composeText() {
  const s = state.session;
  const d = state.draft;
  if (d.suspect < 0) return "Zgjidh një të dyshuar…";
  const name = s.suspects[d.suspect].name;
  const meta = Q.find(q => q.type === d.type);
  if (!meta) return `Pyet ${name}…`;
  let t = meta.label
    .replace("orën X", d.time >= 0 ? s.hours[d.time] : "orën X")
    .replace("dhomën R", d.room >= 0 ? s.rooms[d.room].name : "dhomën R")
    .replace("R?", d.room >= 0 ? s.rooms[d.room].name + "?" : "R?")
    .replace("personin Y", d.other >= 0 ? s.suspects[d.other].name : "personin Y");
  return `${name}: «${t}»`;
}

async function ask() {
  const ready = draftReady();
  if (!ready.ok) { state.flash = ready.msg; renderGame(); return; }
  const d = state.draft;
  const body = { suspect: d.suspect, type: d.type, time: d.time, room: d.room, other: d.other };
  state.session = await api(`/api/session/${state.session.sessionId}/ask`, { method: "POST", body: JSON.stringify(body) });
  state.flash = state.session.conflict || state.session.flash;
  if (d.time >= 0) state.viewTime = d.time;
  advanceHintIfMatched(body);
  renderGame();
}

async function searchRoom(room) {
  state.session = await api(`/api/session/${state.session.sessionId}/search`, {
    method: "POST",
    body: JSON.stringify({ room })
  });
  state.flash = state.session.flash;
  const tut = state.session.tutorial || [];
  while (state.hintIndex < tut.length) {
    const h = tut[state.hintIndex];
    if (h.fill) break;
    state.hintIndex++;
    if (/Q6|Kërko/i.test(h.title || "")) break;
  }
  renderGame();
}

async function accuse() {
  const person = +$("#acc-person").value;
  const room = +$("#acc-room").value;
  const time = +$("#acc-time").value;
  state.session = await api(`/api/session/${state.session.sessionId}/accuse`, {
    method: "POST",
    body: JSON.stringify({ person, room, time })
  });
  state.flash = state.session.flash;
  renderGame();
}

function domainCell(s, t) {
  const row = state.session.domain.find(d => d.suspect === s && d.time === t);
  if (!row) return "—";
  if (row.count === 1) return state.session.rooms[row.possible[0]].name;
  if (row.count === 0) return "∅";
  return row.count + "?";
}

function renderHints(s) {
  const tut = s.tutorial || [];
  if (!tut.length) return "";
  const h = currentHint();
  const n = Math.min(state.hintIndex + 1, tut.length);
  return `
    <div class="hint-box">
      <div class="hint-bar">
        <strong>Udhëzuesi</strong>
        <span class="role">${state.hintsOn ? `hapi ${n} / ${tut.length}` : "fikur"}</span>
        <button class="btn ghost" id="btn-hint-toggle" type="button">${state.hintsOn ? "Fshih" : "Shfaq"}</button>
        ${state.hintsOn && h ? `<button class="btn ghost" id="btn-hint-next" type="button">Tjetri</button>` : ""}
        ${state.hintsOn && h?.fill ? `<button class="btn ghost" id="btn-hint-fill" type="button">Plotëso pyetjen</button>` : ""}
      </div>
      ${state.hintsOn && h ? `
        <div class="hint-title">${esc(h.title)}</div>
        <p class="hint-text">${esc(h.text)}</p>
      ` : state.hintsOn ? `<p class="hint-text">Udhëzuesi përfundoi — akuzo kur je gati.</p>` : ""}
    </div>`;
}

function renderGame() {
  const s = state.session;
  const over = s.phase === "Won" || s.phase === "Lost" || s.phase === "Abandoned";
  const ready = draftReady();
  const qMeta = Q.find(q => q.type === state.draft.type);

  app.innerHTML = `
    <div class="shell">
      <div class="topbar">
        <div>
          <h1>ALIBI</h1>
          <div class="sub">${esc(s.kicker)} · ${esc(s.type)} · seed case</div>
        </div>
        <div class="pill">${esc(s.rooms[s.crimeRoom].name)}${s.crimeTimeKnown ? " · " + esc(s.hours[s.crimeTime]) : " · ora ?"}</div>
        <div class="budget">Buxheti ${s.remaining} / ${s.budget}</div>
        <button class="btn ghost" id="btn-home">Menyja</button>
      </div>

      ${state.flash ? `<div class="flash ${s.conflict || (s.verdict && !s.verdict.won) ? "bad" : ""}">${esc(state.flash)}</div>` : ""}

      ${over ? renderResult(s) : `
      <div class="grid">
        <div class="panel">
          <h2>${esc(s.title)}</h2>
          <p style="color:var(--muted);margin-top:0">${esc(s.premise)}</p>
          <p>${esc(s.objective)}</p>
          ${s.secretExists ? `<p style="color:var(--gold-soft)">Kalimi i fshehtë: ${s.secretActive ? "i zbuluar (R06)" : "ende i fshehtë — Q6 «Kërko» në dhomën e duhur"}</p>` : ""}

          ${renderHints(s)}

          <div class="section">1 · I dyshuari</div>
          <div class="chips">
            ${s.suspects.map(su => `
              <button class="chip ${state.draft.suspect === su.id ? "on" : ""} ${su.candidate ? "" : "cleared"}" data-sus="${su.id}">
                ${esc(su.name)}
                <span class="role">${esc(su.role)} · ${su.candidate ? "kandidat" : "jashtë dyshimit"}</span>
              </button>
            `).join("")}
          </div>

          <div class="section">2 · Pyetja</div>
          <div class="chips">
            ${Q.map(q => `
              <button class="chip ${state.draft.type === q.type ? "on" : ""}" data-q="${q.type}" ${q.cost > s.remaining ? "disabled" : ""}>
                Q${q.type} · ${esc(q.label)}
                <span class="role">${q.cost} pyetje</span>
              </button>
            `).join("")}
          </div>

          <div class="section">3 · Parametri</div>
          <div class="chips" id="params"></div>

          <div class="compose">
            <code>${esc(composeText())}</code>
            <button class="btn primary" id="btn-ask" ${ready.ok ? "" : "disabled"}>Pyet</button>
          </div>

          <div class="section">Dhoma · Q6 Kërko (R06)</div>
          <div class="rooms">
            ${s.rooms.map(r => {
              const clue = (s.clues || []).find(c => c.room === r.id);
              const crime = r.id === s.crimeRoom;
              return `<div class="room ${crime ? "crime" : ""}">
                <h4>${esc(r.name)}${crime ? " · krimi" : ""}</h4>
                <div class="links">Lidhet: ${r.links.map(i => s.rooms[i].name).join(", ") || "—"}</div>
                ${clue ? `<button class="btn ghost" style="margin-top:8px;width:100%" data-search="${r.id}" ${clue.found || s.remaining <= 0 ? "disabled" : ""}>${clue.found ? "E kërkuar" : "Q6 Kërko · −1"}</button>` : ""}
              </div>`;
            }).join("")}
          </div>
        </div>

        <div class="panel">
          <h2>Tabela e deduksionit</h2>
          <div class="chips" style="margin-bottom:10px">
            ${s.hours.map((h, i) => `<button class="chip ${state.viewTime === i ? "on" : ""}" data-time="${i}">${esc(h)}</button>`).join("")}
          </div>
          <div style="overflow:auto">
            <table class="table">
              <thead>
                <tr><th></th>${s.hours.map(h => `<th>${esc(h)}</th>`).join("")}</tr>
              </thead>
              <tbody>
                ${s.suspects.map(su => `
                  <tr>
                    <th style="text-align:left;color:${su.color}">${esc(su.name)}</th>
                    ${s.hours.map((_, t) => {
                      const row = s.domain.find(d => d.suspect === su.id && d.time === t);
                      const cls = !row ? "" : row.count === 1 ? "cell-1" : row.count === 0 ? "cell-0" : "cell-n";
                      return `<td class="${cls}">${esc(domainCell(su.id, t))}</td>`;
                    }).join("")}
                  </tr>
                `).join("")}
              </tbody>
            </table>
          </div>

          <div class="section">Dosja e dëshmive</div>
          <div>
            ${s.statements.length === 0 ? `<p style="color:var(--muted)">Ende pa deklarata.</p>` : s.statements.map(st => `
              <div class="stmt ${st.flagged ? "flag" : ""}">
                <div class="q">#${st.id} · ${esc(s.suspects[st.suspect]?.name || "?")} · ${esc(st.question)}</div>
                <div class="a">${esc(st.text)}</div>
                <div class="f">${esc(st.formal)}</div>
              </div>
            `).join("")}
          </div>

          <div class="section">Akuzo</div>
          <div class="chips" style="align-items:center">
            <select id="acc-person">${s.suspects.map(su => `<option value="${su.id}">${esc(su.name)}</option>`).join("")}</select>
            <select id="acc-room">${s.rooms.map(r => `<option value="${r.id}" ${r.id === s.crimeRoom ? "selected" : ""}>${esc(r.name)}</option>`).join("")}</select>
            <select id="acc-time">${s.hours.map((h, i) => `<option value="${i}" ${i === s.crimeTime ? "selected" : ""}>${esc(h)}</option>`).join("")}</select>
            <button class="btn danger" id="btn-accuse">Akuzo</button>
          </div>
        </div>
      </div>`}
    </div>`;

  $("#btn-home")?.addEventListener("click", () => { state.session = null; renderHome(); });
  $("#btn-hint-toggle")?.addEventListener("click", () => { state.hintsOn = !state.hintsOn; renderGame(); });
  $("#btn-hint-next")?.addEventListener("click", () => {
    const tut = s.tutorial || [];
    if (state.hintIndex < tut.length) state.hintIndex++;
    renderGame();
  });
  $("#btn-hint-fill")?.addEventListener("click", () => {
    const h = currentHint();
    if (!h?.fill) return;
    state.draft = {
      suspect: h.fill.suspect,
      type: h.fill.type,
      time: h.fill.time,
      room: h.fill.room,
      other: h.fill.other
    };
    renderGame();
  });
  if (over) {
    $("#btn-again")?.addEventListener("click", () => startCard(s.card));
    $("#btn-next")?.addEventListener("click", () => startCard(Math.min(4, s.card + 1)));
    return;
  }

  app.querySelectorAll("[data-sus]").forEach(b => b.addEventListener("click", () => {
    state.draft.suspect = +b.dataset.sus;
    state.draft.other = -1;
    renderGame();
  }));
  app.querySelectorAll("[data-q]").forEach(b => b.addEventListener("click", () => {
    state.draft.type = +b.dataset.q;
    state.draft.time = -1;
    state.draft.room = -1;
    state.draft.other = -1;
    renderGame();
  }));
  app.querySelectorAll("[data-time]").forEach(b => b.addEventListener("click", () => {
    state.viewTime = +b.dataset.time;
    renderGame();
  }));
  app.querySelectorAll("[data-search]").forEach(b => b.addEventListener("click", () => searchRoom(+b.dataset.search)));
  $("#btn-ask")?.addEventListener("click", ask);
  $("#btn-accuse")?.addEventListener("click", accuse);

  const params = $("#params");
  if (!qMeta) {
    params.innerHTML = `<span style="color:var(--muted)">Zgjidh një pyetje.</span>`;
  } else {
    let html = "";
    if (qMeta.needs.includes("other")) {
      html += s.suspects.filter(su => su.id !== state.draft.suspect).map(su =>
        `<button class="chip ${state.draft.other === su.id ? "on" : ""}" data-other="${su.id}">${esc(su.name)}</button>`
      ).join("");
    }
    if (qMeta.needs.includes("time")) {
      html += s.hours.map((h, i) =>
        `<button class="chip ${state.draft.time === i ? "on" : ""}" data-ptime="${i}">${esc(h)}</button>`
      ).join("");
    }
    if (qMeta.needs.includes("room")) {
      html += s.rooms.map(r =>
        `<button class="chip ${state.draft.room === r.id ? "on" : ""}" data-proom="${r.id}">${esc(r.name)}</button>`
      ).join("");
    }
    params.innerHTML = html || `<span style="color:var(--muted)">Pa parametër shtesë.</span>`;
    params.querySelectorAll("[data-other]").forEach(b => b.addEventListener("click", () => { state.draft.other = +b.dataset.other; renderGame(); }));
    params.querySelectorAll("[data-ptime]").forEach(b => b.addEventListener("click", () => { state.draft.time = +b.dataset.ptime; renderGame(); }));
    params.querySelectorAll("[data-proom]").forEach(b => b.addEventListener("click", () => { state.draft.room = +b.dataset.proom; renderGame(); }));
  }
}

function renderResult(s) {
  const won = s.phase === "Won";
  return `
    <div class="panel result ${won ? "won" : "lost"}">
      <p class="big">${won ? "Rasti u zgjidh" : "LOST"}</p>
      <p>${won ? `${s.stars} yje · ${s.spent} pyetje (par ${s.par})` : esc(s.flash || "Akuza ra.")}</p>
      ${s.culprit != null ? `<p style="color:var(--muted)">Fajtori: ${esc(s.suspects[s.culprit].name)} · ${esc(s.rooms[s.crimeRoom].name)} · ${esc(s.hours[s.crimeTime])}</p>` : ""}
      <div style="margin-top:18px;display:flex;gap:10px;justify-content:center;flex-wrap:wrap">
        <button class="btn primary" id="btn-again">Provo përsëri</button>
        ${won && s.card < 4 ? `<button class="btn ghost" id="btn-next">Karta ${s.card + 1}</button>` : `<button class="btn ghost" id="btn-home">Menyja</button>`}
      </div>
    </div>`;
}

boot().catch(err => {
  app.innerHTML = `<div class="shell"><div class="flash bad">Gabim: ${esc(err.message)}</div></div>`;
});
