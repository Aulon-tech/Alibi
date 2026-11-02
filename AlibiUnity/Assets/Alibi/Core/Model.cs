using System;
using System.Collections.Generic;

namespace Alibi.Core
{
    public enum QType { Q1 = 1, Q2 = 2, Q3 = 3, Q4 = 4, Q5 = 5 }

    public enum CaseType { Vrasja, Sabotimi }

    public enum Phase { CaseLoaded, Questioning, Accusing, Won, Lost, Abandoned }

    public enum ConsKind { In, Ever, Prev, Same, Diff }

    public sealed class RoomInfo
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Loc;

        public RoomInfo(string id, string name, string loc)
        {
            Id = id;
            Name = name;
            Loc = loc;
        }
    }

    public sealed class SuspectInfo
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Def;
        public readonly string Acc;
        public readonly string Role;
        public readonly string ColorHex;

        public SuspectInfo(string id, string name, string def, string acc, string role, string colorHex)
        {
            Id = id;
            Name = name;
            Def = def;
            Acc = acc;
            Role = role;
            ColorHex = colorHex;
        }

        public string Initial { get { return Name.Substring(0, 1); } }
    }

    public struct Constraint
    {
        public const int PrevStart = -1;
        public const int PrevNever = -2;

        public ConsKind Kind;
        public int S;
        public int T;
        public int R;
        public bool V;
        public int A;
        public int B;
        public int Prev;

        public bool IsBinary { get { return Kind == ConsKind.Same || Kind == ConsKind.Diff; } }

        public bool Involves(int s)
        {
            return IsBinary ? (A == s || B == s) : S == s;
        }

        public static Constraint In(int s, int t, int r)
        {
            return new Constraint { Kind = ConsKind.In, S = s, T = t, R = r };
        }

        public static Constraint Ever(int s, int r, bool v)
        {
            return new Constraint { Kind = ConsKind.Ever, S = s, R = r, V = v };
        }

        public static Constraint PrevRoom(int s, int r, int prev)
        {
            return new Constraint { Kind = ConsKind.Prev, S = s, R = r, Prev = prev };
        }

        public static Constraint Rel(bool same, int a, int b, int t)
        {
            return new Constraint { Kind = same ? ConsKind.Same : ConsKind.Diff, A = a, B = b, T = t };
        }
    }

    public sealed class Question
    {
        public int Suspect = -1;
        public bool HasType;
        public QType Type = QType.Q1;
        public int Time = -1;
        public int Room = -1;
        public int Other = -1;

        public static Question Make(int suspect, QType type, int time = -1, int room = -1, int other = -1)
        {
            return new Question { Suspect = suspect, HasType = true, Type = type, Time = time, Room = room, Other = other };
        }

        public Question Clone()
        {
            return new Question { Suspect = Suspect, HasType = HasType, Type = Type, Time = Time, Room = Room, Other = Other };
        }

        public Question Normalized()
        {
            var q = Clone();
            if (!q.HasType) return q;
            if (!QuestionEngine.NeedsTime(q.Type)) q.Time = -1;
            if (!QuestionEngine.NeedsRoom(q.Type)) q.Room = -1;
            if (!QuestionEngine.NeedsOther(q.Type)) q.Other = -1;
            return q;
        }

        public string Key
        {
            get
            {
                var q = Normalized();
                return q.Suspect + "|" + (q.HasType ? ((int)q.Type).ToString() : "-") + "|" + q.Time + "|" + q.Room + "|" + q.Other;
            }
        }
    }

    public sealed class Statement
    {
        public int Id;
        public int Suspect;
        public Question Question;
        public string QuestionText;
        public string Text;
        public string Formal;
        public Constraint[] Constraints;
        public int Cost;
        public bool IsMistake;
    }

    public sealed class MistakeSpec
    {
        public int Suspect;
        public QType Type;
        public int Time;
        public int WrongRoom;
    }

    public sealed class TutorialStep
    {
        public string Title;
        public string Text;
        public Question Fill;
        public bool Accuse;
    }

    public sealed class CaseDefinition
    {
        public string Id;
        public string Title;
        public string Kicker;
        public string Objective;
        public string Premise;
        public int Level;
        public int CardNumber;
        public int Seed;
        public CaseType Type = CaseType.Vrasja;
        public string MapName = "Vila";
        public RoomInfo[] Rooms;
        public bool UseGraph;
        public bool[,] Adj;
        public string[] Hours;
        public SuspectInfo[] Suspects;
        public int[][] Truth;
        public int Culprit;
        public int[] Cover;
        public int CrimeRoom;
        public int CrimeTime;
        public int Budget;
        public int Par;
        public int ErrorsAllowed;
        public bool IsGenerated;
        public List<MistakeSpec> Mistakes = new List<MistakeSpec>();
        public List<string> Rules = new List<string>();
        public List<TutorialStep> Tutorial = new List<TutorialStep>();
        public List<string> GeneratorNotes = new List<string>();

        public int PathCount;
        public int[] PathRooms;
        public int[] PathEver;
        public int[] PathPrev;

        public int T { get { return Hours.Length; } }
        public int N { get { return Suspects.Length; } }
        public int RoomCount { get { return Rooms.Length; } }
        public bool CrimeTimeKnown { get { return Type == CaseType.Vrasja; } }

        public int[] CrimeTimes
        {
            get
            {
                if (CrimeTimeKnown) return new[] { CrimeTime };
                var all = new int[T];
                for (int i = 0; i < T; i++) all[i] = i;
                return all;
            }
        }

        public int RoomIndex(string id)
        {
            for (int i = 0; i < Rooms.Length; i++) if (Rooms[i].Id == id) return i;
            return -1;
        }

        public int SuspectIndex(string id)
        {
            for (int i = 0; i < Suspects.Length; i++) if (Suspects[i].Id == id) return i;
            return -1;
        }

        public bool Adjacent(int a, int b)
        {
            return a == b || Adj[a, b];
        }

        public bool CanMove(int a, int b)
        {
            return !UseGraph || Adjacent(a, b);
        }

        public int[] WorldPath(int s)
        {
            return (s == Culprit && Cover != null) ? Cover : Truth[s];
        }

        public MistakeSpec MistakeFor(Question q)
        {
            if (!q.HasType) return null;
            foreach (var m in Mistakes)
            {
                if (m.Suspect == q.Suspect && m.Type == q.Type && (!QuestionEngine.NeedsTime(q.Type) || m.Time == q.Time)) return m;
            }
            return null;
        }

        public int Distance(int a, int b)
        {
            if (a == b) return 0;
            if (!UseGraph) return 1;
            var dist = new int[RoomCount];
            for (int i = 0; i < dist.Length; i++) dist[i] = -1;
            var queue = new Queue<int>();
            dist[a] = 0;
            queue.Enqueue(a);
            while (queue.Count > 0)
            {
                int x = queue.Dequeue();
                for (int y = 0; y < RoomCount; y++)
                {
                    if (y == x || !Adj[x, y] || dist[y] >= 0) continue;
                    dist[y] = dist[x] + 1;
                    if (y == b) return dist[y];
                    queue.Enqueue(y);
                }
            }
            return -1;
        }

        public void Prepare()
        {
            int t = T;
            var paths = new List<int[]>();
            var cur = new int[t];
            Enumerate(0, cur, paths);
            PathCount = paths.Count;
            PathRooms = new int[PathCount * t];
            PathEver = new int[PathCount];
            PathPrev = new int[PathCount * RoomCount];
            for (int p = 0; p < PathCount; p++)
            {
                var path = paths[p];
                int ever = 0;
                for (int i = 0; i < t; i++)
                {
                    PathRooms[p * t + i] = path[i];
                    ever |= 1 << path[i];
                }
                PathEver[p] = ever;
                for (int r = 0; r < RoomCount; r++) PathPrev[p * RoomCount + r] = QuestionEngine.PrevOf(path, r);
            }
        }

        void Enumerate(int i, int[] cur, List<int[]> outList)
        {
            if (i == cur.Length)
            {
                outList.Add((int[])cur.Clone());
                return;
            }
            for (int r = 0; r < RoomCount; r++)
            {
                if (i > 0 && !CanMove(cur[i - 1], r)) continue;
                cur[i] = r;
                Enumerate(i + 1, cur, outList);
            }
        }

        public bool PathValid(int[] path)
        {
            for (int i = 1; i < path.Length; i++) if (!CanMove(path[i - 1], path[i])) return false;
            return true;
        }

        public string Formal(int s)
        {
            return Suspects[s].Name;
        }
    }
}
