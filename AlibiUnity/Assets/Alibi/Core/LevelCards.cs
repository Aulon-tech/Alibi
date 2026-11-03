using System;
using System.Collections.Generic;

namespace Alibi.Core
{
    public sealed class LevelSpec
    {
        public int Level;
        public string Name;
        public string[] SuspectIds;
        public string[] RoomIds;
        public string[] Hours;
        public int Budget;
        public bool UseGraph;
        public CaseType[] Types;
        public string Unlocks;
        public bool Playable;
    }

    public static class Villa
    {
        public static readonly RoomInfo[] AllRooms =
        {
            new RoomInfo("HO", "Holli", "Holl"),
            new RoomInfo("SA", "Salloni", "Sallon"),
            new RoomInfo("BI", "Biblioteka", "Bibliotekë"),
            new RoomInfo("KU", "Kuzhina", "Kuzhinë"),
            new RoomInfo("DN", "Dhoma e ngrënies", "Dhomën e ngrënies"),
            new RoomInfo("SH", "Shkallët", "Shkallë"),
            new RoomInfo("KO", "Korridori", "Korridor"),
            new RoomInfo("ZY", "Zyra", "Zyrë"),
            new RoomInfo("BO", "Bodrumi", "Bodrum")
        };

        public static readonly string[,] Edges =
        {
            { "HO", "SA" }, { "HO", "KU" }, { "HO", "SH" }, { "SA", "BI" }, { "SA", "DN" }, { "KU", "DN" }, { "SH", "KO" },
            { "KO", "ZY" }, { "SH", "BO" }
        };

        public static readonly SuspectInfo[] AllSuspects =
        {
            new SuspectInfo("A", "Arta", "Arta", "Artën", "kuzhiniere", "#3FA79A"),
            new SuspectInfo("B", "Blerimi", "Blerimi", "Blerimin", "shofer", "#D08B3A"),
            new SuspectInfo("D", "Drita", "Drita", "Dritën", "sekretare", "#A785E0"),
            new SuspectInfo("E", "Elmazi", "Elmazi", "Elmazin", "kopshtar", "#5B9BD5"),
            new SuspectInfo("F", "Fatmiri", "Fatmiri", "Fatmirin", "nipi i pronarit", "#D9667A"),
            new SuspectInfo("G", "Genta", "Genta", "Gentën", "ekonomiste", "#C4A35A")
        };

        public static RoomInfo Room(string id)
        {
            foreach (var r in AllRooms) if (r.Id == id) return r;
            throw new ArgumentException("Dhome e panjohur: " + id);
        }

        public static SuspectInfo Suspect(string id)
        {
            foreach (var s in AllSuspects) if (s.Id == id) return s;
            throw new ArgumentException("I dyshuar i panjohur: " + id);
        }

        public static void ApplyRooms(CaseDefinition c, string[] roomIds, bool useGraph)
        {
            c.Rooms = new RoomInfo[roomIds.Length];
            for (int i = 0; i < roomIds.Length; i++) c.Rooms[i] = Room(roomIds[i]);
            c.UseGraph = useGraph;
            c.Adj = new bool[roomIds.Length, roomIds.Length];
            for (int e = 0; e < Edges.GetLength(0); e++)
            {
                int a = c.RoomIndex(Edges[e, 0]);
                int b = c.RoomIndex(Edges[e, 1]);
                if (a < 0 || b < 0) continue;
                c.Adj[a, b] = true;
                c.Adj[b, a] = true;
            }
        }

        public static void ApplySuspects(CaseDefinition c, string[] ids)
        {
            c.Suspects = new SuspectInfo[ids.Length];
            for (int i = 0; i < ids.Length; i++) c.Suspects[i] = Suspect(ids[i]);
        }

        public static int[] Path(CaseDefinition c, params string[] rooms)
        {
            var p = new int[rooms.Length];
            for (int i = 0; i < rooms.Length; i++)
            {
                p[i] = c.RoomIndex(rooms[i]);
                if (p[i] < 0) throw new ArgumentException("Dhoma " + rooms[i] + " nuk eshte ne kete rast");
            }
            return p;
        }

        public static string EdgeList(CaseDefinition c)
        {
            var parts = new List<string>();
            for (int a = 0; a < c.RoomCount; a++)
            {
                for (int b = a + 1; b < c.RoomCount; b++)
                {
                    if (c.Adj[a, b]) parts.Add(c.Rooms[a].Name + "–" + c.Rooms[b].Name);
                }
            }
            if (c.SecretEdgeExists && c.SecretEdgeActive)
            {
                parts.Add(c.Rooms[c.SecretA].Name + "–" + c.Rooms[c.SecretB].Name + " (i zbuluar)");
            }
            return string.Join(", ", parts.ToArray());
        }

        public static void SetSecretEdge(CaseDefinition c, string a, string b)
        {
            c.SecretA = c.RoomIndex(a);
            c.SecretB = c.RoomIndex(b);
            if (c.SecretA < 0 || c.SecretB < 0) throw new ArgumentException("Kalimi i fshehtë kërkon dhoma të pranishme: " + a + ", " + b);
            c.SecretEdgeExists = true;
            c.SecretEdgeActive = false;
        }
    }

    public static class LevelCards
    {
        public static readonly string[] HoursL1 = { "20:00", "21:00", "22:00" };
        public static readonly string[] HoursL2 = { "20:00", "21:00", "22:00", "23:00" };
        public static readonly string[] HoursL3 = { "20:00", "21:00", "22:00", "23:00", "00:00" };
        public static readonly string[] HoursL4 = { "20:00", "21:00", "22:00", "23:00", "00:00" };
        public static readonly string[] RoomsL1 = { "HO", "SA", "KU", "DN", "BI" };
        public static readonly string[] RoomsL2 = { "HO", "SA", "BI", "KU", "DN", "SH", "KO" };
        public static readonly string[] RoomsL4 = { "HO", "SA", "BI", "KU", "DN", "SH", "KO", "ZY", "BO" };

        public static readonly LevelSpec[] Levels =
        {
            new LevelSpec { Level = 1, Name = "Mësimi", SuspectIds = new[] { "A", "B", "D", "E" }, RoomIds = RoomsL1, Hours = HoursL1, Budget = 12, UseGraph = false, Types = new[] { CaseType.Vrasja }, Unlocks = "Vetëm Vrasja. Mësohet tabela dhe menyja e pyetjeve.", Playable = true },
            new LevelSpec { Level = 2, Name = "Lëvizja", SuspectIds = new[] { "A", "B", "D", "E", "F" }, RoomIds = RoomsL2, Hours = HoursL2, Budget = 12, UseGraph = true, Types = new[] { CaseType.Vrasja }, Unlocks = "Grafi i lëvizjes: askush nuk zhvendoset pa kaluar nëpër dhomat ndërmjetëse.", Playable = true },
            new LevelSpec { Level = 3, Name = "Sabotimi", SuspectIds = new[] { "A", "B", "D", "E", "F" }, RoomIds = RoomsL2, Hours = HoursL3, Budget = 11, UseGraph = true, Types = new[] { CaseType.Sabotimi }, Unlocks = "Dëshmitari me gabim (Karta 3) dhe Sabotimi: ora e krimit nuk dihet.", Playable = true },
            new LevelSpec { Level = 4, Name = "Kalimi", SuspectIds = new[] { "A", "B", "D", "E", "F", "G" }, RoomIds = RoomsL4, Hours = HoursL4, Budget = 10, UseGraph = true, Types = new[] { CaseType.Vjedhja }, Unlocks = "Vjedhja dhe kalimi i fshehtë Biblioteka–Zyra (R06).", Playable = true },
            new LevelSpec { Level = 5, Name = "Fajtori", Unlocks = "Të tria llojet së bashku dhe refuzimet (R10).", Playable = false, Budget = 9 },
            new LevelSpec { Level = 6, Name = "I pafund", Unlocks = "Lloj dhe hartë të rastësishme.", Playable = false }
        };

        public static readonly LevelSpec Sabotage = new LevelSpec
        {
            Level = 3,
            Name = "Sabotimi (variant eksperimental)",
            SuspectIds = new[] { "A", "B", "D", "E" },
            RoomIds = RoomsL2,
            Hours = new[] { "21:00", "22:00", "23:00" },
            Budget = 12,
            UseGraph = true,
            Types = new[] { CaseType.Sabotimi },
            Unlocks = "Ora e krimit nuk dihet: akuza duhet të provojë personin dhe orën.",
            Playable = true
        };

        public static LevelSpec Spec(int level)
        {
            foreach (var l in Levels) if (l.Level == level) return l;
            return Levels[0];
        }

        public static LevelSpec SpecFor(int level, CaseType type)
        {
            return type == CaseType.Sabotimi ? Sabotage : Spec(level);
        }

        public static CaseDefinition Card(int number)
        {
            switch (number)
            {
                case 2: return Card2();
                case 3: return Card3();
                case 4: return Card4();
                default: return Card1();
            }
        }

        static void BaseRules(CaseDefinition c)
        {
            c.Rules.Add("Çdo i dyshuar ndodhet në saktësisht një dhomë në çdo orë (R01).");
            c.Rules.Add("Të dyshuarit e ndershëm thonë gjithmonë të vërtetën (R03).");
            c.Rules.Add("Fajtori gënjen të paktën një herë (R04). Deklarata e dikujt për vetveten nuk e përjashton atë.");
            c.Rules.Add("Fajtori duhet të ketë qenë në " + c.Rooms[c.CrimeRoom].Loc + (c.CrimeTimeKnown ? " në " + c.Hours[c.CrimeTime] : "") + " (R08).");
        }

        public static CaseDefinition Card1()
        {
            var c = new CaseDefinition
            {
                Id = "karta1",
                CardNumber = 1,
                Level = 1,
                Seed = 101,
                Title = "Karta 1 — Njohja me lojën",
                Kicker = "FAZA 1 · NIVELI 1",
                Objective = "Gjej fajtorin dhe akuzo me provë — person, dhomë dhe orë — brenda buxhetit prej 12 pyetjesh.",
                Premise = "Pronari i vilës u gjet i vdekur në Bibliotekë, në 21:00. Askush nuk e pa çfarë ndodhi.",
                Hours = HoursL1,
                Budget = 12,
                Par = 5,
                Type = CaseType.Vrasja
            };
            Villa.ApplyRooms(c, RoomsL1, false);
            Villa.ApplySuspects(c, new[] { "A", "B", "D", "E" });
            c.CrimeRoom = c.RoomIndex("BI");
            c.CrimeTime = 1;
            c.Culprit = c.SuspectIndex("D");
            c.Truth = new[]
            {
                Villa.Path(c, "KU", "SA", "DN"),
                Villa.Path(c, "HO", "SA", "SA"),
                Villa.Path(c, "DN", "BI", "HO"),
                Villa.Path(c, "SA", "SA", "KU")
            };
            c.Cover = Villa.Path(c, "DN", "SA", "HO");
            BaseRules(c);
            c.Rules.Add("Pa graf lëvizjeje: në këtë nivel lëvizja mes dhomave nuk kufizohet.");
            int A = 0, B = 1;
            int bi = c.RoomIndex("BI");
            c.Tutorial.Add(new TutorialStep { Title = "Mirë se vjen, detektiv", Text = "Krimi ndodhi në Bibliotekë në 21:00. Detyra jote nuk është ta hamendësosh fajtorin, por ta provosh me deklaratat e të tjerëve." });
            c.Tutorial.Add(new TutorialStep { Title = "Deklarata për vetveten nuk mjafton", Text = "Nëse dikush thotë «isha në Sallon», kjo nuk e përjashton: mund të jetë fajtori që gënjen. Përjashtimi vjen nga deklarata e një personi tjetër (R03, R04)." });
            c.Tutorial.Add(new TutorialStep { Title = "Hapi 1", Text = "Pyet Artën: «Kush ishte me ty në 21:00?» (kosto 2).", Fill = Question.Make(A, QType.Q4, 1) });
            c.Tutorial.Add(new TutorialStep { Title = "Hapi 2", Text = "Arta ishte me Blerimin dhe Elmazin. Tani pyete: «A ke qenë ndonjëherë në Bibliotekë?»", Fill = Question.Make(A, QType.Q2, -1, bi) });
            c.Tutorial.Add(new TutorialStep { Title = "Hapi 3", Text = "Arta nuk ka qenë kurrë në Bibliotekë, pra Blerimi dhe Elmazi që ishin me të në 21:00 dolën jashtë dyshimit. Pyet Blerimin: «A e ke parë Artën në 21:00?»", Fill = Question.Make(B, QType.Q3, 1, -1, A) });
            c.Tutorial.Add(new TutorialStep { Title = "Hapi 4", Text = "Blerimi e konfirmon. Pyete edhe: «A ke qenë ndonjëherë në Bibliotekë?»", Fill = Question.Make(B, QType.Q2, -1, bi) });
            c.Tutorial.Add(new TutorialStep { Title = "Mbeti një kandidate", Text = "Arta ishte me Blerimin, që nuk ka qenë kurrë në Bibliotekë: edhe ajo doli jashtë dyshimit. Mbetet vetëm Drita. Shtyp Akuzo.", Accuse = true });
            c.Prepare();
            return c;
        }

        public static CaseDefinition Card2()
        {
            var c = new CaseDefinition
            {
                Id = "karta2",
                CardNumber = 2,
                Level = 2,
                Seed = 202,
                Title = "Karta 2 — Sfida bazë",
                Kicker = "FAZA 2 · NIVELI 2",
                Objective = "Provo kush e kreu krimin në Korridor në 22:00 duke përdorur grafin e lëvizjes: askush nuk teleportohet.",
                Premise = "Mysafiri u gjet i vdekur në Korridorin e katit të parë, në 22:00. Shkallët janë rruga e vetme lart.",
                Hours = HoursL2,
                Budget = 12,
                Par = 6,
                Type = CaseType.Vrasja
            };
            Villa.ApplyRooms(c, RoomsL2, true);
            Villa.ApplySuspects(c, new[] { "A", "B", "D", "E", "F" });
            c.CrimeRoom = c.RoomIndex("KO");
            c.CrimeTime = 2;
            c.Culprit = c.SuspectIndex("E");
            c.Truth = new[]
            {
                Villa.Path(c, "KU", "KU", "DN", "DN"),
                Villa.Path(c, "KU", "HO", "SA", "SA"),
                Villa.Path(c, "SA", "SA", "HO", "HO"),
                Villa.Path(c, "HO", "SH", "KO", "KO"),
                Villa.Path(c, "KU", "HO", "HO", "SA")
            };
            c.Cover = Villa.Path(c, "HO", "HO", "SA", "SA");
            BaseRules(c);
            c.Rules.Add("Lëvizja vetëm mes dhomave të lidhura, një kalim për orë; qëndrimi në vend lejohet (R02).");
            c.Rules.Add("Lidhjet: " + Villa.EdgeList(c) + ".");
            c.Prepare();
            return c;
        }

        public static CaseDefinition Card3()
        {
            var c = new CaseDefinition
            {
                Id = "karta3",
                CardNumber = 3,
                Level = 3,
                Seed = 303,
                Title = "Karta 3 — Sfida e avancuar",
                Kicker = "FAZA 3 · NIVELI 3",
                Objective = "Akuzo me provë pa u mashtruar nga gabimi i pavullnetshëm i një dëshmitari të ndershëm.",
                Premise = "Një dorëshkrim i vjetër u zhduk nga Biblioteka dhe kujdestari u gjet i vdekur aty, në 22:00.",
                Hours = HoursL3,
                Budget = 11,
                Par = 9,
                Type = CaseType.Vrasja,
                ErrorsAllowed = 1
            };
            Villa.ApplyRooms(c, RoomsL2, true);
            Villa.ApplySuspects(c, new[] { "A", "B", "D", "E", "F" });
            c.CrimeRoom = c.RoomIndex("BI");
            c.CrimeTime = 2;
            c.Culprit = c.SuspectIndex("E");
            c.Truth = new[]
            {
                Villa.Path(c, "HO", "SH", "KO", "KO", "SH"),
                Villa.Path(c, "HO", "SH", "SH", "HO", "HO"),
                Villa.Path(c, "SH", "SH", "HO", "KU", "KU"),
                Villa.Path(c, "SA", "SA", "BI", "BI", "SA"),
                Villa.Path(c, "KO", "SH", "KO", "KO", "KO")
            };
            c.Cover = Villa.Path(c, "SA", "SA", "SA", "SA", "SA");
            c.Mistakes.Add(new MistakeSpec { Suspect = c.SuspectIndex("D"), Type = QType.Q1, Time = 1, WrongRoom = c.RoomIndex("KU") });
            BaseRules(c);
            c.Rules.Add("Lëvizja vetëm mes dhomave të lidhura, një kalim për orë (R02). Lidhjet: " + Villa.EdgeList(c) + ".");
            c.Rules.Add("Një dëshmitar i ndershëm gabon pa dashje në një përgjigje të vetme (R05): një kundërshtim i vetëm nuk mjafton, duhen dy të pavarura.");
            c.Prepare();
            return c;
        }

        public static CaseDefinition Card4()
        {
            var c = new CaseDefinition
            {
                Id = "karta4",
                CardNumber = 4,
                Level = 4,
                Seed = 404,
                Title = "Karta 4 — Kalimi i fshehtë",
                Kicker = "FAZA 4 · NIVELI 4",
                Objective = "Provo kush e mori dosjen nga Zyra dhe në cilën orë. Dosja lëviz vetëm bashkë me atë që e mori.",
                Premise = "Dosja e kontratave u zhduk nga Zyra e katit të parë gjatë mbrëmjes. Ora nuk dihet. Në Bibliotekë, rafti i vjetër duket i lëvizur.",
                Hours = HoursL4,
                Budget = 16,
                Par = 14,
                Type = CaseType.Vjedhja,
                ItemName = "dosja",
                ItemAcc = "dosjen"
            };
            Villa.ApplyRooms(c, RoomsL4, true);
            Villa.ApplySuspects(c, new[] { "A", "B", "D", "E", "F", "G" });
            Villa.SetSecretEdge(c, "BI", "ZY");
            c.CrimeRoom = c.RoomIndex("ZY");
            c.CrimeTime = 2;
            c.Culprit = c.SuspectIndex("F");
            c.Truth = new[]
            {
                Villa.Path(c, "KU", "HO", "SA", "SA", "HO"),
                Villa.Path(c, "DN", "SA", "SA", "SA", "DN"),
                Villa.Path(c, "HO", "SH", "KO", "KO", "SH"),
                Villa.Path(c, "SA", "BI", "SA", "HO", "SH"),
                Villa.Path(c, "SA", "BI", "ZY", "KO", "SH"),
                Villa.Path(c, "ZY", "ZY", "KO", "SH", "HO")
            };
            c.Cover = Villa.Path(c, "SA", "SA", "SA", "SA", "SA");
            c.PhysicalClues.Add(new PhysicalClue
            {
                Room = c.RoomIndex("BI"),
                Title = "Rafti i rrëshqitshëm",
                Text = "Pas librit të rëndë hapet një kalim i ngushtë drejt Zyrës. Lidhja Biblioteka–Zyra ekziston.",
                RevealsSecret = true
            });
            BaseRules(c);
            c.Rules.Add("Lëvizja vetëm mes dhomave të lidhura, një kalim për orë (R02). Lidhjet e njohura: " + Villa.EdgeList(c) + ".");
            c.Rules.Add("Lloji: Vjedhja — dihet sendi (dosja) dhe dhoma e tij (Zyra); duhet të provosh kush e mori dhe në cilën orë (R09).");
            c.Rules.Add("Sendi lëviz vetëm bashkë me një person (R10): para vjedhjes dosja është në Zyrë, pas saj është gjithmonë me hajnin.");
            c.Rules.Add("Pyetja Q6 «A e ke parë dosjen në orën X?» kushton 1 dhe tregon nëse dikush ishte në të njëjtën dhomë me dosjen.");
            c.Rules.Add("Prova fizike mund të zbulojë kalimin e fshehtë Biblioteka–Zyra; pastaj lëvizjet rillogariten (R06). Kërko dhomën me butonin «Kërko» (kosto 1).");
            c.Prepare();
            return c;
        }
    }
}
