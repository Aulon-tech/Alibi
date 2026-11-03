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
        for (int s = 0; s < c4.N; s++) Assert(c4.PathValid(c4.Truth[s]), "truth " + c4.Suspects[s].Id);
        Assert(c4.PathValid(c4.Cover), "cover");
        Assert(!c4.PathValidKnown(c4.Truth[c4.Culprit]), "known graph blocks culprit");
        int sol;
        Assert(CaseGenerator.IsUniqueAllowingSecret(c4, out sol), "unique with secret sol=" + sol);
        Assert(!c4.SecretEdgeActive && c4.PathRooms != null, "restored hidden secret after check");

        var g = new GameSession(LevelCards.Card4());
        var c = g.Case;
        Assert(g.SearchRoom(c.RoomIndex("BI")).Accepted && c.SecretEdgeActive, "R06 search");
        string[] route = { "A,1,2", "F,4,2", "B,3,2,F", "A,3,2,F", "D,1,2", "D,3,2,G", "E,1,1", "E,3,1,F", "G,6,1", "A,3,2,B", "E,1,4", "E,6,4" };
        foreach (var step in route)
        {
            var p = step.Split(',');
            var q = Question.Make(c.SuspectIndex(p[0]), (QType)int.Parse(p[1]), int.Parse(p[2]), -1, p.Length > 3 ? c.SuspectIndex(p[3]) : -1);
            var o = g.Ask(q);
            Console.WriteLine("  " + step + " -> " + o.Statement.Text);
        }
        Assert(g.Eval.ViableCount == 1 && g.Eval.ViableK[0] == c.Culprit && g.Eval.ViableTau[0] == 2, "only Fatmiri at 22:00 remains");
        var acc = g.Accuse(c.Culprit, c.CrimeRoom, 2);
        Console.WriteLine("  phase=" + g.Phase + " spent=" + g.Spent + " stars=" + g.Stars);
        Assert(g.Phase == Phase.Won, "Card4 won");

        var g1 = new GameSession(LevelCards.Card1());
        g1.Ask(Question.Make(0, QType.Q4, 1));
        g1.Ask(Question.Make(0, QType.Q2, -1, g1.Case.RoomIndex("BI")));
        g1.Ask(Question.Make(1, QType.Q3, 1, -1, 0));
        g1.Ask(Question.Make(1, QType.Q2, -1, g1.Case.RoomIndex("BI")));
        Assert(g1.Accuse(2, g1.Case.RoomIndex("BI"), 1).Accepted && g1.Phase == Phase.Won, "Card1 intact");

        Console.WriteLine("ALL CHECKS PASSED");
    }
}
