using System;
using Alibi.Core;

class Program
{
    static void Assert(bool ok, string msg)
    {
        if (!ok) { Console.WriteLine("FAIL: " + msg); Environment.Exit(1); }
        Console.WriteLine("OK  " + msg);
    }

    static void Main()
    {
        for (int k = 1; k <= 3; k++)
        {
            int solutions;
            Assert(CaseGenerator.IsUnique(LevelCards.Card(k), new CspSolver(LevelCards.Card(k)), out solutions), "Karta " + k);
        }

        var c4 = LevelCards.Card4();
        Assert(c4.Budget == 12, "Card4 budget 12");
        Assert(c4.Par == 6, "Card4 par 6");
        Assert(c4.Tutorial.Count >= 7, "Card4 hints");
        for (int s = 0; s < c4.N; s++) Assert(c4.PathValid(c4.Truth[s]), "truth " + c4.Suspects[s].Id);
        Assert(c4.PathValid(c4.Cover), "cover");
        Assert(!c4.PathValidKnown(c4.Truth[c4.Culprit]), "known graph blocks culprit");
        int sol;
        Assert(CaseGenerator.IsUniqueAllowingSecret(c4, out sol), "unique with secret sol=" + sol);
        Assert(!c4.SecretEdgeActive && c4.PathRooms != null, "restored hidden secret after check");
        var route = new System.Collections.Generic.List<Question>();
        int computedPar = ParPolicy.Compute(LevelCards.Card4(), new CspSolver(LevelCards.Card4()), 40, route);
        Assert(computedPar == 6, "ParPolicy with Q6 = 6 (got " + computedPar + ")");

        var g = new GameSession(LevelCards.Card4());
        int bi = g.Case.RoomIndex("BI");
        int zy = g.Case.RoomIndex("ZY");
        int a = g.Case.SuspectIndex("A");
        int e = g.Case.SuspectIndex("E");
        int f = g.Case.SuspectIndex("F");

        Assert(g.SearchRoom(bi).Accepted && g.Case.SecretEdgeActive, "R06 search");
        Assert(g.Ask(Question.Make(e, QType.Q1, 1)).Accepted, "E Q1");
        var q4e = g.Ask(Question.Make(e, QType.Q4, 1));
        Console.WriteLine("  " + q4e.Statement.Text);
        var q1f = g.Ask(Question.Make(f, QType.Q1, 1));
        Console.WriteLine("  " + q1f.Statement.Text + " conflict=" + (q1f.Conflict != null));
        Assert(q1f.Conflict != null, "lie exposed");

        Assert(g.Ask(Question.Make(a, QType.Q1, 2)).Accepted, "A Q1 crime");
        var q4a = g.Ask(Question.Make(a, QType.Q4, 2));
        Console.WriteLine("  " + q4a.Statement.Text);
        Assert(g.Ask(Question.Make(a, QType.Q2, -1, zy)).Accepted, "A Q2 ZY");

        Console.Write("  candidates:");
        for (int k = 0; k < g.Case.N; k++) if (g.Eval.Candidate[k]) Console.Write(" " + g.Case.Suspects[k].Id);
        Console.WriteLine(" viable=" + g.Eval.ViableCount + " spent=" + g.Spent);

        var acc = g.Accuse(f, zy, 2);
        Console.WriteLine("  phase=" + g.Phase + " proven=" + acc.Verdict.Proven + " stars=" + g.Stars);
        Assert(g.Phase == Phase.Won, "Card4 won");
        Assert(g.Stars >= 1, "stars");

        var g1 = new GameSession(LevelCards.Card1());
        g1.Ask(Question.Make(0, QType.Q4, 1));
        g1.Ask(Question.Make(0, QType.Q2, -1, g1.Case.RoomIndex("BI")));
        g1.Ask(Question.Make(1, QType.Q3, 1, -1, 0));
        g1.Ask(Question.Make(1, QType.Q2, -1, g1.Case.RoomIndex("BI")));
        Assert(g1.Accuse(2, g1.Case.RoomIndex("BI"), 1).Accepted && g1.Phase == Phase.Won, "Card1 intact");

        Console.WriteLine("ALL CHECKS PASSED");
    }
}
