using System.Collections.Generic;
using Alibi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Alibi.Unity
{
    public sealed class GameManager : MonoBehaviour
    {
        const float BarH = 76f;
        const float RightW = 700f;

        public GameSession Session { get; private set; }
        public Question Draft = new Question();
        public AskOutcome LastOutcome;
        public int AccPerson = -1, AccRoom = -1, AccTime = -1;
        public int TutorialIndex;

        CaseDefinition caseDef;
        CameraRig rig;
        VillaView villa;
        ScreenLabels labels;
        PropagationAnimator animator;
        Canvas canvas;
        RectTransform overlayRoot, rightContent, tabsRow, villaFrame, roomCardHost, hudHost, barHost;
        ScrollRect rightScroll;
        DeductionBoardView board;
        TimelineController timeline;
        InterrogationPanel interrogation;
        GamePanels panels;
        int currentTime;
        int selectedRoom = -1;
        int tab;
        bool tutorialVisible;

        void Start()
        {
            caseDef = AlibiFlow.PendingCase ?? LevelCards.Card(AlibiFlow.NextCard());
            AlibiFlow.PendingCase = null;
            if (caseDef.PathRooms == null) caseDef.Prepare();
            tutorialVisible = caseDef.Tutorial.Count > 0 && AlibiFlow.TutorialEnabled;
            BuildWorld();
            BuildUI();
            StartSession(false);
            ShowBrief();
        }

        void BuildWorld()
        {
            villa = MapBuilder.Build(transform, caseDef);
            SceneSetup.Lights(transform, villa.Bounds);
            rig = CameraRig.Create(transform, Theme.Night);
            rig.Target = villa.Bounds;
            villa.Rig = rig;
            villa.RoomClicked = OnRoomClicked;
            villa.BackgroundClicked = () => OnRoomClicked(-1);
            villa.SuspectClicked = s =>
            {
                tab = 0;
                SelectSuspect(s);
            };
            AlibiFlow.DisableForeignCamerasAndLights(transform);
            animator = gameObject.AddComponent<PropagationAnimator>();
        }

        void BuildUI()
        {
            canvas = AlibiFlow.CreateCanvas(transform, "GameCanvas", 10);
            var root = (RectTransform)canvas.transform;

            var labelLayer = UI.Rect(root, "VillaLabels");
            UI.Stretch(labelLayer, 0, 0, 0, 0);

            var left = UI.Rect(root, "LeftColumn");
            left.anchorMin = Vector2.zero;
            left.anchorMax = Vector2.one;
            left.offsetMin = new Vector2(16, 16);
            left.offsetMax = new Vector2(-(RightW + 32), -(BarH + 12));
            float boardH = DeductionBoardView.PreferredHeight(caseDef);

            var boardRt = UI.Rect(left, "Board");
            UI.StretchBottom(boardRt, 0, 0, 0, boardH);
            board = gameObject.AddComponent<DeductionBoardView>();
            board.Animator = animator;
            board.OnHourClicked = t => SetTime(t);
            board.OnSuspectClicked = s =>
            {
                tab = 0;
                SelectSuspect(s);
            };

            var tlRt = UI.Rect(left, "Timeline");
            UI.StretchBottom(tlRt, 0, boardH + 12, 0, 56);
            timeline = gameObject.AddComponent<TimelineController>();
            timeline.Build(tlRt, caseDef);
            timeline.OnTimeChanged = t => SetTime(t);

            villaFrame = UI.Rect(left, "VillaFrame");
            UI.Stretch(villaFrame, 0, 0, 0, boardH + 12 + 56 + 12);
            UI.Border(villaFrame, Theme.Line, 14);
            hudHost = UI.Rect(villaFrame, "Hud");
            UI.Stretch(hudHost, 0, 0, 0, 0);
            roomCardHost = UI.Rect(villaFrame, "RoomCard");
            UI.PlaceRight(roomCardHost, 14, 14, 300, 200);
            BuildCameraControls();
            rig.Frame = villaFrame;

            labels = gameObject.AddComponent<ScreenLabels>();
            labels.View = villa;
            labels.Cam = rig.Cam;
            labels.Layer = labelLayer;
            labels.Clip = villaFrame;
            labels.Build();

            var right = UI.Rect(root, "RightColumn");
            right.anchorMin = new Vector2(1, 0);
            right.anchorMax = new Vector2(1, 1);
            right.pivot = new Vector2(1, 0.5f);
            right.offsetMin = new Vector2(-(RightW + 16), 16);
            right.offsetMax = new Vector2(-16, -(BarH + 12));
            tabsRow = UI.Rect(right, "Tabs");
            UI.StretchTop(tabsRow, 0, 0, 0, 44);
            rightScroll = UI.Scroll(right, "RightScroll", out rightContent);
            UI.Stretch((RectTransform)rightScroll.transform, 0, 54, 0, 0);

            barHost = UI.Rect(root, "TopBar");
            UI.StretchTop(barHost, 0, 0, 0, BarH);

            board.Build(boardRt, new GameSession(caseDef), villa);

            overlayRoot = UI.Rect(root, "Overlays");
            UI.Stretch(overlayRoot, 0, 0, 0, 0);

            interrogation = new InterrogationPanel(this);
            panels = new GamePanels(this);
        }

        void BuildCameraControls()
        {
            var host = UI.Rect(villaFrame, "CameraControls");
            host.anchorMin = host.anchorMax = new Vector2(0, 0);
            host.pivot = new Vector2(0, 0);
            host.anchoredPosition = new Vector2(14, 14);
            host.sizeDelta = new Vector2(420, 40);
            string[] labelsTxt = { "‹", "›", "+", "–", "Qendër" };
            float x = 0;
            for (int i = 0; i < labelsTxt.Length; i++)
            {
                int k = i;
                float w = i == 4 ? 86 : 40;
                var b = UI.Button(host, labelsTxt[i], BtnStyle.Ghost, () =>
                {
                    if (k == 0) rig.RotateStep(-1);
                    else if (k == 1) rig.RotateStep(1);
                    else if (k == 2) rig.ZoomBy(1.15f);
                    else if (k == 3) rig.ZoomBy(1f / 1.15f);
                    else rig.ResetView();
                }, i == 4 ? 14 : 20, 8);
                b.Background.color = new Color(0.05f, 0.06f, 0.08f, 0.8f);
                UI.Place(b.Rect, x, 0, w, 40);
                x += w + 6;
            }
            var hint = UI.LabelAt(host, "Kliko një dhomë ose një figurë · rrota e miut zmadhon", Theme.Sans, 12, Theme.Muted, x + 6, 0, 360, 40, TextAnchor.MiddleLeft);
            hint.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        void StartSession(bool announce)
        {
            Session = new GameSession(caseDef);
            board.Rebind(Session);
            Draft = new Question();
            LastOutcome = null;
            AccPerson = AccRoom = AccTime = -1;
            TutorialIndex = 0;
            tab = 0;
            selectedRoom = -1;
            villa.SelectRoom(-1);
            currentTime = caseDef.CrimeTimeKnown ? caseDef.CrimeTime : 0;
            timeline.Select(currentTime, false);
            CloseOverlay();
            RefreshAll();
            if (announce) Panels.Toast(canvas.transform, "Rasti u rihap me të njëjtin seed (T11). Buxheti u kthye në " + caseDef.Budget + ".", Theme.Gold, 3.5f);
        }

        bool Truth { get { return Session.IsOver; } }

        public void RefreshAll()
        {
            BuildTopBar();
            BuildHud();
            board.Refresh(currentTime, Truth);
            BuildTabs();
            RebuildRight(false);
            BuildRoomCard();
        }

        void BuildTopBar()
        {
            UI.Clear(barHost);
            var bg = UI.Panel(barHost, "Bar", Theme.Bar, 0, true);
            UI.Stretch(bg.rectTransform, 0, 0, 0, 0);
            var line = UI.Panel(barHost, "Line", Theme.Line, 0, false);
            UI.StretchBottom(line.rectTransform, 0, 0, 0, 1);
            UI.LabelAt(barHost, "ALIBI", Theme.Serif, 34, Theme.Parchment, 24, 18, 130, 40);
            string title = caseDef.Title;
            string typeName = caseDef.Type == CaseType.Vrasja ? "Vrasja" : caseDef.Type == CaseType.Vjedhja ? "Vjedhja" : "Sabotimi";
            string sub = typeName + " · Vila · Niveli " + caseDef.Level + " · seed " + caseDef.Seed + (Session.IsOver ? " · rasti u mbyll" : "");
            var t1 = UI.LabelAt(barHost, title, Theme.SansSemi, 17, Theme.Text, 150, 14, 520, 24);
            t1.horizontalOverflow = HorizontalWrapMode.Overflow;
            var t2 = UI.LabelAt(barHost, sub, Theme.Mono, 13, Theme.Muted, 150, 42, 520, 20);
            t2.horizontalOverflow = HorizontalWrapMode.Overflow;
            float tw = Mathf.Max(UI.TextWidth(title, Theme.SansSemi, 17), UI.TextWidth(sub, Theme.Mono, 13));
            string crime = caseDef.Rooms[caseDef.CrimeRoom].Name + " · " + (caseDef.CrimeTimeKnown ? caseDef.Hours[caseDef.CrimeTime] : "ora ?");
            float pw = UI.TextWidth(crime, Theme.SansSemi, 15) + 34;
            var pill = UI.Card(barHost, "CrimePill", Theme.WithAlpha(Theme.Red, 0.12f), Theme.WithAlpha(Theme.Red, 0.7f), 18);
            UI.Place(pill.rectTransform, 150 + tw + 24, 20, pw, 36);
            var pt = UI.Label(pill.transform, crime, Theme.SansSemi, 15, Theme.RedText, TextAnchor.MiddleCenter);
            UI.Stretch(pt.rectTransform, 0, 0, 0, 0);
            pt.horizontalOverflow = HorizontalWrapMode.Overflow;

            float r = 24;
            var exit = UI.Button(barHost, Session.IsOver ? "Menyja" : "Dil nga rasti", BtnStyle.Ghost, ExitToMenu, 15, 10);
            UI.PlaceRight(exit.Rect, r, 16, 150, 44);
            r += 160;
            var accuse = UI.Button(barHost, "Akuzo", Session.IsOver ? BtnStyle.Disabled : BtnStyle.Danger, OpenAccusation, 16, 10);
            UI.PlaceRight(accuse.Rect, r, 16, 120, 44);
            accuse.SetInteractable(!Session.IsOver);
            r += 130;
            var howto = UI.Button(barHost, "Si luhet", BtnStyle.Ghost, ShowHowTo, 15, 10);
            UI.PlaceRight(howto.Rect, r, 16, 110, 44);
            r += 120;
            if (caseDef.Tutorial.Count > 0)
            {
                var tut = UI.Button(barHost, tutorialVisible ? "Udhëzuesi: ndezur" : "Udhëzuesi: fikur", tutorialVisible ? BtnStyle.ChipOn : BtnStyle.Ghost, ToggleTutorial, 14, 10);
                UI.PlaceRight(tut.Rect, r, 16, 170, 44);
                r += 180;
            }
            int remaining = Mathf.Max(0, Session.Remaining);
            var count = UI.LabelAt(barHost, remaining + " / " + caseDef.Budget, Theme.Mono, 17, Theme.Text, 0, 0, 80, BarH, TextAnchor.MiddleRight);
            UI.PlaceRight(count.rectTransform, r, 0, 80, BarH);
            r += 90;
            float pipW = 9, pipGap = 3;
            float pipsW = caseDef.Budget * (pipW + pipGap);
            for (int i = 0; i < caseDef.Budget; i++)
            {
                bool full = i < remaining;
                var pip = UI.Panel(barHost, "Pip", full ? Theme.Gold : Theme.Line, 3, false);
                UI.PlaceRight(pip.rectTransform, r + (caseDef.Budget - 1 - i) * (pipW + pipGap), 27, pipW, 22);
            }
            r += pipsW + 8;
            var lbl = UI.LabelAt(barHost, "BUXHETI", Theme.Mono, 13, Theme.Muted, 0, 0, 90, BarH, TextAnchor.MiddleRight);
            UI.PlaceRight(lbl.rectTransform, r, 0, 90, BarH);
        }

        void BuildHud()
        {
            UI.Clear(hudHost);
            var chip = UI.Card(hudHost, "HudTitle", new Color(0.05f, 0.06f, 0.08f, 0.85f), Theme.Line, 10);
            string title = Truth ? "E VËRTETA E ZBULUAR" : (caseDef.Level >= 2 ? "VILA · PËRDHESA DHE KATI I PARË" : "VILA · PËRDHESA");
            string sub = Truth ? "Pozicionet reale në " + caseDef.Hours[currentTime] : "Çfarë dihet për " + caseDef.Hours[currentTime];
            float w = Mathf.Max(UI.TextWidth(title, Theme.Mono, 13), UI.TextWidth(sub, Theme.Sans, 14)) + 30;
            UI.Place(chip.rectTransform, 14, 14, w, 58);
            UI.LabelAt(chip.transform, title, Theme.Mono, 13, Theme.Gold, 14, 10, w, 18).horizontalOverflow = HorizontalWrapMode.Overflow;
            UI.LabelAt(chip.transform, sub, Theme.Sans, 14, Theme.Text2, 14, 31, w, 20).horizontalOverflow = HorizontalWrapMode.Overflow;

            var legend = UI.Card(hudHost, "Legend", new Color(0.05f, 0.06f, 0.08f, 0.85f), Theme.Line, 10);
            legend.rectTransform.anchorMin = legend.rectTransform.anchorMax = new Vector2(1, 0);
            legend.rectTransform.pivot = new Vector2(1, 0);
            legend.rectTransform.anchoredPosition = new Vector2(-14, 14);
            legend.rectTransform.sizeDelta = new Vector2(330, 74);
            string legendText = "Figurë e plotë: pozicion i konfirmuar\nFigurë e tejdukshme me unazë: ende e mundshme\nPa figurë: e përjashtuar";
            if (Truth) legendText = "Figurat tregojnë të vërtetën për orën e zgjedhur.\nE kuqja: vendi i krimit.";
            var lt = UI.Label(legend.transform, legendText, Theme.Sans, 12, Theme.Text2, TextAnchor.MiddleLeft);
            UI.Stretch(lt.rectTransform, 12, 6, 12, 6);
        }

        void BuildRoomCard()
        {
            UI.Clear(roomCardHost);
            var card = UI.Card(roomCardHost, "Card", new Color(0.05f, 0.06f, 0.08f, 0.9f), Theme.Line2, 10);
            var root = card.transform;
            float iy = 12;
            if (selectedRoom < 0)
            {
                var t = UI.LabelAt(root, "Kliko një dhomë për të parë kush mund të ketë qenë aty në orën e zgjedhur. Kliko një figurë për ta marrë në pyetje.", Theme.Sans, 13, Theme.Text2, 14, iy, 272, 20);
                iy += UI.FitHeight(t, 272) + 12;
            }
            else
            {
                UI.LabelAt(root, caseDef.Rooms[selectedRoom].Name + " · " + caseDef.Hours[currentTime], Theme.SansBold, 15, Theme.Text, 14, iy, 272, 22);
                iy += 28;
                var dm = Session.Domains;
                for (int s = 0; s < caseDef.N; s++)
                {
                    string mark;
                    Color col;
                    if (Truth)
                    {
                        bool here = caseDef.Truth[s][currentTime] == selectedRoom;
                        mark = here ? "ishte këtu" : "nuk ishte këtu";
                        col = here ? Theme.GoldText : Theme.Muted;
                    }
                    else if (dm.Possible[s, currentTime, selectedRoom] && dm.Count[s, currentTime] == 1)
                    {
                        mark = "ishte këtu (e konfirmuar)";
                        col = Theme.GoldText;
                    }
                    else if (dm.Possible[s, currentTime, selectedRoom])
                    {
                        mark = "mund të ketë qenë këtu";
                        col = Theme.Text;
                    }
                    else
                    {
                        mark = "nuk ishte këtu";
                        col = Theme.Muted;
                    }
                    UI.LabelAt(root, caseDef.Suspects[s].Name, Theme.SansSemi, 13, col, 14, iy, 80, 20);
                    UI.LabelAt(root, mark, Theme.Sans, 13, col, 96, iy, 190, 20);
                    iy += 21;
                }
                if (caseDef.UseGraph)
                {
                    var links = new List<string>();
                    for (int r = 0; r < caseDef.RoomCount; r++)
                    {
                        if (r == selectedRoom) continue;
                        if (caseDef.CanMove(selectedRoom, r) && selectedRoom != r) links.Add(caseDef.Rooms[r].Name);
                    }
                    var lt = UI.LabelAt(root, "Lidhet me: " + (links.Count == 0 ? "—" : string.Join(", ", links.ToArray())), Theme.Sans, 12, Theme.Muted, 14, iy + 4, 272, 20);
                    iy += UI.FitHeight(lt, 272) + 6;
                }
                if (caseDef.HasItem && Session.Eval.ItemPossible != null)
                {
                    bool here = Session.Eval.ItemPossible[currentTime, selectedRoom];
                    var it = UI.LabelAt(root, here ? "Dosja mund të jetë këtu në " + caseDef.Hours[currentTime] + "." : "Dosja nuk është këtu në " + caseDef.Hours[currentTime] + ".", Theme.Sans, 12, here ? Theme.Gold : Theme.Muted, 14, iy + 4, 272, 20);
                    iy += UI.FitHeight(it, 272) + 6;
                }
                if (!Truth && caseDef.PhysicalClues.Count > 0 && Session.Remaining > 0)
                {
                    bool already = false;
                    foreach (var pc in Session.FoundClues) if (pc.Room == selectedRoom) already = true;
                    foreach (var pc in caseDef.PhysicalClues) if (pc.Room == selectedRoom && pc.Found) already = true;
                    var search = UI.Button(root, already ? "E kërkuar" : "Kërko dhomën · −1", already || Session.IsOver ? BtnStyle.Disabled : BtnStyle.Chip, () => SearchSelectedRoom(), 14, 8);
                    UI.Place(search.Rect, 14, iy + 4, 272, 40);
                    search.SetInteractable(!already && !Session.IsOver && Session.Remaining > 0);
                    iy += 50;
                }
                iy += 8;
            }
            UI.Stretch(card.rectTransform, 0, 0, 0, 0);
            roomCardHost.sizeDelta = new Vector2(300, iy);
        }

        public void SearchSelectedRoom()
        {
            if (selectedRoom < 0) return;
            var outcome = Session.SearchRoom(selectedRoom);
            if (!outcome.Accepted)
            {
                Panels.Toast(canvas.transform, outcome.Message, Theme.Red, 3f);
                if (outcome.BudgetExhausted) OpenAccusation();
                RefreshAll();
                return;
            }
            LastOutcome = outcome;
            board.Refresh(currentTime, Truth);
            RefreshAll();
            Panels.Toast(canvas.transform, outcome.Message, caseDef.SecretEdgeActive ? Theme.Gold : Theme.Text, 4f);
            if (outcome.BudgetExhausted)
            {
                Panels.Toast(canvas.transform, "Buxheti mbaroi. Duhet të akuzosh.", Theme.Gold, 3.5f);
                OpenAccusation();
            }
        }

        void BuildTabs()
        {
            UI.Clear(tabsRow);
            string first = Session.IsOver ? "Rezultati" : "Marrja në pyetje";
            var a = UI.Button(tabsRow, first, tab == 0 ? BtnStyle.TabOn : BtnStyle.Tab, () => SetTab(0), 16, 10);
            UI.Place(a.Rect, 0, 0, 220, 44);
            int flagged = Session.Flagged.Count;
            string second = "Dosja e dëshmive (" + Session.Statements.Count + ")" + (flagged > 0 && !Session.IsOver ? "  <color=#F09A8F>!</color>" : "");
            var b = UI.Button(tabsRow, second, tab == 1 ? BtnStyle.TabOn : BtnStyle.Tab, () => SetTab(1), 16, 10);
            UI.Place(b.Rect, 228, 0, 280, 44);
        }

        void SetTab(int t)
        {
            tab = t;
            BuildTabs();
            RebuildRight(false);
            rightScroll.verticalNormalizedPosition = 1f;
        }

        void RebuildRight(bool scrollToEnd)
        {
            UI.Clear(rightContent);
            float y = 0, w = RightW, gap = 12;
            if (tab == 0)
            {
                if (Session.IsOver)
                {
                    y += panels.Result(rightContent, y, w) + gap;
                }
                else
                {
                    float h = panels.Coach(rightContent, y, w);
                    if (h > 0) y += h + gap;
                    y += interrogation.Build(rightContent, y, w) + gap;
                    h = panels.LastAnswer(rightContent, y, w);
                    if (h > 0) y += h + gap;
                }
            }
            else
            {
                y += panels.Dossier(rightContent, y, w) + gap;
            }
            rightContent.sizeDelta = new Vector2(0, y + 8);
            if (scrollToEnd) rightScroll.verticalNormalizedPosition = 0f;
        }

        public Question TutorialHint
        {
            get
            {
                var step = CurrentTutorialStep;
                return step != null ? step.Fill : null;
            }
        }

        public TutorialStep CurrentTutorialStep
        {
            get
            {
                if (!tutorialVisible || Session.IsOver || TutorialIndex >= caseDef.Tutorial.Count) return null;
                return caseDef.Tutorial[TutorialIndex];
            }
        }

        public void FillTutorial()
        {
            var step = CurrentTutorialStep;
            if (step == null || step.Fill == null) return;
            Draft = step.Fill.Clone();
            if (step.Fill.Time >= 0) SetTime(step.Fill.Time);
            RebuildRight(false);
        }

        public void NextTutorial()
        {
            TutorialIndex++;
            RebuildRight(false);
        }

        public void ToggleTutorial()
        {
            tutorialVisible = !tutorialVisible;
            AlibiFlow.TutorialEnabled = tutorialVisible;
            BuildTopBar();
            RebuildRight(false);
        }

        public void SelectSuspect(int s)
        {
            if (Session.IsOver) return;
            Draft.Suspect = s;
            if (Draft.Other == s) Draft.Other = -1;
            BuildTabs();
            RebuildRight(false);
        }

        public void SelectType(QType t)
        {
            Draft.HasType = true;
            Draft.Type = t;
            RebuildRight(false);
        }

        public void SelectOther(int y)
        {
            Draft.Other = y;
            RebuildRight(false);
        }

        public void SelectTime(int t)
        {
            Draft.Time = t;
            SetTime(t);
            RebuildRight(false);
        }

        public void SelectRoomParam(int r)
        {
            Draft.Room = r;
            RebuildRight(false);
        }

        public string ComposeText()
        {
            if (Draft.Suspect < 0) return "Zgjidh të dyshuarin dhe pyetjen.";
            var sus = caseDef.Suspects[Draft.Suspect];
            if (!Draft.HasType) return "«" + sus.Def + ", …»";
            string phrase = QuestionEngine.Phrase(caseDef, Draft);
            return "«" + sus.Def + ", " + char.ToLowerInvariant(phrase[0]) + phrase.Substring(1) + "»";
        }

        public bool CanAsk(out string message)
        {
            if (Session.IsOver)
            {
                message = "Rasti u mbyll.";
                return false;
            }
            if (Session.Remaining <= 0 || (Session.Phase == Phase.Accusing && Session.ForcedAccusation))
            {
                message = "Buxheti mbaroi. Duhet të akuzosh.";
                return false;
            }
            var missing = QuestionEngine.Missing(Draft);
            if (missing.Count > 0)
            {
                message = "Mungon: " + string.Join(", ", missing.ToArray()) + ". Dërgimi është i bllokuar dhe buxheti nuk preket (D10).";
                return false;
            }
            var invalid = QuestionEngine.InvalidReason(caseDef, Draft);
            if (invalid != null)
            {
                message = invalid;
                return false;
            }
            var budget = Session.BudgetMessage(Draft.Type);
            if (budget != null)
            {
                message = budget;
                return false;
            }
            int dup = Session.DuplicateOf(Draft);
            int cost = QuestionEngine.Cost(Draft.Type);
            message = dup >= 0
                ? "Kjo pyetje u bë te #" + dup + " dhe nuk sjell informacion të ri. Kostoja " + cost + "."
                : "Gati · kostoja " + cost + " · mbeten " + Session.Remaining;
            return true;
        }

        public void Ask()
        {
            string message;
            if (!CanAsk(out message))
            {
                Panels.Toast(canvas.transform, message, Theme.Red, 3f);
                return;
            }
            var asked = Draft.Clone();
            var outcome = Session.Ask(asked);
            if (!outcome.Accepted)
            {
                Panels.Toast(canvas.transform, outcome.Message, Theme.Red, 3f);
                if (outcome.BudgetExhausted) OpenAccusation();
                RefreshAll();
                return;
            }
            LastOutcome = outcome;
            var step = CurrentTutorialStep;
            if (step != null && step.Fill != null && step.Fill.Key == asked.Key) TutorialIndex++;
            if (asked.Time >= 0) currentTime = asked.Time;
            timeline.Select(currentTime, false);
            BuildTopBar();
            BuildHud();
            board.Refresh(currentTime, Truth);
            BuildTabs();
            RebuildRight(true);
            BuildRoomCard();
            if (outcome.Conflict != null) Panels.Toast(canvas.transform, "Kontradiktë: disa deklarata nuk mund të jenë të gjitha të vërteta.", Theme.Red, 3f);
            if (outcome.BudgetExhausted)
            {
                Panels.Toast(canvas.transform, "Buxheti mbaroi. Duhet të akuzosh.", Theme.Gold, 3.5f);
                OpenAccusation();
            }
        }

        public void SetTime(int t)
        {
            currentTime = Mathf.Clamp(t, 0, caseDef.T - 1);
            timeline.Select(currentTime, false);
            board.Refresh(currentTime, Truth);
            BuildHud();
            BuildRoomCard();
        }

        void OnRoomClicked(int r)
        {
            selectedRoom = selectedRoom == r ? -1 : r;
            villa.SelectRoom(selectedRoom);
            BuildRoomCard();
        }

        void ShowOverlay(int kind)
        {
            UI.Clear(overlayRoot);
            var shade = Panels.Overlay(overlayRoot, "Shade");
            if (kind == 1) panels.Brief(shade);
            else if (kind == 2) panels.Accusation(shade);
            else if (kind == 3) Panels.HowTo(shade, CloseOverlay);
        }

        public void CloseOverlay()
        {
            UI.Clear(overlayRoot);
        }

        void ShowBrief()
        {
            ShowOverlay(1);
        }

        void ShowHowTo()
        {
            ShowOverlay(3);
        }

        public void EnterCase()
        {
            Session.Begin();
            CloseOverlay();
            RefreshAll();
        }

        public void OpenAccusation()
        {
            if (Session.IsOver) return;
            Session.StartAccusation();
            if (AccTime < 0 && caseDef.CrimeTimeKnown) AccTime = -1;
            ShowOverlay(2);
        }

        public void CancelAccusation()
        {
            Session.CancelAccusation();
            CloseOverlay();
        }

        public void SetAccusation(int person, int room, int time)
        {
            AccPerson = person;
            AccRoom = room;
            AccTime = time;
            ShowOverlay(2);
        }

        public void SubmitAccusation()
        {
            var o = Session.Accuse(AccPerson, AccRoom, AccTime);
            if (!o.Accepted)
            {
                Panels.Toast(canvas.transform, o.Message, Theme.Gold, 3f);
                ShowOverlay(2);
                return;
            }
            CloseOverlay();
            if (o.Verdict.Won && caseDef.CardNumber > 0) AlibiFlow.RecordWin(caseDef.CardNumber, Session.Stars);
            currentTime = caseDef.CrimeTime;
            tab = 0;
            selectedRoom = o.Verdict.Won ? caseDef.CrimeRoom : caseDef.Truth[AccPerson][caseDef.CrimeTime];
            villa.SelectRoom(selectedRoom);
            timeline.Select(currentTime, false);
            RefreshAll();
            rightScroll.verticalNormalizedPosition = 1f;
            Panels.Toast(canvas.transform, o.Verdict.Won ? "Rasti u zgjidh." : "LOST: akuza ra. Shiko kufizimin që e përjashtonte.", o.Verdict.Won ? Theme.Gold : Theme.Red, 3.5f);
        }

        public void Restart()
        {
            StartSession(true);
            Session.Begin();
            RefreshAll();
        }

        public void AfterResult()
        {
            if (Session.Phase == Phase.Won && caseDef.CardNumber > 0 && caseDef.CardNumber < 4)
            {
                AlibiFlow.StartCase(LevelCards.Card(caseDef.CardNumber + 1));
                return;
            }
            string note = Session.Phase == Phase.Won ? caseDef.Title + " u zgjidh me " + Session.Stars + (Session.Stars == 1 ? " yll." : " yje.") : null;
            AlibiFlow.ToMenu(note);
        }

        public void ExitToMenu()
        {
            string note = null;
            if (!Session.IsOver && Session.Statements.Count > 0)
            {
                Session.Abandon();
                note = "Rasti u braktis pa akuzë (ABANDONED): pa yje. Mund ta rifillosh me të njëjtin seed (" + caseDef.Seed + ").";
            }
            else if (Session.IsOver && Session.Phase == Phase.Won)
            {
                note = caseDef.Title + " u zgjidh me " + Session.Stars + (Session.Stars == 1 ? " yll." : " yje.");
            }
            AlibiFlow.ToMenu(note);
        }
    }
}
