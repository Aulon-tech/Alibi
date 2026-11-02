using System;
using System.Collections.Generic;
using Alibi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Alibi.Unity
{
    public sealed class TimelineController : MonoBehaviour
    {
        public Action<int> OnTimeChanged;
        CaseDefinition c;
        readonly List<UIButton> ticks = new List<UIButton>();
        Slider slider;
        int current;
        bool suppress;

        public void Build(RectTransform parent, CaseDefinition caseDef)
        {
            c = caseDef;
            var bg = UI.Card(parent, "TimelineCard", Theme.Panel, Theme.Line, 12);
            UI.Stretch(bg.rectTransform, 0, 0, 0, 0);
            UI.LabelAt(bg.transform, "KOHA", Theme.Mono, 14, Theme.Muted, 18, 0, 60, 56, TextAnchor.MiddleLeft);
            float x = 78;
            ticks.Clear();
            for (int t = 0; t < c.T; t++)
            {
                int tt = t;
                bool crime = c.CrimeTimeKnown && t == c.CrimeTime;
                var b = UI.Button(bg.transform, c.Hours[t] + (crime ? "\n<size=11><color=#F09A8F>KRIMI</color></size>" : ""), BtnStyle.Chip, () => Select(tt, true), 16, 9);
                UI.Place(b.Rect, x, 7, 88, 42);
                b.Label.lineSpacing = 0.9f;
                ticks.Add(b);
                x += 96;
            }
            var label = UI.LabelAt(bg.transform, "Rrëshqit kohën", Theme.Sans, 14, Theme.Muted, x + 10, 0, 130, 56, TextAnchor.MiddleLeft);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            slider = BuildSlider(bg.transform);
            var srt = (RectTransform)slider.transform;
            srt.anchorMin = new Vector2(0, 0.5f);
            srt.anchorMax = new Vector2(1, 0.5f);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.offsetMin = new Vector2(x + 150, -10);
            srt.offsetMax = new Vector2(-24, 10);
            slider.minValue = 0;
            slider.maxValue = Mathf.Max(1, c.T - 1);
            slider.wholeNumbers = true;
            slider.onValueChanged.AddListener(v =>
            {
                if (!suppress) Select(Mathf.RoundToInt(v), true);
            });
        }

        static Slider BuildSlider(Transform parent)
        {
            var root = UI.Rect(parent, "Slider");
            var bgImg = UI.Panel(root, "Track", Theme.Line, 6, true);
            var bgRt = bgImg.rectTransform;
            bgRt.anchorMin = new Vector2(0, 0.5f);
            bgRt.anchorMax = new Vector2(1, 0.5f);
            bgRt.offsetMin = new Vector2(0, -4);
            bgRt.offsetMax = new Vector2(0, 4);
            var fillArea = UI.Rect(root, "FillArea");
            fillArea.anchorMin = new Vector2(0, 0.5f);
            fillArea.anchorMax = new Vector2(1, 0.5f);
            fillArea.offsetMin = new Vector2(0, -4);
            fillArea.offsetMax = new Vector2(0, 4);
            var fill = UI.Panel(fillArea, "Fill", Theme.Gold, 6, false);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            var handleArea = UI.Rect(root, "HandleArea");
            UI.Stretch(handleArea, 10, 0, 10, 0);
            var handle = UI.Panel(handleArea, "Handle", Theme.Gold, 64, true);
            handle.rectTransform.sizeDelta = new Vector2(22, 22);
            handle.rectTransform.anchorMin = new Vector2(0, 0.5f);
            handle.rectTransform.anchorMax = new Vector2(0, 0.5f);
            var s = root.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.direction = Slider.Direction.LeftToRight;
            var nav = new Navigation();
            nav.mode = Navigation.Mode.None;
            s.navigation = nav;
            return s;
        }

        public void Select(int t, bool notify)
        {
            current = Mathf.Clamp(t, 0, c.T - 1);
            for (int i = 0; i < ticks.Count; i++) ticks[i].SetStyle(i == current ? BtnStyle.ChipOn : BtnStyle.Chip);
            suppress = true;
            slider.value = current;
            suppress = false;
            if (notify && OnTimeChanged != null) OnTimeChanged(current);
        }
    }
}
