using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Alibi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Alibi.Unity
{
    public sealed class MainMenuController : MonoBehaviour
    {
        Canvas canvas;
        RectTransform screen, overlay, frame;
        CameraRig rig;
        VillaView villa;
        CaseDefinition showcase;
        int showcaseTime;
        float nextTick;
        Task<GenerationResult> genTask;
        Text genStatus;
        float genStarted;
        int pickMode;
        int seed;

        static readonly string[] ModeNames = { "Niveli 1 · Vrasja", "Niveli 2 · Vrasja", "Niveli 3 · Sabotimi" };
        static readonly string[] ModeDesc =
        {
            "4 të dyshuar, 5 dhoma, 3 orë, buxhet 12. Pa graf lëvizjeje.",
            "5 të dyshuar, 7 dhoma me graf, 4 orë, buxhet 12. Askush nuk teleportohet.",
            "4 të dyshuar, 7 dhoma, 3 orë, buxhet 12. Ora e krimit nuk dihet: provo personin dhe orën."
        };
        static readonly string[] ModeNotes =
        {
            "Rast i shpejtë për të mësuar tabelën.",
            "Kush nuk arrin dot në dhomën e krimit me kalime përjashtohet (R02, R08).",
            "Variant eksperimental: me parametrat e plotë të nivelit 3 (5 të dyshuar, 5 orë, buxhet 11) par-i i llogaritur del 18–25, prandaj ky variant është i ngushtuar."
        };

        void Start()
        {
            seed = UnityEngine.Random.Range(1000, 9999);
            showcase = LevelCards.Card2();
            villa = MapBuilder.Build(transform, showcase);
            SceneSetup.Lights(transform, villa.Bounds);
            rig = CameraRig.Create(transform, Theme.Night);
            rig.Target = villa.Bounds;
            rig.Orbit = true;
            rig.OrbitSpeed = 5f;
            rig.Zoom = 0.95f;
            villa.Rig = rig;
            AlibiFlow.DisableForeignCamerasAndLights(transform);
            canvas = AlibiFlow.CreateCanvas(transform, "MenuCanvas", 10);
            var root = (RectTransform)canvas.transform;
            frame = UI.Rect(root, "ShowcaseFrame");
            frame.anchorMin = new Vector2(0, 0);
            frame.anchorMax = new Vector2(1, 1);
            frame.offsetMin = new Vector2(760, 120);
            frame.offsetMax = new Vector2(-60, -80);
            rig.Frame = frame;
            screen = UI.Rect(root, "Screen");
            UI.Stretch(screen, 0, 0, 0, 0);
            overlay = UI.Rect(root, "Overlay");
            UI.Stretch(overlay, 0, 0, 0, 0);
            ShowHome();
            TickShowcase();
        }

        void TickShowcase()
        {
            for (int s = 0; s < showcase.N; s++)
            {
                for (int r = 0; r < showcase.RoomCount; r++)
                {
                    villa.Markers[s, r].SetState(showcase.Truth[s][showcaseTime] == r ? MarkerState.Confirmed : MarkerState.Hidden, false);
                }
            }
            villa.SetCrimeActive(showcaseTime == showcase.CrimeTime);
            nextTick = Time.time + 2.6f;
        }

        void Update()
        {
            if (villa != null && Time.time > nextTick && genTask == null)
            {
                showcaseTime = (showcaseTime + 1) % showcase.T;
                TickShowcase();
            }
            if (genTask != null)
            {
                if (genStatus != null)
                {
                    int dots = (int)((Time.time - genStarted) * 3f) % 4;
                    genStatus.text = "seed " + seed + " · CaseGenerator + CspSolver kontrollojnë unikësinë, ParPolicy llogarit par-in" + new string('.', dots);
                }
                if (genTask.IsCompleted)
                {
                    var task = genTask;
                    genTask = null;
                    if (task.IsFaulted || task.Result == null || task.Result.Case == null)
                    {
                        UI.Clear(overlay);
                        ShowNewCase();
                        Panels.Toast(canvas.transform, "Gjeneratori nuk gjeti rast të vlefshëm. Provo një seed tjetër.", Theme.Red, 4f);
                        return;
                    }
                    AlibiFlow.StartCase(task.Result.Case);
                }
            }
        }

        static UIButton MenuButton(Transform parent, string title, string sub, bool primary, Action onClick, float y)
        {
            var b = UI.Button(parent, null, primary ? BtnStyle.Primary : BtnStyle.Chip, onClick, 17, 12);
            UI.Place(b.Rect, 0, y, 470, 74);
            if (!primary) b.Background.color = new Color(0.08f, 0.09f, 0.12f, 0.92f);
            UI.LabelAt(b.Rect, title, Theme.SansSemi, 19, primary ? Theme.Ink : Theme.Text, 22, 13, 400, 26);
            UI.LabelAt(b.Rect, sub, Theme.Sans, 14, primary ? Theme.Hex("#3D3017") : Theme.Muted, 22, 41, 420, 22);
            return b;
        }

        void ShowHome()
        {
            UI.Clear(screen);
            rig.Frame = frame;
            var left = UI.Rect(screen, "Left");
            left.anchorMin = new Vector2(0, 0);
            left.anchorMax = new Vector2(0, 1);
            left.pivot = new Vector2(0, 1);
            left.offsetMin = new Vector2(64, 40);
            left.offsetMax = new Vector2(64 + 620, -64);
            float y = 0;
            UI.LabelAt(left, "LOJË MISTERI DEDUKTIVE · 1 LOJTAR · OFFLINE", Theme.Mono, 15, Theme.Gold, 0, y, 620, 22);
            y += 30;
            var title = UI.LabelAt(left, "ALIBI", Theme.Serif, 150, Theme.Parchment, -6, y, 640, 150);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            y += 150;
            y += Panels.Paragraph(left, "Çdo dëshmi është një kufizim. Fajtori gënjen, por gënjeshtra e tij del në dritë sapo dy deklarata nuk mund të jenë të vërteta njëkohësisht.", Theme.Sans, 20, Theme.Text2, 0, y, 520) + 22;
            if (!string.IsNullOrEmpty(AlibiFlow.MenuNote))
            {
                var note = UI.Card(left, "Note", Theme.Panel2, Theme.Line2, 10);
                float nh = Panels.Paragraph(note.transform, AlibiFlow.MenuNote, Theme.Sans, 15, Theme.Text, 14, 10, 442) + 20;
                UI.Place(note.rectTransform, 0, y, 470, nh);
                y += nh + 14;
            }
            int next = AlibiFlow.NextCard();
            var nextCase = LevelCards.Card(next);
            MenuButton(left, "Vazhdo", nextCase.Title + " · " + nextCase.Kicker.ToLowerInvariant(), true, () => AlibiFlow.StartCase(LevelCards.Card(next)), y);
            y += 84;
            MenuButton(left, "Kartat e niveleve", "Progresioni: Faza 1 deri në Fazën 3, me zgjidhje të verifikuara", false, ShowCards, y);
            y += 84;
            MenuButton(left, "Rast i ri", "Gjeneratori: raste të reja me zgjidhje të garantuar unike", false, ShowNewCase, y);
            y += 84;
            MenuButton(left, "Si luhet", "Rregullat në një minutë", false, ShowHowTo, y);
            y += 84;
            MenuButton(left, "Dil", "Mbyll lojën", false, Quit, y);

            var foot = UI.Rect(screen, "Footer");
            foot.anchorMin = foot.anchorMax = new Vector2(0, 0);
            foot.pivot = new Vector2(0, 0);
            foot.anchoredPosition = new Vector2(64, 30);
            foot.sizeDelta = new Vector2(900, 70);
            var progress = new List<string>();
            for (int k = 1; k <= 3; k++)
            {
                string state = AlibiFlow.CardWon(k) ? AlibiFlow.BestStars(k) + "/3 yje" : AlibiFlow.CardUnlocked(k) ? "e hapur" : "e kyçur";
                progress.Add("Karta " + k + ": " + state);
            }
            UI.LabelAt(foot, "Progresi — " + string.Join(" · ", progress.ToArray()), Theme.Sans, 14, Theme.Muted, 0, 0, 900, 20);
            UI.LabelAt(foot, "Grupi 6 · Dea Berisha · Nysret Ukaj · Aulon Miftari · Enis Dapko", Theme.Sans, 14, Theme.Muted, 0, 22, 900, 20);
            UI.LabelAt(foot, "demo v0.2 · Unity · Alibi.Core (C# pa UnityEngine) + Alibi.Unity", Theme.Mono, 13, Theme.Dim, 0, 44, 900, 20);

            var caption = UI.Rect(screen, "Caption");
            caption.anchorMin = caption.anchorMax = new Vector2(1, 0);
            caption.pivot = new Vector2(1, 0);
            caption.anchoredPosition = new Vector2(-60, 40);
            caption.sizeDelta = new Vector2(900, 24);
            var ct = UI.Label(caption, "VILA · " + showcase.N + " TË DYSHUAR, " + showcase.RoomCount + " DHOMA, NJË GËNJESHTAR", Theme.Mono, 14, Theme.Muted, TextAnchor.MiddleRight);
            UI.Stretch(ct.rectTransform, 0, 0, 0, 0);
        }

        RectTransform FullPanel(string kicker, string title, string intro)
        {
            UI.Clear(screen);
            var bg = UI.Panel(screen, "Shade", new Color(0.03f, 0.04f, 0.06f, 0.9f), 0, true);
            UI.Stretch(bg.rectTransform, 0, 0, 0, 0);
            var body = UI.Rect(bg.transform, "Body");
            UI.Stretch(body, 64, 48, 64, 40);
            UI.LabelAt(body, kicker, Theme.Mono, 15, Theme.Gold, 0, 0, 900, 22);
            UI.LabelAt(body, title, Theme.Serif, 54, Theme.Parchment, 0, 24, 1100, 64);
            UI.LabelAt(body, intro, Theme.Sans, 18, Theme.Text2, 0, 92, 1150, 52);
            var back = UI.Button(body, "Menyja", BtnStyle.Ghost, ShowHome, 16, 10);
            UI.PlaceRight(back.Rect, 0, 6, 140, 46);
            return body;
        }

        void ShowCards()
        {
            var body = FullPanel("KARTAT E NIVELEVE · PROGRESSION MAP", "Zgjidh kartën", "Tri karta me të njëjtën mekanikë. Vështirësia rritet përmes vendimeve, jo shpejtësisë: Faza 1 mëson tabelën, Faza 2 grafin e lëvizjes, Faza 3 dëshmitarin që gabon.");
            float cw = 560, gap = 24, y = 160;
            for (int k = 1; k <= 3; k++)
            {
                int card = k;
                var c = LevelCards.Card(k);
                bool unlocked = AlibiFlow.CardUnlocked(k);
                bool won = AlibiFlow.CardWon(k);
                var box = UI.Card(body, "Card" + k, unlocked ? Theme.Panel : new Color(0.08f, 0.09f, 0.11f, 0.9f), won ? Theme.Gold : Theme.Line2, 14);
                UI.Place(box.rectTransform, (k - 1) * (cw + gap), y, cw, 430);
                var r = box.transform;
                UI.LabelAt(r, c.Kicker, Theme.Mono, 13, Theme.Gold, 22, 20, cw - 44, 18);
                UI.LabelAt(r, "Karta " + k, Theme.Serif, 40, Theme.Parchment, 22, 42, cw - 44, 48);
                UI.LabelAt(r, c.Title.Substring(c.Title.IndexOf('—') + 1).Trim(), Theme.SansSemi, 18, Theme.Text, 22, 92, cw - 44, 24);
                string config = c.N + " të dyshuar · " + c.RoomCount + " dhoma · " + c.T + " orë (" + c.Hours[0] + "–" + c.Hours[c.T - 1] + ") · buxhet " + c.Budget + " · par " + c.Par;
                float iy = 124;
                iy += Panels.Paragraph(r, config, Theme.Mono, 13, Theme.Text2, 22, iy, cw - 44) + 10;
                iy += Panels.Paragraph(r, c.Objective, Theme.Sans, 15, Theme.Text2, 22, iy, cw - 44) + 10;
                string skill = k == 1 ? "Aftësia: deklarata për vetveten nuk e përjashton dikë (R03, R04)." : k == 2 ? "Aftësia: koha + grafi — kush nuk arrin në dhomën e krimit përjashtohet (R02, R08)." : "Aftësia: mos beso çdo kontradiktë — duhen dy mbështetje të pavarura (R05).";
                Panels.Paragraph(r, skill, Theme.Sans, 14, Theme.Muted, 22, iy, cw - 44);
                string status = won ? "E fituar · " + AlibiFlow.BestStars(k) + "/3 yje" : unlocked ? "E hapur" : "E kyçur · fito Kartën " + (k - 1);
                UI.LabelAt(r, status, Theme.SansSemi, 15, won ? Theme.GoldText : unlocked ? Theme.Text : Theme.Muted, 22, 372, 300, 30, TextAnchor.MiddleLeft);
                var play = UI.Button(r, unlocked ? "Luaj" : "E kyçur", unlocked ? BtnStyle.Primary : BtnStyle.Disabled, () => AlibiFlow.StartCase(LevelCards.Card(card)), 17, 10);
                UI.PlaceRight(play.Rect, 22, 362, 140, 48);
                play.SetInteractable(unlocked);
            }
            y += 450;
            float lw = (3 * cw + 2 * gap - 2 * gap) / 3f;
            for (int i = 3; i < 6; i++)
            {
                var spec = LevelCards.Levels[i];
                var box = UI.Card(body, "Level" + spec.Level, new Color(0.07f, 0.08f, 0.1f, 0.9f), Theme.Line, 12);
                UI.Place(box.rectTransform, (i - 3) * (lw + gap), y, lw, 96);
                UI.LabelAt(box.transform, "Niveli " + spec.Level + " · " + spec.Name, Theme.SansSemi, 17, Theme.Text2, 18, 14, lw - 36, 24);
                UI.LabelAt(box.transform, spec.Unlocks + " Jashtë kësaj demoje; hapet pas Kartës 3.", Theme.Sans, 13, Theme.Muted, 18, 42, lw - 36, 44);
            }
            y += 116;
            var unlock = UI.Button(body, AlibiFlow.UnlockAll ? "Demo: të gjitha kartat të hapura" : "Demo: hap të gjitha kartat", AlibiFlow.UnlockAll ? BtnStyle.ChipOn : BtnStyle.Ghost, () =>
            {
                AlibiFlow.UnlockAll = !AlibiFlow.UnlockAll;
                ShowCards();
            }, 15, 10);
            UI.Place(unlock.Rect, 0, y, 300, 44);
            var reset = UI.Button(body, "Rifillo progresin", BtnStyle.Ghost, () =>
            {
                AlibiFlow.ResetProgress();
                ShowCards();
            }, 15, 10);
            UI.Place(reset.Rect, 312, y, 200, 44);
        }

        void ShowNewCase()
        {
            var body = FullPanel("RAST I RI · CASEGENERATOR", "Gjenero një rast", "Gjeneratori ndërton të vërtetën, zgjedh gënjeshtrën e fajtorit (LieSelector), kontrollon me zgjidhës që rasti të ketë saktësisht një zgjidhje dhe llogarit par-in me ParPolicy. Rastet me defekt refuzohen dhe hapet vetë seed-i tjetër.");
            float cw = 560, gap = 24, y = 170;
            for (int i = 0; i < 3; i++)
            {
                int mode = i;
                bool on = pickMode == i;
                var b = UI.Button(body, null, on ? BtnStyle.ChipOn : BtnStyle.Chip, () =>
                {
                    pickMode = mode;
                    ShowNewCase();
                }, 16, 14);
                UI.Place(b.Rect, i * (cw + gap), y, cw, 230);
                UI.LabelAt(b.Rect, ModeNames[i], Theme.Serif, 32, Theme.Parchment, 22, 20, cw - 44, 40);
                float iy = 70;
                iy += Panels.Paragraph(b.Rect, ModeDesc[i], Theme.Sans, 16, Theme.Text, 22, iy, cw - 44) + 10;
                Panels.Paragraph(b.Rect, ModeNotes[i], Theme.Sans, 14, Theme.Muted, 22, iy, cw - 44);
                if (i == 2)
                {
                    var tag = UI.Card(b.Rect, "Tag", new Color(0, 0, 0, 0), Theme.WithAlpha(Theme.Gold, 0.6f), 12);
                    UI.PlaceRight(tag.rectTransform, 20, 26, 130, 26);
                    var tt = UI.Label(tag.transform, "eksperimental", Theme.Sans, 13, Theme.GoldText, TextAnchor.MiddleCenter);
                    UI.Stretch(tt.rectTransform, 0, 0, 0, 0);
                }
            }
            y += 256;
            var seedBox = UI.Card(body, "Seed", Theme.Panel, Theme.Line, 12);
            UI.Place(seedBox.rectTransform, 0, y, 3 * cw + 2 * gap, 86);
            UI.LabelAt(seedBox.transform, "SEED", Theme.Mono, 13, Theme.Muted, 22, 14, 200, 18);
            UI.LabelAt(seedBox.transform, seed.ToString(), Theme.Mono, 30, Theme.Parchment, 22, 36, 200, 40);
            var reroll = UI.Button(seedBox.transform, "Seed i ri", BtnStyle.Ghost, () =>
            {
                seed = UnityEngine.Random.Range(1000, 9999);
                ShowNewCase();
            }, 15, 10);
            UI.Place(reroll.Rect, 180, 22, 140, 44);
            UI.LabelAt(seedBox.transform, "I njëjti seed jep gjithmonë të njëjtin rast, prandaj rasti mund të riprodhohet dhe të luhet përsëri (T11).", Theme.Sans, 14, Theme.Muted, 340, 22, 900, 44, TextAnchor.MiddleLeft);
            var go = UI.Button(seedBox.transform, "Gjenero rastin", BtnStyle.Primary, StartGeneration, 17, 10);
            UI.PlaceRight(go.Rect, 22, 20, 220, 48);
        }

        void StartGeneration()
        {
            int level = pickMode == 0 ? 1 : pickMode == 1 ? 2 : 3;
            var type = pickMode == 2 ? CaseType.Sabotimi : CaseType.Vrasja;
            int s = seed;
            UI.Clear(overlay);
            var shade = Panels.Overlay(overlay, "Generating");
            var card = UI.Card(shade, "Card", Theme.Panel, Theme.Line2, 16);
            Panels.Center(card.rectTransform, 760, 190);
            UI.LabelAt(card.transform, "RAST I RI", Theme.Mono, 14, Theme.Gold, 30, 28, 700, 20);
            UI.LabelAt(card.transform, "Gjeneratori po ndërton rastin…", Theme.Serif, 36, Theme.Parchment, 30, 52, 700, 46);
            genStatus = UI.LabelAt(card.transform, "", Theme.Sans, 15, Theme.Text2, 30, 112, 700, 50);
            genStarted = Time.time;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                var r = CaseGenerator.Generate(level, type, s);
                genTask = Task.FromResult(r);
            }
            else
            {
                genTask = Task.Run(() => CaseGenerator.Generate(level, type, s));
            }
        }

        void ShowHowTo()
        {
            UI.Clear(overlay);
            var shade = Panels.Overlay(overlay, "HowTo");
            Panels.HowTo(shade, () => UI.Clear(overlay));
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
