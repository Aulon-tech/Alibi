using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Alibi.Unity
{
    public enum BtnStyle { Primary, Danger, Ghost, Chip, ChipOn, Disabled, Dark, Parchment, ParchmentGhost, Tab, TabOn }

    public sealed class UIButton
    {
        public RectTransform Rect;
        public Image Background;
        public Image Border;
        public Text Label;
        public Button Button;
        public BtnStyle Style;
        Action onClick;

        public void SetStyle(BtnStyle style)
        {
            Style = style;
            Color bg, border, text;
            UI.StyleColors(style, out bg, out border, out text);
            Background.color = bg;
            Border.color = border;
            if (Label != null) Label.color = text;
        }

        public void SetText(string text)
        {
            if (Label != null) Label.text = text;
        }

        public void SetInteractable(bool on)
        {
            Button.interactable = on;
        }

        public void SetAction(Action action)
        {
            onClick = action;
        }

        public void Invoke()
        {
            if (onClick != null) onClick();
        }

        public void SetDashed(bool dashed)
        {
            Border.color = dashed ? Theme.Gold : Border.color;
        }
    }

    public static class UI
    {
        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform PlaceRight(RectTransform rt, float right, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-right, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left, float top, float right, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform StretchTop(RectTransform rt, float left, float top, float right, float height)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(left, -top - height);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform StretchBottom(RectTransform rt, float left, float bottom, float right, float height)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, bottom + height);
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color color, int radius = 12, bool raycast = true)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (radius > 0)
            {
                img.sprite = Theme.Rounded(radius);
                img.type = Image.Type.Sliced;
            }
            img.raycastTarget = raycast;
            return img;
        }

        public static Image Border(Transform parent, Color color, int radius = 12)
        {
            var rt = Rect(parent, "Border");
            Stretch(rt, 0, 0, 0, 0);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.OutlineSprite(radius);
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Image Card(Transform parent, string name, Color fill, Color border, int radius = 12)
        {
            var img = Panel(parent, name, fill, radius);
            Border(img.transform, border, radius);
            return img;
        }

        public static Text Label(Transform parent, string text, Font font, int size, Color color, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.lineSpacing = 1.05f;
            return t;
        }

        public static Text LabelAt(Transform parent, string text, Font font, int size, Color color, float x, float y, float w, float h, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var t = Label(parent, text, font, size, color, anchor);
            Place(t.rectTransform, x, y, w, h);
            return t;
        }

        public static float FitHeight(Text t, float width)
        {
            t.rectTransform.sizeDelta = new Vector2(width, t.rectTransform.sizeDelta.y);
            float h = Mathf.Ceil(t.preferredHeight) + 2f;
            t.rectTransform.sizeDelta = new Vector2(width, h);
            return h;
        }

        public static float TextWidth(string text, Font font, int size)
        {
            var settings = new TextGenerationSettings
            {
                font = font,
                fontSize = size,
                fontStyle = FontStyle.Normal,
                scaleFactor = 1f,
                richText = true,
                textAnchor = TextAnchor.MiddleLeft,
                horizontalOverflow = HorizontalWrapMode.Overflow,
                verticalOverflow = VerticalWrapMode.Overflow,
                generationExtents = new Vector2(2000f, 200f),
                pivot = new Vector2(0.5f, 0.5f),
                color = Color.white,
                lineSpacing = 1f,
                updateBounds = false,
                generateOutOfBounds = true
            };
            var gen = new TextGenerator();
            return gen.GetPreferredWidth(text, settings);
        }

        public static void StyleColors(BtnStyle style, out Color bg, out Color border, out Color text)
        {
            switch (style)
            {
                case BtnStyle.Primary: bg = Theme.Gold; border = Theme.Gold; text = Theme.Ink; break;
                case BtnStyle.Danger: bg = Theme.Hex("#B8433A"); border = Theme.Red; text = Theme.Hex("#FFF6EE"); break;
                case BtnStyle.Ghost: bg = new Color(1, 1, 1, 0.02f); border = Theme.Line2; text = Theme.Text; break;
                case BtnStyle.Chip: bg = Theme.Panel2; border = Theme.Line2; text = Theme.Text; break;
                case BtnStyle.ChipOn: bg = Theme.WithAlpha(Theme.Gold, 0.18f); border = Theme.Gold; text = Theme.GoldText; break;
                case BtnStyle.Disabled: bg = Theme.Hex("#1B2029"); border = Theme.Line; text = Theme.Hex("#7F858F"); break;
                case BtnStyle.Dark: bg = Theme.ParchInk; border = Theme.ParchInk; text = Theme.Parchment; break;
                case BtnStyle.Parchment: bg = Theme.ParchInk; border = Theme.ParchInk; text = Theme.Parchment; break;
                case BtnStyle.ParchmentGhost: bg = new Color(0, 0, 0, 0); border = Theme.WithAlpha(Theme.ParchInk, 0.45f); text = Theme.ParchInk; break;
                case BtnStyle.Tab: bg = new Color(0, 0, 0, 0); border = new Color(0, 0, 0, 0); text = Theme.Muted; break;
                case BtnStyle.TabOn: bg = Theme.Panel2; border = Theme.Line2; text = Theme.GoldText; break;
                default: bg = Theme.Panel2; border = Theme.Line2; text = Theme.Text; break;
            }
        }

        public static UIButton Button(Transform parent, string label, BtnStyle style, Action onClick, int fontSize = 18, int radius = 10, Font font = null)
        {
            var rt = Rect(parent, "Button");
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.Rounded(radius);
            img.type = Image.Type.Sliced;
            var border = Border(rt, Color.clear, radius);
            Text text = null;
            if (label != null)
            {
                text = Label(rt, label, font ?? Theme.SansSemi, fontSize, Color.white, TextAnchor.MiddleCenter);
                Stretch(text.rectTransform, 10, 2, 10, 2);
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.75f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            var nav = new Navigation();
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;
            var b = new UIButton { Rect = rt, Background = img, Border = border, Label = text, Button = btn };
            b.SetAction(onClick);
            btn.onClick.AddListener(b.Invoke);
            b.SetStyle(style);
            return b;
        }

        public static Image Avatar(Transform parent, Core.SuspectInfo s, float size, float x, float y)
        {
            var img = Panel(parent, "Avatar", Theme.Hex(s.ColorHex), 64, false);
            Place(img.rectTransform, x, y, size, size);
            var t = Label(img.transform, s.Initial, Theme.SansBold, Mathf.RoundToInt(size * 0.42f), Theme.Night, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform, 0, 0, 0, 0);
            return img;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var child = t.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
        }

        public static ScrollRect Scroll(Transform parent, string name, out RectTransform content)
        {
            var root = Rect(parent, name);
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0);
            bg.raycastTarget = true;
            var viewport = Rect(root, "Viewport");
            Stretch(viewport, 0, 0, 0, 0);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Rect(viewport, "Content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0, 100);
            var sr = root.gameObject.AddComponent<ScrollRect>();
            sr.viewport = viewport;
            sr.content = content;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 40f;
            sr.inertia = true;
            return sr;
        }

        public static Image Divider(Transform parent, float x, float y, float w)
        {
            var img = Panel(parent, "Divider", Theme.Line, 0, false);
            Place(img.rectTransform, x, y, w, 1);
            return img;
        }

        public static Text SectionLabel(Transform parent, string text, float x, float y, float w)
        {
            return LabelAt(parent, text, Theme.Mono, 14, Theme.Gold, x, y, w, 20);
        }
    }

    public sealed class ClickRelay : MonoBehaviour, IPointerClickHandler
    {
        public Action OnClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (OnClick != null) OnClick();
        }
    }
}
