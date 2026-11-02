using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Alibi.Unity
{
    public sealed class ScreenLabels : MonoBehaviour
    {
        public VillaView View;
        public Camera Cam;
        public RectTransform Layer;
        public RectTransform Clip;
        public bool ShowRoomNames = true;

        readonly List<Text> roomLabels = new List<Text>();
        readonly List<RectTransform> roomRects = new List<RectTransform>();
        Image[,] badges;
        Text[,] badgeTexts;
        readonly Vector3[] clipCorners = new Vector3[4];

        public void Build()
        {
            UI.Clear(Layer);
            roomLabels.Clear();
            roomRects.Clear();
            var c = View.Case;
            for (int r = 0; r < c.RoomCount; r++)
            {
                bool crime = r == c.CrimeRoom;
                var t = UI.Label(Layer, c.Rooms[r].Name.ToUpperInvariant(), Theme.SansSemi, 13, crime ? Theme.RedText : Theme.WithAlpha(Theme.Parchment, 0.78f), TextAnchor.MiddleCenter);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                var shadow = t.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, 0.8f);
                shadow.effectDistance = new Vector2(1, -1);
                t.rectTransform.sizeDelta = new Vector2(220, 20);
                t.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                roomLabels.Add(t);
                roomRects.Add(t.rectTransform);
            }
            badges = new Image[c.N, c.RoomCount];
            badgeTexts = new Text[c.N, c.RoomCount];
            for (int s = 0; s < c.N; s++)
            {
                for (int r = 0; r < c.RoomCount; r++)
                {
                    var img = UI.Panel(Layer, "Badge", Theme.Hex(c.Suspects[s].ColorHex), 5, false);
                    img.rectTransform.sizeDelta = new Vector2(22, 20);
                    img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    img.rectTransform.pivot = new Vector2(0.5f, 0f);
                    var t = UI.Label(img.transform, c.Suspects[s].Initial, Theme.SansBold, 13, Theme.Night, TextAnchor.MiddleCenter);
                    UI.Stretch(t.rectTransform, 0, 0, 0, 0);
                    t.horizontalOverflow = HorizontalWrapMode.Overflow;
                    badges[s, r] = img;
                    badgeTexts[s, r] = t;
                    img.gameObject.SetActive(false);
                }
            }
        }

        bool ToLocal(Vector3 world, out Vector2 local)
        {
            local = Vector2.zero;
            var sp = Cam.WorldToScreenPoint(world);
            if (sp.z < 0) return false;
            if (Clip != null)
            {
                Clip.GetWorldCorners(clipCorners);
                if (sp.x < clipCorners[0].x || sp.x > clipCorners[2].x || sp.y < clipCorners[0].y || sp.y > clipCorners[2].y) return false;
            }
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(Layer, sp, null, out local);
        }

        void LateUpdate()
        {
            if (View == null || Cam == null || badges == null) return;
            var c = View.Case;
            for (int r = 0; r < roomLabels.Count; r++)
            {
                Vector2 local = Vector2.zero;
                bool ok = ShowRoomNames && ToLocal(View.Rooms[r].LabelPoint, out local);
                roomLabels[r].enabled = ok;
                if (ok) roomRects[r].anchoredPosition = local;
            }
            for (int s = 0; s < c.N; s++)
            {
                for (int r = 0; r < c.RoomCount; r++)
                {
                    var m = View.Markers[s, r];
                    var badge = badges[s, r];
                    bool show = m != null && m.State != MarkerState.Hidden && m.IsVisible;
                    Vector2 local = Vector2.zero;
                    if (show) show = ToLocal(m.HeadPoint, out local);
                    if (badge.gameObject.activeSelf != show) badge.gameObject.SetActive(show);
                    if (!show) continue;
                    badge.rectTransform.anchoredPosition = local;
                    var col = Theme.Hex(c.Suspects[s].ColorHex);
                    bool confirmed = m.State == MarkerState.Confirmed;
                    badge.color = confirmed ? col : new Color(0.05f, 0.06f, 0.08f, 0.85f);
                    badgeTexts[s, r].color = confirmed ? Theme.Night : Theme.Shift(col, 0.25f);
                    badgeTexts[s, r].text = c.Suspects[s].Initial + (m.Conflict ? "!" : "");
                    badge.rectTransform.sizeDelta = new Vector2(m.Conflict ? 30 : 22, 20);
                }
            }
        }
    }
}
