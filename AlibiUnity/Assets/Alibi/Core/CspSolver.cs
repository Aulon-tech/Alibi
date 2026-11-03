using System;
using System.Collections.Generic;

namespace Alibi.Core
{
    public sealed class EvalResult
    {
        public readonly List<int> ViableK = new List<int>();
        public readonly List<int> ViableTau = new List<int>();
        public bool[] Candidate;
        public bool[] TimeCandidate;
        public bool[,,] Possible;
        public int[,] Count;
        public bool[,] ItemPossible;

        public int ViableCount { get { return ViableK.Count; } }

        public int CandidateCount
        {
            get
            {
                int n = 0;
                foreach (var b in Candidate) if (b) n++;
                return n;
            }
        }

        public bool IsSolved { get { return ViableK.Count == 1; } }

        public bool IsViable(int k, int tau)
        {
            for (int i = 0; i < ViableK.Count; i++) if (ViableK[i] == k && ViableTau[i] == tau) return true;
            return false;
        }

        public int Mass
        {
            get
            {
                int m = ViableK.Count * 1000;
                foreach (var v in Count) m += v;
                return m;
            }
        }
    }

    public sealed class Conflict
    {
        public int At;
        public List<int> StatementIds = new List<int>();
        public bool InvolvesCrimeFact;
    }

    public sealed class ExclusionChain
    {
        public int Suspect = -1;
        public int Time = -1;
        public List<Statement> Reason = new List<Statement>();
    }

    public sealed class Verdict
    {
        public bool Won;
        public bool OkPerson;
        public bool OkRoom;
        public bool OkTime;
        public bool Proven;
        public EvalResult Eval;
        public List<Statement> ExcludedBy;
        public List<ExclusionChain> Chain = new List<ExclusionChain>();
        public List<string> WrongFields = new List<string>();
    }

    public sealed class CspSolver
    {
        readonly CaseDefinition c;
        readonly int n;
        readonly int tCount;
        readonly int rCount;
        readonly int[] pr;
        readonly int[] crimeTimes;

        public CspSolver(CaseDefinition caseDefinition)
        {
            c = caseDefinition;
            if (c.PathRooms == null) c.Prepare();
            n = c.N;
            tCount = c.T;
            rCount = c.RoomCount;
            pr = c.PathRooms;
            crimeTimes = c.CrimeTimes;
        }

        public CaseDefinition Case { get { return c; } }

        bool Collect(IList<Statement> stmts, int skipSuspect, int skipId, int hypK, int hypTau, List<Constraint> unary, List<Constraint> bins)
        {
            foreach (var st in stmts)
            {
                if (st.Suspect == skipSuspect || st.Id == skipId) continue;
                foreach (var k in st.Constraints)
                {
                    if (k.Kind == ConsKind.ItemSeen)
                    {
                        if (hypK < 0) continue;
                        if (k.T < hypTau) unary.Add(k.V ? Constraint.In(k.S, k.T, c.CrimeRoom) : Constraint.NotIn(k.S, k.T, c.CrimeRoom));
                        else if (k.S == hypK)
                        {
                            if (!k.V) return false;
                        }
                        else bins.Add(Constraint.Rel(k.V, k.S, hypK, k.T));
                    }
                    else if (k.IsBinary) bins.Add(k);
                    else unary.Add(k);
                }
            }
            return true;
        }

        bool UnaryOk(int p, Constraint k)
        {
            switch (k.Kind)
            {
                case ConsKind.In: return pr[p * tCount + k.T] == k.R;
                case ConsKind.NotIn: return pr[p * tCount + k.T] != k.R;
                case ConsKind.Ever: return ((c.PathEver[p] & (1 << k.R)) != 0) == k.V;
                case ConsKind.Prev: return c.PathPrev[p * rCount + k.R] == k.Prev;
            }
            return true;
        }

        int[][] Propagate(List<Constraint> unary, List<Constraint> bins, int hypK, int hypTau)
        {
            var d = new int[n][];
            var buf = new List<int>(c.PathCount);
            for (int s = 0; s < n; s++)
            {
                buf.Clear();
                for (int p = 0; p < c.PathCount; p++)
                {
                    if (s == hypK && pr[p * tCount + hypTau] != c.CrimeRoom) continue;
                    bool ok = true;
                    for (int i = 0; i < unary.Count; i++)
                    {
                        if (unary[i].S == s && !UnaryOk(p, unary[i]))
                        {
                            ok = false;
                            break;
                        }
                    }
                    if (ok) buf.Add(p);
                }
                if (buf.Count == 0) return null;
                d[s] = buf.ToArray();
            }
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < bins.Count; i++)
                {
                    var k = bins[i];
                    for (int dir = 0; dir < 2; dir++)
                    {
                        int x = dir == 0 ? k.A : k.B;
                        int y = dir == 0 ? k.B : k.A;
                        int mask = 0;
                        var dy = d[y];
                        for (int j = 0; j < dy.Length; j++) mask |= 1 << pr[dy[j] * tCount + k.T];
                        var dx = d[x];
                        int keep = 0;
                        var next = new int[dx.Length];
                        bool same = k.Kind == ConsKind.Same;
                        for (int j = 0; j < dx.Length; j++)
                        {
                            int bit = 1 << pr[dx[j] * tCount + k.T];
                            bool ok = same ? (mask & bit) != 0 : (mask & ~bit) != 0;
                            if (ok) next[keep++] = dx[j];
                        }
                        if (keep == 0) return null;
                        if (keep != dx.Length)
                        {
                            Array.Resize(ref next, keep);
                            d[x] = next;
                            changed = true;
                        }
                    }
                }
            }
            return d;
        }

        bool HasSolution(int[][] d, List<Constraint> bins, bool needCrime)
        {
            if (!needCrime) return Search(d, bins);
            foreach (var tau in crimeTimes)
            {
                for (int s = 0; s < n; s++)
                {
                    var list = new List<int>();
                    foreach (var p in d[s]) if (pr[p * tCount + tau] == c.CrimeRoom) list.Add(p);
                    if (list.Count == 0) continue;
                    var d2 = (int[][])d.Clone();
                    d2[s] = list.ToArray();
                    if (Search(d2, bins)) return true;
                }
            }
            return false;
        }

        bool Search(int[][] d, List<Constraint> bins)
        {
            if (bins.Count == 0) return true;
            var rel = new bool[n, tCount];
            var byS = new List<Constraint>[n];
            for (int i = 0; i < n; i++) byS[i] = new List<Constraint>();
            foreach (var k in bins)
            {
                rel[k.A, k.T] = true;
                rel[k.B, k.T] = true;
                byS[k.A].Add(k);
                byS[k.B].Add(k);
            }
            var dom = new int[n][];
            var involved = new List<int>();
            for (int s = 0; s < n; s++)
            {
                if (byS[s].Count == 0) continue;
                involved.Add(s);
                var seen = new HashSet<int>();
                var list = new List<int>();
                foreach (var p in d[s])
                {
                    int key = 0;
                    for (int t = 0; t < tCount; t++)
                    {
                        if (rel[s, t]) key = key * 16 + pr[p * tCount + t] + 1;
                    }
                    if (seen.Add(key)) list.Add(p);
                }
                dom[s] = list.ToArray();
            }
            var order = involved.ToArray();
            Array.Sort(order, (x, y) =>
            {
                int byDeg = byS[y].Count.CompareTo(byS[x].Count);
                return byDeg != 0 ? byDeg : dom[x].Length.CompareTo(dom[y].Length);
            });
            var asg = new int[n];
            for (int i = 0; i < n; i++) asg[i] = -1;
            return Backtrack(0, order, dom, asg, byS);
        }

        bool Backtrack(int i, int[] order, int[][] dom, int[] asg, List<Constraint>[] byS)
        {
            if (i == order.Length) return true;
            int sus = order[i];
            foreach (var p in dom[sus])
            {
                bool ok = true;
                foreach (var k in byS[sus])
                {
                    int o = k.A == sus ? k.B : k.A;
                    if (asg[o] < 0) continue;
                    bool same = pr[p * tCount + k.T] == pr[asg[o] * tCount + k.T];
                    if (same != (k.Kind == ConsKind.Same))
                    {
                        ok = false;
                        break;
                    }
                }
                if (!ok) continue;
                asg[sus] = p;
                if (Backtrack(i + 1, order, dom, asg, byS)) return true;
                asg[sus] = -1;
            }
            return false;
        }

        public bool ConsistentAll(IList<Statement> stmts)
        {
            var unary = new List<Constraint>();
            var bins = new List<Constraint>();
            Collect(stmts, -1, -1, -1, -1, unary, bins);
            var d = Propagate(unary, bins, -1, -1);
            return d != null && HasSolution(d, bins, true);
        }

        public bool ConsistentNoCrime(IList<Statement> stmts)
        {
            var unary = new List<Constraint>();
            var bins = new List<Constraint>();
            Collect(stmts, -1, -1, -1, -1, unary, bins);
            var d = Propagate(unary, bins, -1, -1);
            return d != null && HasSolution(d, bins, false);
        }

        int[][] HypDomains(IList<Statement> stmts, int k, int tau, int dropId)
        {
            var unary = new List<Constraint>();
            var bins = new List<Constraint>();
            if (!Collect(stmts, k, dropId, k, tau, unary, bins)) return null;
            var d = Propagate(unary, bins, k, tau);
            if (d == null) return null;
            if (!HasSolution(d, bins, false)) return null;
            return d;
        }

        bool ViableFor(IList<Statement> stmts, int k, int tau, bool[,,] union, bool[,] itemUnion)
        {
            bool any = false;
            var d = HypDomains(stmts, k, tau, -1);
            if (d != null)
            {
                any = true;
                if (union == null) return true;
                Merge(d, union);
                MergeItem(d, k, tau, itemUnion);
            }
            if (c.ErrorsAllowed > 0)
            {
                foreach (var st in stmts)
                {
                    if (st.Suspect == k) continue;
                    var d2 = HypDomains(stmts, k, tau, st.Id);
                    if (d2 == null) continue;
                    any = true;
                    if (union == null) return true;
                    Merge(d2, union);
                    MergeItem(d2, k, tau, itemUnion);
                }
            }
            return any;
        }

        void Merge(int[][] d, bool[,,] union)
        {
            for (int s = 0; s < n; s++)
            {
                foreach (var p in d[s])
                {
                    for (int t = 0; t < tCount; t++) union[s, t, pr[p * tCount + t]] = true;
                }
            }
        }

        void MergeItem(int[][] d, int k, int tau, bool[,] itemUnion)
        {
            if (itemUnion == null) return;
            for (int t = 0; t < tCount; t++)
            {
                if (t < tau)
                {
                    itemUnion[t, c.CrimeRoom] = true;
                    continue;
                }
                foreach (var p in d[k]) itemUnion[t, pr[p * tCount + t]] = true;
            }
        }

        public EvalResult Evaluate(IList<Statement> stmts, EvalResult prior = null)
        {
            var ev = new EvalResult
            {
                Candidate = new bool[n],
                TimeCandidate = new bool[tCount],
                Possible = new bool[n, tCount, rCount],
                Count = new int[n, tCount],
                ItemPossible = c.HasItem ? new bool[tCount, rCount] : null
            };
            for (int k = 0; k < n; k++)
            {
                foreach (var tau in crimeTimes)
                {
                    if (prior != null && !prior.IsViable(k, tau)) continue;
                    if (ViableFor(stmts, k, tau, ev.Possible, ev.ItemPossible))
                    {
                        ev.ViableK.Add(k);
                        ev.ViableTau.Add(tau);
                        ev.Candidate[k] = true;
                        ev.TimeCandidate[tau] = true;
                    }
                }
            }
            for (int s = 0; s < n; s++)
            {
                for (int t = 0; t < tCount; t++)
                {
                    int cnt = 0;
                    for (int r = 0; r < rCount; r++) if (ev.Possible[s, t, r]) cnt++;
                    ev.Count[s, t] = cnt;
                }
            }
            return ev;
        }

        public bool IsViable(IList<Statement> stmts, int k, int tau)
        {
            if (tau >= 0) return ViableFor(stmts, k, tau, null, null);
            foreach (var t in crimeTimes) if (ViableFor(stmts, k, t, null, null)) return true;
            return false;
        }

        public Conflict ConflictFor(IList<Statement> prev, Statement next, ICollection<int> flagged)
        {
            var all = new List<Statement>(prev) { next };
            if (ConsistentAll(all)) return null;
            var m = new List<Statement>(prev);
            var order = new List<Statement>(prev);
            order.Sort((a, b) =>
            {
                int fa = flagged != null && flagged.Contains(a.Id) ? 1 : 0;
                int fb = flagged != null && flagged.Contains(b.Id) ? 1 : 0;
                if (fa != fb) return fb.CompareTo(fa);
                return a.Id.CompareTo(b.Id);
            });
            foreach (var e in order)
            {
                var trial = new List<Statement>(m);
                trial.Remove(e);
                var withNext = new List<Statement>(trial) { next };
                if (!ConsistentAll(withNext)) m = trial;
            }
            if (!ConsistentAll(m)) return null;
            var core = new List<Statement>(m) { next };
            var conflict = new Conflict { At = next.Id, InvolvesCrimeFact = ConsistentNoCrime(core) };
            foreach (var st in core) conflict.StatementIds.Add(st.Id);
            return conflict;
        }

        public List<Statement> ExclusionReason(IList<Statement> stmts, int k, int tau)
        {
            var m = new List<Statement>();
            foreach (var st in stmts) if (st.Suspect != k) m.Add(st);
            var snapshot = new List<Statement>(m);
            foreach (var e in snapshot)
            {
                var trial = new List<Statement>(m);
                trial.Remove(e);
                if (!IsViable(trial, k, tau)) m = trial;
            }
            return m;
        }

        public Verdict Verify(IList<Statement> stmts, int person, int room, int time)
        {
            var ev = Evaluate(stmts);
            var v = new Verdict
            {
                Eval = ev,
                OkPerson = person == c.Culprit,
                OkRoom = room == c.CrimeRoom,
                OkTime = time == c.CrimeTime
            };
            v.Proven = ev.ViableCount == 1 && ev.ViableK[0] == c.Culprit && ev.ViableTau[0] == c.CrimeTime;
            if (!v.OkPerson) v.WrongFields.Add("personi");
            if (!v.OkRoom) v.WrongFields.Add("dhoma");
            if (!v.OkTime) v.WrongFields.Add("ora");
            if (v.OkPerson && v.OkRoom && v.OkTime && v.Proven)
            {
                v.Won = true;
                for (int k = 0; k < n; k++)
                {
                    if (k == c.Culprit) continue;
                    v.Chain.Add(new ExclusionChain { Suspect = k, Reason = ExclusionReason(stmts, k, -1) });
                }
                if (!c.CrimeTimeKnown)
                {
                    for (int t = 0; t < tCount; t++)
                    {
                        if (t == c.CrimeTime) continue;
                        v.Chain.Add(new ExclusionChain { Suspect = c.Culprit, Time = t, Reason = ExclusionReason(stmts, c.Culprit, t) });
                    }
                }
                return v;
            }
            int hypTime = c.CrimeTimeKnown ? c.CrimeTime : time;
            if (person >= 0 && person < n && hypTime >= 0 && hypTime < tCount && !ev.IsViable(person, hypTime))
            {
                v.ExcludedBy = ExclusionReason(stmts, person, hypTime);
            }
            return v;
        }
    }
}
