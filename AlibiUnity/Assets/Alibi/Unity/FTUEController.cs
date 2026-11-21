// FTUEController.cs — First-Time User Experience spotlight/coachmark system
// Overlay me cutout rreth target UI elementeve + floating tooltip + action gating
// Procedural UI: nuk ka prefab, ScriptableObject, apo Editor wiring
// GameManager/MainMenuController regjistrojne target elementet me Register()

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Alibi.Unity
{
    public class FTUEController : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        //  REGISTRATION — skriptet tjera regjistrojne UI elementet
        // ═══════════════════════════════════════════════════════════

        static readonly Dictionary<string, RectTransform> targets = new();

        /// <summary>Regjistro nje UI element qe FTUE mund ta highlight.
        /// Thirret nga GameManager/MainMenuController pas ndertimit te elementit.</summary>
        public static void Register(string key, RectTransform rt)
        {
            if (rt) targets[key] = rt;
        }
        public static void Unregister(string key) => targets.Remove(key);
        public static void ClearTargets() => targets.Clear();

        // ═══════════════════════════════════════════════════════════
        //  ACTION NOTIFICATION — skriptet tjera lajmerojne veprime
        // ═══════════════════════════════════════════════════════════

        static FTUEController active;

        /// <summary>Lajmero qe ndodhi nje veprim (psh "room_clicked", "camera_switched").
        /// Nese stepi aktual pret kete veprim, kalohet automatikisht.</summary>
        public static void NotifyAction(string action)
        {
            if (active != null && active.running && active.idx < active.steps.Length)
            {
                var s = active.steps[active.idx];
                if (s.Completion == Gate.Notify && s.ActionId == action)
                    active.Advance();
            }
        }

        /// <summary>True kur FTUE eshte aktiv ne nje Info step (duhet te bllokohen
        /// world clicks perveç UI tooltip). GameManager mund ta perdore.</summary>
        public static bool BlockWorldInput =>
            active != null && active.running && active.idx < active.steps.Length
            && active.steps[active.idx].Completion == Gate.Info;

        // ═══════════════════════════════════════════════════════════
        //  PERSISTENCE — PlayerPrefs
        // ═══════════════════════════════════════════════════════════

        const string PrefMenu  = "alibi_ftue_menu";
        const string PrefCard1 = "alibi_ftue_card1";

        public static bool MenuDone  => PlayerPrefs.GetInt(PrefMenu, 0) == 1;
        public static bool Card1Done => PlayerPrefs.GetInt(PrefCard1, 0) == 1;

        static void SaveMenu()  { PlayerPrefs.SetInt(PrefMenu, 1);  PlayerPrefs.Save(); }
        static void SaveCard1() { PlayerPrefs.SetInt(PrefCard1, 1); PlayerPrefs.Save(); }

        /// <summary>Reset FTUE flags — per testing ose kur krijohet llogari e re.</summary>
        public static void ResetFTUE()
        {
            PlayerPrefs.DeleteKey(PrefMenu);
            PlayerPrefs.DeleteKey(PrefCard1);
            PlayerPrefs.Save();
        }

        // ═══════════════════════════════════════════════════════════
        //  STEP DATA
        // ═══════════════════════════════════════════════════════════

        enum Gate { Info, Move, Key, Notify }

        class Step
        {
            public string[] Targets;    // registration keys per spotlight (null = full dim, tooltip centered)
            public string   Header;     // bold header
            public string   Body;       // body text
            public Gate     Completion; // si kalohet stepi
            public KeyCode  WaitKey;    // per Gate.Key (Tab, E, etj)
            public string   ActionId;   // per Gate.Notify
            public float    Pad = 14f;  // padding rreth cutout
        }

        // ═══════════════════════════════════════════════════════════
        //  STATE
        // ═══════════════════════════════════════════════════════════

        Step[] steps;
        int    idx;
        bool   running;
        bool   isMenu;      // true = menu sequence, false = card1 sequence
        Action onDone;       // callback kur mbaron

        // ═══════════════════════════════════════════════════════════
        //  UI REFERENCES
        // ═══════════════════════════════════════════════════════════

        Canvas   overlayCanvas;
        Image[]  dimPanels = new Image[4];   // top, bot, left, right
        Image    frameBorder;                // gold border rreth cutout
        RectTransform frameRect;

        // Tooltip
        RectTransform tooltipRect;
        Text     lblCounter;
        Text     lblHeader;
        Text     lblBody;
        GameObject btnVazhdo;

        float pulseT;

        // ═══════════════════════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════════════════════

        /// <summary>Fillo sekuencen e menu (highlight Card 1). Kthen null nese eshte bere.</summary>
        public static FTUEController StartMenu(Action onDone = null)
        {
            // TODO: ri-aktivizo kur versioni final — per tani tregoje cdo here per profesorin
            // if (MenuDone) { onDone?.Invoke(); return null; }
            var ctrl = Create(true, onDone);
            ctrl.steps = DefineMenuSteps();
            ctrl.BuildUI();
            ctrl.Begin();
            return ctrl;
        }

        /// <summary>Fillo sekuencen e Card 1 gameplay. Kthen null nese eshte bere.</summary>
        public static FTUEController StartCard1(Action onDone = null)
        {
            // TODO: ri-aktivizo kur versioni final — per tani tregoje cdo here per profesorin
            // if (Card1Done) { onDone?.Invoke(); return null; }
            var ctrl = Create(false, onDone);
            ctrl.steps = DefineCard1Steps();
            ctrl.BuildUI();
            ctrl.Begin();
            return ctrl;
        }

        static FTUEController Create(bool menu, Action done)
        {
            var go = new GameObject("FTUEController");
            var ctrl = go.AddComponent<FTUEController>();
            ctrl.isMenu = menu;
            ctrl.onDone = done;
            return ctrl;
        }

        /// <summary>Skip-o gjithe walkthrough-in. Markon si te perfunduar.</summary>
        public void Skip()
        {
            MarkDone();
            running = false;
            Teardown();
            onDone?.Invoke();
        }

        /// <summary>Ndalo pa markuar si te perfunduar (psh kur "Udh: OFF").</summary>
        public void Stop()
        {
            running = false;
            Teardown();
        }

        // ═══════════════════════════════════════════════════════════
        //  BUILD OVERLAY UI
        // ═══════════════════════════════════════════════════════════

        void BuildUI()
        {
            active = this;

            // --- Canvas me sort order te larte ---
            var cgo = new GameObject("FTUE_Overlay");
            overlayCanvas = cgo.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 200;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode  = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();
            var crt = cgo.GetComponent<RectTransform>();

            // --- 4 dim panels (errta, bllokon raycast jashte cutout) ---
            for (int i = 0; i < 4; i++)
            {
                var p = new GameObject($"Dim{i}");
                p.transform.SetParent(crt, false);
                var img = p.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0.72f);
                img.raycastTarget = true;
                var rt = p.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
                dimPanels[i] = img;
            }

            // --- Highlight frame (gold border rreth target) ---
            var hgo = new GameObject("Frame");
            hgo.transform.SetParent(crt, false);
            frameRect = hgo.AddComponent<RectTransform>();
            frameRect.anchorMin = frameRect.anchorMax = frameRect.pivot = Vector2.zero;
            frameBorder = hgo.AddComponent<Image>();
            frameBorder.color = Color.clear;             // fill transparent
            frameBorder.raycastTarget = false;
            var outline = hgo.AddComponent<Outline>();    // gold glow
            outline.effectColor    = Theme.Hex("D2AD62");
            outline.effectDistance = new Vector2(3, 3);

            // --- Tooltip card ---
            BuildTooltip(crt);
        }

        void BuildTooltip(RectTransform parent)
        {
            // Root container
            var root = UI.Rect(parent, "Tooltip");
            tooltipRect = root;
            root.sizeDelta = new Vector2(380, 0);
            root.pivot = new Vector2(0, 1);

            // Background
            var bg = root.gameObject.AddComponent<Image>();
            bg.color  = Theme.Hex("1C2028");
            bg.sprite = Theme.Rounded(12);
            bg.type   = Image.Type.Sliced;
            bg.raycastTarget = true;

            // Gold accent line lart
            var accent = UI.Rect(root, "AccentLine");
            accent.anchorMin = new Vector2(0, 1);
            accent.anchorMax = new Vector2(1, 1);
            accent.pivot     = new Vector2(0, 1);
            accent.anchoredPosition = Vector2.zero;
            accent.sizeDelta = new Vector2(0, 3);
            var accentImg = accent.gameObject.AddComponent<Image>();
            accentImg.color = Theme.Hex("D2AD62");
            accentImg.raycastTarget = false;

            // VerticalLayoutGroup per auto-height
            var vl = root.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(20, 20, 18, 14);
            vl.spacing = 6;
            vl.childForceExpandWidth  = true;
            vl.childForceExpandHeight = false;
            vl.childControlWidth  = true;
            vl.childControlHeight = true;

            var fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Counter "1 / 10"
            lblCounter = Lbl(root, 13, Theme.Hex("6E747D"), Theme.Sans);

            // Header
            lblHeader = Lbl(root, 18, Theme.Hex("F4DFB0"), Theme.SansBold);

            // Body
            lblBody = Lbl(root, 15, Theme.Hex("ECE6D8"), Theme.Sans);

            // Spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(root, false);
            var sle = spacer.AddComponent<LayoutElement>();
            sle.preferredHeight = 4;

            // "Vazhdo" button (vetem per Info steps)
            btnVazhdo = MakeBtn(root, "Vazhdo ▸", BtnStyle.Primary, () => Advance());

            // "Kalo udhezuesin" skip button (gjithmone visible)
            MakeBtn(root, "Kalo udhëzuesin", BtnStyle.Ghost, () => Skip());
        }

        Text Lbl(RectTransform parent, int size, Color color, Font font)
        {
            var go = new GameObject("L");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font      = font;
            t.fontSize   = size;
            t.color      = color;
            t.alignment  = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            return t;
        }

        GameObject MakeBtn(RectTransform parent, string label, BtnStyle style, Action click)
        {
            var rt = UI.Button(parent, label, style, click, 14, 8, Theme.SansSemi);
            var le = rt.Rect.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 36;
            return rt.Rect.gameObject;
        }

        // ═══════════════════════════════════════════════════════════
        //  STEP FLOW
        // ═══════════════════════════════════════════════════════════

        void Begin()
        {
            idx     = 0;
            running = true;
            ShowStep();
        }

        void ShowStep()
        {
            if (idx >= steps.Length) { Finish(); return; }
            var step = steps[idx];

            // --- Gjej target rect(s) ---
            Rect union   = default;
            bool hasTgt  = false;

            if (step.Targets != null)
            {
                foreach (var key in step.Targets)
                {
                    if (targets.TryGetValue(key, out var rt) && rt != null)
                    {
                        var r = ScreenRect(rt);
                        if (!hasTgt) { union = r; hasTgt = true; }
                        else union = Union(union, r);
                    }
                }
            }

            // --- Poziciono cutout ose full dim ---
            if (hasTgt)
                ApplyCutout(union, step.Pad);
            else
                ApplyFullDim();

            // --- Tooltip content ---
            lblCounter.text = $"{idx + 1} / {steps.Length}";
            lblHeader.text  = step.Header;
            lblBody.text    = step.Body;

            // Vazhdo visible vetem per Info steps
            btnVazhdo.SetActive(step.Completion == Gate.Info);

            // --- Poziciono tooltip ---
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
            PositionTooltip(union, hasTgt);

            pulseT = 0;
        }

        void Advance()
        {
            idx++;
            if (idx >= steps.Length) Finish();
            else ShowStep();
        }

        void Finish()
        {
            MarkDone();
            running = false;
            Teardown();
            onDone?.Invoke();
        }

        void MarkDone()
        {
            // TODO: ri-aktivizo kur versioni final
            // if (isMenu) SaveMenu(); else SaveCard1();
        }

        void Teardown()
        {
            if (active == this) active = null;
            if (overlayCanvas) Destroy(overlayCanvas.gameObject);
            Destroy(gameObject);
        }

        // ═══════════════════════════════════════════════════════════
        //  CUTOUT POSITIONING (4 dim panels rreth target rect)
        // ═══════════════════════════════════════════════════════════

        void ApplyCutout(Rect screenRect, float pad)
        {
            var crt = overlayCanvas.GetComponent<RectTransform>();
            float cw = crt.rect.width;
            float ch = crt.rect.height;

            // Convert screen coords → canvas local coords
            ToLocal(crt, screenRect.xMin - pad, screenRect.yMin - pad, out var lo);
            ToLocal(crt, screenRect.xMax + pad, screenRect.yMax + pad, out var hi);

            // Offset sepse canvas pivot = 0.5
            float ox = cw * 0.5f;
            float oy = ch * 0.5f;
            float L  = Mathf.Max(0, lo.x + ox);
            float R  = Mathf.Min(cw, hi.x + ox);
            float B  = Mathf.Max(0, lo.y + oy);
            float T  = Mathf.Min(ch, hi.y + oy);

            SetDim(0, 0, T, cw, ch - T);         // top
            SetDim(1, 0, 0, cw, B);               // bottom
            SetDim(2, 0, B, L, T - B);            // left
            SetDim(3, R, B, cw - R, T - B);       // right

            // Frame
            frameRect.gameObject.SetActive(true);
            frameRect.anchoredPosition = new Vector2(L, B);
            frameRect.sizeDelta = new Vector2(R - L, T - B);
        }

        void ApplyFullDim()
        {
            var crt = overlayCanvas.GetComponent<RectTransform>();
            SetDim(0, 0, 0, crt.rect.width, crt.rect.height);
            SetDim(1, 0, 0, 0, 0);
            SetDim(2, 0, 0, 0, 0);
            SetDim(3, 0, 0, 0, 0);
            frameRect.gameObject.SetActive(false);
        }

        void SetDim(int i, float x, float y, float w, float h)
        {
            var rt = dimPanels[i].rectTransform;
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(Mathf.Max(0, w), Mathf.Max(0, h));
        }

        // ═══════════════════════════════════════════════════════════
        //  TOOLTIP POSITIONING (auto-anchor: right, left, bottom, top)
        // ═══════════════════════════════════════════════════════════

        void PositionTooltip(Rect screenTarget, bool hasTgt)
        {
            var crt = overlayCanvas.GetComponent<RectTransform>();
            float cw = crt.rect.width;
            float ch = crt.rect.height;
            float tw = tooltipRect.rect.width;
            float th = tooltipRect.rect.height;

            if (!hasTgt)
            {
                // Qendro ne ekran
                tooltipRect.anchorMin = tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
                tooltipRect.pivot = new Vector2(0.5f, 0.5f);
                tooltipRect.anchoredPosition = Vector2.zero;
                return;
            }

            tooltipRect.anchorMin = tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);

            // Target ne canvas coords
            ToLocal(crt, screenTarget.center.x, screenTarget.center.y, out var center);
            ToLocal(crt, screenTarget.xMin, screenTarget.yMin, out var tMin);
            ToLocal(crt, screenTarget.xMax, screenTarget.yMax, out var tMax);

            float gap   = 20;
            float halfW = cw * 0.5f;
            float halfH = ch * 0.5f;

            float spR = halfW - tMax.x;
            float spL = tMin.x + halfW;
            float spB = tMin.y + halfH;
            float spT = halfH - tMax.y;

            Vector2 pos;
            if (spR >= tw + gap)
            {
                tooltipRect.pivot = new Vector2(0, 0.5f);
                pos = new Vector2(tMax.x + gap, center.y);
            }
            else if (spL >= tw + gap)
            {
                tooltipRect.pivot = new Vector2(1, 0.5f);
                pos = new Vector2(tMin.x - gap, center.y);
            }
            else if (spB >= th + gap)
            {
                tooltipRect.pivot = new Vector2(0.5f, 1);
                pos = new Vector2(center.x, tMin.y - gap);
            }
            else
            {
                tooltipRect.pivot = new Vector2(0.5f, 0);
                pos = new Vector2(center.x, tMax.y + gap);
            }

            // Clamp brenda ekranit
            float px = tooltipRect.pivot.x;
            float py = tooltipRect.pivot.y;
            pos.x = Mathf.Clamp(pos.x, -halfW + tw * px + 10, halfW - tw * (1 - px) - 10);
            pos.y = Mathf.Clamp(pos.y, -halfH + th * py + 10, halfH - th * (1 - py) - 10);

            tooltipRect.anchoredPosition = pos;
        }

        // ═══════════════════════════════════════════════════════════
        //  UPDATE — input detection + highlight pulse
        // ═══════════════════════════════════════════════════════════

        void Update()
        {
            if (!running || idx >= steps.Length) return;

            // Gold border pulse
            pulseT += Time.unscaledDeltaTime;
            if (frameBorder && frameRect.gameObject.activeSelf)
            {
                var o = frameRect.GetComponent<Outline>();
                if (o != null)
                {
                    var c = Theme.Hex("D2AD62");
                    c.a = 0.5f + 0.4f * Mathf.Sin(pulseT * 3.5f);
                    o.effectColor = c;
                }
            }

            var step = steps[idx];
            var kb   = Keyboard.current;
            if (kb == null) return;

            switch (step.Completion)
            {
                case Gate.Move:
                    if (kb.wKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame ||
                        kb.aKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame ||
                        kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame ||
                        kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)
                        Advance();
                    break;

                case Gate.Key:
                    if (step.WaitKey == KeyCode.Tab && kb.tabKey.wasPressedThisFrame)
                        Advance();
                    else if (step.WaitKey == KeyCode.E && kb.eKey.wasPressedThisFrame)
                        Advance();
                    break;

                // Gate.Info → kalohet me butonin Vazhdo (onClick)
                // Gate.Notify → kalohet me NotifyAction() nga skriptet tjera
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════

        /// <summary>Merr screen-space Rect te nje RectTransform (ScreenSpaceOverlay canvas).</summary>
        static Rect ScreenRect(RectTransform rt)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            float xMin = Mathf.Min(c[0].x, c[1].x, c[2].x, c[3].x);
            float xMax = Mathf.Max(c[0].x, c[1].x, c[2].x, c[3].x);
            float yMin = Mathf.Min(c[0].y, c[1].y, c[2].y, c[3].y);
            float yMax = Mathf.Max(c[0].y, c[1].y, c[2].y, c[3].y);
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        static Rect Union(Rect a, Rect b)
        {
            float x0 = Mathf.Min(a.xMin, b.xMin);
            float y0 = Mathf.Min(a.yMin, b.yMin);
            float x1 = Mathf.Max(a.xMax, b.xMax);
            float y1 = Mathf.Max(a.yMax, b.yMax);
            return new Rect(x0, y0, x1 - x0, y1 - y0);
        }

        static void ToLocal(RectTransform canvas, float sx, float sy, out Vector2 local)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas, new Vector2(sx, sy), null, out local);
        }

        // ═══════════════════════════════════════════════════════════
        //  STEP DEFINITIONS
        // ═══════════════════════════════════════════════════════════

        // ---- Sequence A: Main Menu (1 step) ----

        static Step[] DefineMenuSteps() => new[]
        {
            new Step
            {
                Targets    = new[] { "menu_card1" },
                Header     = "Fillo hetimin e parë",
                Body       = "Kliko Kartën 1 — \"Mësimi\" për të filluar " +
                             "hetimin tënd të parë. Do të mësosh si të " +
                             "marrsh në pyetje të dyshuarit dhe të zgjidhsh rastin.",
                Completion = Gate.Notify,
                ActionId   = "card1_clicked"
            }
        };

        // ---- Sequence B: Card 1 Gameplay (10 steps) ----

        static Step[] DefineCard1Steps() => new[]
        {
            // 1. Objektivi i Rastit — crime pill + title chip
            new Step
            {
                Targets    = new[] { "hud_crime_pill", "hud_title_chip" },
                Header     = "Objektivi i Rastit",
                Body       = "Vrasje në Bibliotekë, ora 21:00. Ti duhet të gjesh " +
                             "kush e kreu.\n\n" +
                             "Të dyshuarit e pafajshëm thonë gjithmonë " +
                             "të vërtetën. Fajtori gënjen të paktën " +
                             "një herë.",
                Completion = Gate.Info,
                Pad        = 16
            },

            // 2. Buxheti
            new Step
            {
                Targets    = new[] { "topbar_budget" },
                Header     = "Buxheti i Pyetjeve",
                Body       = "Ke 12 pikë buxheti. Çdo pyetje kushton 1–2 " +
                             "pikë, kështu që zgjedh me kujdes se kënë pyet.",
                Completion = Gate.Info
            },

            // 3. Levizja + Minimap
            new Step
            {
                Targets    = new[] { "hud_minimap" },
                Header     = "Lëvizja dhe Minimap-i",
                Body       = "Pika e artë në minimap je ti. Lëvizu me W/A/S/D " +
                             "(ose shigjetat) për të eksploruar villën.\n\n" +
                             "Provo tani — lëvizu pak!",
                Completion = Gate.Move,
                Pad        = 16
            },

            // 4. Kamera
            new Step
            {
                Targets    = new[] { "cam_controls" },
                Header     = "Këndi i Kamerës",
                Body       = "Kliko për të ndërruar mes pamjes së " +
                             "parë, të tretë dhe nga lart.\n\n" +
                             "Provo — kliko një buton të kamerës!",
                Completion = Gate.Notify,
                ActionId   = "camera_switched"
            },

            // 5. Marrja ne pyetje (approach suspect + E)
            new Step
            {
                Targets    = null, // paneli nuk ekziston ende
                Header     = "Marrja në Pyetje",
                Body       = "Afrohu një të dyshuari (figurat në dhoma) " +
                             "dhe shtyp [E] për të hapur panelin e pyetjeve.\n\n" +
                             "Pastaj zgjidh pyetjen dhe kliko për të pyetur.",
                Completion = Gate.Key,
                WaitKey    = KeyCode.E
            },

            // 6. Dosja e deshmive
            new Step
            {
                Targets    = new[] { "panel_evidence_tab" },
                Header     = "Dosja e Dëshmive",
                Body       = "Këtu ruhen të gjitha dëshmirët që " +
                             "ke mbledhur. Shiko kundërshtitë — ato të " +
                             "ndihmojnë të identifikosh fajtoring.",
                Completion = Gate.Info
            },

            // 7. Tabela e deduksionit
            new Step
            {
                Targets    = new[] { "hud_tab_hint" },
                Header     = "Tabela e Deduksionit",
                Body       = "Shtyp [Tab] për të hapur tabelën e deduksionit. " +
                             "Aty sheh kush ku mund të ketë qenë në " +
                             "çdo orë. Tabela mbushet automatikisht (AC-3).\n\n" +
                             "Shtyp [Tab] tani!",
                Completion = Gate.Key,
                WaitKey    = KeyCode.Tab
            },

            // 8. Inspektimi i dhomave
            new Step
            {
                Targets    = null,
                Header     = "Inspektimi i Dhomave",
                Body       = "Kliko një dhomë në hartë (ose në minimap) " +
                             "për të hapur kartën e dhomës. Aty sheh kush " +
                             "ishte brenda, kush mund të ketë qenë, e kush jo.\n\n" +
                             "Provo — kliko një dhomë!",
                Completion = Gate.Notify,
                ActionId   = "room_clicked"
            },

            // 9. Ndihma & Udhezuesi
            new Step
            {
                Targets    = new[] { "topbar_help", "topbar_guide_toggle" },
                Header     = "Ndihma & Udhëzuesi",
                Body       = "\"?\" hap rregullat e lojës.\n" +
                             "\"Udh: ON/OFF\" aktivizon ose çaktivizon hapat e " +
                             "udhëzuesit në Kartën 1.",
                Completion = Gate.Info
            },

            // 10. Akuzo
            new Step
            {
                Targets    = new[] { "topbar_accuse" },
                Header     = "Akuza Përfundimtare",
                Body       = "Kur ke mbledhur mjaft dëshmire dhe ke identifikuar " +
                             "fajtoring, kliko \"Akuzo\" për të zgjedhur personin, " +
                             "dhomën dhe orën.\n\n" +
                             "Sukses në hetimin tënd të parë!",
                Completion = Gate.Info
            }
        };
    }
}
