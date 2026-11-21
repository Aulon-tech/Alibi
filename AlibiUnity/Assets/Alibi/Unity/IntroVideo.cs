using System.Collections;
using System.Collections.Generic;
using Alibi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Alibi.Unity
{
    public sealed class IntroVideo : MonoBehaviour
    {
        const string PrefKey = "alibi_intro_seen";
        const float SlideDuration = 3.2f;
        const float FadeDuration = 0.6f;

        System.Action onFinished;
        RectTransform root;
        CanvasGroup slideGroup;
        int current;
        bool advancing;
        CaseDefinition caseDef;
        string[][] slides = Slides;
        string heading;
        bool genericIntro = true;
        float slideDuration = SlideDuration;

        static readonly string[][] Slides = new string[][]
        {
            new[] { "✦", "Dikush ka kryer një krim...", "Një krim ka ndodhur në vilë. Dyshuarit janë aty, por secili ka një alibi të vet. Dikush gënjen." },
            new[] { "?", "Pyetni të dyshuarit", "Zgjidh një të dyshuar dhe një lloj pyetjeje. Çdo përgjigje bëhet kufizim në tabelën e deduksionit." },
            new[] { "◉", "Tabela mbush vetë", "Sistemi AC-3 përhap kufizimet: dhomat e pamundshme zbehen, të konfirmuarat bëhen ari." },
            new[] { "!", "Buxheti është i kufizuar", "Çdo pyetje kushton 1 ose 2 pikë. Kur buxheti mbaron, duhet të akuzosh menjëherë." },
            new[] { "★", "Jeni gati, detektiv?", "Gjeni fajtorin, dhomën dhe orën e krimit. Sa më pak pyetje, aq më shumë yje." },
        };

        public static bool ShouldShow()
        {
            return PlayerPrefs.GetInt(PrefKey, 0) == 0;
        }

        public static void MarkSeen()
        {
            PlayerPrefs.SetInt(PrefKey, 1);
            PlayerPrefs.Save();
        }

        public void Play(Transform canvas, CaseDefinition c, System.Action finished)
        {
            slides = Slides;
            heading = null;
            genericIntro = true;
            slideDuration = SlideDuration;
            Begin(canvas, c, finished);
        }

        // Rrëfimi i një karte (Karta 3, Karta 4): pamje më të gjata, me titullin e kartës sipër.
        public void PlayStory(Transform canvas, CaseDefinition c, CaseStory.Story story, System.Action finished)
        {
            slides = story.Slides;
            heading = story.Heading;
            genericIntro = false;
            slideDuration = 7f;
            Begin(canvas, c, finished);
        }

        void Begin(Transform canvas, CaseDefinition c, System.Action finished)
        {
            caseDef = c;
            onFinished = finished;
            current = -1;
            advancing = false;

            root = UI.Rect(canvas, "IntroVideo");
            UI.Stretch(root, 0, 0, 0, 0);

            // Djark background
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = Theme.Hex("#07090D");
            bg.raycastTarget = true;

            // Click-to-advance
            var click = root.gameObject.AddComponent<Button>();
            click.transition = Selectable.Transition.None;
            click.onClick.AddListener(() => { if (!advancing) AdvanceSlide(); });

            // Skip button — always visible, top-right
            var skip = UI.Button(root, "Kalo ▸", BtnStyle.Ghost, Finish, 14, 8);
            skip.Rect.anchorMin = skip.Rect.anchorMax = new Vector2(1, 1);
            skip.Rect.pivot = new Vector2(1, 1);
            skip.Rect.anchoredPosition = new Vector2(-24, -24);
            skip.Rect.sizeDelta = new Vector2(100, 36);

            // Progress dots at bottom
            BuildProgressDots();

            // Slide content group — fade target
            var slideHost = UI.Rect(root, "SlideHost");
            UI.Stretch(slideHost, 0, 80, 0, 80);
            slideGroup = slideHost.gameObject.AddComponent<CanvasGroup>();
            slideGroup.alpha = 0;

            StartCoroutine(AutoPlay());
        }

        void BuildProgressDots()
        {
            var bar = UI.Rect(root, "Dots");
            bar.anchorMin = new Vector2(0.5f, 0);
            bar.anchorMax = new Vector2(0.5f, 0);
            bar.pivot = new Vector2(0.5f, 0);
            bar.anchoredPosition = new Vector2(0, 32);
            float dotW = 10, gap = 8;
            float totalW = slides.Length * dotW + (slides.Length - 1) * gap;
            bar.sizeDelta = new Vector2(totalW, dotW);
            for (int i = 0; i < slides.Length; i++)
            {
                var dot = new GameObject("Dot" + i, typeof(RectTransform), typeof(Image));
                dot.transform.SetParent(bar, false);
                var drt = dot.GetComponent<RectTransform>();
                drt.anchorMin = drt.anchorMax = new Vector2(0, 0.5f);
                drt.pivot = new Vector2(0, 0.5f);
                drt.anchoredPosition = new Vector2(i * (dotW + gap), 0);
                drt.sizeDelta = new Vector2(dotW, dotW);
                var img = dot.GetComponent<Image>();
                img.sprite = Theme.Rounded(5);
                img.type = Image.Type.Sliced;
                img.color = Theme.WithAlpha(Theme.Gold, 0.2f);
                img.raycastTarget = false;
            }
        }

        void UpdateDots()
        {
            var bar = root.Find("Dots");
            if (bar == null) return;
            for (int i = 0; i < slides.Length; i++)
            {
                var dot = bar.GetChild(i);
                var img = dot.GetComponent<Image>();
                if (img != null) img.color = i <= current ? Theme.Gold : Theme.WithAlpha(Theme.Gold, 0.2f);
            }
        }

        IEnumerator AutoPlay()
        {
            yield return null; // frame delay
            AdvanceSlide();
            while (current < slides.Length)
            {
                yield return new WaitForSeconds(slideDuration);
                if (current < slides.Length - 1)
                    AdvanceSlide();
                else
                    break;
            }
        }

        void AdvanceSlide()
        {
            current++;
            if (current >= slides.Length)
            {
                Finish();
                return;
            }
            StartCoroutine(TransitionSlide());
        }

        IEnumerator TransitionSlide()
        {
            advancing = true;

            // Fade out current
            if (slideGroup.alpha > 0)
            {
                float t = 0;
                while (t < FadeDuration * 0.5f)
                {
                    t += Time.deltaTime;
                    slideGroup.alpha = 1f - t / (FadeDuration * 0.5f);
                    yield return null;
                }
                slideGroup.alpha = 0;
            }

            // Build new slide content
            var host = slideGroup.GetComponent<RectTransform>();
            for (int i = host.childCount - 1; i >= 0; i--)
                Destroy(host.GetChild(i).gameObject);

            BuildSlide(host, slides[current]);
            UpdateDots();

            // Fade in
            float fadeIn = 0;
            while (fadeIn < FadeDuration)
            {
                fadeIn += Time.deltaTime;
                slideGroup.alpha = fadeIn / FadeDuration;
                yield return null;
            }
            slideGroup.alpha = 1;
            advancing = false;
        }

        void BuildSlide(RectTransform host, string[] slide)
        {
            string icon = slide[0];
            string title = slide[1];
            string body = slide[2];

            // Centered layout
            var container = UI.Rect(host, "Content");
            container.anchorMin = container.anchorMax = new Vector2(0.5f, 0.5f);
            container.pivot = new Vector2(0.5f, 0.5f);
            container.anchoredPosition = new Vector2(0, 20);
            container.sizeDelta = new Vector2(700, 400);

            if (!string.IsNullOrEmpty(heading))
            {
                var head = UI.LabelAt(container, heading, Theme.Mono, 13, Theme.Gold, 0, -46, 700, 20);
                head.alignment = TextAnchor.UpperCenter;
            }

            // Icon badge — large centered circle
            var badge = UI.Panel(container, "Badge", Theme.WithAlpha(Theme.Gold, 0.12f), 40, false);
            badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(0.5f, 1);
            badge.rectTransform.pivot = new Vector2(0.5f, 1);
            badge.rectTransform.anchoredPosition = new Vector2(0, 0);
            badge.rectTransform.sizeDelta = new Vector2(80, 80);
            var iconBorder = UI.Border(badge.transform, Theme.WithAlpha(Theme.Gold, 0.3f), 40);
            var iconText = UI.Label(badge.transform, icon, Theme.Serif, 36, Theme.Gold, TextAnchor.MiddleCenter);
            UI.Stretch(iconText.rectTransform, 0, 0, 0, 0);

            // Title
            var titleLabel = UI.LabelAt(container, title, Theme.Serif, 36, Theme.Parchment, 0, 100, 700, 50);
            titleLabel.alignment = TextAnchor.UpperCenter;

            // Decorative line under title
            var line = UI.Panel(container, "Line", Theme.WithAlpha(Theme.Gold, 0.35f), 0, false);
            line.rectTransform.anchorMin = line.rectTransform.anchorMax = new Vector2(0.5f, 1);
            line.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            line.rectTransform.anchoredPosition = new Vector2(0, -160);
            line.rectTransform.sizeDelta = new Vector2(120, 1);

            // Body text
            var bodyLabel = UI.LabelAt(container, body, Theme.Sans, 18, Theme.Text2, 40, 180, 620, 150);
            bodyLabel.alignment = TextAnchor.UpperCenter;

            // Slide number indicator
            var numText = UI.LabelAt(container, (current + 1) + " / " + slides.Length, Theme.Mono, 12, Theme.Muted, 0, 340, 700, 20);
            numText.alignment = TextAnchor.UpperCenter;
        }

        void Finish()
        {
            StopAllCoroutines();
            if (genericIntro) MarkSeen();
            else CaseStory.MarkSeen(caseDef);
            if (root != null) Destroy(root.gameObject);
            if (onFinished != null) onFinished();
        }
    }
}
