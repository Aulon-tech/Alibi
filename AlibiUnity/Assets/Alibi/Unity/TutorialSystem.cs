// TutorialSystem.cs — Tutorial interaktiv me hapa, Next/Prev, auto-advance
// Zevendeson IntroVideo me nje sistem me te pasur qe tregon rregullat
// Cdo karte ka tutorial shtese vetem per rregullat e reja te asaj karte
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Alibi.Unity
{
    public sealed class TutorialSystem : MonoBehaviour
    {
        const float AutoAdvanceTime = 5.0f;
        const float FadeDuration = 0.4f;

        Action onFinished;
        RectTransform root;
        CanvasGroup slideGroup;
        int current;
        bool advancing;
        int cardNumber; // 0 = tutorial i plote (nga "Si luhet"), 1-5 = tutorial per karte specifike
        List<TutorialStep> steps;
        Coroutine autoCoroutine;

        // Nje hap tutoriali
        class TutorialStep
        {
            public string Icon;
            public string Title;
            public string Body;
            public Color AccentColor;

            public TutorialStep(string icon, string title, string body, Color accent)
            {
                Icon = icon; Title = title; Body = body; AccentColor = accent;
            }
        }

        /// <summary>
        /// Hap tutorialin. card=0 per tutorialin e plote, card=1-5 per rregulla te reja
        /// </summary>
        public void Show(Transform canvas, int card, Action finished)
        {
            cardNumber = card;
            onFinished = finished;
            current = -1;
            advancing = false;
            steps = BuildSteps(card);
            if (steps.Count == 0) { Finish(); return; }
            BuildUI(canvas);
        }

        List<TutorialStep> BuildSteps(int card)
        {
            var list = new List<TutorialStep>();

            if (card == 0 || card == 1)
            {
                // Tutorial i plote — te gjitha rregullat baze
                AddBaseSteps(list);
            }
            else if (card == 2)
            {
                // Karta 2 — Rregull e re: Grafi i levizjes
                list.Add(new TutorialStep(
                    "⭐", "Karta 2: Rregull e re!",
                    "Ne kete karte shtohet nje rregull e re qe ndryshon lojën.",
                    Theme.Gold
                ));
                list.Add(new TutorialStep(
                    "↔", "Grafi i lëvizjes",
                    "Tani i dyshuari nuk mund të zhvendoset nga nje dhomë ne tjetrën pa kaluar nëpër dhomat ndërmjetëse. Psh: Kuzhina → Holli → Salloni.",
                    Theme.Blue
                ));
                list.Add(new TutorialStep(
                    "⚠", "Pse ka rëndësi?",
                    "Nëse dikush ishte ne Kuzhine ne 21:00 dhe ne Salon ne 22:00, duhet të ketë kaluar nëpër Holl. Kjo ngushton mundësite!",
                    Theme.Gold
                ));
            }
            else if (card == 3)
            {
                // Karta 3 — Dëshmitari me gabim + Sabotimi
                list.Add(new TutorialStep(
                    "⭐", "Karta 3: Rregulla te reja!",
                    "Dy rregulla te reja qe e bëjnë lojën me sfiduese.",
                    Theme.Gold
                ));
                list.Add(new TutorialStep(
                    "❓", "Dëshmitari me gabim",
                    "Nje nga te dyshuarit mund të japi përgjigje te gabuar! Nuk e dini cili eshte — duhet ta zbuloni duke krahëzuar dëshmite.",
                    Theme.Red
                ));
                list.Add(new TutorialStep(
                    "⏰", "Sabotimi: ora nuk dihet",
                    "Ne rastin e sabotimit, ora e krimit nuk eshte e dhënë. Duhet ta gjeni vete duke pyetur te dyshuarit.",
                    Theme.Blue
                ));
            }
            else if (card == 4)
            {
                // Karta 4 — Vjedhja + kalimi i fshehte
                list.Add(new TutorialStep(
                    "⭐", "Karta 4: Rregulla te reja!",
                    "Vjedhja dhe kalimi i fshehte — dy elementë qe shtojnë thellësi.",
                    Theme.Gold
                ));
                list.Add(new TutorialStep(
                    "📁", "Vjedhja: dosja lëviz",
                    "Ne rastin e vjedhjes, hajni ka marre nje dosje. Dosja lëviz bashkë me hajnin — kerkoni ne dhomat ku ai ka qenë!",
                    Theme.Gold
                ));
                list.Add(new TutorialStep(
                    "🔍", "Kërko dhomën",
                    "Mundeni te kerkoni nje dhomë per te pare nëse dosja eshte aty. Kjo kushton nga buxheti juaj!",
                    Theme.Blue
                ));
                list.Add(new TutorialStep(
                    "🚪", "Kalimi i fshehtë",
                    "Ka nje kalim te fshehte midis Bibliotekës dhe Zyrës. I dyshuari mund te kalojë direkt pa shkuar nëpër korridorin!",
                    Theme.Hex("#9B59B6")
                ));
            }
            else if (card == 5)
            {
                // Karta 5 — Permbledhje e te gjitha rregullave
                list.Add(new TutorialStep(
                    "⭐", "Karta 5: Sfida finale!",
                    "Te gjitha rregullat bashkë: 6 te dyshuar, 9 dhoma, grafi levizjeje, deshmitar gabimi, kalim i fshehte — me buxhet te ngushte!",
                    Theme.Gold
                ));
                list.Add(new TutorialStep(
                    "!", "Buxhet i ngushte: 9 pike",
                    "Keni vetem 9 pike per pyetje. Mendoni mire para se te pyesni. Çdo pyetje kushton 1 ose 2 pike.",
                    Theme.Red
                ));
            }

            return list;
        }

        void AddBaseSteps(List<TutorialStep> list)
        {
            list.Add(new TutorialStep(
                "✦", "Mirë se vini, detektiv!",
                "Nje krim ka ndodhur ne vilë. Dyshuarit janë aty, secili me nje alibi. Dikush gënjen — detyra juaj eshte ta gjeni.",
                Theme.Gold
            ));
            list.Add(new TutorialStep(
                "🏠", "Vila ka 9 dhoma",
                "Klikoni nje dhomë ne hartë per te pare kush ka qenë aty ne orën e zgjedhur. Dhomat me ngjyrë ari kane informacion te ri.",
                Theme.Parchment
            ));
            list.Add(new TutorialStep(
                "?", "Pyesni të dyshuarit",
                "Zgjidh nje te dyshuar dhe pastaj zgjidh llojin e pyetjes. Çdo përgjigje shtohet si kufizim ne tabelë.",
                Theme.Blue
            ));
            list.Add(new TutorialStep(
                "◉", "Tabela mbushet vetë",
                "Sistemi AC-3 përhap kufizimet automatikisht. Dhomat e pamundshme zbehen, te konfirmuarat bëhen ari.\n\n✓ = i konfirmuar    ? = i mundshëm    ✗ = i pamundshëm",
                Theme.Gold
            ));
            list.Add(new TutorialStep(
                "!", "Buxheti eshte i kufizuar",
                "Çdo pyetje kushton 1 ose 2 pike nga buxheti. Kur mbaron buxheti, duhet te akuzoni menjëherë!",
                Theme.Red
            ));
            list.Add(new TutorialStep(
                "★", "Bëni akuzën!",
                "Gjeni fajtorin, dhomën dhe orën e krimit. Sa me pak pyetje perdorni, aq me shume yje fitoni!",
                Theme.Gold
            ));
        }

        void BuildUI(Transform canvas)
        {
            var go = new GameObject("TutorialOverlay");
            go.transform.SetParent(canvas, false);
            root = go.AddComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;

            // Sfond i erret
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.04f, 0.06f, 0.96f);

            // Slide host me fade
            var slideHost = UI.Rect(root, "SlideHost");
            UI.Stretch(slideHost, 0, 80, 0, 100);
            slideGroup = slideHost.gameObject.AddComponent<CanvasGroup>();
            slideGroup.alpha = 0;

            // ── Butoni "Kalo" (skip) — lart-djathtas ──
            var skipBtn = UI.Button(root, "Kalo ▸", BtnStyle.Ghost, Finish, 14, 8);
            skipBtn.Rect.anchorMin = skipBtn.Rect.anchorMax = new Vector2(1, 1);
            skipBtn.Rect.pivot = new Vector2(1, 1);
            skipBtn.Rect.anchoredPosition = new Vector2(-24, -24);
            skipBtn.Rect.sizeDelta = new Vector2(100, 36);

            // ── Butonat Prev / Next — posht ──
            BuildNavButtons();

            // Progress bar posht
            BuildProgressBar();

            // Fillo
            StartCoroutine(AutoPlayCoroutine());
        }

        void BuildNavButtons()
        {
            var navBar = UI.Rect(root, "NavBar");
            navBar.anchorMin = new Vector2(0.5f, 0);
            navBar.anchorMax = new Vector2(0.5f, 0);
            navBar.pivot = new Vector2(0.5f, 0);
            navBar.anchoredPosition = new Vector2(0, 36);
            navBar.sizeDelta = new Vector2(400, 44);

            // Prev
            var prev = UI.Button(navBar, "◀  Mbrapa", BtnStyle.Ghost, GoPrev, 16, 8);
            prev.Rect.anchorMin = new Vector2(0, 0);
            prev.Rect.anchorMax = new Vector2(0.35f, 1);
            prev.Rect.offsetMin = prev.Rect.offsetMax = Vector2.zero;

            // Dots ne mes
            var dots = UI.Rect(navBar, "Dots");
            dots.anchorMin = new Vector2(0.35f, 0);
            dots.anchorMax = new Vector2(0.65f, 1);
            dots.offsetMin = dots.offsetMax = Vector2.zero;

            float dotW = 8, gap = 6;
            float totalW = steps.Count * dotW + (steps.Count - 1) * gap;
            float startX = -totalW / 2f;
            for (int i = 0; i < steps.Count; i++)
            {
                var dot = new GameObject("dot_" + i, typeof(RectTransform), typeof(Image));
                dot.transform.SetParent(dots, false);
                var drt = dot.GetComponent<RectTransform>();
                drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
                drt.sizeDelta = new Vector2(dotW, dotW);
                drt.anchoredPosition = new Vector2(startX + i * (dotW + gap) + dotW / 2f, 0);
                var img = dot.GetComponent<Image>();
                img.sprite = Theme.Rounded(4);
                img.type = Image.Type.Sliced;
                img.color = Theme.WithAlpha(Theme.Gold, 0.2f);
                img.raycastTarget = false;
            }

            // Next
            var next = UI.Button(navBar, "Tutje  ▶", BtnStyle.Ghost, GoNext, 16, 8);
            next.Rect.anchorMin = new Vector2(0.65f, 0);
            next.Rect.anchorMax = new Vector2(1, 1);
            next.Rect.offsetMin = next.Rect.offsetMax = Vector2.zero;
        }

        void BuildProgressBar()
        {
            var bar = UI.Panel(root, "ProgressBg", Theme.WithAlpha(Theme.Gold, 0.1f), 0, false);
            bar.rectTransform.anchorMin = new Vector2(0, 1);
            bar.rectTransform.anchorMax = new Vector2(1, 1);
            bar.rectTransform.pivot = new Vector2(0, 1);
            bar.rectTransform.anchoredPosition = Vector2.zero;
            bar.rectTransform.sizeDelta = new Vector2(0, 4);

            var fill = UI.Panel(bar.rectTransform, "ProgressFill", Theme.Gold, 0, false);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
        }

        void UpdateProgressBar()
        {
            var bg = root.Find("ProgressBg");
            if (bg == null) return;
            var fill = bg.Find("ProgressFill");
            if (fill == null) return;
            var rt = fill.GetComponent<RectTransform>();
            float pct = steps.Count > 0 ? (current + 1f) / steps.Count : 0;
            rt.anchorMax = new Vector2(pct, 1);
        }

        void UpdateDots()
        {
            var navBar = root.Find("NavBar");
            if (navBar == null) return;
            var dots = navBar.Find("Dots");
            if (dots == null) return;
            for (int i = 0; i < dots.childCount; i++)
            {
                var img = dots.GetChild(i).GetComponent<Image>();
                if (img != null)
                {
                    img.color = i == current ? Theme.Gold :
                                i < current ? Theme.WithAlpha(Theme.Gold, 0.6f) :
                                Theme.WithAlpha(Theme.Gold, 0.2f);
                    var rt = dots.GetChild(i).GetComponent<RectTransform>();
                    rt.sizeDelta = i == current ? new Vector2(14, 8) : new Vector2(8, 8);
                }
            }
        }

        IEnumerator AutoPlayCoroutine()
        {
            yield return null;
            GoNext();
            while (current < steps.Count - 1)
            {
                yield return new WaitForSeconds(AutoAdvanceTime);
                if (current < steps.Count - 1)
                    GoNext();
                else break;
            }
        }

        void GoNext()
        {
            if (advancing) return;
            current++;
            if (current >= steps.Count) { Finish(); return; }
            RestartAutoAdvance();
            StartCoroutine(TransitionSlide());
        }

        void GoPrev()
        {
            if (advancing || current <= 0) return;
            current--;
            RestartAutoAdvance();
            StartCoroutine(TransitionSlide());
        }

        void RestartAutoAdvance()
        {
            if (autoCoroutine != null) StopCoroutine(autoCoroutine);
            autoCoroutine = StartCoroutine(AutoAdvanceAfterDelay());
        }

        IEnumerator AutoAdvanceAfterDelay()
        {
            yield return new WaitForSeconds(AutoAdvanceTime);
            if (current < steps.Count - 1 && !advancing)
                GoNext();
        }

        IEnumerator TransitionSlide()
        {
            advancing = true;

            // Fade out
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

            // Pastro dhe nderto slidin e re
            var host = slideGroup.GetComponent<RectTransform>();
            for (int i = host.childCount - 1; i >= 0; i--)
                Destroy(host.GetChild(i).gameObject);

            BuildSlide(host, steps[current]);
            UpdateDots();
            UpdateProgressBar();

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

        void BuildSlide(RectTransform host, TutorialStep step)
        {
            // Container qendror
            var container = UI.Rect(host, "Content");
            container.anchorMin = container.anchorMax = new Vector2(0.5f, 0.5f);
            container.pivot = new Vector2(0.5f, 0.5f);
            container.anchoredPosition = new Vector2(0, 20);
            container.sizeDelta = new Vector2(600, 400);

            // ── Badge me ikone ──
            var badge = UI.Panel(container, "Badge", Theme.WithAlpha(step.AccentColor, 0.15f), 40, false);
            badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(0.5f, 1);
            badge.rectTransform.pivot = new Vector2(0.5f, 1);
            badge.rectTransform.anchoredPosition = Vector2.zero;
            badge.rectTransform.sizeDelta = new Vector2(80, 80);
            UI.Border(badge.transform, Theme.WithAlpha(step.AccentColor, 0.4f), 40);
            var iconTxt = UI.Label(badge.transform, step.Icon, Theme.Serif, 36,
                step.AccentColor, TextAnchor.MiddleCenter);
            UI.Stretch(iconTxt.rectTransform, 0, 0, 0, 0);

            // ── Titulli ──
            var titleLbl = UI.LabelAt(container, step.Title, Theme.Serif, 32, Theme.Parchment,
                0, 100, 600, 50);
            titleLbl.alignment = TextAnchor.UpperCenter;

            // ── Vije dekorative ──
            var line = UI.Panel(container, "Line", Theme.WithAlpha(step.AccentColor, 0.4f), 0, false);
            line.rectTransform.anchorMin = line.rectTransform.anchorMax = new Vector2(0.5f, 1);
            line.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            line.rectTransform.anchoredPosition = new Vector2(0, -160);
            line.rectTransform.sizeDelta = new Vector2(100, 2);

            // ── Teksti i trupit ──
            var bodyLbl = UI.LabelAt(container, step.Body, Theme.Sans, 20, Theme.Text2,
                40, 180, 520, 160);
            bodyLbl.alignment = TextAnchor.UpperCenter;

            // ── Numri i slidit ──
            var numLbl = UI.LabelAt(container, (current + 1) + " / " + steps.Count,
                Theme.Mono, 13, Theme.Muted, 0, 370, 600, 20);
            numLbl.alignment = TextAnchor.UpperCenter;
        }

        void Finish()
        {
            StopAllCoroutines();
            if (cardNumber <= 1) IntroVideo.MarkSeen();
            if (root != null) Destroy(root.gameObject);
            if (onFinished != null) onFinished();
            Destroy(this);
        }
    }
}
