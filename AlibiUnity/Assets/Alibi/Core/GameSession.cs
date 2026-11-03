using System;
using System.Collections.Generic;

namespace Alibi.Core
{
    public struct CellChange
    {
        public int Suspect;
        public int Time;
        public int Room;
    }

    public sealed class DomainMatrix
    {
        public bool[,,] Possible;
        public int[,] Count;
        public bool[] Candidate;
        public event Action<List<CellChange>> OnDomainChanged;

        public bool IsConfirmed(int s, int t, int r)
        {
            return Possible[s, t, r] && Count[s, t] == 1;
        }

        public List<CellChange> Update(EvalResult ev)
        {
            var changes = new List<CellChange>();
            if (Possible != null)
            {
                int n = Possible.GetLength(0), tc = Possible.GetLength(1), rc = Possible.GetLength(2);
                for (int s = 0; s < n; s++)
                    for (int t = 0; t < tc; t++)
                        for (int r = 0; r < rc; r++)
                            if (Possible[s, t, r] && !ev.Possible[s, t, r]) changes.Add(new CellChange { Suspect = s, Time = t, Room = r });
            }
            Possible = ev.Possible;
            Count = ev.Count;
            Candidate = ev.Candidate;
            if (OnDomainChanged != null) OnDomainChanged(changes);
            return changes;
        }
    }

    public sealed class AskOutcome
    {
        public bool Accepted;
        public string Message;
        public Statement Statement;
        public Conflict Conflict;
        public List<int> NewlyCleared = new List<int>();
        public List<int> NewlyClearedTimes = new List<int>();
        public List<CellChange> Changes = new List<CellChange>();
        public bool Duplicate;
        public bool BudgetExhausted;
    }

    public sealed class AccuseOutcome
    {
        public bool Accepted;
        public string Message;
        public List<string> Missing = new List<string>();
        public Verdict Verdict;
    }

    public sealed class GameSession
    {
        public readonly CaseDefinition Case;
        public CspSolver Solver { get; private set; }
        public readonly DomainMatrix Domains = new DomainMatrix();
        public readonly List<Statement> Statements = new List<Statement>();
        public readonly HashSet<int> Flagged = new HashSet<int>();
        public readonly List<Conflict> Conflicts = new List<Conflict>();
        public readonly List<PhysicalClue> FoundClues = new List<PhysicalClue>();
        public Phase Phase { get; private set; }
        public int Spent { get; private set; }
        public EvalResult Eval { get; private set; }
        public Verdict Verdict { get; private set; }
        public int AccusedPerson = -1;
        public int AccusedRoom = -1;
        public int AccusedTime = -1;
        public bool ForcedAccusation { get; private set; }

        public GameSession(CaseDefinition c)
        {
            Case = c;
            if (c.PathRooms == null) c.Prepare();
            Solver = new CspSolver(c);
            Phase = Phase.CaseLoaded;
            Eval = Solver.Evaluate(Statements);
            Domains.Update(Eval);
        }

        void RebuildSolver()
        {
            Solver = new CspSolver(Case);
            Eval = Solver.Evaluate(Statements);
            Domains.Update(Eval);
        }

        public int Remaining { get { return Case.Budget - Spent; } }

        public bool IsOver { get { return Phase == Phase.Won || Phase == Phase.Lost || Phase == Phase.Abandoned; } }

        public void Begin()
        {
            if (Phase == Phase.CaseLoaded) Phase = Phase.Questioning;
        }

        public string BudgetMessage(QType type)
        {
            int cost = QuestionEngine.Cost(type);
            if (Remaining <= 0) return "Buxheti mbaroi. Duhet të akuzosh.";
            if (cost > Remaining) return "Buxheti nuk mjafton: kjo pyetje kushton " + cost + ", të ka mbetur " + Remaining + ".";
            return null;
        }

        public int DuplicateOf(Question q)
        {
            string key = q.Key;
            foreach (var st in Statements) if (st.Question.Key == key) return st.Id;
            return -1;
        }

        public AskOutcome Ask(Question raw)
        {
            var outcome = new AskOutcome();
            if (IsOver)
            {
                outcome.Message = "Rasti është mbyllur.";
                return outcome;
            }
            if (Remaining <= 0)
            {
                Phase = Phase.Accusing;
                ForcedAccusation = true;
                outcome.Message = "Buxheti mbaroi. Duhet të akuzosh.";
                outcome.BudgetExhausted = true;
                return outcome;
            }
            if (Phase == Phase.Accusing && ForcedAccusation)
            {
                outcome.Message = "Buxheti mbaroi. Duhet të akuzosh.";
                return outcome;
            }
            var q = raw.Normalized();
            var invalid = QuestionEngine.InvalidReason(Case, q);
            if (invalid != null)
            {
                outcome.Message = invalid;
                return outcome;
            }
            var missing = QuestionEngine.Missing(q);
            if (missing.Count > 0)
            {
                outcome.Message = "Mungon: " + string.Join(", ", missing.ToArray()) + ".";
                return outcome;
            }
            var budget = BudgetMessage(q.Type);
            if (budget != null)
            {
                outcome.Message = budget;
                return outcome;
            }
            Begin();
            if (Phase == Phase.Accusing) Phase = Phase.Questioning;
            outcome.Duplicate = DuplicateOf(q) >= 0;
            var answer = QuestionEngine.Answer(Case, q);
            var st = new Statement
            {
                Id = Statements.Count + 1,
                Suspect = q.Suspect,
                Question = q,
                QuestionText = QuestionEngine.Phrase(Case, q),
                Text = answer.Text,
                Formal = answer.Formal,
                Constraints = answer.Constraints,
                Cost = QuestionEngine.Cost(q.Type),
                IsMistake = answer.IsMistake
            };
            var conflict = Solver.ConflictFor(Statements, st, Flagged);
            var before = Eval;
            Statements.Add(st);
            Spent += st.Cost;
            if (conflict != null)
            {
                Conflicts.Add(conflict);
                foreach (var id in conflict.StatementIds) Flagged.Add(id);
            }
            Eval = Solver.Evaluate(Statements, before);
            outcome.Changes = Domains.Update(Eval);
            for (int k = 0; k < Case.N; k++) if (before.Candidate[k] && !Eval.Candidate[k]) outcome.NewlyCleared.Add(k);
            for (int t = 0; t < Case.T; t++) if (before.TimeCandidate[t] && !Eval.TimeCandidate[t]) outcome.NewlyClearedTimes.Add(t);
            outcome.Accepted = true;
            outcome.Statement = st;
            outcome.Conflict = conflict;
            if (Remaining <= 0)
            {
                Phase = Phase.Accusing;
                ForcedAccusation = true;
                outcome.BudgetExhausted = true;
            }
            return outcome;
        }

        public AskOutcome SearchRoom(int room)
        {
            var outcome = new AskOutcome();
            if (IsOver)
            {
                outcome.Message = "Rasti është mbyllur.";
                return outcome;
            }
            if (Remaining <= 0)
            {
                Phase = Phase.Accusing;
                ForcedAccusation = true;
                outcome.Message = "Buxheti mbaroi. Duhet të akuzosh.";
                outcome.BudgetExhausted = true;
                return outcome;
            }
            if (room < 0 || room >= Case.RoomCount)
            {
                outcome.Message = "Dhomë e pavlefshme";
                return outcome;
            }
            Begin();
            if (Phase == Phase.Accusing && !ForcedAccusation) Phase = Phase.Questioning;
            PhysicalClue clue = null;
            foreach (var pc in Case.PhysicalClues)
            {
                if (pc.Room == room && !pc.Found) { clue = pc; break; }
            }
            Spent += 1;
            var before = Eval;
            if (clue == null)
            {
                outcome.Accepted = true;
                outcome.Message = "Nuk gjen asgjë të re në " + Case.Rooms[room].Loc + ".";
                if (Remaining <= 0)
                {
                    Phase = Phase.Accusing;
                    ForcedAccusation = true;
                    outcome.BudgetExhausted = true;
                }
                return outcome;
            }
            clue.Found = true;
            FoundClues.Add(clue);
            bool rebuilt = false;
            if (clue.RevealsSecret && Case.ActivateSecretEdge())
            {
                RebuildSolver();
                rebuilt = true;
            }
            else
            {
                Eval = Solver.Evaluate(Statements, before);
                Domains.Update(Eval);
            }
            outcome.Accepted = true;
            outcome.Message = clue.Title + ": " + clue.Text + (rebuilt ? " Grafi i lëvizjes u përditësua (R06)." : "");
            if (Remaining <= 0)
            {
                Phase = Phase.Accusing;
                ForcedAccusation = true;
                outcome.BudgetExhausted = true;
            }
            return outcome;
        }

        public void StartAccusation()
        {
            if (IsOver) return;
            Begin();
            Phase = Phase.Accusing;
        }

        public void CancelAccusation()
        {
            if (Phase == Phase.Accusing && !ForcedAccusation) Phase = Phase.Questioning;
        }

        public static List<string> MissingAccusationFields(int person, int room, int time)
        {
            var missing = new List<string>();
            if (person < 0) missing.Add("personin");
            if (room < 0) missing.Add("dhomën");
            if (time < 0) missing.Add("orën");
            return missing;
        }

        public static string MissingAccusationMessage(List<string> missing)
        {
            if (missing.Count == 0) return null;
            return "Plotëso edhe " + QuestionEngine.JoinNames(missing) + " para se të akuzosh.";
        }

        public AccuseOutcome Accuse(int person, int room, int time)
        {
            var outcome = new AccuseOutcome();
            if (IsOver)
            {
                outcome.Message = "Rasti është mbyllur.";
                return outcome;
            }
            outcome.Missing = MissingAccusationFields(person, room, time);
            if (outcome.Missing.Count > 0)
            {
                Begin();
                Phase = Phase.Accusing;
                outcome.Message = MissingAccusationMessage(outcome.Missing);
                return outcome;
            }
            AccusedPerson = person;
            AccusedRoom = room;
            AccusedTime = time;
            Verdict = Solver.Verify(Statements, person, room, time);
            Phase = Verdict.Won ? Phase.Won : Phase.Lost;
            outcome.Accepted = true;
            outcome.Verdict = Verdict;
            return outcome;
        }

        public void Abandon()
        {
            if (!IsOver) Phase = Phase.Abandoned;
        }

        public int Stars
        {
            get
            {
                if (Phase != Phase.Won) return 0;
                if (Spent <= Case.Par + 1) return 3;
                if (Spent <= Case.Par + 3) return 2;
                return 1;
            }
        }

        public bool IsCleared(int suspect)
        {
            return !Eval.Candidate[suspect];
        }

        public bool UnverifiedConfirmation(Statement st)
        {
            if (IsCleared(st.Suspect)) return false;
            foreach (var k in st.Constraints)
            {
                if (k.Kind != ConsKind.Same) continue;
                int other = k.A == st.Suspect ? k.B : k.A;
                if (!IsCleared(other)) return true;
            }
            return false;
        }

        public List<Statement> Lies()
        {
            var list = new List<Statement>();
            foreach (var st in Statements)
            {
                if (st.Suspect == Case.Culprit && !QuestionEngine.HoldsInTruth(Case, st)) list.Add(st);
            }
            return list;
        }

        public string ConflictText(Conflict cf)
        {
            if (cf == null) return "";
            var parts = new List<string>();
            foreach (var id in cf.StatementIds)
            {
                var st = Statements[id - 1];
                parts.Add("#" + id + " " + Case.Suspects[st.Suspect].Def);
            }
            string text = "Këto deklarata nuk mund të jenë të gjitha të vërteta: " + string.Join(", ", parts.ToArray());
            if (cf.InvolvesCrimeFact && Case.HasItem)
            {
                text += ", bashkë me faktin e vjedhjes (dikush e mori " + Case.ItemAcc + " nga " + Case.Rooms[Case.CrimeRoom].Loc + " dhe " + Case.ItemName + " lëviz vetëm me të)";
            }
            else if (cf.InvolvesCrimeFact)
            {
                string when = Case.CrimeTimeKnown ? " në " + Case.Hours[Case.CrimeTime] : "";
                text += ", bashkë me faktin e dosjes (dikush ishte në " + Case.Rooms[Case.CrimeRoom].Loc + when + ")";
            }
            text += ".";
            if (Case.ErrorsAllowed > 0) text += " Kujdes: një dëshmitar i ndershëm mund të gabojë një herë (R05).";
            else text += " Dikush prej tyre gënjen.";
            return text;
        }
    }
}
