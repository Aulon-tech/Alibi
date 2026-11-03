using System.Collections.Concurrent;
using System.Text.Json;
using Alibi.Core;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var sessions = new ConcurrentDictionary<string, GameSession>();
var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

var webRoot = Path.Combine(app.Environment.ContentRootPath, "www");
var files = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRoot);
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
app.UseStaticFiles(new StaticFileOptions { FileProvider = files });
app.MapGet("/", () => Results.File(Path.Combine(webRoot, "index.html"), "text/html"));

app.MapGet("/api/cards", () =>
{
    var list = new List<object>();
    for (int k = 1; k <= 4; k++)
    {
        var c = LevelCards.Card(k);
        list.Add(new
        {
            card = k,
            title = c.Title,
            kicker = c.Kicker,
            objective = c.Objective,
            premise = c.Premise,
            level = c.Level,
            type = c.Type.ToString(),
            suspects = c.N,
            rooms = c.RoomCount,
            hours = c.T,
            budget = c.Budget,
            par = c.Par,
            playable = true
        });
    }
    return Results.Json(list, jsonOpts);
});

app.MapPost("/api/session", (StartReq req) =>
{
    int card = req.Card < 1 || req.Card > 4 ? 1 : req.Card;
    var c = LevelCards.Card(card);
    var g = new GameSession(c);
    g.Begin();
    var id = Guid.NewGuid().ToString("N");
    sessions[id] = g;
    return Results.Json(Snapshot(id, g, null), jsonOpts);
});

app.MapGet("/api/session/{id}", (string id) =>
{
    if (!sessions.TryGetValue(id, out var g)) return Results.NotFound();
    return Results.Json(Snapshot(id, g, null), jsonOpts);
});

app.MapPost("/api/session/{id}/ask", (string id, AskReq req) =>
{
    if (!sessions.TryGetValue(id, out var g)) return Results.NotFound();
    var q = Question.Make(req.Suspect, (QType)req.Type, req.Time, req.Room, req.Other);
    var o = g.Ask(q);
    return Results.Json(Snapshot(id, g, o.Accepted ? null : o.Message, o), jsonOpts);
});

app.MapPost("/api/session/{id}/search", (string id, SearchReq req) =>
{
    if (!sessions.TryGetValue(id, out var g)) return Results.NotFound();
    var o = g.SearchRoom(req.Room);
    return Results.Json(Snapshot(id, g, o.Message, o), jsonOpts);
});

app.MapPost("/api/session/{id}/accuse", (string id, AccuseReq req) =>
{
    if (!sessions.TryGetValue(id, out var g)) return Results.NotFound();
    var o = g.Accuse(req.Person, req.Room, req.Time);
    return Results.Json(Snapshot(id, g, o.Accepted ? null : o.Message, null, o), jsonOpts);
});

app.Run("http://127.0.0.1:43123");

static object Snapshot(string id, GameSession g, string flash = null, AskOutcome ask = null, AccuseOutcome accuse = null)
{
    var c = g.Case;
    var suspects = new List<object>();
    for (int i = 0; i < c.N; i++)
    {
        suspects.Add(new
        {
            id = i,
            code = c.Suspects[i].Id,
            name = c.Suspects[i].Name,
            role = c.Suspects[i].Role,
            color = c.Suspects[i].ColorHex,
            candidate = g.Eval.Candidate[i]
        });
    }
    var rooms = new List<object>();
    for (int i = 0; i < c.RoomCount; i++)
    {
        var links = new List<int>();
        for (int j = 0; j < c.RoomCount; j++) if (i != j && c.CanMove(i, j)) links.Add(j);
        rooms.Add(new { id = i, code = c.Rooms[i].Id, name = c.Rooms[i].Name, links });
    }
    var domain = new List<object>();
    for (int s = 0; s < c.N; s++)
        for (int t = 0; t < c.T; t++)
        {
            var possible = new List<int>();
            for (int r = 0; r < c.RoomCount; r++) if (g.Eval.Possible[s, t, r]) possible.Add(r);
            domain.Add(new { suspect = s, time = t, possible, count = g.Eval.Count[s, t] });
        }
    var statements = new List<object>();
    foreach (var st in g.Statements)
    {
        statements.Add(new
        {
            id = st.Id,
            suspect = st.Suspect,
            question = st.QuestionText,
            text = st.Text,
            formal = st.Formal,
            cost = st.Cost,
            flagged = g.Flagged.Contains(st.Id),
            mistake = st.IsMistake
        });
    }
    var clues = new List<object>();
    foreach (var pc in c.PhysicalClues)
        clues.Add(new { room = pc.Room, title = pc.Title, text = pc.Text, found = pc.Found, revealsSecret = pc.RevealsSecret });

    object verdict = null;
    if (accuse != null && accuse.Verdict != null)
    {
        verdict = new
        {
            won = accuse.Verdict.Won,
            proven = accuse.Verdict.Proven,
            wrong = accuse.Verdict.WrongFields,
            stars = g.Stars
        };
    }

    return new
    {
        sessionId = id,
        flash,
        phase = g.Phase.ToString(),
        remaining = g.Remaining,
        spent = g.Spent,
        budget = c.Budget,
        par = c.Par,
        stars = g.Stars,
        forced = g.ForcedAccusation,
        secretActive = c.SecretEdgeActive,
        secretExists = c.SecretEdgeExists,
        card = c.CardNumber,
        title = c.Title,
        kicker = c.Kicker,
        premise = c.Premise,
        objective = c.Objective,
        type = c.Type.ToString(),
        crimeRoom = c.CrimeRoom,
        crimeTime = c.CrimeTime,
        crimeTimeKnown = c.CrimeTimeKnown,
        hours = c.Hours,
        rules = c.Rules,
        tutorial = c.Tutorial.ConvertAll(t => new
        {
            title = t.Title,
            text = t.Text,
            accuse = t.Accuse,
            fill = t.Fill == null ? null : new
            {
                suspect = t.Fill.Suspect,
                type = t.Fill.HasType ? (int)t.Fill.Type : 0,
                time = t.Fill.Time,
                room = t.Fill.Room,
                other = t.Fill.Other
            }
        }),
        suspects,
        rooms,
        domain,
        statements,
        clues,
        conflict = ask != null && ask.Conflict != null ? g.ConflictText(ask.Conflict) : null,
        askAccepted = ask == null ? (bool?)null : ask.Accepted,
        verdict,
        truth = g.IsOver ? c.Truth : null,
        culprit = g.IsOver ? c.Culprit : (int?)null
    };
}

record StartReq(int Card);
record AskReq(int Suspect, int Type, int Time = -1, int Room = -1, int Other = -1);
record SearchReq(int Room);
record AccuseReq(int Person, int Room, int Time);
