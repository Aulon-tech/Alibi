using System.Collections.Generic;
using System.Text;

namespace Alibi.Core
{
    public sealed class Answer
    {
        public string Text;
        public string Formal;
        public Constraint[] Constraints;
        public bool IsMistake;
    }

    public static class QuestionEngine
    {
        public static readonly QType[] All = { QType.Q1, QType.Q2, QType.Q3, QType.Q4, QType.Q5 };

        public static int Cost(QType t)
        {
            return (t == QType.Q4 || t == QType.Q5) ? 2 : 1;
        }

        public static bool NeedsTime(QType t)
        {
            return t == QType.Q1 || t == QType.Q3 || t == QType.Q4;
        }

        public static bool NeedsRoom(QType t)
        {
            return t == QType.Q2 || t == QType.Q5;
        }

        public static bool NeedsOther(QType t)
        {
            return t == QType.Q3;
        }

        public static string Code(QType t)
        {
            return "Q" + (int)t;
        }

        public static string GenericLabel(QType t)
        {
            switch (t)
            {
                case QType.Q1: return "Ku ishe në orën X?";
                case QType.Q2: return "A ke qenë ndonjëherë në dhomën R?";
                case QType.Q3: return "A e ke parë personin Y në orën X?";
                case QType.Q4: return "Kush ishte me ty në orën X?";
                default: return "Në cilën dhomë ishe para se të kaloje në R?";
            }
        }

        public static string Info(QType t)
        {
            switch (t)
            {
                case QType.Q1: return "Një kufizim mbi një interval.";
                case QType.Q2: return "Kufizim mbi të gjitha intervalet njëherësh.";
                case QType.Q3: return "Lidh dy persona në një interval.";
                case QType.Q4: return "Lidh një person me të gjithë të tjerët në atë interval.";
                default: return "Lidh dy intervale dhe përdor grafin e lëvizjes.";
            }
        }

        public static string Phrase(CaseDefinition c, Question q)
        {
            if (!q.HasType) return "";
            string x = q.Time >= 0 && q.Time < c.T ? c.Hours[q.Time] : "orën X";
            string room = q.Room >= 0 && q.Room < c.RoomCount ? c.Rooms[q.Room].Loc : null;
            switch (q.Type)
            {
                case QType.Q1: return "Ku ishe në " + x + "?";
                case QType.Q2: return "A ke qenë ndonjëherë në " + (room ?? "dhomën R") + "?";
                case QType.Q3: return "A e ke parë " + (q.Other >= 0 && q.Other < c.N ? c.Suspects[q.Other].Acc : "personin Y") + " në " + x + "?";
                case QType.Q4: return "Kush ishte me ty në " + x + "?";
                default: return "Në cilën dhomë ishe para se të kaloje në " + (room ?? "R") + "?";
            }
        }

        public static List<string> Missing(Question q)
        {
            var list = new List<string>();
            if (q.Suspect < 0) list.Add("i dyshuari");
            if (!q.HasType)
            {
                list.Add("pyetja");
                return list;
            }
            if (NeedsOther(q.Type) && q.Other < 0) list.Add("personi Y");
            if (NeedsTime(q.Type) && q.Time < 0) list.Add("ora");
            if (NeedsRoom(q.Type) && q.Room < 0) list.Add("dhoma");
            return list;
        }

        public static string InvalidReason(CaseDefinition c, Question q)
        {
            if (q.Suspect >= c.N) return "I dyshuar i pavlefshëm";
            if (!q.HasType) return null;
            if (NeedsRoom(q.Type) && q.Room >= c.RoomCount) return "Dhomë e pavlefshme";
            if (NeedsTime(q.Type) && q.Time >= c.T) return "Orë e pavlefshme";
            if (NeedsOther(q.Type) && q.Other >= 0 && (q.Other == q.Suspect || q.Other >= c.N)) return "Person i pavlefshëm";
            return null;
        }

        public static int PrevOf(int[] path, int r)
        {
            for (int t = 1; t < path.Length; t++)
            {
                if (path[t] == r && path[t - 1] != r) return path[t - 1];
            }
            return path[0] == r ? Constraint.PrevStart : Constraint.PrevNever;
        }

        static bool Contains(int[] path, int r)
        {
            for (int i = 0; i < path.Length; i++) if (path[i] == r) return true;
            return false;
        }

        public static string JoinNames(List<string> names)
        {
            if (names.Count == 0) return "";
            if (names.Count == 1) return names[0];
            var sb = new StringBuilder();
            for (int i = 0; i < names.Count - 1; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(names[i]);
            }
            sb.Append(" dhe ").Append(names[names.Count - 1]);
            return sb.ToString();
        }

        public static Answer Answer(CaseDefinition c, Question q)
        {
            var p = c.WorldPath(q.Suspect);
            var me = c.Suspects[q.Suspect];
            var mistake = c.Culprit == q.Suspect ? null : c.MistakeFor(q);
            string hour = q.Time >= 0 && q.Time < c.T ? c.Hours[q.Time] : "";
            switch (q.Type)
            {
                case QType.Q1:
                {
                    int r = mistake != null ? mistake.WrongRoom : p[q.Time];
                    return new Answer
                    {
                        Text = "Isha në " + c.Rooms[r].Loc + ".",
                        Constraints = new[] { Constraint.In(q.Suspect, q.Time, r) },
                        Formal = me.Name + "(" + hour + ") = " + c.Rooms[r].Name,
                        IsMistake = mistake != null
                    };
                }
                case QType.Q2:
                {
                    bool v = Contains(p, q.Room);
                    var room = c.Rooms[q.Room];
                    return new Answer
                    {
                        Text = v ? "Po, kam qenë në " + room.Loc + "." : "Jo, nuk kam qenë kurrë në " + room.Loc + ".",
                        Constraints = new[] { Constraint.Ever(q.Suspect, q.Room, v) },
                        Formal = v ? "∃t: " + me.Name + "(t) = " + room.Name : "∀t: " + me.Name + "(t) ≠ " + room.Name
                    };
                }
                case QType.Q3:
                {
                    var y = c.Suspects[q.Other];
                    bool same = p[q.Time] == c.Truth[q.Other][q.Time];
                    return new Answer
                    {
                        Text = same ? "Po, " + y.Def + " ishte me mua." : "Jo, nuk e pashë " + y.Acc + ".",
                        Constraints = new[] { Constraint.Rel(same, q.Suspect, q.Other, q.Time) },
                        Formal = me.Name + "(" + hour + ") " + (same ? "=" : "≠") + " " + y.Name + "(" + hour + ")"
                    };
                }
                case QType.Q4:
                {
                    int room = p[q.Time];
                    var cons = new List<Constraint>();
                    var with = new List<string>();
                    var formal = new StringBuilder();
                    for (int y = 0; y < c.N; y++)
                    {
                        if (y == q.Suspect) continue;
                        bool same = c.Truth[y][q.Time] == room;
                        if (same) with.Add(c.Suspects[y].Def);
                        cons.Add(Constraint.Rel(same, q.Suspect, y, q.Time));
                        if (formal.Length > 0) formal.Append(" · ");
                        formal.Append(me.Name).Append("(").Append(hour).Append(") ").Append(same ? "=" : "≠").Append(" ").Append(c.Suspects[y].Name);
                    }
                    string text = with.Count == 0 ? "Isha vetëm." : with.Count == 1 ? "Me mua ishte " + with[0] + "." : "Me mua ishin " + JoinNames(with) + ".";
                    return new Answer { Text = text, Constraints = cons.ToArray(), Formal = formal.ToString() };
                }
                default:
                {
                    int v = PrevOf(p, q.Room);
                    var room = c.Rooms[q.Room];
                    string text;
                    string formal;
                    if (v == Constraint.PrevNever)
                    {
                        text = "Nuk kam qenë në " + room.Loc + ".";
                        formal = "∀t: " + me.Name + "(t) ≠ " + room.Name;
                    }
                    else if (v == Constraint.PrevStart)
                    {
                        text = "Isha në " + room.Loc + " që nga " + c.Hours[0] + ".";
                        formal = me.Name + "(" + c.Hours[0] + ") = " + room.Name;
                    }
                    else
                    {
                        text = "Para se të hyja në " + room.Loc + ", isha në " + c.Rooms[v].Loc + ".";
                        formal = me.Name + ": " + c.Rooms[v].Name + " → " + room.Name;
                    }
                    return new Answer { Text = text, Formal = formal, Constraints = new[] { Constraint.PrevRoom(q.Suspect, q.Room, v) } };
                }
            }
        }

        public static bool HoldsInTruth(CaseDefinition c, Statement st)
        {
            foreach (var k in st.Constraints)
            {
                if (k.IsBinary)
                {
                    bool same = c.Truth[k.A][k.T] == c.Truth[k.B][k.T];
                    if (same != (k.Kind == ConsKind.Same)) return false;
                }
                else
                {
                    var p = c.Truth[k.S];
                    if (k.Kind == ConsKind.In && p[k.T] != k.R) return false;
                    if (k.Kind == ConsKind.Ever && Contains(p, k.R) != k.V) return false;
                    if (k.Kind == ConsKind.Prev && PrevOf(p, k.R) != k.Prev) return false;
                }
            }
            return true;
        }
    }
}
