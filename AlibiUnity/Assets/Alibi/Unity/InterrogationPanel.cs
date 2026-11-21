using System.Collections.Generic;
using Alibi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Alibi.Unity
{
    public sealed class InterrogationPanel
    {
        readonly GameManager gm;

        public InterrogationPanel(GameManager manager)
        {
            gm = manager;
        }

        // Paleta e pergamenës — e njëjtë me kartën e Udhëzuesit (Panels.Coach)
        static Color Accent { get { return Theme.Hex("#7A5A22"); } }
        static Color Fill { get { return new Color(1f, 1f, 1f, 0.35f); } }
        static Color Tint { get { return new Color(0.16f, 0.13f, 0.09f, 0.07f); } }
        static Color Edge { get { return new Color(0.16f, 0.13f, 0.09f, 0.22f); } }

        static void Hint(UIButton b, bool on)
        {
            if (on) b.Border.color = Theme.Hex("#A3352C");
        }

        // Shënon zgjedhjen pa e errësuar butonin, që teksti të mbetet i lexueshëm mbi pergamenë.
        static void Mark(UIButton b, bool on)
        {
            if (!on) return;
            b.Background.color = new Color(0.16f, 0.13f, 0.09f, 0.14f);
            b.Border.color = Accent;
        }

        static string QIcon(QType t)
        {
            switch (t)
            {
                case QType.Q1: return "?";
                case QType.Q2: return "=";
                case QType.Q3: return "#";
                case QType.Q4: return "!";
                case QType.Q5: return "X";
                default: return "~";
            }
        }

        public float Build(RectTransform content, float y, float w)
        {
            var s = gm.Session;
            var c = s.Case;
            var q = gm.Draft;
            var hint = gm.TutorialHint;
            bool locked = s.IsOver || s.Remaining <= 0 || (s.Phase == Phase.Accusing && s.ForcedAccusation);
            var card = Panels.ParchmentCard(content, "Interrogation");
            var root = card.transform;
            float pad = 18, iw = w - pad * 2, iy = 14;

            // Header with detective badge
            var headerBg = UI.Panel(root, "HeaderBg", Fill, 10, false);
            UI.Border(headerBg.transform, Edge, 10);
            UI.Place(headerBg.rectTransform, pad, iy, iw, 56);
            var badge = UI.Panel(headerBg.transform, "Badge", Tint, 20, false);
            UI.Place(badge.rectTransform, 14, 12, 32, 32);
            UI.Label(badge.transform, "✧", Theme.Serif, 20, Accent, TextAnchor.MiddleCenter);
            UI.Stretch(((Text)badge.GetComponentInChildren<Text>()).rectTransform, 0, 0, 0, 0);
            UI.LabelAt(headerBg.transform, "Dhoma e marrjes në pyetje", Theme.Serif, 24, Theme.ParchInk, 56, 8, iw - 70, 28);
            UI.LabelAt(headerBg.transform, "Zgjidh · pyet · zbulo", Theme.Sans, 12, Theme.ParchMuted, 56, 34, iw - 70, 18);
            iy += 68;

            // ===== STEP 1: SUSPECT SELECTION =====
            iy += StepHeader(root, "1", "ZGJIDH TE DYSHUARIN", pad, iy, iw);

            if (q.Suspect >= 0)
            {
                // I zgjedhuri tregohet vetëm; lista e emrave fshihet derisa të shtypet «Ndrysho».
                var sus = c.Suspects[q.Suspect];
                var selCard = UI.Panel(root, "SelSuspect", Fill, 10, false);
                UI.Border(selCard.transform, Theme.WithAlpha(Theme.ParchInk, 0.3f), 10);
                UI.Place(selCard.rectTransform, pad, iy, iw, 64);
                UI.Avatar(selCard.transform, sus, 42, 12, 11);
                UI.LabelAt(selCard.transform, sus.Name, Theme.SansBold, 18, Theme.ParchInk, 66, 8, iw - 160, 24);
                bool cleared = s.IsCleared(q.Suspect);
                UI.LabelAt(selCard.transform, sus.Role + " · " + (cleared ? "<color=#1F4E79>jashtë dyshimit</color>" : "<color=#7A5A22>i dyshuar</color>"), Theme.Sans, 13, Theme.ParchMuted, 66, 34, iw - 160, 20);
                var change = UI.Button(selCard.transform, "Ndrysho", BtnStyle.ParchmentGhost, () => gm.SelectSuspect(-1), 13, 6);
                UI.PlaceRight(change.Rect, 12, 18, 80, 28);
                change.SetInteractable(!locked);
                iy += 72;
            }
            else
            {
                // Suspect grid
                int cols = c.N <= 4 ? 2 : 3;
                float gap = 6;
                float cw = (iw - gap * (cols - 1)) / cols, ch = 52;
                for (int i = 0; i < c.N; i++)
                {
                    int si = i;
                    var sus = c.Suspects[i];
                    var b = UI.Button(root, null, BtnStyle.ParchmentGhost, () => gm.SelectSuspect(si), 16, 10);
                    UI.Place(b.Rect, pad + (i % cols) * (cw + gap), iy + (i / cols) * (ch + gap), cw, ch);
                    b.SetInteractable(!locked);
                    Hint(b, hint != null && hint.Suspect == i);
                    UI.Avatar(b.Rect, sus, 32, 8, 10);
                    var name = UI.LabelAt(b.Rect, sus.Name, Theme.SansSemi, 15, Theme.ParchInk, 48, 6, cw - 54, 20);
                    name.horizontalOverflow = HorizontalWrapMode.Overflow;
                    bool cleared = s.IsCleared(i);
                    var st = UI.LabelAt(b.Rect, cleared ? "i pastër" : "kandidat", Theme.Sans, 11, cleared ? Theme.Hex("#1F4E79") : Accent, 48, 27, cw - 54, 16);
                    st.horizontalOverflow = HorizontalWrapMode.Overflow;
                }
                iy += Mathf.CeilToInt(c.N / (float)cols) * (ch + gap) + 8;
            }

            // ===== STEP 2: QUESTION TYPE =====
            iy += StepHeader(root, "2", "ZGJIDH PYETJEN", pad, iy, iw);

            int qCols = 2;
            float qGap = 8;
            float qcw = (iw - qGap * (qCols - 1)) / qCols;
            int qi = 0;
            foreach (var type in QuestionEngine.TypesFor(c))
            {
                var qt = type;
                bool on = q.HasType && q.Type == type;
                int cost = QuestionEngine.Cost(type);
                bool tooCostly = !locked && cost > s.Remaining;
                var b = UI.Button(root, null, BtnStyle.ParchmentGhost, () => gm.SelectType(qt), 16, 9);
                float qx = pad + (qi % qCols) * (qcw + qGap);
                float qy = iy + (qi / qCols) * 76;
                UI.Place(b.Rect, qx, qy, qcw, 70);
                b.SetInteractable(!locked && !tooCostly);
                Mark(b, on);
                Hint(b, hint != null && hint.HasType && hint.Type == type && !on);

                // Icon circle
                var icon = UI.Panel(b.Rect, "Icon", on ? Theme.WithAlpha(Theme.ParchInk, 0.18f) : Tint, 16, false);
                UI.Place(icon.rectTransform, 10, 10, 32, 32);
                UI.Label(icon.transform, QIcon(type), Theme.SansBold, 16, on ? Accent : Theme.ParchMuted, TextAnchor.MiddleCenter);
                UI.Stretch(((Text)icon.GetComponentInChildren<Text>()).rectTransform, 0, 0, 0, 0);

                // Code label
                UI.LabelAt(b.Rect, QuestionEngine.Code(type), Theme.Mono, 11, Accent, 48, 8, qcw - 100, 16);
                // Question name
                UI.LabelAt(b.Rect, QuestionEngine.GenericLabel(type), Theme.SansSemi, 14, tooCostly ? Theme.Hex("#8E2C24") : Theme.ParchInk, 48, 24, qcw - 60, 20);
                // Cost badge
                var costBg = UI.Panel(b.Rect, "Cost", on ? Theme.WithAlpha(Theme.ParchInk, 0.16f) : Tint, 8, false);
                UI.PlaceRight(costBg.rectTransform, 8, 8, 44, 22);
                UI.Label(costBg.transform, "-" + cost, Theme.Mono, 13, cost == 2 ? Theme.Hex("#8E2C24") : Theme.ParchMuted, TextAnchor.MiddleCenter);
                UI.Stretch(((Text)costBg.GetComponentInChildren<Text>()).rectTransform, 0, 0, 0, 0);
                // Info line
                string info = tooCostly ? s.BudgetMessage(type) : QuestionEngine.Info(type);
                UI.LabelAt(b.Rect, info, Theme.Sans, 11, tooCostly ? Theme.Hex("#8E2C24") : Theme.ParchMuted, 48, 46, qcw - 60, 18);
                qi++;
            }
            iy += Mathf.CeilToInt(qi / (float)qCols) * 76 + 8;

            // ===== STEP 3: PARAMETERS =====
            if (q.HasType)
            {
                bool needsAny = QuestionEngine.NeedsOther(q.Type) || QuestionEngine.NeedsTime(q.Type) || QuestionEngine.NeedsRoom(q.Type);
                if (needsAny)
                {
                    iy += StepHeader(root, "3", "PLOTËSO PARAMETRIN", pad, iy, iw);

                    if (QuestionEngine.NeedsOther(q.Type))
                    {
                        var items = new List<KeyValuePair<string, int>>();
                        for (int i = 0; i < c.N; i++) if (i != q.Suspect) items.Add(new KeyValuePair<string, int>(c.Suspects[i].Name, i));
                        iy = ChipRow(root, "Personi Y", items, q.Other, hint != null ? hint.Other : -1, v => gm.SelectOther(v), pad, iy, iw, locked);
                    }
                    if (QuestionEngine.NeedsTime(q.Type))
                    {
                        var items = new List<KeyValuePair<string, int>>();
                        for (int t = 0; t < c.T; t++) items.Add(new KeyValuePair<string, int>(c.Hours[t], t));
                        iy = ChipRow(root, "Ora X", items, q.Time, hint != null ? hint.Time : -1, v => gm.SelectTime(v), pad, iy, iw, locked);
                    }
                    if (QuestionEngine.NeedsRoom(q.Type))
                    {
                        var items = new List<KeyValuePair<string, int>>();
                        for (int r = 0; r < c.RoomCount; r++) items.Add(new KeyValuePair<string, int>(c.Rooms[r].Name, r));
                        iy = ChipRow(root, "Dhoma R", items, q.Room, hint != null ? hint.Room : -1, v => gm.SelectRoomParam(v), pad, iy, iw, locked);
                    }
                }
            }
            else if (q.Suspect >= 0)
            {
                UI.LabelAt(root, "Zgjidh pyetjen për të parë parametrat.", Theme.Sans, 13, Theme.ParchMuted, pad, iy, iw, 22);
                iy += 28;
            }

            // ===== COMPOSE & ASK =====
            var box = UI.Panel(root, "Compose", Fill, 12, false);
            UI.Border(box.transform, Edge, 12);
            float bpad = 14;

            // Speech bubble indicator
            var bubble = UI.Panel(box.transform, "Bubble", Tint, 8, false);
            UI.Place(bubble.rectTransform, bpad, bpad, iw - bpad * 2, 0); // height set below

            var compose = UI.LabelAt(bubble.transform, gm.ComposeText(), Theme.SerifItalic, 16, Theme.ParchInk, 12, 10, iw - bpad * 2 - 24, 40);
            float composeH = UI.FitHeight(compose, iw - bpad * 2 - 24) + 20;
            UI.Place(bubble.rectTransform, bpad, bpad, iw - bpad * 2, composeH);

            string message;
            bool ready = gm.CanAsk(out message);
            float msgY = bpad + composeH + 8;
            var msg = UI.LabelAt(box.transform, message, Theme.Sans, 13, ready ? Theme.ParchMuted : Theme.Hex("#8E2C24"), bpad, msgY, iw - 200, 40);
            float msgH = UI.FitHeight(msg, iw - 200);

            float boxH = Mathf.Max(msgY + msgH + bpad, msgY + 52);
            var ask = UI.Button(box.transform, ready ? "Pyet · −" + QuestionEngine.Cost(q.Type) : "Pyet", ready ? BtnStyle.Parchment : BtnStyle.ParchmentGhost, gm.Ask, 17, 10);
            UI.PlaceRight(ask.Rect, 14, boxH - 54, 150, 42);
            ask.SetInteractable(ready);
            UI.Place(box.rectTransform, pad, iy, iw, boxH);
            iy += boxH + 18;

            UI.Place(card.rectTransform, 0, y, w, iy);
            return iy;
        }

        float StepHeader(Transform root, string num, string label, float pad, float iy, float iw)
        {
            var numBg = UI.Panel(root, "StepNum", Tint, 12, false);
            UI.Place(numBg.rectTransform, pad, iy, 24, 24);
            UI.Label(numBg.transform, num, Theme.Mono, 13, Accent, TextAnchor.MiddleCenter);
            UI.Stretch(((Text)numBg.GetComponentInChildren<Text>()).rectTransform, 0, 0, 0, 0);
            UI.LabelAt(root, label, Theme.Mono, 12, Theme.ParchMuted, pad + 32, iy + 3, iw - 32, 18);
            var line = UI.Panel(root, "StepLine", Edge, 0, false);
            UI.Place(line.rectTransform, pad + 32 + UI.TextWidth(label, Theme.Mono, 12) + 8, iy + 12, iw - 32 - UI.TextWidth(label, Theme.Mono, 12) - 8, 1);
            return 32;
        }

        float ChipRow(Transform root, string label, List<KeyValuePair<string, int>> items, int selected, int hinted, System.Action<int> pick, float pad, float iy, float iw, bool locked)
        {
            UI.LabelAt(root, label, Theme.Sans, 13, Theme.ParchMuted, pad, iy + 10, 90, 20);
            float x = pad + 94, rowY = iy, maxX = pad + iw;
            foreach (var kv in items)
            {
                int v = kv.Value;
                float bw = Mathf.Max(64, UI.TextWidth(kv.Key, Theme.SansSemi, 14) + 28);
                if (x + bw > maxX)
                {
                    x = pad + 94;
                    rowY += 44;
                }
                var b = UI.Button(root, kv.Key, BtnStyle.ParchmentGhost, () => pick(v), 14, 8);
                UI.Place(b.Rect, x, rowY, bw, 38);
                b.SetInteractable(!locked);
                Mark(b, selected == v);
                Hint(b, hinted == v && selected != v);
                x += bw + 6;
            }
            return rowY + 48;
        }
    }
}
