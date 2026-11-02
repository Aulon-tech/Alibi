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

        static void Hint(UIButton b, bool on)
        {
            if (on) b.Border.color = Theme.Gold;
        }

        public float Build(RectTransform content, float y, float w)
        {
            var s = gm.Session;
            var c = s.Case;
            var q = gm.Draft;
            var hint = gm.TutorialHint;
            bool locked = s.IsOver || s.Remaining <= 0 || (s.Phase == Phase.Accusing && s.ForcedAccusation);
            var card = UI.Card(content, "Interrogation", Theme.Panel, Theme.Line, 14);
            var root = card.transform;
            float pad = 18, iw = w - pad * 2, iy = 14;
            UI.LabelAt(root, "Marrja në pyetje", Theme.Serif, 30, Theme.Parchment, pad, iy, iw, 38);
            var tag = UI.LabelAt(root, "Hyrje e strukturuar · D10", Theme.Sans, 13, Theme.Muted, pad, iy + 10, iw, 20, TextAnchor.UpperRight);
            tag.horizontalOverflow = HorizontalWrapMode.Overflow;
            iy += 48;

            UI.SectionLabel(root, "1 · I DYSHUARI", pad, iy, iw);
            iy += 24;
            int cols = c.N <= 4 ? 2 : 3;
            float gap = 8;
            float cw = (iw - gap * (cols - 1)) / cols, ch = 60;
            for (int i = 0; i < c.N; i++)
            {
                int si = i;
                var sus = c.Suspects[i];
                bool on = q.Suspect == i;
                var b = UI.Button(root, null, on ? BtnStyle.ChipOn : BtnStyle.Chip, () => gm.SelectSuspect(si), 16, 10);
                UI.Place(b.Rect, pad + (i % cols) * (cw + gap), iy + (i / cols) * (ch + gap), cw, ch);
                b.SetInteractable(!locked);
                Hint(b, hint != null && hint.Suspect == i && !on);
                UI.Avatar(b.Rect, sus, 36, 10, 12);
                var name = UI.LabelAt(b.Rect, sus.Name, Theme.SansSemi, 16, Theme.Text, 56, 9, cw - 60, 22);
                name.horizontalOverflow = HorizontalWrapMode.Overflow;
                bool cleared = s.IsCleared(i);
                var st = UI.LabelAt(b.Rect, sus.Role + " · " + (cleared ? "<color=#A9C3E6>jashtë dyshimit</color>" : "<color=#E2C27A>kandidat</color>"), Theme.Sans, 12, Theme.Muted, 56, 31, cw - 60, 20);
                st.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            iy += Mathf.CeilToInt(c.N / (float)cols) * (ch + gap) + 6;

            UI.SectionLabel(root, "2 · PYETJA", pad, iy, iw);
            iy += 24;
            foreach (var type in QuestionEngine.All)
            {
                var qt = type;
                bool on = q.HasType && q.Type == type;
                int cost = QuestionEngine.Cost(type);
                bool tooCostly = !locked && cost > s.Remaining;
                var b = UI.Button(root, null, on ? BtnStyle.ChipOn : BtnStyle.Chip, () => gm.SelectType(qt), 16, 9);
                UI.Place(b.Rect, pad, iy, iw, 46);
                b.SetInteractable(!locked && !tooCostly);
                Hint(b, hint != null && hint.HasType && hint.Type == type && !on);
                var code = UI.Panel(b.Rect, "Code", Theme.Line, 5, false);
                UI.Place(code.rectTransform, 10, 12, 34, 22);
                var codeText = UI.Label(code.transform, QuestionEngine.Code(type), Theme.Mono, 13, Theme.Gold, TextAnchor.MiddleCenter);
                UI.Stretch(codeText.rectTransform, 0, 0, 0, 0);
                UI.LabelAt(b.Rect, QuestionEngine.GenericLabel(type), Theme.SansSemi, 15, Theme.Text, 54, 5, iw - 150, 22);
                UI.LabelAt(b.Rect, tooCostly ? s.BudgetMessage(type) : QuestionEngine.Info(type), Theme.Sans, 12, tooCostly ? Theme.RedText : Theme.Muted, 54, 25, iw - 130, 18);
                UI.LabelAt(b.Rect, cost == 1 ? "1 pyetje" : "2 pyetje", Theme.Mono, 13, Theme.Text2, iw - 96, 0, 86, 46, TextAnchor.MiddleRight);
                iy += 52;
            }
            iy += 4;

            UI.SectionLabel(root, "3 · PARAMETRI", pad, iy, iw);
            iy += 26;
            if (!q.HasType)
            {
                UI.LabelAt(root, "Zgjidh fillimisht një pyetje; parametri i panevojshëm fshihet sipas llojit.", Theme.Sans, 14, Theme.Muted, pad, iy, iw, 22);
                iy += 30;
            }
            else
            {
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

            var box = UI.Card(root, "Compose", Theme.Hex("#0F1218"), Theme.Line, 10);
            var compose = UI.LabelAt(box.transform, gm.ComposeText(), Theme.Mono, 15, Theme.Parchment, 14, 12, iw - 28, 40);
            float composeH = UI.FitHeight(compose, iw - 28);
            string message;
            bool ready = gm.CanAsk(out message);
            var msg = UI.LabelAt(box.transform, message, Theme.Sans, 13, ready ? Theme.Muted : Theme.Hex("#E2C27A"), 14, 20 + composeH, iw - 210, 40);
            float msgH = UI.FitHeight(msg, iw - 210);
            var ask = UI.Button(box.transform, ready ? "Pyet · −" + QuestionEngine.Cost(q.Type) : "Pyet", ready ? BtnStyle.Primary : BtnStyle.Disabled, gm.Ask, 17, 10);
            float boxH = Mathf.Max(20 + composeH + msgH + 14, 20 + composeH + 58);
            UI.PlaceRight(ask.Rect, 14, boxH - 58, 160, 46);
            ask.SetInteractable(ready);
            UI.Place(box.rectTransform, pad, iy, iw, boxH);
            iy += boxH + 18;

            UI.Place(card.rectTransform, 0, y, w, iy);
            return iy;
        }

        float ChipRow(Transform root, string label, List<KeyValuePair<string, int>> items, int selected, int hinted, System.Action<int> pick, float pad, float iy, float iw, bool locked)
        {
            UI.LabelAt(root, label, Theme.Sans, 13, Theme.Muted, pad, iy + 10, 90, 20);
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
                var b = UI.Button(root, kv.Key, selected == v ? BtnStyle.ChipOn : BtnStyle.Chip, () => pick(v), 14, 8);
                UI.Place(b.Rect, x, rowY, bw, 38);
                b.SetInteractable(!locked);
                Hint(b, hinted == v && selected != v);
                x += bw + 6;
            }
            return rowY + 48;
        }
    }
}
