using System.Collections.Generic;
using Alibi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Alibi.Unity
{
    public sealed class ProgressiveRules
    {
        readonly GameManager gm;
        readonly HashSet<string> shown = new HashSet<string>();
        float hideTime;
        RectTransform banner;

        public ProgressiveRules(GameManager manager) { gm = manager; }

        static readonly Dictionary<string, string[]> Tips = new Dictionary<string, string[]>
        {
            { "first_question", new[] { "PYETJA E PARË", "Zgjidh një të dyshuar, pastaj një lloj pyetjeje. Çdo përgjigje bëhet kufizim në tabelë." } },
            { "contradiction", new[] { "KONTRADITË", "Dy deklarata nuk mund të jenë të vërteta njëkohësisht. Një nga të dyshuarit po gënjen — ai është fajtori." } },
            { "elimination", new[] { "PËRJASHTIM", "Dikush doli jashtë dyshimit: deklaratat e të tjerëve provuan që nuk mund të ishte në vendin e krimit." } },
            { "budget_half", new[] { "BUXHETI", "Ke përdorur gjysmën e pyetjeve. Zgjidh me kujdes — Q4 dhe Q5 kushtojnë 2." } },
            { "budget_low", new[] { "BUXHETI I ULËT", "Kanë mbetur vetëm disa pyetje. Mund të jetë koha për të akuzuar." } },
            { "graph_move", new[] { "GRAFI I LËVIZJES", "Këta dhoma janë të lidhura me kalime. Një person që nuk arrin dot në dh. e krimit përjashtohet (R02, R08)." } },
            { "duplicate", new[] { "PYETJE E PËRSËRITUR", "Këtë pyetje e ke bërë më parë. Përgjigja nuk sjell informacion të ri, por kostoja humbet." } },
            { "accuse_ready", new[] { "GATI PËR AKUZË", "Mbeti vetëm një kandidat. Shtyp Akuzo për të mbërritur rastin." } },
            { "crime_time", new[] { "ORA E KRIMIT", "Ora e krimit është e panjohur në këtë nivel. Duhet ta gjesh me pyetje." } },
        };

        public void OnQuestionAsked(AskOutcome outcome)
        {
            var s = gm.Session;
            if (s.Statements.Count == 1) Show("first_question");
            if (outcome.Conflict != null) Show("contradiction");
            if (outcome.NewlyCleared.Count > 0) Show("elimination");
            if (outcome.Duplicate) Show("duplicate");
            if (s.Remaining <= s.Case.Budget / 2 && s.Remaining > 3) Show("budget_half");
            if (s.Remaining <= 3 && s.Remaining > 0) Show("budget_low");
            if (s.Case.UseGraph && s.Statements.Count == 1) Show("graph_move");
            int candidates = 0;
            for (int i = 0; i < s.Case.N; i++) if (s.Domains.Candidate[i]) candidates++;
            if (candidates == 1) Show("accuse_ready");
            if (!s.Case.CrimeTimeKnown && s.Statements.Count == 1) Show("crime_time");
        }

        void Show(string key)
        {
            if (shown.Contains(key)) return;
            string[] tip;
            if (!Tips.TryGetValue(key, out tip)) return;
            shown.Add(key);
            ShowBanner(tip[0], tip[1]);
        }

        void ShowBanner(string title, string text)
        {
            if (banner != null) Object.Destroy(banner.gameObject);
            var canvas = gm.CanvasTransform;
            if (canvas == null) return;
            var rt = UI.Rect(canvas, "RuleTip");
            banner = rt;
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -12);
            var bg = UI.Card(rt, "BannerBg", Theme.Hex("#0D1017"), Theme.WithAlpha(Theme.Gold, 0.5f), 12);
            UI.Stretch(bg.rectTransform, 0, 0, 0, 0);
            var badge = UI.Panel(bg.transform, "Badge", Theme.WithAlpha(Theme.Gold, 0.2f), 14, false);
            UI.Place(badge.rectTransform, 14, 14, 28, 28);
            var il = UI.Label(badge.transform, "i", Theme.Serif, 18, Theme.Gold, TextAnchor.MiddleCenter);
            UI.Stretch(il.rectTransform, 0, 0, 0, 0);
            UI.LabelAt(bg.transform, title, Theme.Mono, 12, Theme.Gold, 52, 12, 460, 16);
            var body = UI.LabelAt(bg.transform, text, Theme.Sans, 15, Theme.Text, 52, 30, 460, 40);
            float h = UI.FitHeight(body, 460);
            float totalH = 30 + h + 14;
            rt.sizeDelta = new Vector2(530, totalH);
            hideTime = Time.time + 6f;
        }

        public void Update()
        {
            if (banner != null && Time.time > hideTime)
            {
                var cg = banner.GetComponent<CanvasGroup>();
                if (cg == null) cg = banner.gameObject.AddComponent<CanvasGroup>();
                cg.alpha -= Time.deltaTime * 2f;
                if (cg.alpha <= 0)
                {
                    Object.Destroy(banner.gameObject);
                    banner = null;
                }
            }
        }
    }
}
