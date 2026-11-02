using System.Collections.Generic;
using Alibi.Core;
using NUnit.Framework;

namespace Alibi.Tests
{
    public class CoreTests
    {
        static List<int> Candidates(GameSession g)
        {
            var list = new List<int>();
            for (int k = 0; k < g.Case.N; k++) if (g.Eval.Candidate[k]) list.Add(k);
            return list;
        }

        static string Names(GameSession g)
        {
            var parts = new List<string>();
            foreach (var k in Candidates(g)) parts.Add(g.Case.Suspects[k].Id);
            return string.Join(",", parts.ToArray());
        }

        static AskOutcome Ask(GameSession g, string suspect, QType type, int time = -1, string room = null, string other = null)
        {
            var q = Question.Make(g.Case.SuspectIndex(suspect), type, time, room == null ? -1 : g.Case.RoomIndex(room), other == null ? -1 : g.Case.SuspectIndex(other));
            var o = g.Ask(q);
            Assert.IsTrue(o.Accepted, "pyetja u refuzua: " + o.Message);
            return o;
        }

        [Test]
        public void Karta1_ZgjidhjaEDokumentuar()
        {
            var g = new GameSession(LevelCards.Card1());
            Assert.AreEqual("A,B,D,E", Names(g));
            Assert.AreEqual("Me mua ishin Blerimi dhe Elmazi.", Ask(g, "A", QType.Q4, 1).Statement.Text);
            Assert.AreEqual("Jo, nuk kam qenë kurrë në Bibliotekë.", Ask(g, "A", QType.Q2, -1, "BI").Statement.Text);
            Assert.AreEqual("A,D", Names(g));
            Assert.AreEqual("Po, Arta ishte me mua.", Ask(g, "B", QType.Q3, 1, null, "A").Statement.Text);
            Ask(g, "B", QType.Q2, -1, "BI");
            Assert.AreEqual("D", Names(g));
            Assert.AreEqual(5, g.Spent);
            var r = g.Accuse(g.Case.SuspectIndex("D"), g.Case.RoomIndex("BI"), 1);
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(Phase.Won, g.Phase);
            Assert.AreEqual(3, g.Stars);
        }

        [Test]
        public void Karta1_DeklaratatPerVetvetenNukPerjashtojne()
        {
            var g = new GameSession(LevelCards.Card1());
            Ask(g, "A", QType.Q1, 1);
            Ask(g, "B", QType.Q1, 1);
            Ask(g, "E", QType.Q1, 1);
            Assert.AreEqual("A,B,D,E", Names(g));
        }

        [Test]
        public void Karta2_ZgjidhjaEDokumentuar()
        {
            var g = new GameSession(LevelCards.Card2());
            Assert.AreEqual("Me mua ishin Arta dhe Fatmiri.", Ask(g, "B", QType.Q4, 0).Statement.Text);
            Ask(g, "B", QType.Q1, 0);
            Assert.AreEqual("B,D,E", Names(g));
            Ask(g, "A", QType.Q3, 0, null, "B");
            Ask(g, "A", QType.Q1, 0);
            Assert.AreEqual("D,E", Names(g));
            Ask(g, "F", QType.Q3, 2, null, "D");
            Assert.AreEqual("E", Names(g));
            Assert.AreEqual(6, g.Spent);
            g.Accuse(g.Case.SuspectIndex("E"), g.Case.RoomIndex("KO"), 2);
            Assert.AreEqual(Phase.Won, g.Phase);
        }

        [Test]
        public void Karta3_ZgjidhjaEDokumentuarMeDeshmitarGabimi()
        {
            var g = new GameSession(LevelCards.Card3());
            Assert.AreEqual("Me mua ishin Blerimi, Drita dhe Fatmiri.", Ask(g, "A", QType.Q4, 1).Statement.Text);
            Ask(g, "A", QType.Q1, 1);
            Assert.AreEqual("A,B,D,E,F", Names(g));
            Ask(g, "B", QType.Q4, 1);
            Ask(g, "B", QType.Q1, 1);
            Assert.AreEqual("A,B,E", Names(g));
            var o5 = Ask(g, "D", QType.Q1, 1);
            Assert.AreEqual("Isha në Kuzhinë.", o5.Statement.Text);
            Assert.IsTrue(o5.Statement.IsMistake);
            Assert.IsNotNull(o5.Conflict, "duhet te shfaqet kontradikta");
            Assert.AreEqual("A,B,E", Names(g));
            Ask(g, "D", QType.Q4, 1);
            Assert.AreEqual("E", Names(g));
            Assert.AreEqual(9, g.Spent);
            g.Accuse(g.Case.SuspectIndex("E"), g.Case.RoomIndex("BI"), 2);
            Assert.AreEqual(Phase.Won, g.Phase);
        }

        [Test]
        public void Karta3_AkuzaEDritesPasKontradiktesHumb()
        {
            var g = new GameSession(LevelCards.Card3());
            Ask(g, "A", QType.Q4, 1);
            Ask(g, "A", QType.Q1, 1);
            Ask(g, "B", QType.Q4, 1);
            Ask(g, "B", QType.Q1, 1);
            Ask(g, "D", QType.Q1, 1);
            var r = g.Accuse(g.Case.SuspectIndex("D"), g.Case.RoomIndex("BI"), 2);
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(Phase.Lost, g.Phase);
            Assert.IsNotNull(r.Verdict.ExcludedBy);
            Assert.IsTrue(r.Verdict.ExcludedBy.Count > 0);
        }

        [Test]
        public void TC01_PyetjaQ1ShtonKufizimDheZbretBuxhetin()
        {
            var g = new GameSession(LevelCards.Card1());
            int s2 = 1;
            Assert.AreEqual(5, g.Eval.Count[s2, 1]);
            var o = g.Ask(Question.Make(s2, QType.Q1, 1));
            Assert.IsTrue(o.Accepted);
            Assert.AreEqual(11, g.Remaining);
            Assert.AreEqual(1, o.Statement.Constraints.Length);
            Assert.IsTrue(g.Eval.Count[s2, 1] < 5);
            Assert.AreEqual(Phase.Questioning, g.Phase);
        }

        [Test]
        public void TC02_PyetjaQ4KushtonDyDheLidhTeGjithe()
        {
            var g = new GameSession(LevelCards.Card1());
            g.Ask(Question.Make(1, QType.Q1, 0));
            var o = g.Ask(Question.Make(0, QType.Q4, 0));
            Assert.IsTrue(o.Accepted);
            Assert.AreEqual(9, g.Remaining);
            Assert.AreEqual(g.Case.N - 1, o.Statement.Constraints.Length);
        }

        [Test]
        public void TC03_PyetjaMeKosto2KurBuxhetiEshte1()
        {
            var g = new GameSession(LevelCards.Card1());
            for (int i = 0; i < 11; i++) g.Ask(Question.Make(3, QType.Q1, i % 3));
            Assert.AreEqual(1, g.Remaining);
            var o = g.Ask(Question.Make(0, QType.Q4, 1));
            Assert.IsFalse(o.Accepted);
            Assert.AreEqual("Buxheti nuk mjafton: kjo pyetje kushton 2, të ka mbetur 1.", o.Message);
            Assert.AreEqual(1, g.Remaining);
            Assert.AreEqual(11, g.Statements.Count);
            Assert.IsTrue(g.Ask(Question.Make(0, QType.Q1, 1)).Accepted);
        }

        [Test]
        public void TC04_AkuzaEPaplotePaOre()
        {
            var g = new GameSession(LevelCards.Card1());
            g.Ask(Question.Make(0, QType.Q1, 1));
            var r = g.Accuse(2, g.Case.RoomIndex("BI"), -1);
            Assert.IsFalse(r.Accepted);
            CollectionAssert.AreEqual(new[] { "orën" }, r.Missing);
            Assert.AreEqual("Plotëso edhe orën para se të akuzosh.", r.Message);
            Assert.AreEqual(11, g.Remaining);
            Assert.AreEqual(Phase.Accusing, g.Phase);
        }

        [Test]
        public void TC05_DhomeQeNukEkzistonNeNivel()
        {
            var g = new GameSession(LevelCards.Card1());
            var o = g.Ask(Question.Make(0, QType.Q2, -1, 99));
            Assert.IsFalse(o.Accepted);
            Assert.AreEqual("Dhomë e pavlefshme", o.Message);
            Assert.AreEqual(12, g.Remaining);
            var incomplete = g.Ask(new Question { Suspect = 0, HasType = true, Type = QType.Q4 });
            Assert.IsFalse(incomplete.Accepted);
            Assert.AreEqual(12, g.Remaining);
        }

        [Test]
        public void TC06_KontradiktaTheksohet()
        {
            var g = new GameSession(LevelCards.Card1());
            Ask(g, "A", QType.Q1, 1);
            var first = Ask(g, "A", QType.Q4, 1);
            Assert.IsNull(first.Conflict);
            var o = Ask(g, "D", QType.Q1, 1);
            Assert.IsNotNull(o.Conflict);
            CollectionAssert.Contains(o.Conflict.StatementIds, 2);
            CollectionAssert.Contains(o.Conflict.StatementIds, 3);
            Assert.IsTrue(g.Flagged.Contains(2) && g.Flagged.Contains(3));
            Assert.IsTrue(g.ConflictText(o.Conflict).StartsWith("Këto deklarata nuk mund të jenë të gjitha të vërteta"));
        }

        [Test]
        public void TC07_PersonDheDhomeESakteOreEGabuar()
        {
            var g = new GameSession(LevelCards.Card1());
            Ask(g, "A", QType.Q4, 1);
            Ask(g, "A", QType.Q2, -1, "BI");
            Ask(g, "B", QType.Q3, 1, null, "A");
            Ask(g, "B", QType.Q2, -1, "BI");
            var r = g.Accuse(2, g.Case.RoomIndex("BI"), 0);
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(Phase.Lost, g.Phase);
            CollectionAssert.AreEqual(new[] { "ora" }, r.Verdict.WrongFields);
        }

        [Test]
        public void TC07b_SabotimOraEGabuarTregonKufizimin()
        {
            var gen = CaseGenerator.Generate(3, CaseType.Sabotimi, 4100);
            Assert.IsNotNull(gen.Case);
            var c = gen.Case;
            var g = new GameSession(c);
            foreach (var st in CaseGenerator.FullInformation(c))
            {
                if (g.Remaining <= 0) break;
                if (st.Question.Type != QType.Q1) continue;
                g.Ask(st.Question);
            }
            int wrong = c.CrimeTime == 0 ? 1 : 0;
            var verdict = g.Solver.Verify(g.Statements, c.Culprit, c.CrimeRoom, wrong);
            Assert.IsFalse(verdict.Won);
            CollectionAssert.Contains(verdict.WrongFields, "ora");
        }

        [Test]
        public void TC08_PyetjeKurBuxhetiEshte0()
        {
            var g = new GameSession(LevelCards.Card1());
            for (int i = 0; i < 12; i++) g.Ask(Question.Make(3, QType.Q1, i % 3));
            Assert.AreEqual(0, g.Remaining);
            Assert.AreEqual(Phase.Accusing, g.Phase);
            var o = g.Ask(Question.Make(0, QType.Q1, 1));
            Assert.IsFalse(o.Accepted);
            Assert.AreEqual("Buxheti mbaroi. Duhet të akuzosh.", o.Message);
            Assert.AreEqual(12, g.Statements.Count);
            Assert.AreEqual(Phase.Accusing, g.Phase);
            Assert.IsTrue(g.ForcedAccusation);
        }

        [Test]
        public void TC09_KonfirmimIPaverifikuar()
        {
            var g = new GameSession(LevelCards.Card1());
            var o = Ask(g, "B", QType.Q3, 1, null, "A");
            Assert.IsTrue(g.UnverifiedConfirmation(o.Statement));
            Assert.AreEqual(4, Candidates(g).Count);
        }

        [Test]
        public void TC10_DaljaPaAkuzeEshteAbandoned()
        {
            var c = LevelCards.Card1();
            var g = new GameSession(c);
            Ask(g, "A", QType.Q1, 1);
            g.Abandon();
            Assert.AreEqual(Phase.Abandoned, g.Phase);
            Assert.AreEqual(0, g.Stars);
            var again = new GameSession(LevelCards.Card(c.CardNumber));
            Assert.AreEqual(c.Seed, again.Case.Seed);
            Assert.AreEqual(12, again.Remaining);
        }

        [Test]
        public void R13_Yjet()
        {
            var g = new GameSession(LevelCards.Card1());
            Ask(g, "A", QType.Q4, 1);
            Ask(g, "A", QType.Q2, -1, "BI");
            Ask(g, "B", QType.Q3, 1, null, "A");
            Ask(g, "B", QType.Q2, -1, "BI");
            Ask(g, "E", QType.Q1, 0);
            Ask(g, "E", QType.Q1, 2);
            Ask(g, "E", QType.Q1, 1);
            Assert.AreEqual(8, g.Spent);
            g.Accuse(2, g.Case.RoomIndex("BI"), 1);
            Assert.AreEqual(2, g.Stars);
        }

        [Test]
        public void Gjeneratori_RasteMeZgjidhjeUnike()
        {
            var cases = new[] { new[] { 1, 0 }, new[] { 2, 0 }, new[] { 3, 1 } };
            foreach (var spec in cases)
            {
                var type = spec[1] == 0 ? CaseType.Vrasja : CaseType.Sabotimi;
                var r = CaseGenerator.Generate(spec[0], type, 1000 + spec[0] * 37 + spec[1]);
                Assert.IsNotNull(r.Case, "niveli " + spec[0] + " " + type);
                int solutions;
                Assert.IsTrue(CaseGenerator.IsUnique(r.Case, new CspSolver(r.Case), out solutions));
                Assert.IsTrue(r.Case.Par >= 3 && r.Case.Par < r.Case.Budget);
                var again = CaseGenerator.Generate(spec[0], type, 1000 + spec[0] * 37 + spec[1]);
                Assert.AreEqual(r.Case.Seed, again.Case.Seed);
                Assert.AreEqual(r.Case.Culprit, again.Case.Culprit);
            }
        }

        [Test]
        public void Gjeneratori_DefektiZbulohet()
        {
            var c = LevelCards.Card1();
            c.Truth[3] = Villa.Path(c, "SA", "BI", "KU");
            c.Cover = null;
            c.Prepare();
            int solutions;
            Assert.IsFalse(CaseGenerator.IsUnique(c, new CspSolver(c), out solutions));
            Assert.AreEqual(2, solutions);
        }

        [Test]
        public void KartatKaneZgjidhjeUnike()
        {
            for (int k = 1; k <= 4; k++)
            {
                var c = LevelCards.Card(k);
                int solutions;
                Assert.IsTrue(CaseGenerator.IsUniqueAllowingSecret(c, out solutions), "Karta " + k);
                for (int s = 0; s < c.N; s++) Assert.IsTrue(c.PathValid(c.Truth[s]), "rruga e " + c.Suspects[s].Name);
                Assert.IsTrue(c.PathValid(c.Cover));
            }
        }

        [Test]
        public void Karta4_KalimiIFshehteDheZgjidhja()
        {
            var c = LevelCards.Card4();
            Assert.AreEqual(CaseType.Vjedhja, c.Type);
            Assert.IsTrue(c.SecretEdgeExists);
            Assert.IsFalse(c.SecretEdgeActive);
            Assert.AreEqual(6, c.N);
            Assert.AreEqual(9, c.RoomCount);
            Assert.AreEqual(10, c.Budget);
            Assert.IsFalse(c.PathValidKnown(c.Truth[c.Culprit]));

            var g = new GameSession(c);
            int bi = c.RoomIndex("BI");
            int zy = c.RoomIndex("ZY");
            Assert.IsTrue(g.SearchRoom(bi).Accepted);
            Assert.IsTrue(g.Case.SecretEdgeActive);
            Ask(g, "E", QType.Q1, 1);
            Assert.AreEqual("Me mua ishte Fatmiri.", Ask(g, "E", QType.Q4, 1).Statement.Text);
            var lie = Ask(g, "F", QType.Q1, 1);
            Assert.IsNotNull(lie.Conflict);
            Ask(g, "A", QType.Q1, 2);
            Ask(g, "A", QType.Q4, 2);
            Ask(g, "A", QType.Q2, -1, "ZY");
            Assert.AreEqual("F", Names(g));
            var r = g.Accuse(g.Case.SuspectIndex("F"), zy, 2);
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(Phase.Won, g.Phase);
        }

        [Test]
        public void TC10_SecretEdgeR06()
        {
            var g = new GameSession(LevelCards.Card4());
            int bi = g.Case.RoomIndex("BI");
            int zy = g.Case.RoomIndex("ZY");
            Assert.IsFalse(g.Case.CanMove(bi, zy));
            var miss = g.SearchRoom(g.Case.RoomIndex("HO"));
            Assert.IsTrue(miss.Accepted);
            Assert.IsFalse(g.Case.SecretEdgeActive);
            var found = g.SearchRoom(bi);
            Assert.IsTrue(found.Accepted);
            Assert.IsTrue(g.Case.SecretEdgeActive);
            Assert.IsTrue(g.Case.CanMove(bi, zy));
            Assert.AreEqual(8, g.Remaining);
        }
    }
}
