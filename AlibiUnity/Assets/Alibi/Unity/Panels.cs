using System;
using System.Collections.Generic;
using System.Text;
using Alibi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Alibi.Unity
{
    public static class Panels
    {
        public static readonly string[] HowToRules =
        {
            "Zgjidh të dyshuarin, pyetjen dhe parametrin. Pyetjet Q1–Q3 kushtojnë 1, Q4–Q5 kushtojnë 2 nga buxheti (R11).",
            "Çdo përgjigje bëhet kufizim dhe tabela përditësohet vetë me AC-3: ari = e konfirmuar, kuti = e mundshme, zbehur = e përjashtuar (R12).",
            "Të ndershmit thonë gjithmonë të vërtetën. Fajtori gënjen, dhe gënjeshtra del kur dy deklarata përplasen: sistemi i thekson si «Kontradiktë» (D9).",
            "Deklarata e dikujt për vetveten nuk e përjashton atë. Përjashtimi vjen nga deklaratat e të tjerëve dhe nga grafi i lëvizjes (R02, R08).",
            "Akuza kërkon personin, dhomën dhe orën. Fiton vetëm kur zgjidhësi konfirmon që asnjë kombinim tjetër nuk qëndron (R09).",
            "Në fund shfaqet par-i i rastit: 3 yje deri në par+1, 2 yje deri në par+3, përndryshe 1 yll (R13)."
        };

        public static RectTransform Overlay(Transform canvas, string name)
        {
            var shade = UI.Panel(canvas, name, Theme.Shade, 0, true);
            UI.Stretch(shade.rectTransform, 0, 0, 0, 0);
            return shade.rectTransform;
        }

        public static Image ParchmentCard(Transform parent, string name)
        {
            var card = UI.Panel(parent, name, Theme.Parchment, 10, true);
            var tone = UI.Panel(card.transform, "Tone", new Color(0.86f, 0.78f, 0.6f, 0.35f), 10, false);
            UI.Stretch(tone.rectTransform, 0, 0, 0, 0);
            return card;
        }

        public static void Center(RectTransform rt, float w, float h, float yOffset = 0)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, yOffset);
            rt.sizeDelta = new Vector2(w, h);
        }

        public static float Paragraph(Transform root, string text, Font font, int size, Color color, float x, float y, float w)
        {
            var t = UI.LabelAt(root, text, font, size, color, x, y, w, 20);
            return UI.FitHeight(t, w);
        }

        public static void Stamp(Transform root, string text, Color color, float x, float y)
        {
            var w = UI.TextWidth(text, Theme.Mono, 18) + 44;
            var holder = UI.Rect(root, "Stamp");
            UI.Place(holder, x, y, w, 44);
            holder.localRotation = Quaternion.Euler(0, 0, 4f);
            var border = UI.Border(holder, color, 6);
            border.color = color;
            var t = UI.Label(holder, text, Theme.Mono, 18, color, TextAnchor.MiddleCenter);
            UI.Stretch(t.rectTransform, 0, 0, 0, 0);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        public static void Stars(Transform root, int stars, float x, float y, Color on, Color off)
        {
            for (int i = 0; i < 3; i++)
            {
                var img = UI.Rect(root, "Star").gameObject.AddComponent<Image>();
                img.sprite = Theme.Star;
                img.color = i < stars ? on : off;
                img.raycastTarget = false;
                UI.Place(img.rectTransform, x + i * 36, y, 32, 32);
            }
        }

        public static float HowTo(Transform overlay, Action close)
        {
            var card = ParchmentCard(overlay, "HowTo");
            float w = 760, pad = 34, y = 30;
            var root = card.transform;
            UI.LabelAt(root, "SI LUHET", Theme.Mono, 14, Theme.ParchMuted, pad, y, 400, 20);
            y += 26;
            UI.LabelAt(root, "Gjashtë rregulla, një minutë", Theme.Serif, 36, Theme.ParchInk, pad, y, w - pad * 2, 44);
            y += 56;
            for (int i = 0; i < HowToRules.Length; i++)
            {
                UI.LabelAt(root, (i + 1) + ".", Theme.SansSemi, 17, Theme.ParchMuted, pad, y, 30, 24);
                y += Paragraph(root, HowToRules[i], Theme.Sans, 17, Theme.ParchInk, pad + 30, y, w - pad * 2 - 30) + 10;
            }
            y += 8;
            var ok = UI.Button(root, "E kuptova", BtnStyle.Parchment, close, 17, 10);
            UI.Place(ok.Rect, pad, y, 180, 48);
            y += 48 + 30;
            Center(card.rectTransform, w, y);
            return y;
        }

        public static void Toast(Transform canvas, string text, Color accent, float seconds)
        {
            var host = canvas.GetComponent<ToastHost>();
            if (host == null) host = canvas.gameObject.AddComponent<ToastHost>();
            host.Show(text, accent, seconds);
        }
    }

    public sealed class ToastHost : MonoBehaviour
    {
        RectTransform current;
        float hideAt;

        public void Show(string text, Color accent, float seconds)
        {
            if (current != null) Destroy(current.gameObject);
            var card = UI.Card(transform, "Toast", Theme.Panel2, accent, 12);
            current = card.rectTransform;
            float w = Mathf.Min(900, UI.TextWidth(text, Theme.SansSemi, 18) + 60);
            current.anchorMin = current.anchorMax = new Vector2(0.5f, 1f);
            current.pivot = new Vector2(0.5f, 1f);
            current.anchoredPosition = new Vector2(0, -92);
            current.sizeDelta = new Vector2(w, 54);
            var t = UI.Label(card.transform, text, Theme.SansSemi, 18, Theme.Text, TextAnchor.MiddleCenter);
            UI.Stretch(t.rectTransform, 16, 0, 16, 0);
            hideAt = Time.unscaledTime + seconds;
        }

        void Update()
        {
            if (current != null && Time.unscaledTime > hideAt)
            {
                Destroy(current.gameObject);
                current = null;
            }
        }
    }

    public sealed class GamePanels
    {
        readonly GameManager gm;

        public GamePanels(GameManager manager)
        {
            gm = manager;
        }

        GameSession S { get { return gm.Session; } }
        CaseDefinition C { get { return gm.Session.Case; } }

        public float Coach(RectTransform content, float y, float w)
        {
            var step = gm.CurrentTutorialStep;
            if (step == null) return 0;
            var card = Panels.ParchmentCard(content, "Coach");
            var root = card.transform;
            float pad = 18, iw = w - pad * 2, iy = 14;
            UI.LabelAt(root, "UDHËZUESI · KARTA 1", Theme.Mono, 13, Theme.Hex("#7A5A22"), pad, iy, iw, 18);
            UI.LabelAt(root, "Hapi " + (gm.TutorialIndex + 1) + " nga " + C.Tutorial.Count, Theme.Mono, 13, Theme.ParchMuted, pad, iy, iw, 18, TextAnchor.UpperRight);
            iy += 24;
            UI.LabelAt(root, step.Title, Theme.Serif, 26, Theme.ParchInk, pad, iy, iw, 32);
            iy += 36;
            iy += Panels.Paragraph(root, step.Text, Theme.Sans, 16, Theme.ParchInk, pad, iy, iw) + 10;
            float x = pad;
            if (step.Fill != null)
            {
                var b = UI.Button(root, "Plotëso pyetjen", BtnStyle.Parchment, gm.FillTutorial, 15, 9);
                UI.Place(b.Rect, x, iy, 180, 40);
                x += 190;
            }
            else if (step.Accuse)
            {
                var b = UI.Button(root, "Akuzo", BtnStyle.Danger, gm.OpenAccusation, 15, 9);
                UI.Place(b.Rect, x, iy, 130, 40);
                x += 140;
            }
            else
            {
                var b = UI.Button(root, "Tjetri", BtnStyle.Parchment, gm.NextTutorial, 15, 9);
                UI.Place(b.Rect, x, iy, 130, 40);
                x += 140;
            }
            var hide = UI.Button(root, "Fshih udhëzuesin", BtnStyle.ParchmentGhost, gm.ToggleTutorial, 14, 9);
            UI.Place(hide.Rect, x, iy, 180, 40);
            iy += 54;
            UI.Place(card.rectTransform, 0, y, w, iy);
            return iy;
        }

        public float LastAnswer(RectTransform content, float y, float w)
        {
            var o = gm.LastOutcome;
            if (o == null || o.Statement == null) return 0;
            var st = o.Statement;
            var sus = C.Suspects[st.Suspect];
            var card = UI.Card(content, "LastAnswer", Theme.Hex("#171B24"), Theme.Line2, 14);
            var root = card.transform;
            float pad = 18, iw = w - pad * 2, iy = 16;
            UI.Avatar(root, sus, 38, pad, iy);
            UI.LabelAt(root, sus.Def + " · #" + st.Id, Theme.SansSemi, 17, Theme.Text, pad + 50, iy, iw - 50, 22);
            UI.LabelAt(root, st.QuestionText, Theme.Sans, 14, Theme.Muted, pad + 50, iy + 22, iw - 50, 20);
            iy += 48;
            iy += Panels.Paragraph(root, "«" + st.Text + "»", Theme.SerifItalic, 28, Theme.Parchment, pad, iy, iw) + 4;
            iy += Panels.Paragraph(root, "Kufizim i ri: " + st.Formal, Theme.Mono, 14, Theme.Gold, pad, iy, iw) + 8;
            string budgetChip = "−" + st.Cost + " pyetje · mbeten " + Mathf.Max(0, S.Remaining);
            string elimChip = o.Changes.Count > 0 ? o.Changes.Count + " mundësi u përjashtuan në tabelë" : "Asnjë mundësi e re nuk u përjashtua";
            float x = pad;
            foreach (var chipText in new[] { budgetChip, elimChip })
            {
                float cw = UI.TextWidth(chipText, Theme.Sans, 13) + 24;
                var chip = UI.Card(root, "Chip", new Color(0, 0, 0, 0), Theme.Line2, 14);
                UI.Place(chip.rectTransform, x, iy, cw, 28);
                var t = UI.Label(chip.transform, chipText, Theme.Sans, 13, Theme.Text2, TextAnchor.MiddleCenter);
                UI.Stretch(t.rectTransform, 0, 0, 0, 0);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                x += cw + 8;
            }
            iy += 38;
            if (o.Duplicate)
            {
                iy += Panels.Paragraph(root, "Kjo pyetje ishte bërë më parë dhe nuk solli informacion të ri.", Theme.Sans, 14, Theme.Hex("#E2C27A"), pad, iy, iw) + 6;
            }
            if (S.UnverifiedConfirmation(st))
            {
                iy += Panels.Paragraph(root, "Konfirmim i paverifikuar (R07b): asnjëri nga të dy nuk është ende jashtë dyshimit, prandaj kjo nuk i nxjerr jashtë.", Theme.Sans, 14, Theme.Muted, pad, iy, iw) + 6;
            }
            if (o.NewlyCleared.Count > 0)
            {
                var names = new List<string>();
                foreach (var k in o.NewlyCleared) names.Add(C.Suspects[k].Def);
                string verb = names.Count > 1 ? "dolën jashtë dyshimit: nuk mund të ishin" : "doli jashtë dyshimit: nuk mund të ishte";
                string when = C.CrimeTimeKnown ? " në " + C.Hours[C.CrimeTime] : " në asnjë orë të mundshme";
                iy += Panels.Paragraph(root, QuestionEngine.JoinNames(names) + " " + verb + " në " + C.Rooms[C.CrimeRoom].Loc + when + ".", Theme.Sans, 15, Theme.Blue, pad, iy, iw) + 6;
            }
            if (o.NewlyClearedTimes.Count > 0 && !C.CrimeTimeKnown)
            {
                var hours = new List<string>();
                foreach (var t in o.NewlyClearedTimes) hours.Add(C.Hours[t]);
                iy += Panels.Paragraph(root, "Ora e krimit nuk mund të jetë më: " + string.Join(", ", hours.ToArray()) + ".", Theme.Sans, 15, Theme.Blue, pad, iy, iw) + 6;
            }
            if (o.Conflict != null)
            {
                var box = UI.Card(root, "Conflict", Theme.WithAlpha(Theme.Red, 0.12f), Theme.WithAlpha(Theme.Red, 0.7f), 10);
                var badge = UI.Panel(box.transform, "Icon", Theme.Red, 64, false);
                UI.Place(badge.rectTransform, 12, 12, 24, 24);
                var ex = UI.Label(badge.transform, "!", Theme.SansBold, 16, Theme.Night, TextAnchor.MiddleCenter);
                UI.Stretch(ex.rectTransform, 0, 0, 0, 0);
                UI.LabelAt(box.transform, "Kontradiktë", Theme.SansBold, 15, Theme.RedText, 46, 12, iw - 60, 22);
                float th = Panels.Paragraph(box.transform, S.ConflictText(o.Conflict), Theme.Sans, 14, Theme.Hex("#F1D6D0"), 46, 36, iw - 60);
                float bh = 36 + th + 12;
                UI.Place(box.rectTransform, pad, iy, iw, bh);
                iy += bh + 8;
            }
            iy += 8;
            UI.Place(card.rectTransform, 0, y, w, iy);
            return iy;
        }

        public float Dossier(RectTransform content, float y, float w)
        {
            float iy = y;
            var head = UI.Rect(content, "DossierHead");
            UI.Place(head, 0, iy, w, 40);
            UI.LabelAt(head, "Dosja e dëshmive", Theme.Serif, 30, Theme.Parchment, 4, 0, w - 160, 40);
            UI.LabelAt(head, S.Statements.Count + (S.Statements.Count == 1 ? " deklaratë" : " deklarata"), Theme.Sans, 14, Theme.Muted, 0, 12, w - 4, 20, TextAnchor.UpperRight);
            iy += 48;
            if (S.Statements.Count == 0)
            {
                iy += Panels.Paragraph(content, "Ende asnjë deklaratë. Çdo përgjigje ruhet këtu vetë, pa shënime në letër.", Theme.Sans, 15, Theme.Muted, 4, iy, w - 8) + 10;
                return iy - y;
            }
            var lies = new HashSet<int>();
            if (S.IsOver) foreach (var l in S.Lies()) lies.Add(l.Id);
            for (int i = S.Statements.Count - 1; i >= 0; i--)
            {
                var st = S.Statements[i];
                var sus = C.Suspects[st.Suspect];
                bool flagged = S.Flagged.Contains(st.Id);
                var card = UI.Card(content, "Statement", flagged ? Theme.WithAlpha(Theme.Red, 0.08f) : Theme.Hex("#161A22"), flagged ? Theme.WithAlpha(Theme.Red, 0.55f) : Theme.Line, 10);
                var root = card.transform;
                float pad = 14, iw = w - pad * 2, cy = 10;
                UI.LabelAt(root, "#" + st.Id, Theme.Mono, 13, Theme.Muted, pad, cy + 3, 40, 20);
                UI.Avatar(root, sus, 24, pad + 36, cy);
                UI.LabelAt(root, sus.Def, Theme.SansSemi, 15, Theme.Text, pad + 68, cy + 2, 140, 22);
                UI.LabelAt(root, st.QuestionText, Theme.Sans, 13, Theme.Muted, pad + 160, cy + 4, iw - 160, 20);
                cy += 32;
                cy += Panels.Paragraph(root, "«" + st.Text + "»", Theme.SerifItalic, 21, Theme.Parchment, pad, cy, iw) + 2;
                cy += Panels.Paragraph(root, st.Formal, Theme.Mono, 13, Theme.Hex("#C9A45C"), pad, cy, iw) + 6;
                var tags = new List<KeyValuePair<string, Color>>();
                if (flagged) tags.Add(new KeyValuePair<string, Color>("Në kontradiktë", Theme.RedText));
                if (S.IsOver && lies.Contains(st.Id)) tags.Add(new KeyValuePair<string, Color>("Gënjeshtër e fajtorit", Theme.RedText));
                if (S.IsOver && st.IsMistake) tags.Add(new KeyValuePair<string, Color>("Gabim i pavullnetshëm (R05)", Theme.Hex("#E2C27A")));
                if (S.IsCleared(st.Suspect)) tags.Add(new KeyValuePair<string, Color>("E vërtetuar", Theme.Blue));
                else if (S.UnverifiedConfirmation(st)) tags.Add(new KeyValuePair<string, Color>("Konfirmim i paverifikuar", Theme.Text2));
                else tags.Add(new KeyValuePair<string, Color>("E paverifikuar", Theme.Text2));
                float x = pad;
                foreach (var tag in tags)
                {
                    float tw = UI.TextWidth(tag.Key, Theme.Sans, 12) + 20;
                    var chip = UI.Card(root, "Tag", new Color(0, 0, 0, 0), Theme.WithAlpha(tag.Value, 0.6f), 12);
                    UI.Place(chip.rectTransform, x, cy, tw, 24);
                    var t = UI.Label(chip.transform, tag.Key, Theme.Sans, 12, tag.Value, TextAnchor.MiddleCenter);
                    UI.Stretch(t.rectTransform, 0, 0, 0, 0);
                    t.horizontalOverflow = HorizontalWrapMode.Overflow;
                    x += tw + 6;
                }
                cy += 34;
                UI.Place(card.rectTransform, 0, iy, w, cy);
                iy += cy + 8;
            }
            return iy - y;
        }

        string StatementLine(Statement st)
        {
            return "#" + st.Id + " " + C.Suspects[st.Suspect].Def + ": «" + st.Text + "»";
        }

        public float Result(RectTransform content, float y, float w)
        {
            var v = S.Verdict;
            if (v == null) return 0;
            bool won = v.Won;
            var card = Panels.ParchmentCard(content, "Result");
            var root = card.transform;
            float pad = 22, iw = w - pad * 2, iy = 20;
            Panels.Stamp(root, won ? "RASTI U ZGJIDH" : "AKUZA RA (LOST)", won ? Theme.Hex("#7A5A1C") : Theme.Hex("#A3352C"), pad, iy);
            UI.LabelAt(root, C.Title + " · seed " + C.Seed, Theme.Mono, 12, Theme.ParchMuted, pad, iy + 4, iw, 18, TextAnchor.UpperRight);
            iy += 62;
            UI.LabelAt(root, "AKUZA", Theme.Mono, 12, Theme.ParchMuted, pad, iy, iw, 16);
            iy += 18;
            string accLine = C.Suspects[S.AccusedPerson].Name + " · " + C.Rooms[S.AccusedRoom].Name + " · " + C.Hours[S.AccusedTime];
            UI.LabelAt(root, accLine, Theme.Serif, 32, Theme.ParchInk, pad, iy, iw, 40);
            iy += 46;
            Panels.Stars(root, S.Stars, pad, iy, Theme.Hex("#A37B22"), new Color(0.16f, 0.13f, 0.09f, 0.18f));
            UI.LabelAt(root, won ? S.Stars + (S.Stars == 1 ? " yll" : " yje") : "Pa yje", Theme.SansBold, 17, Theme.ParchInk, pad + 118, iy + 6, 120, 24);
            UI.LabelAt(root, "Pyetje të përdorura: <b>" + S.Spent + "</b>   ·   Par: <b>" + C.Par + "</b>", Theme.Sans, 15, Theme.ParchInk, pad, iy + 7, iw, 22, TextAnchor.UpperRight);
            iy += 44;
            if (won)
            {
                UI.LabelAt(root, "ZINXHIRI I KUFIZIMEVE", Theme.Mono, 12, Theme.ParchMuted, pad, iy, iw, 16);
                iy += 20;
                foreach (var chain in v.Chain)
                {
                    var box = UI.Panel(root, "Chain", new Color(0.16f, 0.13f, 0.09f, 0.07f), 8, false);
                    string title = chain.Time >= 0 ? "Ora " + C.Hours[chain.Time] + " u përjashtua" : C.Suspects[chain.Suspect].Def + " doli jashtë dyshimit";
                    UI.LabelAt(box.transform, title, Theme.SansBold, 15, Theme.ParchInk, 12, 8, iw - 24, 20);
                    var lines = new List<string>();
                    foreach (var st in chain.Reason) lines.Add(StatementLine(st));
                    if (lines.Count == 0) lines.Add("Grafi i lëvizjes dhe fakti i dosjes mjaftojnë.");
                    float th = Panels.Paragraph(box.transform, string.Join("  ·  ", lines.ToArray()), Theme.Sans, 13, Theme.ParchInk, 12, 30, iw - 24);
                    float bh = 30 + th + 10;
                    UI.Place(box.rectTransform, pad, iy, iw, bh);
                    iy += bh + 6;
                }
            }
            else
            {
                var sb = new StringBuilder();
                var evidence = new List<Statement>();
                string title;
                if (!v.OkRoom)
                {
                    title = "Akuza është e gabuar: " + string.Join(", ", v.WrongFields.ToArray()) + ".";
                    sb.Append("Krimi ndodhi në ").Append(C.Rooms[C.CrimeRoom].Loc).Append(". Nëse njëra pjesë e akuzës është e gabuar, akuza bie e tëra (R09b).");
                }
                else if (v.ExcludedBy != null)
                {
                    title = "Akuza është e gabuar: " + string.Join(", ", v.WrongFields.ToArray()) + ". Ja kufizimi që e përjashtonte:";
                    evidence.AddRange(v.ExcludedBy);
                    if (v.ExcludedBy.Count == 0) sb.Append("Grafi i lëvizjes e bënte të pamundur këtë kombinim. ");
                    string when = C.CrimeTimeKnown ? C.Hours[C.CrimeTime] : C.Hours[S.AccusedTime];
                    sb.Append("Me këto deklarata, ").Append(C.Suspects[S.AccusedPerson].Def).Append(" nuk mund të ishte në ").Append(C.Rooms[C.CrimeRoom].Loc).Append(" në ").Append(when).Append(".");
                }
                else if (!v.OkTime && C.CrimeTimeKnown)
                {
                    title = "Akuza është e gabuar: ora.";
                    sb.Append("Ora e krimit dihej nga dosja: ").Append(C.Hours[C.CrimeTime]).Append(".");
                }
                else if (v.OkPerson && v.OkTime && !v.Proven)
                {
                    title = "Personi ishte i saktë, por akuza nuk ishte e provuar.";
                    var others = new List<string>();
                    for (int i = 0; i < v.Eval.ViableK.Count; i++)
                    {
                        int k = v.Eval.ViableK[i];
                        string label = C.Suspects[k].Def + (C.CrimeTimeKnown ? "" : " (" + C.Hours[v.Eval.ViableTau[i]] + ")");
                        if (!others.Contains(label) && !(k == C.Culprit && v.Eval.ViableTau[i] == C.CrimeTime)) others.Add(label);
                    }
                    sb.Append("Zgjidhësi gjente ende shpjegime të tjera: ").Append(QuestionEngine.JoinNames(others)).Append(". Alibi nuk fitohet me hamendje.");
                }
                else
                {
                    title = "Akuza është e gabuar: " + string.Join(", ", v.WrongFields.ToArray()) + ".";
                    var cands = new List<string>();
                    for (int k = 0; k < C.N; k++) if (v.Eval.Candidate[k]) cands.Add(C.Suspects[k].Def);
                    sb.Append("Asnjë kufizim nuk e provonte këtë akuzë. Kandidatët që mbeteshin: ").Append(QuestionEngine.JoinNames(cands)).Append(".");
                }
                var box = UI.Panel(root, "Reason", new Color(0.64f, 0.21f, 0.17f, 0.1f), 8, false);
                UI.Border(box.transform, new Color(0.64f, 0.21f, 0.17f, 0.5f), 8);
                float by = 10;
                by += Panels.Paragraph(box.transform, title, Theme.SansBold, 15, Theme.Hex("#8E2C24"), 12, by, iw - 24) + 4;
                foreach (var st in evidence)
                {
                    by += Panels.Paragraph(box.transform, StatementLine(st), Theme.Sans, 14, Theme.ParchInk, 12, by, iw - 24);
                    by += Panels.Paragraph(box.transform, st.Formal, Theme.Mono, 13, Theme.Hex("#7A5A22"), 12, by, iw - 24) + 4;
                }
                by += Panels.Paragraph(box.transform, sb.ToString().TrimEnd(), Theme.Sans, 14, Theme.ParchInk, 12, by, iw - 24);
                float bh = by + 12;
                UI.Place(box.rectTransform, pad, iy, iw, bh);
                iy += bh + 10;
            }
            UI.LabelAt(root, "E VËRTETA", Theme.Mono, 12, Theme.ParchMuted, pad, iy, iw, 16);
            iy += 18;
            var truth = new StringBuilder();
            truth.Append(C.Suspects[C.Culprit].Def).Append(" ishte në ").Append(C.Rooms[C.CrimeRoom].Loc).Append(" në ").Append(C.Hours[C.CrimeTime]).Append(". ");
            var lies = S.Lies();
            if (lies.Count > 0) truth.Append("Gënjeshtra: ").Append(StatementLine(lies[0]));
            else truth.Append("Po ta pyesje, do të thoshte: «").Append(QuestionEngine.Answer(C, Question.Make(C.Culprit, QType.Q1, C.CrimeTime)).Text).Append("»");
            foreach (var st in S.Statements) if (st.IsMistake) truth.Append("\nGabimi i dëshmitarit të ndershëm (R05): ").Append(StatementLine(st));
            iy += Panels.Paragraph(root, truth.ToString(), Theme.Sans, 15, Theme.ParchInk, pad, iy, iw) + 6;
            iy += Panels.Paragraph(root, "Pozicionet e vërteta janë tani në vilë. Lëviz kohën për t'i parë orë pas ore. 3 yje deri në par+1 · 2 yje deri në par+3 · 1 yll më shumë.", Theme.Sans, 13, Theme.ParchMuted, pad, iy, iw) + 12;
            var retry = UI.Button(root, "Provo përsëri · seed " + C.Seed, BtnStyle.Parchment, gm.Restart, 16, 10);
            UI.Place(retry.Rect, pad, iy, 250, 48);
            string nextLabel = won && C.CardNumber > 0 && C.CardNumber < 4 ? "Karta " + (C.CardNumber + 1) : "Menyja kryesore";
            var next = UI.Button(root, nextLabel, BtnStyle.ParchmentGhost, gm.AfterResult, 16, 10);
            UI.Place(next.Rect, pad + 262, iy, 220, 48);
            iy += 48 + 22;
            UI.Place(card.rectTransform, 0, y, w, iy);
            return iy;
        }

        public void Brief(Transform overlay)
        {
            var card = Panels.ParchmentCard(overlay, "Brief");
            var root = card.transform;
            float w = 1060, pad = 40, iw = w - pad * 2, iy = 34;
            UI.LabelAt(root, (C.Kicker ?? "") + "  ·  SEED " + C.Seed, Theme.Mono, 13, Theme.ParchMuted, pad, iy, iw, 18);
            string stamp = C.Type == CaseType.Vrasja ? "VRASJE" : C.Type == CaseType.Vjedhja ? "VJEDHJE" : "SABOTIM";
            Panels.Stamp(root, stamp, Theme.Hex("#A3352C"), w - pad - 150, iy);
            iy += 24;
            UI.LabelAt(root, C.Title, Theme.Serif, 42, Theme.ParchInk, pad, iy, iw - 180, 50);
            iy += 58;
            iy += Panels.Paragraph(root, C.Premise, Theme.SerifItalic, 24, Theme.ParchInk, pad, iy, iw) + 14;
            var facts = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Vendi i krimit", C.Rooms[C.CrimeRoom].Name),
                new KeyValuePair<string, string>("Ora e krimit", C.CrimeTimeKnown ? C.Hours[C.CrimeTime] : "E panjohur"),
                new KeyValuePair<string, string>("Harta", "Vila · " + C.RoomCount + " dhoma" + (C.UseGraph ? " me graf" : "")),
                new KeyValuePair<string, string>("Orët", C.Hours[0] + " – " + C.Hours[C.T - 1]),
                new KeyValuePair<string, string>("Buxheti", C.Budget + " pyetje"),
                new KeyValuePair<string, string>("Kërkohet", C.CrimeTimeKnown ? "Personi, i provuar" : "Personi dhe ora")
            };
            float fw = (iw - 5 * 10) / 6f;
            for (int i = 0; i < facts.Count; i++)
            {
                var box = UI.Panel(root, "Fact", new Color(1, 1, 1, 0.35f), 8, false);
                UI.Border(box.transform, new Color(0.16f, 0.13f, 0.09f, 0.2f), 8);
                UI.Place(box.rectTransform, pad + i * (fw + 10), iy, fw, 62);
                UI.LabelAt(box.transform, facts[i].Key.ToUpperInvariant(), Theme.Mono, 10, Theme.ParchMuted, 10, 9, fw - 16, 16);
                UI.LabelAt(box.transform, facts[i].Value, Theme.SansSemi, 15, Theme.ParchInk, 10, 28, fw - 16, 30);
            }
            iy += 76;
            UI.LabelAt(root, "TË DYSHUARIT", Theme.Mono, 12, Theme.ParchMuted, pad, iy, iw, 16);
            iy += 22;
            float sw = (iw - (C.N - 1) * 10) / C.N;
            for (int i = 0; i < C.N; i++)
            {
                var box = UI.Panel(root, "Suspect", new Color(0.16f, 0.13f, 0.09f, 0.06f), 10, false);
                UI.Place(box.rectTransform, pad + i * (sw + 10), iy, sw, 58);
                UI.Avatar(box.transform, C.Suspects[i], 36, 10, 11);
                UI.LabelAt(box.transform, C.Suspects[i].Name, Theme.SansSemi, 16, Theme.ParchInk, 54, 9, sw - 58, 22);
                UI.LabelAt(box.transform, C.Suspects[i].Role, Theme.Sans, 13, Theme.ParchMuted, 54, 31, sw - 58, 20);
            }
            iy += 72;
            UI.LabelAt(root, "RREGULLAT E RASTIT", Theme.Mono, 12, Theme.ParchMuted, pad, iy, iw, 16);
            iy += 22;
            for (int i = 0; i < C.Rules.Count; i++)
            {
                UI.LabelAt(root, (i + 1) + ".", Theme.SansSemi, 15, Theme.ParchMuted, pad, iy, 26, 20);
                iy += Panels.Paragraph(root, C.Rules[i], Theme.Sans, 15, Theme.ParchInk, pad + 26, iy, iw - 26) + 4;
            }
            iy += 6;
            iy += Panels.Paragraph(root, "Objektivi: " + C.Objective, Theme.SansSemi, 15, Theme.ParchInk, pad, iy, iw) + 6;
            if (C.GeneratorNotes.Count > 0)
            {
                string note = "Gjeneratori refuzoi " + C.GeneratorNotes.Count + " seed-e me defekt para se ta hapte këtë rast (p.sh. " + C.GeneratorNotes[0] + "). Ky rast ka një defekt dhe nuk llogaritet si humbje: rastet e tilla nuk i shfaqen lojtarit, sistemi hap vetë seed-in tjetër.";
                iy += Panels.Paragraph(root, note, Theme.Sans, 13, Theme.Hex("#7A5A22"), pad, iy, iw) + 6;
            }
            iy += 10;
            var enter = UI.Button(root, "Hyr në vilë", BtnStyle.Parchment, gm.EnterCase, 18, 10);
            UI.Place(enter.Rect, pad, iy, 200, 52);
            var back = UI.Button(root, "Kthehu te menyja", BtnStyle.ParchmentGhost, gm.ExitToMenu, 16, 10);
            UI.Place(back.Rect, pad + 212, iy, 200, 52);
            iy += 52 + 34;
            Panels.Center(card.rectTransform, w, iy);
            float maxH = 1040;
            if (iy > maxH)
            {
                float s = maxH / iy;
                card.rectTransform.localScale = new Vector3(s, s, 1);
            }
        }

        public void Accusation(Transform overlay)
        {
            var card = UI.Card(overlay, "Accusation", Theme.Panel, Theme.Line2, 16);
            var root = card.transform;
            float w = 820, pad = 30, iw = w - pad * 2, iy = 28;
            UI.LabelAt(root, "AKUZA", Theme.Mono, 14, Theme.RedText, pad, iy, iw, 18);
            iy += 24;
            UI.LabelAt(root, "Kush, ku dhe kur?", Theme.Serif, 38, Theme.Parchment, pad, iy, iw, 46);
            iy += 52;
            string intro = C.CrimeTimeKnown
                ? "Dhoma dhe ora dihen nga dosja, por akuza i kërkon të tria si konfirmim (R09). Nëse njëra pjesë është e gabuar, akuza bie e tëra."
                : "Sabotimi: dhoma dihet, por ora jo. Akuza duhet të provojë personin dhe orën.";
            iy += Panels.Paragraph(root, intro, Theme.Sans, 15, Theme.Text2, pad, iy, iw) + 10;
            if (S.ForcedAccusation)
            {
                var forced = UI.Card(root, "Forced", Theme.WithAlpha(Theme.Gold, 0.12f), Theme.WithAlpha(Theme.Gold, 0.55f), 10);
                UI.Place(forced.rectTransform, pad, iy, iw, 44);
                var t = UI.Label(forced.transform, "Buxheti mbaroi. Pyetjet janë çaktivizuar dhe akuza është e detyruar (R11b).", Theme.Sans, 15, Theme.GoldText, TextAnchor.MiddleLeft);
                UI.Stretch(t.rectTransform, 14, 0, 14, 0);
                iy += 54;
            }
            UI.SectionLabel(root, "PERSONI", pad, iy, iw);
            iy += 24;
            float x = pad;
            for (int i = 0; i < C.N; i++)
            {
                int si = i;
                bool on = gm.AccPerson == i;
                float bw = (iw - (C.N - 1) * 8) / C.N;
                var b = UI.Button(root, null, on ? BtnStyle.ChipOn : BtnStyle.Chip, () => gm.SetAccusation(si, gm.AccRoom, gm.AccTime), 15, 10);
                UI.Place(b.Rect, x, iy, bw, 56);
                UI.Avatar(b.Rect, C.Suspects[i], 32, 8, 12);
                UI.LabelAt(b.Rect, C.Suspects[i].Name, Theme.SansSemi, 15, Theme.Text, 46, 9, bw - 50, 20);
                UI.LabelAt(b.Rect, S.IsCleared(i) ? "jashtë dyshimit" : "kandidat", Theme.Sans, 12, S.IsCleared(i) ? Theme.Blue : Theme.Gold, 46, 30, bw - 50, 18);
                x += bw + 8;
            }
            iy += 66;
            UI.SectionLabel(root, "DHOMA", pad, iy, iw);
            iy += 24;
            x = pad;
            for (int r = 0; r < C.RoomCount; r++)
            {
                int ri = r;
                float bw = UI.TextWidth(C.Rooms[r].Name, Theme.SansSemi, 14) + 28;
                if (x + bw > pad + iw)
                {
                    x = pad;
                    iy += 46;
                }
                var b = UI.Button(root, C.Rooms[r].Name, gm.AccRoom == r ? BtnStyle.ChipOn : BtnStyle.Chip, () => gm.SetAccusation(gm.AccPerson, ri, gm.AccTime), 14, 9);
                UI.Place(b.Rect, x, iy, bw, 40);
                x += bw + 8;
            }
            iy += 52;
            UI.SectionLabel(root, "ORA", pad, iy, iw);
            iy += 24;
            x = pad;
            for (int t = 0; t < C.T; t++)
            {
                int ti = t;
                var b = UI.Button(root, C.Hours[t], gm.AccTime == t ? BtnStyle.ChipOn : BtnStyle.Chip, () => gm.SetAccusation(gm.AccPerson, gm.AccRoom, ti), 14, 9);
                UI.Place(b.Rect, x, iy, 92, 40);
                x += 100;
            }
            iy += 54;
            var eval = S.Eval;
            if (eval.ViableCount > 1)
            {
                var names = new List<string>();
                for (int k = 0; k < C.N; k++) if (eval.Candidate[k]) names.Add(C.Suspects[k].Def);
                string text = "Zgjidhësi sheh ende " + eval.ViableCount + " shpjegime të mundshme (" + QuestionEngine.JoinNames(names) + "). Një akuzë e paprovuar humbet, edhe nëse personi është i saktë.";
                var warn = UI.Card(root, "Warn", Theme.WithAlpha(Theme.Red, 0.1f), Theme.WithAlpha(Theme.Red, 0.5f), 10);
                float th = Panels.Paragraph(warn.transform, text, Theme.Sans, 14, Theme.Hex("#F1D6D0"), 14, 10, iw - 28);
                UI.Place(warn.rectTransform, pad, iy, iw, th + 20);
                iy += th + 30;
            }
            UI.Divider(root, pad, iy, iw);
            iy += 14;
            var missing = GameSession.MissingAccusationFields(gm.AccPerson, gm.AccRoom, gm.AccTime);
            string msg = missing.Count > 0 ? GameSession.MissingAccusationMessage(missing) + " Dërgimi është i bllokuar dhe buxheti nuk preket (R09c)." : "Akuza është e plotë. Zgjidhësi do ta verifikojë.";
            float mh = Panels.Paragraph(root, msg, Theme.Sans, 14, missing.Count > 0 ? Theme.Hex("#E2C27A") : Theme.Muted, pad, iy + 12, iw - 380);
            var submit = UI.Button(root, "Dërgo akuzën", missing.Count > 0 ? BtnStyle.Disabled : BtnStyle.Danger, gm.SubmitAccusation, 17, 10);
            UI.PlaceRight(submit.Rect, pad, iy, 180, 48);
            submit.SetInteractable(missing.Count == 0);
            if (!S.ForcedAccusation)
            {
                var cancel = UI.Button(root, "Kthehu te pyetjet", BtnStyle.Ghost, gm.CancelAccusation, 16, 10);
                UI.PlaceRight(cancel.Rect, pad + 192, iy, 180, 48);
            }
            iy += Mathf.Max(48, mh + 12) + 28;
            Panels.Center(card.rectTransform, w, iy);
        }
    }
}
