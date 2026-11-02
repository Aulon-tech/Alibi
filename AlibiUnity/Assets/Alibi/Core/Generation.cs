using System;
using System.Collections.Generic;

namespace Alibi.Core
{
    public static class ParPolicy
    {
        public const int Unsolvable = 999;

        public static List<Question> Candidates(CaseDefinition c)
        {
            var list = new List<Question>();
            for (int s = 0; s < c.N; s++)
            {
                for (int t = 0; t < c.T; t++)
                {
                    list.Add(Question.Make(s, QType.Q1, t));
                    list.Add(Question.Make(s, QType.Q4, t));
                    for (int y = 0; y < c.N; y++) if (y != s) list.Add(Question.Make(s, QType.Q3, t, -1, y));
                }
                for (int r = 0; r < c.RoomCount; r++)
                {
                    list.Add(Question.Make(s, QType.Q2, -1, r));
                    list.Add(Question.Make(s, QType.Q5, -1, r));
                }
            }
            return list;
        }

        public static Statement MakeStatement(CaseDefinition c, Question q, int id)
        {
            var a = QuestionEngine.Answer(c, q);
            return new Statement
            {
                Id = id,
                Suspect = q.Suspect,
                Question = q,
                QuestionText = QuestionEngine.Phrase(c, q),
                Text = a.Text,
                Formal = a.Formal,
                Constraints = a.Constraints,
                Cost = QuestionEngine.Cost(q.Type),
                IsMistake = a.IsMistake
            };
        }

        public static int Compute(CaseDefinition c, CspSolver solver, int limit, List<Question> route)
        {
            var stmts = new List<Statement>();
            var asked = new HashSet<string>();
            var candidates = Candidates(c);
            int cost = 0;
            var ev = solver.Evaluate(stmts);
            while (ev.ViableCount > 1 && cost < limit)
            {
                int baseMass = ev.Mass;
                double bestGain = 0;
                Question best = null;
                Statement bestSt = null;
                EvalResult bestEv = null;
                foreach (var q in candidates)
                {
                    if (asked.Contains(q.Key)) continue;
                    var st = MakeStatement(c, q, stmts.Count + 1);
                    stmts.Add(st);
                    var e2 = solver.Evaluate(stmts, ev);
                    stmts.RemoveAt(stmts.Count - 1);
                    double gain = (double)(baseMass - e2.Mass) / st.Cost;
                    if (gain > bestGain + 1e-9)
                    {
                        bestGain = gain;
                        best = q;
                        bestSt = st;
                        bestEv = e2;
                    }
                }
                if (best == null) return Unsolvable;
                asked.Add(best.Key);
                stmts.Add(bestSt);
                cost += bestSt.Cost;
                ev = bestEv;
                if (route != null) route.Add(best);
            }
            return ev.ViableCount == 1 ? cost : Unsolvable;
        }
    }

    public static class LieSelector
    {
        public static int[] Choose(CaseDefinition c, Random rng)
        {
            var truth = c.Truth[c.Culprit];
            int t = c.T;
            int[] best = null;
            double bestScore = double.MaxValue;
            for (int p = 0; p < c.PathCount; p++)
            {
                int atCrime = c.PathRooms[p * t + c.CrimeTime];
                if (atCrime == c.CrimeRoom) continue;
                if (!c.CrimeTimeKnown && (c.PathEver[p] & (1 << c.CrimeRoom)) != 0) continue;
                int dist = 0;
                for (int i = 0; i < t; i++) if (c.PathRooms[p * t + i] != truth[i]) dist++;
                bool exposable = false;
                for (int s = 0; s < c.N; s++)
                {
                    if (s != c.Culprit && c.Truth[s][c.CrimeTime] == atCrime) exposable = true;
                }
                double score = dist + (exposable ? 0 : 0.75) + rng.NextDouble() * 0.5;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = new int[t];
                    for (int i = 0; i < t; i++) best[i] = c.PathRooms[p * t + i];
                }
            }
            return best;
        }
    }

    public sealed class GenerationResult
    {
        public CaseDefinition Case;
        public List<string> Rejected = new List<string>();
        public int Attempts;
    }

    public static class CaseGenerator
    {
        public static List<Statement> FullInformation(CaseDefinition c)
        {
            var list = new List<Statement>();
            for (int s = 0; s < c.N; s++)
            {
                for (int t = 0; t < c.T; t++)
                {
                    list.Add(ParPolicy.MakeStatement(c, Question.Make(s, QType.Q1, t), list.Count + 1));
                    list.Add(ParPolicy.MakeStatement(c, Question.Make(s, QType.Q4, t), list.Count + 1));
                }
                for (int r = 0; r < c.RoomCount; r++)
                {
                    list.Add(ParPolicy.MakeStatement(c, Question.Make(s, QType.Q2, -1, r), list.Count + 1));
                }
            }
            return list;
        }

        public static bool IsUnique(CaseDefinition c, CspSolver solver, out int solutions)
        {
            var ev = solver.Evaluate(FullInformation(c));
            solutions = ev.ViableCount;
            return ev.ViableCount == 1 && ev.ViableK[0] == c.Culprit && ev.ViableTau[0] == c.CrimeTime;
        }

        public static GenerationResult Generate(int level, CaseType type, int seed, int maxAttempts = 80)
        {
            var spec = LevelCards.SpecFor(level, type);
            var result = new GenerationResult();
            for (int a = 0; a < maxAttempts; a++)
            {
                int s = seed + a;
                string reason;
                var c = TryBuild(spec, type, s, out reason);
                result.Attempts = a + 1;
                if (c != null)
                {
                    c.GeneratorNotes.AddRange(result.Rejected);
                    result.Case = c;
                    return result;
                }
                result.Rejected.Add("seed " + s + ": " + reason);
            }
            return result;
        }

        static int PickPath(CaseDefinition c, Random rng, Func<int, bool> allowed, List<int[]> others)
        {
            int t = c.T;
            int best = -1;
            int bestScore = int.MinValue;
            for (int tries = 0; tries < 600; tries++)
            {
                int p = rng.Next(c.PathCount);
                if (!allowed(p)) continue;
                int moves = 0;
                for (int i = 1; i < t; i++) if (c.PathRooms[p * t + i] != c.PathRooms[p * t + i - 1]) moves++;
                int overlap = 0;
                foreach (var o in others)
                {
                    for (int i = 0; i < t; i++) if (c.PathRooms[p * t + i] == o[i]) overlap++;
                }
                int score = (moves >= 1 && moves <= t - 1 ? 6 : 0) - overlap * 3 + rng.Next(5);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = p;
                }
                if (tries > 60 && best >= 0) break;
            }
            return best;
        }

        static void Shuffle(int[] a, Random rng)
        {
            for (int i = a.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = a[i];
                a[i] = a[j];
                a[j] = tmp;
            }
        }

        static int[] PathOf(CaseDefinition c, int p)
        {
            var path = new int[c.T];
            for (int i = 0; i < c.T; i++) path[i] = c.PathRooms[p * c.T + i];
            return path;
        }

        static CaseDefinition TryBuild(LevelSpec spec, CaseType type, int seed, out string reason)
        {
            var rng = new Random(seed);
            var c = new CaseDefinition
            {
                Id = "gen-" + spec.Level + "-" + type + "-" + seed,
                Level = spec.Level,
                Seed = seed,
                Type = type,
                Hours = spec.Hours,
                Budget = spec.Budget,
                IsGenerated = true,
                Kicker = "RAST I GJENERUAR · NIVELI " + spec.Level
            };
            Villa.ApplyRooms(c, spec.RoomIds, spec.UseGraph);
            Villa.ApplySuspects(c, spec.SuspectIds);
            c.Prepare();
            bool sabotageCase = type == CaseType.Sabotimi;
            if (sabotageCase && c.UseGraph)
            {
                var deadEnds = new List<int>();
                for (int r = 0; r < c.RoomCount; r++)
                {
                    int deg = 0;
                    for (int o = 0; o < c.RoomCount; o++) if (o != r && c.Adj[r, o]) deg++;
                    if (deg == 1) deadEnds.Add(r);
                }
                c.CrimeRoom = deadEnds.Count > 0 ? deadEnds[rng.Next(deadEnds.Count)] : rng.Next(c.RoomCount);
                c.CrimeTime = 1 + rng.Next(Math.Max(1, c.T - 2));
            }
            else
            {
                c.CrimeRoom = rng.Next(c.RoomCount);
                c.CrimeTime = type == CaseType.Vrasja ? 1 + rng.Next(c.T - 1) : rng.Next(c.T);
            }
            c.Culprit = rng.Next(c.N);
            int tc = c.T;
            c.Truth = new int[c.N][];
            int crimeRoom = c.CrimeRoom;
            int crimeTime = c.CrimeTime;
            bool sabotage = type == CaseType.Sabotimi;
            var order = new int[c.N];
            for (int i = 0; i < c.N; i++) order[i] = i;
            Shuffle(order, rng);
            int[] sizes = c.N <= 3 ? new[] { c.N } : c.N == 4 ? new[] { 2, 2 } : (rng.Next(2) == 0 ? new[] { 2, 3 } : new[] { 3, 2 });
            var groupPaths = new List<int[]>();
            int idx = 0;
            foreach (var size in sizes)
            {
                var members = new List<int>();
                for (int m = 0; m < size; m++) members.Add(order[idx++]);
                bool hasCulprit = members.Contains(c.Culprit);
                int gp = PickPath(c, rng, p =>
                {
                    if (sabotage && (c.PathEver[p] & (1 << crimeRoom)) != 0) return false;
                    if (c.PathRooms[p * tc + crimeTime] == crimeRoom) return false;
                    if (hasCulprit)
                    {
                        if (crimeTime > 0 && !c.CanMove(c.PathRooms[p * tc + crimeTime - 1], crimeRoom)) return false;
                        if (crimeTime < tc - 1 && !c.CanMove(crimeRoom, c.PathRooms[p * tc + crimeTime + 1])) return false;
                    }
                    else if (sabotage && c.UseGraph)
                    {
                        for (int i = 0; i < tc; i++) if (c.Distance(c.PathRooms[p * tc + i], crimeRoom) < 2) return false;
                    }
                    return true;
                }, groupPaths);
                if (gp < 0)
                {
                    reason = "nuk u gjet rrugë për grupin";
                    return null;
                }
                var groupPath = PathOf(c, gp);
                groupPaths.Add(groupPath);
                foreach (var s in members)
                {
                    var path = (int[])groupPath.Clone();
                    if (s == c.Culprit)
                    {
                        path[crimeTime] = crimeRoom;
                    }
                    else if (rng.NextDouble() < 0.35)
                    {
                        int h = rng.Next(tc);
                        if (h != crimeTime || sabotage)
                        {
                            var options = new List<int>();
                            for (int r = 0; r < c.RoomCount; r++)
                            {
                                if (r == crimeRoom || r == path[h]) continue;
                                if (h > 0 && !c.CanMove(path[h - 1], r)) continue;
                                if (h < tc - 1 && !c.CanMove(r, path[h + 1])) continue;
                                options.Add(r);
                            }
                            if (options.Count > 0) path[h] = options[rng.Next(options.Count)];
                        }
                    }
                    c.Truth[s] = path;
                }
            }
            for (int s = 0; s < c.N; s++)
            {
                if (!c.PathValid(c.Truth[s]))
                {
                    reason = "rrugë e pavlefshme sipas grafit";
                    return null;
                }
            }
            c.Cover = LieSelector.Choose(c, rng);
            if (c.Cover == null)
            {
                reason = "LieSelector nuk gjeti gënjeshtër të vlefshme";
                return null;
            }
            var solver = new CspSolver(c);
            int solutions;
            if (!IsUnique(c, solver, out solutions))
            {
                reason = solutions + " zgjidhje të mundshme";
                return null;
            }
            var route = new List<Question>();
            int maxPar = sabotage ? c.Budget - 1 : c.Budget - 3;
            int par = ParPolicy.Compute(c, solver, maxPar, route);
            if (par == ParPolicy.Unsolvable || par > maxPar)
            {
                reason = "par-i kalon kufirin " + maxPar + " për buxhetin " + c.Budget;
                return null;
            }
            if (par < 3)
            {
                reason = "rast shumë i lehtë (par " + par + ")";
                return null;
            }
            c.Par = par;
            var room = c.Rooms[c.CrimeRoom];
            if (sabotage)
            {
                c.Title = "Sabotimi në " + room.Loc;
                c.Objective = "Gjej kush e bëri dhe kur: personi dhe ora duhen provuar.";
                c.Premise = "Dikush sabotoi sistemin e alarmit në " + room.Loc + " gjatë mbrëmjes. Ora e sabotimit nuk dihet.";
            }
            else
            {
                c.Title = "Vrasja në " + room.Loc;
                c.Objective = "Gjej fajtorin dhe akuzo me provë — person, dhomë dhe orë.";
                c.Premise = "Pronari i vilës u gjet i vdekur në " + room.Loc + ", në " + c.Hours[c.CrimeTime] + ". Askush nuk e pa çfarë ndodhi.";
            }
            c.Rules.Add("Çdo i dyshuar ndodhet në saktësisht një dhomë në çdo orë (R01).");
            c.Rules.Add("Të dyshuarit e ndershëm thonë gjithmonë të vërtetën (R03). Fajtori gënjen të paktën një herë (R04).");
            c.Rules.Add(sabotage
                ? "Fajtori ka qenë në " + room.Loc + " në orën e sabotimit, të cilën duhet ta provosh (R08, R09)."
                : "Fajtori duhet të ketë qenë në " + room.Loc + " në " + c.Hours[c.CrimeTime] + " (R08).");
            if (c.UseGraph) c.Rules.Add("Lëvizja vetëm mes dhomave të lidhura, një kalim për orë (R02). Lidhjet: " + Villa.EdgeList(c) + ".");
            else c.Rules.Add("Pa graf lëvizjeje në këtë nivel.");
            c.Rules.Add("Rasti u gjenerua me seed " + seed + " dhe u kontrollua që të ketë saktësisht një zgjidhje. Par-i i llogaritur nga ParPolicy: " + par + ".");
            reason = null;
            return c;
        }
    }
}
