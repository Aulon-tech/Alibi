// MainMenuController.cs — Alibi kryefaqja (redizajnuar v3)
// Procedural UI: titull + butona (majtas), siluet detektivi me rim-light (djathtas)
// Lupe SDF, rreze drite, grimca ari — pa prefab, pa asset
using System.Collections;
using Alibi.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Alibi.Unity
{
    public class MainMenuController : MonoBehaviour
    {
        // ── Cache per animacion ──
        Canvas     _canvas;
        RawImage   _lensGlow;
        float      _glowT;

        // ── Grimca (particles) ──
        const int  PCount = 22;
        RectTransform[] _pRTs;
        float[]  _pSpeeds;
        float[]  _pDrifts;

        // ── Sprite rrethi (generohet 1 here, riperdoret) ──
        static Sprite _circleSpr;

        // ── Level-select overlay ref ──
        GameObject _levelOverlay;

        // ==============================================================
        //  LIFECYCLE
        // ==============================================================

        void Start()
        {
            HideSceneObjects();
            Build();
        }

        void Update()
        {
            // Pulsi i drites ne lentet e lupes
            if (_lensGlow != null)
            {
                _glowT += Time.deltaTime;
                float a = 0.22f + 0.16f * Mathf.Sin(_glowT * 1.2f);
                _lensGlow.color = Theme.WithAlpha(Theme.Gold, a);
            }

            // Grimcat qe notojne lart
            if (_pRTs != null)
            {
                for (int i = 0; i < _pRTs.Length; i++)
                {
                    if (_pRTs[i] == null) continue;
                    var pos = _pRTs[i].anchoredPosition;
                    pos.y += _pSpeeds[i] * Time.deltaTime;
                    pos.x += _pDrifts[i] * Time.deltaTime;
                    if (pos.y > 1200f)
                    {
                        pos.y = Random.Range(-80f, -20f);
                        pos.x = Random.Range(60f, 1860f);
                    }
                    _pRTs[i].anchoredPosition = pos;
                }
            }
        }

        // ==============================================================
        //  FSHEH OBJEKTET 3D TE SKENES
        // ==============================================================

        void HideSceneObjects()
        {
            foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                cam.gameObject.SetActive(false);

            var camGo = new GameObject("_MenuCam");
            var c     = camGo.AddComponent<Camera>();
            c.clearFlags      = CameraClearFlags.SolidColor;
            // Midnight blue — me i pasur se zi i thjeshtë
            c.backgroundColor = new Color(0.035f, 0.040f, 0.065f);
            c.cullingMask     = 0;
        }

        // ==============================================================
        //  NDERTO MENU-NE
        // ==============================================================

        void Build()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            var root = _canvas.GetComponent<RectTransform>();

            // Background midnight blue
            var bg = SolidRect(root, "bg", new Color(0.040f, 0.045f, 0.070f));
            Stretch(bg);

            // Vinjete
            BuildVignette(bg);

            // Rreze drite (prapa te gjithash)
            BuildLightRays(bg);

            // Silueta e detektivit (djathtas)
            BuildDetective(bg);

            // Paneli majtas: titulli + butonat
            BuildLeft(bg);

            // Grimca ari (mbi te gjitha)
            BuildParticles(bg);
        }

        // ==============================================================
        //  VIGNETTE
        // ==============================================================

        void BuildVignette(RectTransform parent)
        {
            int s = 8;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            float r = s * 0.5f;
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - r + 0.5f) / r;
                    float dy = (y - r + 0.5f) / r;
                    float d  = Mathf.Sqrt(dx * dx + dy * dy);
                    byte a   = (byte)(Mathf.Clamp01(d - 0.15f) * 140);
                    px[y * s + x] = new Color32(0, 0, 8, a);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            tex.filterMode = FilterMode.Bilinear;

            var go = new GameObject("vignette");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            Stretch(rt);
            var ri = go.AddComponent<RawImage>();
            ri.texture = tex;
            ri.raycastTarget = false;
        }

        // ==============================================================
        //  RREZE DRITE (light rays — atmosfer noir)
        // ==============================================================

        void BuildLightRays(RectTransform parent)
        {
            var rayTex = RayGradientTex(64, 4);

            float[] angles = { -55f, -38f, -22f, -5f, 12f, 28f };
            float[] alphas = { 0.025f, 0.045f, 0.065f, 0.05f, 0.035f, 0.02f };
            float[] widths = { 110f, 85f, 100f, 90f, 70f, 80f };

            for (int i = 0; i < angles.Length; i++)
            {
                var go = new GameObject("ray_" + i);
                go.transform.SetParent(parent, false);
                var rt = go.AddComponent<RectTransform>();

                // Qendra: zona e detektivit (djathtas-lart)
                rt.anchorMin = rt.anchorMax = new Vector2(0.72f, 0.62f);
                rt.sizeDelta = new Vector2(widths[i], 2400f);
                rt.localEulerAngles = new Vector3(0, 0, angles[i]);

                var ri = go.AddComponent<RawImage>();
                ri.texture = rayTex;
                ri.color   = Theme.WithAlpha(Theme.Gold, alphas[i]);
                ri.raycastTarget = false;
            }
        }

        // ==============================================================
        //  SILUETA E DETEKTIVIT + LUPA
        // ==============================================================

        void BuildDetective(RectTransform parent)
        {
            // Kontejneri djathtas (58% e ekranit)
            var box = SolidRect(parent, "detective_area", Color.clear);
            box.anchorMin = new Vector2(0.42f, 0f);
            box.anchorMax = Vector2.one;
            box.offsetMin = box.offsetMax = Vector2.zero;

            // Ambient glow prapa detektivit
            var ambGo = new GameObject("ambient_glow");
            ambGo.transform.SetParent(box, false);
            var ambR = ambGo.AddComponent<RectTransform>();
            ambR.anchorMin = ambR.anchorMax = new Vector2(0.45f, 0.52f);
            ambR.sizeDelta = new Vector2(800, 900);
            var ambRI = ambGo.AddComponent<RawImage>();
            ambRI.texture = GlowTex(128);
            ambRI.color   = new Color(0.824f, 0.678f, 0.384f, 0.06f);
            ambRI.raycastTarget = false;

            // Silueta e detektivit (polygon fill + rim light)
            var detGo = new GameObject("detective");
            detGo.transform.SetParent(box, false);
            var detR = detGo.AddComponent<RectTransform>();
            detR.anchorMin = new Vector2(0.02f, 0f);
            detR.anchorMax = new Vector2(0.92f, 1f);
            detR.offsetMin = detR.offsetMax = Vector2.zero;
            var detRI = detGo.AddComponent<RawImage>();
            detRI.texture = DetectiveSilhouetteTex(512);
            detRI.raycastTarget = false;

            // Lupa (SDF) — pozicionohet ne nivelin e fytyres
            var glassGo = new GameObject("glass");
            glassGo.transform.SetParent(box, false);
            var glassR = glassGo.AddComponent<RectTransform>();
            glassR.anchorMin = glassR.anchorMax = new Vector2(0.72f, 0.78f);
            glassR.sizeDelta = new Vector2(240, 240);
            var glassRI = glassGo.AddComponent<RawImage>();
            glassRI.texture = MagnifyingGlassTex(256);
            glassRI.raycastTarget = false;

            // Glow i lentet (animohet)
            var glowGo = new GameObject("lens_glow");
            glowGo.transform.SetParent(box, false);
            var glowR = glowGo.AddComponent<RectTransform>();
            glowR.anchorMin = glowR.anchorMax = new Vector2(0.72f, 0.78f);
            glowR.sizeDelta = new Vector2(240, 240);
            _lensGlow           = glowGo.AddComponent<RawImage>();
            _lensGlow.texture   = LensGlowTex(256);
            _lensGlow.color     = Theme.WithAlpha(Theme.Gold, 0.22f);
            _lensGlow.raycastTarget = false;
        }

        // ==============================================================
        //  PANELI MAJTAS — TITULLI + BUTONAT
        // ==============================================================

        void BuildLeft(RectTransform parent)
        {
            var left = SolidRect(parent, "left", Color.clear);
            left.anchorMin = new Vector2(0f, 0f);
            left.anchorMax = new Vector2(0.42f, 1f);
            left.offsetMin = left.offsetMax = Vector2.zero;

            // ── Hije titulli (per thellesi) ──
            var shadowGo = new GameObject("title_shadow");
            shadowGo.transform.SetParent(left, false);
            var shadowR = shadowGo.AddComponent<RectTransform>();
            shadowR.anchorMin = new Vector2(0.12f, 0.68f);
            shadowR.anchorMax = new Vector2(0.98f, 0.98f);
            shadowR.offsetMin = new Vector2(5, -5);
            shadowR.offsetMax = new Vector2(5, -5);
            var shadowTxt        = shadowGo.AddComponent<Text>();
            shadowTxt.text       = "A L I B I";
            shadowTxt.font       = Theme.Serif;
            shadowTxt.fontSize   = 148;
            shadowTxt.color      = new Color(0, 0, 0, 0.50f);
            shadowTxt.alignment  = TextAnchor.LowerLeft;
            shadowTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            shadowTxt.verticalOverflow   = VerticalWrapMode.Overflow;
            shadowTxt.raycastTarget = false;

            // ── ALIBI — titulli kryesor ──
            var titleGo = new GameObject("title");
            titleGo.transform.SetParent(left, false);
            var titleR = titleGo.AddComponent<RectTransform>();
            titleR.anchorMin = new Vector2(0.12f, 0.68f);
            titleR.anchorMax = new Vector2(0.98f, 0.98f);
            titleR.offsetMin = titleR.offsetMax = Vector2.zero;

            var titleTxt        = titleGo.AddComponent<Text>();
            titleTxt.text       = "A L I B I";
            titleTxt.font       = Theme.Serif;
            titleTxt.fontSize   = 148;
            titleTxt.color      = Theme.Parchment;
            titleTxt.alignment  = TextAnchor.LowerLeft;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleTxt.verticalOverflow   = VerticalWrapMode.Overflow;

            // Vije ari nen titull
            var line = SolidRect(left, "gold_line", Theme.Gold);
            line.anchorMin = new Vector2(0.12f, 0.660f);
            line.anchorMax = new Vector2(0.56f, 0.666f);
            line.offsetMin = line.offsetMax = Vector2.zero;

            // Diamante dekorative ne skajet e vijes
            var dL = SolidRect(left, "dL", Theme.Gold);
            dL.anchorMin = dL.anchorMax = new Vector2(0.115f, 0.663f);
            dL.sizeDelta = new Vector2(8, 8);
            dL.localEulerAngles = new Vector3(0, 0, 45);

            var dR = SolidRect(left, "dR", Theme.Gold);
            dR.anchorMin = dR.anchorMax = new Vector2(0.565f, 0.663f);
            dR.sizeDelta = new Vector2(8, 8);
            dR.localEulerAngles = new Vector3(0, 0, 45);

            // ── BUTONAT (me te mdhaja, font me bold) ──
            string[] labels = {
                "Vazhdo",
                "Kartat e niveleve",
                "Rast i ri",
                "Si luhet",
                "Dil"
            };

            float top = 0.63f;
            float h   = 0.092f;    // shume me e larte
            float gap = 0.018f;

            for (int i = 0; i < labels.Length; i++)
            {
                float y = top - i * (h + gap);
                BuildButton(left, labels[i], y, h, i);
            }
        }

        // ── Nje buton i vetem (i madh, me kornize ari) ──
        void BuildButton(RectTransform parent, string label, float yTop, float h, int idx)
        {
            var go = new GameObject("btn_" + idx);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.12f, yTop - h);
            rt.anchorMax = new Vector2(0.92f, yTop);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            // ── Korniza e arte (outline) ──
            var border       = go.AddComponent<Image>();
            border.sprite    = Theme.Rounded(20);
            border.type      = Image.Type.Sliced;
            border.color     = Theme.WithAlpha(Theme.Gold, 0.7f);

            // ── Sfondi i erret brenda (me padding 2px per kornize) ──
            var inner = new GameObject("inner");
            inner.transform.SetParent(rt, false);
            var innerR = inner.AddComponent<RectTransform>();
            innerR.anchorMin = Vector2.zero;
            innerR.anchorMax = Vector2.one;
            innerR.offsetMin = new Vector2(2, 2);
            innerR.offsetMax = new Vector2(-2, -2);
            var innerBg       = inner.AddComponent<Image>();
            innerBg.sprite    = Theme.Rounded(20);
            innerBg.type      = Image.Type.Sliced;
            innerBg.color     = new Color(0.06f, 0.06f, 0.08f, 0.96f);
            innerBg.raycastTarget = false;

            var labGo = new GameObject("lbl");
            labGo.transform.SetParent(rt, false);
            var labR = labGo.AddComponent<RectTransform>();
            labR.anchorMin = new Vector2(0.045f, 0f);
            labR.anchorMax = new Vector2(1f, 1f);
            labR.offsetMin = labR.offsetMax = Vector2.zero;

            var txt       = labGo.AddComponent<Text>();
            txt.text      = label;
            txt.font      = Theme.Serif;
            txt.fontSize  = 28;                   // me e madhe
            txt.color     = Theme.Parchment;
            txt.alignment = TextAnchor.MiddleLeft;

            var btn  = go.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor      = Color.white;
            cols.highlightedColor = new Color(0.85f, 0.78f, 0.55f, 0.18f);
            cols.pressedColor     = new Color(0.85f, 0.78f, 0.55f, 0.35f);
            cols.fadeDuration     = 0.12f;
            btn.colors = cols;

            int i = idx;
            btn.onClick.AddListener(() => OnClick(i));
        }

        // ==============================================================
        //  GRIMCA ARI (floating golden particles)
        // ==============================================================

        void BuildParticles(RectTransform parent)
        {
            _pRTs    = new RectTransform[PCount];
            _pSpeeds = new float[PCount];
            _pDrifts = new float[PCount];

            var spr = CircleSprite();

            for (int i = 0; i < PCount; i++)
            {
                var go = new GameObject("p" + i);
                go.transform.SetParent(parent, false);
                var rt = go.AddComponent<RectTransform>();

                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.anchoredPosition = new Vector2(
                    Random.Range(60f, 1860f),
                    Random.Range(-50f, 1130f)
                );

                float sz = Random.Range(1.5f, 5f);
                rt.sizeDelta = new Vector2(sz, sz);

                var img = go.AddComponent<Image>();
                img.sprite = spr;
                img.color  = Theme.WithAlpha(Theme.Gold, Random.Range(0.08f, 0.35f));
                img.raycastTarget = false;

                _pRTs[i]    = rt;
                _pSpeeds[i] = Random.Range(5f, 22f);
                _pDrifts[i] = Random.Range(-2.5f, 2.5f);
            }
        }

        // ==============================================================
        //  LEVEL SELECT OVERLAY
        // ==============================================================

        void ShowLevelSelect()
        {
            if (_levelOverlay != null) return;

            _levelOverlay = new GameObject("LevelOverlay");
            _levelOverlay.transform.SetParent(_canvas.transform, false);

            var bgR = _levelOverlay.AddComponent<RectTransform>();
            Stretch(bgR);
            var bgImg     = _levelOverlay.AddComponent<Image>();
            bgImg.color   = new Color(0, 0, 0, 0.90f);

            var closeBg = _levelOverlay.AddComponent<Button>();
            closeBg.onClick.AddListener(CloseLevelSelect);

            // Titull
            var hdrGo = new GameObject("hdr");
            hdrGo.transform.SetParent(bgR, false);
            var hdrR = hdrGo.AddComponent<RectTransform>();
            hdrR.anchorMin = new Vector2(0.2f, 0.72f);
            hdrR.anchorMax = new Vector2(0.8f, 0.82f);
            hdrR.offsetMin = hdrR.offsetMax = Vector2.zero;
            var hdrTxt       = hdrGo.AddComponent<Text>();
            hdrTxt.text      = "Zgjidh Nivelin";
            hdrTxt.font      = Theme.Serif;
            hdrTxt.fontSize  = 46;
            hdrTxt.color     = Theme.Gold;
            hdrTxt.alignment = TextAnchor.MiddleCenter;

            // 4 karta ne rresht — me te mdhaja, me kornize ari
            float cardW   = 0.18f;
            float cardH   = 0.42f;
            float totalW  = cardW * 4 + 0.03f * 3;
            float startX  = 0.5f - totalW * 0.5f + cardW * 0.5f;

            string[] names = { "Karta 1", "Karta 2", "Karta 3", "Karta 4" };

            for (int i = 0; i < 4; i++)
            {
                float cx = startX + i * (cardW + 0.03f);
                BuildLevelCard(bgR, cx, 0.44f, cardW, cardH, names[i], i + 1);
            }

            // Buton mbyll (X)
            var xGo = new GameObject("close_btn");
            xGo.transform.SetParent(bgR, false);
            var xR = xGo.AddComponent<RectTransform>();
            xR.anchorMin = new Vector2(0.88f, 0.86f);
            xR.anchorMax = new Vector2(0.93f, 0.92f);
            xR.offsetMin = xR.offsetMax = Vector2.zero;

            var xBg      = xGo.AddComponent<Image>();
            xBg.sprite   = Theme.Rounded(8);
            xBg.type     = Image.Type.Sliced;
            xBg.color    = new Color(0.12f, 0.12f, 0.14f, 0.92f);

            var xTxt       = new GameObject("xtxt").AddComponent<Text>();
            xTxt.transform.SetParent(xR, false);
            var xTxtR = xTxt.GetComponent<RectTransform>();
            Stretch(xTxtR);
            xTxt.text      = "✕";
            xTxt.font      = Theme.Sans;
            xTxt.fontSize  = 30;
            xTxt.color     = Theme.Parchment;
            xTxt.alignment = TextAnchor.MiddleCenter;

            var xBtn = xGo.AddComponent<Button>();
            xBtn.onClick.AddListener(CloseLevelSelect);
        }

        void BuildLevelCard(RectTransform parent, float cx, float cy,
                            float w, float h, string title, int card)
        {
            var go = new GameObject("card_" + card);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(cx - w * 0.5f, cy - h * 0.5f);
            rt.anchorMax = new Vector2(cx + w * 0.5f, cy + h * 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            // ── Korniza e arte (outline) ──
            var border       = go.AddComponent<Image>();
            border.sprite    = Theme.Rounded(20);
            border.type      = Image.Type.Sliced;
            border.color     = Theme.WithAlpha(Theme.Gold, 0.6f);

            // ── Sfond i erret brenda ──
            var inner = new GameObject("inner");
            inner.transform.SetParent(rt, false);
            var innerR = inner.AddComponent<RectTransform>();
            innerR.anchorMin = Vector2.zero;
            innerR.anchorMax = Vector2.one;
            innerR.offsetMin = new Vector2(2, 2);
            innerR.offsetMax = new Vector2(-2, -2);
            var innerBg       = inner.AddComponent<Image>();
            innerBg.sprite    = Theme.Rounded(20);
            innerBg.type      = Image.Type.Sliced;
            innerBg.color     = new Color(0.055f, 0.055f, 0.075f, 0.97f);
            innerBg.raycastTarget = false;

            // ── Ikone lupe (e vogel, dekorative) ──
            var iconGo = new GameObject("icon");
            iconGo.transform.SetParent(rt, false);
            var iconR = iconGo.AddComponent<RectTransform>();
            iconR.anchorMin = iconR.anchorMax = new Vector2(0.5f, 0.68f);
            iconR.sizeDelta = new Vector2(64, 64);
            var iconRI = iconGo.AddComponent<RawImage>();
            iconRI.texture = MagnifyingGlassTex(128);
            iconRI.color   = Theme.WithAlpha(Theme.Gold, 0.35f);
            iconRI.raycastTarget = false;

            // ── Numri i kartes (i madh, qendror) ──
            var numGo = new GameObject("num");
            numGo.transform.SetParent(rt, false);
            var numR = numGo.AddComponent<RectTransform>();
            numR.anchorMin = new Vector2(0.05f, 0.30f);
            numR.anchorMax = new Vector2(0.95f, 0.60f);
            numR.offsetMin = numR.offsetMax = Vector2.zero;
            var numTxt       = numGo.AddComponent<Text>();
            numTxt.text      = card.ToString();
            numTxt.font      = Theme.Serif;
            numTxt.fontSize  = 72;
            numTxt.color     = Theme.WithAlpha(Theme.Gold, 0.12f);
            numTxt.alignment = TextAnchor.MiddleCenter;
            numTxt.raycastTarget = false;

            // ── Titulli (i madh, font Serif) ──
            var tGo = new GameObject("title");
            tGo.transform.SetParent(rt, false);
            var tR = tGo.AddComponent<RectTransform>();
            tR.anchorMin = new Vector2(0.05f, 0.12f);
            tR.anchorMax = new Vector2(0.95f, 0.45f);
            tR.offsetMin = tR.offsetMax = Vector2.zero;
            var tTxt       = tGo.AddComponent<Text>();
            tTxt.text      = title;
            tTxt.font      = Theme.Serif;
            tTxt.fontSize  = 34;
            tTxt.color     = Theme.Parchment;
            tTxt.alignment = TextAnchor.MiddleCenter;

            // ── Vije dekorative poshte ──
            var cardLine = SolidRect(rt, "cline", Theme.WithAlpha(Theme.Gold, 0.3f));
            cardLine.anchorMin = new Vector2(0.2f, 0.08f);
            cardLine.anchorMax = new Vector2(0.8f, 0.09f);
            cardLine.offsetMin = cardLine.offsetMax = Vector2.zero;

            var btn  = go.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor      = Color.white;
            cols.highlightedColor = new Color(0.85f, 0.78f, 0.55f, 0.18f);
            cols.pressedColor     = new Color(0.85f, 0.78f, 0.55f, 0.35f);
            cols.fadeDuration     = 0.12f;
            btn.colors = cols;

            int c = card;
            btn.onClick.AddListener(() => StartCard(c));
        }

        // «Rast i ri»: rast i gjeneruar ne nivelin ku ka arritur lojtari. Nese gjeneruesi
        // nuk gjen dot rast te vlefshem, kthehemi te karta e radhes.
        void StartNewCase()
        {
            PlayerPrefs.SetInt("alibi_newcase", 1);
            int level = Mathf.Clamp(AlibiFlow.NextCard(), 1, 4);
            var spec = LevelCards.Spec(level);
            var type = spec.Types != null && spec.Types.Length > 0 ? spec.Types[0] : CaseType.Vrasja;
            var result = CaseGenerator.Generate(level, type, Random.Range(1, 999999));
            AlibiFlow.StartCase(result.Case ?? LevelCards.Card(level));
        }

        void CloseLevelSelect()
        {
            if (_levelOverlay != null)
            {
                Destroy(_levelOverlay);
                _levelOverlay = null;
            }
        }

        // Karta qe zgjodhi lojtari i kalohet GameManager-it permes AlibiFlow.PendingCase.
        // Me pare ruhej vetem ne PlayerPrefs ("alibi_card"), te cilin nuk e lexonte askush,
        // prandaj çdo kartë hapte Karten 1.
        void StartCard(int card)
        {
            PlayerPrefs.SetInt("alibi_card", card);
            PlayerPrefs.DeleteKey("alibi_newcase");
            AlibiFlow.StartCase(LevelCards.Card(card));
        }

        // ==============================================================
        //  NAVIGIMI
        // ==============================================================

        void OnClick(int idx)
        {
            switch (idx)
            {
                case 0: StartCard(AlibiFlow.NextCard()); break;   // «Vazhdo»: karta e radhes
                case 1: ShowLevelSelect(); break;
                case 2: StartNewCase(); break;
                case 3: ShowIntro(); break;
                case 4:
                    Application.Quit();
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
                    break;
            }
        }

        void ShowIntro()
        {
            var introGo = new GameObject("_IntroReplay");
            introGo.transform.SetParent(_canvas.transform, false);
            introGo.AddComponent<IntroVideo>();
        }

        // ==============================================================
        //  HELPERS — ndertim formash
        // ==============================================================

        RectTransform SolidRect(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r   = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return r;
        }

        void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        // ==============================================================
        //  TEXTURE: SILUETA E DETEKTIVIT me rim-light 3D
        // ==============================================================

        /// <summary>
        /// Profili i detektivit si poligon (koordinata 0-1, y=0 poshte).
        /// Fedora, kapuc i larte, pallto, hundë e dale — stil noir.
        /// </summary>
        static Vector2[] DetectiveProfile()
        {
            return new Vector2[] {
                // ═══ MAJË E STREHËS SË KAPELËS (djathtas) ═══
                new Vector2(0.73f, 0.825f),
                // Streha lart
                new Vector2(0.69f, 0.838f),
                new Vector2(0.63f, 0.845f),
                new Vector2(0.57f, 0.848f),
                // Kurorë lart
                new Vector2(0.55f, 0.86f),
                new Vector2(0.53f, 0.89f),
                new Vector2(0.51f, 0.925f),
                // Maja e kapelës
                new Vector2(0.47f, 0.95f),
                new Vector2(0.41f, 0.958f),
                new Vector2(0.36f, 0.952f),
                // Kurorë prapa
                new Vector2(0.33f, 0.935f),
                new Vector2(0.30f, 0.90f),
                new Vector2(0.28f, 0.865f),
                // Streha prapa
                new Vector2(0.26f, 0.848f),
                new Vector2(0.23f, 0.838f),
                new Vector2(0.21f, 0.828f),
                // ═══ PRAPA KOKËS ═══
                new Vector2(0.25f, 0.812f),
                new Vector2(0.29f, 0.792f),
                new Vector2(0.31f, 0.76f),
                new Vector2(0.32f, 0.73f),
                // ═══ QAFË ═══
                new Vector2(0.325f, 0.695f),
                new Vector2(0.32f, 0.665f),
                // ═══ JAKA PRAPA (e ngritur lart) ═══
                new Vector2(0.30f, 0.645f),
                new Vector2(0.27f, 0.625f),
                new Vector2(0.24f, 0.61f),
                // ═══ SUP ═══
                new Vector2(0.20f, 0.595f),
                new Vector2(0.17f, 0.58f),
                // ═══ SHPINA E PALLTOSË ═══
                new Vector2(0.15f, 0.54f),
                new Vector2(0.14f, 0.48f),
                new Vector2(0.135f, 0.40f),
                new Vector2(0.135f, 0.30f),
                new Vector2(0.14f, 0.20f),
                new Vector2(0.15f, 0.14f),
                // ═══ FUNDI I PALLTOSË ═══
                new Vector2(0.17f, 0.08f),
                new Vector2(0.20f, 0.05f),
                // ═══ BAZA ═══
                new Vector2(0.57f, 0.05f),
                new Vector2(0.60f, 0.08f),
                // ═══ PARA E PALLTOSË (lart) ═══
                new Vector2(0.58f, 0.16f),
                new Vector2(0.57f, 0.26f),
                new Vector2(0.56f, 0.36f),
                new Vector2(0.555f, 0.44f),
                new Vector2(0.55f, 0.52f),
                // ═══ REVERE ═══
                new Vector2(0.54f, 0.57f),
                new Vector2(0.53f, 0.61f),
                // ═══ JAKA PARA (e ngritur lart — stil detektivi) ═══
                new Vector2(0.55f, 0.64f),
                new Vector2(0.57f, 0.665f),
                // ═══ MJEKRA ═══
                new Vector2(0.59f, 0.695f),
                new Vector2(0.61f, 0.715f),
                // ═══ NOFULL ═══
                new Vector2(0.63f, 0.735f),
                new Vector2(0.65f, 0.75f),
                // ═══ FAQE ═══
                new Vector2(0.66f, 0.765f),
                // ═══ HUNDË ═══
                new Vector2(0.68f, 0.785f),
                new Vector2(0.71f, 0.805f),
                new Vector2(0.72f, 0.815f),   // maja e hundës
                // ═══ KTHEHU TE STREHA ═══
                new Vector2(0.70f, 0.82f),
            };
        }

        /// <summary>Ray-casting point-in-polygon test.</summary>
        static bool PointInPoly(float px, float py, Vector2[] poly)
        {
            bool inside = false;
            int n = poly.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float yi = poly[i].y, yj = poly[j].y;
                if ((yi > py) != (yj > py))
                {
                    float xi = poly[i].x, xj = poly[j].x;
                    if (px < xi + (py - yi) / (yj - yi) * (xj - xi))
                        inside = !inside;
                }
            }
            return inside;
        }

        /// <summary>
        /// Distanca nga pika (px,py) deri ne skajin me te afert
        /// te poligonit. Kthen edhe piken me te afert (nearX, nearY).
        /// </summary>
        static float NearestEdgeDist(float px, float py, Vector2[] poly,
                                      out float nearX, out float nearY)
        {
            float minSq = float.MaxValue;
            nearX = px; nearY = py;
            int n = poly.Length;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float x1 = poly[j].x, y1 = poly[j].y;
                float x2 = poly[i].x, y2 = poly[i].y;
                float dx = x2 - x1, dy = y2 - y1;
                float len2 = dx * dx + dy * dy;
                float t = 0f;
                if (len2 > 0.0001f)
                    t = Mathf.Clamp01(((px - x1) * dx + (py - y1) * dy) / len2);
                float projX = x1 + t * dx;
                float projY = y1 + t * dy;
                float dSq = (px - projX) * (px - projX) + (py - projY) * (py - projY);
                if (dSq < minSq)
                {
                    minSq = dSq;
                    nearX = projX;
                    nearY = projY;
                }
            }

            return Mathf.Sqrt(minSq);
        }

        /// <summary>
        /// Gjeneron teksturën e siluetës me rim-light te arte per efekt 3D.
        /// Polygon fill + ndricim direktiv nga djathtas/lart.
        /// </summary>
        static Texture2D DetectiveSilhouetteTex(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var poly = DetectiveProfile();
            float s = size;
            float invS = 1f / s;

            // Parametra ndricimi
            float rimWidth = 0.028f;    // gjeresia e rim-light (normalizuar)
            float goldR = 0.824f, goldG = 0.678f, goldB = 0.384f;

            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float nx = (px + 0.5f) * invS;
                    float ny = (py + 0.5f) * invS;

                    // Kontrollo bbox per shpejtesi
                    if (nx < 0.12f || nx > 0.74f || ny < 0.04f || ny > 0.97f)
                        continue;

                    if (!PointInPoly(nx, ny, poly))
                        continue;

                    // ── BRENDA SILUETËS ──
                    float nrX, nrY;
                    float eDist = NearestEdgeDist(nx, ny, poly, out nrX, out nrY);

                    // Ngjyre baze: gri shume i erret me nuancë blu
                    float bR = 0.052f, bG = 0.052f, bB = 0.068f;

                    // ── RIM LIGHT (dritë nga djathtas + lart) ──
                    if (eDist < rimWidth)
                    {
                        float rimStr = 1f - eDist / rimWidth;
                        rimStr *= rimStr; // falloff kuadratik

                        // Drejtimi: nga pikseli drejt skajit (jashtë)
                        float dLen = Mathf.Max(eDist, 0.0005f);
                        float outX = (nrX - nx) / dLen;
                        float outY = (nrY - ny) / dLen;

                        // Drita vjen nga djathtas (0.7) + pak lart (0.3)
                        float lightDot = outX * 0.7f + outY * 0.3f;
                        lightDot = Mathf.Clamp01(lightDot);

                        float rimI = rimStr * lightDot * 0.7f;

                        bR += goldR * rimI;
                        bG += goldG * rimI;
                        bB += goldB * rimI;

                        // Dritë sekondare (fill) nga majtas — shumë e lehtë, blu
                        float fillDot = Mathf.Clamp01(-outX * 0.3f - outY * 0.1f);
                        float fillI = rimStr * fillDot * 0.06f;
                        bR += 0.3f * fillI;
                        bG += 0.4f * fillI;
                        bB += 0.7f * fillI;
                    }

                    // ── BREZI I KAPELËS (shirit pak me i erret) ──
                    if (ny > 0.842f && ny < 0.858f)
                    {
                        // Vetem brenda zonës së kapelës (jo hundë/fytyrë)
                        if (nx > 0.22f && nx < 0.70f)
                        {
                            bR *= 0.55f;
                            bG *= 0.55f;
                            bB *= 0.55f;
                        }
                    }

                    // ── SHKËLQIM I SYRIT (çarje e artë eliptike) ──
                    float eyeX = 0.645f, eyeY = 0.792f;
                    float edx = (nx - eyeX) / 0.016f;
                    float edy = (ny - eyeY) / 0.004f;
                    float eyeD = edx * edx + edy * edy;
                    if (eyeD < 1f)
                    {
                        float eyeStr = (1f - eyeD);
                        eyeStr = eyeStr * eyeStr * 0.45f;
                        bR += goldR * eyeStr;
                        bG += goldG * eyeStr;
                        bB += goldB * eyeStr;
                    }

                    // Anti-alias ne skaj (1px feather)
                    float edgePx = eDist * s;
                    float alpha = Mathf.Clamp01(edgePx * 1.5f);

                    pixels[py * size + px] = new Color(
                        Mathf.Min(bR, 1f),
                        Mathf.Min(bG, 1f),
                        Mathf.Min(bB, 1f),
                        alpha
                    );
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        // ==============================================================
        //  TEXTURE: LUPA SDF
        // ==============================================================

        static Texture2D MagnifyingGlassTex(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float s = size;

            float cx = s * 0.52f, cy = s * 0.58f;
            float outerR   = s * 0.30f;
            float ringW    = s * 0.030f;
            float innerR   = outerR - ringW;

            float hAngle = -2.35f;
            float cosA = Mathf.Cos(hAngle), sinA = Mathf.Sin(hAngle);
            float hx1 = cx + cosA * outerR;
            float hy1 = cy + sinA * outerR;
            float hLen = s * 0.30f;
            float hx2 = hx1 + cosA * hLen;
            float hy2 = hy1 + sinA * hLen;
            float hHalfW = s * 0.025f;

            float gripR = s * 0.036f;

            float goldR = 0.824f, goldG = 0.678f, goldB = 0.384f;

            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = px + 0.5f, y = py + 0.5f;
                    float dx = x - cx, dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = 0f;
                    float bright = 1f;

                    // Unaza
                    float ringMid = outerR - ringW * 0.5f;
                    float ringDist = Mathf.Abs(dist - ringMid) - ringW * 0.5f;
                    float ringA = Mathf.Clamp01(1.5f - ringDist);

                    if (ringA > 0.01f)
                    {
                        float ang = Mathf.Atan2(dy, dx);
                        float hl  = Mathf.Clamp01(Mathf.Cos(ang - 0.5f));
                        bright = 1f + hl * 0.35f;
                    }

                    // Lente
                    float lensBorder = innerR - dist;
                    float lensA = Mathf.Clamp01(lensBorder * 0.8f);
                    float lensGrad = 1f - Mathf.Clamp01(dist / innerR);
                    lensA *= lensGrad * 0.12f;

                    // Dora
                    float handleA = 0f;
                    {
                        float ldx = hx2 - hx1, ldy = hy2 - hy1;
                        float len2 = ldx * ldx + ldy * ldy;
                        float t = 0f;
                        if (len2 > 0.001f)
                            t = Mathf.Clamp01(((x - hx1) * ldx + (y - hy1) * ldy) / len2);
                        float projX = hx1 + t * ldx;
                        float projY = hy1 + t * ldy;
                        float hDist = Mathf.Sqrt(
                            (x - projX) * (x - projX) + (y - projY) * (y - projY));
                        float w = Mathf.Lerp(hHalfW * 0.75f, hHalfW * 1.1f, t);
                        handleA = Mathf.Clamp01(w - hDist + 1f);
                    }

                    // Grip
                    float gDist = Mathf.Sqrt(
                        (x - hx2) * (x - hx2) + (y - hy2) * (y - hy2));
                    float gripA = Mathf.Clamp01(gripR - gDist + 1f);

                    float solidA = Mathf.Max(ringA, Mathf.Max(handleA, gripA));
                    alpha = Mathf.Max(solidA, lensA);

                    float cr = Mathf.Min(1f, goldR * bright);
                    float cg = Mathf.Min(1f, goldG * bright);
                    float cb = Mathf.Min(1f, goldB * bright);

                    pixels[py * size + px] = new Color(cr, cg, cb, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        // ==============================================================
        //  TEXTURE: GLOW, RAYS, CIRCLE
        // ==============================================================

        static Texture2D LensGlowTex(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px  = new Color32[size * size];
            float s = size;

            float cx = s * 0.52f, cy = s * 0.58f;
            float innerR = s * 0.30f - s * 0.030f;

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;
                    float d  = Mathf.Sqrt(dx * dx + dy * dy);
                    float n  = d / innerR;
                    float a = Mathf.Clamp01(1f - n);
                    a = a * a * a;
                    byte ab = (byte)(a * 255);
                    px[y * size + x] = new Color32(255, 255, 255, ab);
                }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        static Texture2D RayGradientTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fx = (float)x / (w - 1);
                    float a  = 1f - Mathf.Abs(fx * 2f - 1f);
                    a = a * a * a;
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            tex.Apply(false, true);
            tex.wrapMode   = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        static Sprite CircleSprite()
        {
            if (_circleSpr != null) return _circleSpr;

            int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            float r = s * 0.5f;
            var px  = new Color32[s * s];

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = x - r + 0.5f;
                    float dy = y - r + 0.5f;
                    float d  = Mathf.Sqrt(dx * dx + dy * dy);
                    byte a   = (byte)(Mathf.Clamp01(r - d) * 255);
                    px[y * s + x] = new Color32(255, 255, 255, a);
                }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            _circleSpr = Sprite.Create(tex,
                new Rect(0, 0, s, s), Vector2.one * 0.5f);
            return _circleSpr;
        }

        /// <summary>Gradient radial per ambient glow.</summary>
        static Texture2D GlowTex(int s)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            float r = s * 0.5f;
            var px  = new Color32[s * s];

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - r + 0.5f) / r;
                    float dy = (y - r + 0.5f) / r;
                    float d  = Mathf.Sqrt(dx * dx + dy * dy);
                    float a  = Mathf.Clamp01(1f - d * d);
                    byte ab  = (byte)(a * 255);
                    px[y * s + x] = new Color32(255, 255, 255, ab);
                }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }
    }
}
