using System;
using System.Collections.Generic;
using Alibi.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace Alibi.Unity
{
    public struct RoomLayout
    {
        public string Id;
        public float X0, Z0, X1, Z1, Y;
        public string Floor;

        public RoomLayout(string id, float x0, float z0, float x1, float z1, float y, string floor)
        {
            Id = id;
            X0 = x0;
            Z0 = z0;
            X1 = x1;
            Z1 = z1;
            Y = y;
            Floor = floor;
        }

        public Vector3 Center { get { return new Vector3((X0 + X1) * 0.5f, Y, (Z0 + Z1) * 0.5f); } }
    }

    public struct DoorLayout
    {
        public string A, B;
        public bool Vertical;
        public float At, From, To;

        public DoorLayout(string a, string b, bool vertical, float at, float from, float to)
        {
            A = a;
            B = b;
            Vertical = vertical;
            At = at;
            From = from;
            To = to;
        }
    }

    public static class MapBuilder
    {
        public const float WallHeight = 0.55f;
        public const float WallThickness = 0.14f;
        public const float UpperFloor = 1.6f;

        public static readonly RoomLayout[] Layouts =
        {
            new RoomLayout("HO", 3, 0, 7, 3, 0, "marble"),
            new RoomLayout("SA", 3, 3, 7, 6, 0, "oak"),
            new RoomLayout("BI", 0, 3, 3, 6, 0, "carpet"),
            new RoomLayout("DN", 7, 3, 10, 6, 0, "walnut"),
            new RoomLayout("KU", 7, 0, 10, 3, 0, "tile"),
            new RoomLayout("SH", 0, 0, 3, 3, 0, "stairs"),
            new RoomLayout("KO", -3.6f, 0, 0, 3, UpperFloor, "runner"),
            new RoomLayout("ZY", -3.6f, 3, 0, 6, UpperFloor, "desk"),
            new RoomLayout("BO", 0, -3.2f, 3, 0, -1.35f, "stone")
        };

        public static readonly DoorLayout[] Doors =
        {
            new DoorLayout("HO", "SA", false, 3, 4.5f, 5.5f),
            new DoorLayout("HO", "KU", true, 7, 1, 2),
            new DoorLayout("HO", "SH", true, 3, 1, 2),
            new DoorLayout("SA", "BI", true, 3, 4, 5),
            new DoorLayout("SA", "DN", true, 7, 4, 5),
            new DoorLayout("KU", "DN", false, 3, 8, 9),
            new DoorLayout("SH", "KO", true, 0, 1, 2),
            new DoorLayout("KO", "ZY", false, 3, -2.4f, -1.2f),
            new DoorLayout("SH", "BO", false, 0, 0.8f, 2.2f)
        };

        public static RoomLayout Layout(string id)
        {
            foreach (var l in Layouts) if (l.Id == id) return l;
            throw new ArgumentException(id);
        }

        static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        static GameObject FlatQuad(Transform parent, string name, Vector3 center, Vector2 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        public static VillaView Build(Transform parent, CaseDefinition c)
        {
            var root = new GameObject("Vila");
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<VillaView>();
            view.Case = c;
            var present = new HashSet<string>();
            foreach (var r in c.Rooms) present.Add(r.Id);
            var wallMat = Theme.Lit(Theme.Hex("#4A3E34"), 0.1f);
            var capMat = Theme.Lit(Theme.Hex("#C2A673"), 0.2f);
            var plinthMat = Theme.Lit(Theme.Hex("#2B2A2E"), 0.05f);
            var stepMat = Theme.Lit(Theme.Hex("#7B6450"), 0.15f);
            var doorMat = Theme.Lit(Theme.Hex("#CDAA70"), 0.35f);
            var baseMat = Theme.Lit(Theme.Hex("#1A1D24"), 0.05f);
            var bounds = new Bounds();
            bool first = true;

            for (int i = 0; i < c.RoomCount; i++)
            {
                var info = c.Rooms[i];
                var l = Layout(info.Id);
                var roomGo = new GameObject("Room_" + info.Id);
                roomGo.transform.SetParent(root.transform, false);
                var rv = roomGo.AddComponent<RoomView>();
                rv.Index = i;
                rv.Info = info;
                rv.Layout = l;
                float w = l.X1 - l.X0, d = l.Z1 - l.Z0;
                var floor = Box(roomGo.transform, "Floor", new Vector3(l.Center.x, l.Y - 0.05f, l.Center.z), new Vector3(w, 0.1f, d), Theme.Floor(l.Floor, w / 2f, d / 2f), true);
                rv.FloorCollider = floor.GetComponent<Collider>();
                floor.AddComponent<RoomClickTarget>().Room = rv;
                if (l.Y > 0.01f)
                {
                    Box(roomGo.transform, "Plinth", new Vector3(l.Center.x, l.Y * 0.5f - 0.05f, l.Center.z), new Vector3(w, l.Y - 0.1f, d), plinthMat);
                }
                rv.Highlight = FlatQuad(roomGo.transform, "Highlight", new Vector3(l.Center.x, l.Y + 0.012f, l.Center.z), new Vector2(w - 0.2f, d - 0.2f), Theme.Ghost(Theme.WithAlpha(Theme.Gold, 0.22f)));
                rv.Highlight.SetActive(false);
                if (info.Id == "SH")
                {
                    for (int s = 0; s < 6; s++)
                    {
                        float x1 = 2.52f - s * 0.42f;
                        float h = 0.27f * (s + 1);
                        Box(roomGo.transform, "Step" + s, new Vector3(x1 - 0.21f, h * 0.5f, 1.5f), new Vector3(0.42f, h, 1.3f), stepMat);
                    }
                }
                view.Rooms[i] = rv;
                var rb = new Bounds(new Vector3(l.Center.x, l.Y * 0.5f + 0.4f, l.Center.z), new Vector3(w, l.Y + 0.8f, d));
                if (first) bounds = rb;
                else bounds.Encapsulate(rb);
                first = false;
            }

            var done = new HashSet<string>();
            foreach (var rv in view.Rooms.Values)
            {
                var l = rv.Layout;
                BuildEdge(root.transform, c, present, done, l, true, l.X0, l.Z0, l.Z1, wallMat, capMat, doorMat);
                BuildEdge(root.transform, c, present, done, l, true, l.X1, l.Z0, l.Z1, wallMat, capMat, doorMat);
                BuildEdge(root.transform, c, present, done, l, false, l.Z0, l.X0, l.X1, wallMat, capMat, doorMat);
                BuildEdge(root.transform, c, present, done, l, false, l.Z1, l.X0, l.X1, wallMat, capMat, doorMat);
            }

            var basePlate = Box(root.transform, "Base", new Vector3(bounds.center.x, -0.18f, bounds.center.z), new Vector3(bounds.size.x + 0.8f, 0.24f, bounds.size.z + 0.8f), baseMat);
            basePlate.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            var catcher = GameObject.CreatePrimitive(PrimitiveType.Cube);
            catcher.name = "GroundCatcher";
            catcher.transform.SetParent(root.transform, false);
            catcher.transform.localPosition = new Vector3(bounds.center.x, -0.6f, bounds.center.z);
            catcher.transform.localScale = new Vector3(80f, 0.2f, 80f);
            UnityEngine.Object.Destroy(catcher.GetComponent<Renderer>());
            catcher.AddComponent<GroundCatcher>().View = view;

            var crime = view.Rooms[c.CrimeRoom].Layout;
            view.CrimeDecal = FlatQuad(root.transform, "CrimeScene", new Vector3(crime.Center.x, crime.Y + 0.01f, crime.Center.z), new Vector2(crime.X1 - crime.X0 - 0.5f, crime.Z1 - crime.Z0 - 0.5f), Theme.Ghost(new Color(0.85f, 0.25f, 0.2f, 0.16f)));
            var lightGo = new GameObject("CrimeLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(crime.Center.x, crime.Y + 1.6f, crime.Center.z);
            view.CrimeLight = lightGo.AddComponent<Light>();
            view.CrimeLight.type = LightType.Point;
            view.CrimeLight.color = new Color(1f, 0.35f, 0.28f);
            view.CrimeLight.range = 4.5f;
            view.CrimeLight.intensity = 0f;
            view.CrimeLight.shadows = LightShadows.None;

            view.Bounds = bounds;
            view.BuildMarkers();
            return view;
        }

        static void BuildEdge(Transform root, CaseDefinition c, HashSet<string> present, HashSet<string> done, RoomLayout room, bool vertical, float at, float from, float to, Material wallMat, Material capMat, Material doorMat)
        {
            string key = (vertical ? "V" : "H") + at.ToString("0.00") + ":" + from.ToString("0.00") + "-" + to.ToString("0.00");
            if (done.Contains(key)) return;
            done.Add(key);
            string other = null;
            foreach (var l in Layouts)
            {
                if (l.Id == room.Id || !present.Contains(l.Id)) continue;
                bool shares = vertical
                    ? (Mathf.Approximately(l.X0, at) || Mathf.Approximately(l.X1, at)) && l.Z0 <= from + 0.01f && l.Z1 >= to - 0.01f
                    : (Mathf.Approximately(l.Z0, at) || Mathf.Approximately(l.Z1, at)) && l.X0 <= from + 0.01f && l.X1 >= to - 0.01f;
                if (shares) other = l.Id;
            }
            float baseY = room.Y;
            if (other != null) baseY = Mathf.Max(room.Y, Layout(other).Y);
            var cuts = new List<Vector2>();
            if (other != null)
            {
                foreach (var d in Doors)
                {
                    bool match = (d.A == room.Id && d.B == other) || (d.B == room.Id && d.A == other);
                    if (match && d.Vertical == vertical && Mathf.Approximately(d.At, at)) cuts.Add(new Vector2(d.From, d.To));
                }
            }
            cuts.Sort((a, b) => a.x.CompareTo(b.x));
            float cur = from;
            var segs = new List<Vector2>();
            foreach (var cut in cuts)
            {
                if (cut.x > cur) segs.Add(new Vector2(cur, cut.x));
                cur = cut.y;
            }
            if (cur < to) segs.Add(new Vector2(cur, to));
            foreach (var s in segs)
            {
                float len = s.y - s.x;
                float mid = (s.x + s.y) * 0.5f;
                Vector3 center = vertical ? new Vector3(at, baseY + WallHeight * 0.5f, mid) : new Vector3(mid, baseY + WallHeight * 0.5f, at);
                Vector3 size = vertical ? new Vector3(WallThickness, WallHeight, len + WallThickness) : new Vector3(len + WallThickness, WallHeight, WallThickness);
                Box(root, "Wall", center, size, wallMat);
                Vector3 capCenter = center + new Vector3(0, WallHeight * 0.5f + 0.02f, 0);
                Vector3 capSize = vertical ? new Vector3(WallThickness + 0.04f, 0.04f, len + WallThickness + 0.04f) : new Vector3(len + WallThickness + 0.04f, 0.04f, WallThickness + 0.04f);
                Box(root, "WallCap", capCenter, capSize, capMat);
            }
            foreach (var cut in cuts)
            {
                float mid = (cut.x + cut.y) * 0.5f;
                float len = cut.y - cut.x;
                Vector3 center = vertical ? new Vector3(at, baseY + 0.015f, mid) : new Vector3(mid, baseY + 0.015f, at);
                Vector3 size = vertical ? new Vector3(WallThickness + 0.1f, 0.03f, len) : new Vector3(len, 0.03f, WallThickness + 0.1f);
                Box(root, "Threshold", center, size, doorMat);
            }
        }
    }

    public sealed class VillaView : MonoBehaviour
    {
        public CaseDefinition Case;
        public readonly Dictionary<int, RoomView> Rooms = new Dictionary<int, RoomView>();
        public SuspectMarker[,] Markers;
        public Bounds Bounds;
        public GameObject CrimeDecal;
        public Light CrimeLight;
        public Action<int> RoomClicked;
        public Action<int> SuspectClicked;
        public Action BackgroundClicked;
        public CameraRig Rig;
        float crimeTarget;

        public void BuildMarkers()
        {
            Markers = new SuspectMarker[Case.N, Case.RoomCount];
            for (int s = 0; s < Case.N; s++)
            {
                var color = Theme.Hex(Case.Suspects[s].ColorHex);
                var lit = Theme.Lit(color, 0.35f);
                var ghost = Theme.Ghost(Theme.WithAlpha(color, 0.34f));
                var ringMat = Theme.Ghost(Theme.WithAlpha(color, 0.9f));
                ringMat.mainTexture = Theme.Ring.texture;
                var conflictMat = Theme.Ghost(new Color(0.95f, 0.35f, 0.3f, 1f));
                conflictMat.mainTexture = Theme.Ring.texture;
                for (int r = 0; r < Case.RoomCount; r++)
                {
                    var room = Rooms[r];
                    var pos = Slot(room.Layout, s, Case.N);
                    Markers[s, r] = SuspectMarker.Create(room.transform, this, s, r, pos, lit, ghost, ringMat, conflictMat);
                }
            }
        }

        public static Vector3 Slot(RoomLayout l, int index, int count)
        {
            float x0 = l.X0 + 0.55f, x1 = l.X1 - 0.55f, z0 = l.Z0 + 0.6f, z1 = l.Z1 - 0.55f;
            if (l.Id == "SH")
            {
                z0 = 2.35f;
                z1 = 2.75f;
            }
            int front = count <= 3 ? count : 3;
            int back = count - front;
            float fx, fz;
            if (index < front)
            {
                fx = front == 1 ? 0.5f : index / (float)(front - 1);
                fz = back > 0 ? 0f : 0.5f;
            }
            else
            {
                int j = index - front;
                fx = back == 1 ? 0.5f : 0.2f + 0.6f * j / Mathf.Max(1, back - 1);
                fz = 1f;
            }
            return new Vector3(Mathf.Lerp(x0, x1, fx), l.Y, Mathf.Lerp(z0, z1, fz));
        }

        public void SetCrimeActive(bool strong)
        {
            crimeTarget = strong ? 2.6f : 0.4f;
        }

        public void SelectRoom(int index)
        {
            foreach (var kv in Rooms) kv.Value.Highlight.SetActive(kv.Key == index);
        }

        void Update()
        {
            if (CrimeLight != null) CrimeLight.intensity = Mathf.Lerp(CrimeLight.intensity, crimeTarget, Time.deltaTime * 4f);
        }
    }

    public sealed class RoomView : MonoBehaviour
    {
        public int Index;
        public RoomInfo Info;
        public RoomLayout Layout;
        public Collider FloorCollider;
        public GameObject Highlight;

        public Vector3 LabelPoint { get { return new Vector3(Layout.Center.x, Layout.Y + 0.05f, Layout.Z0 + 0.3f); } }
    }

    public sealed class RoomClickTarget : MonoBehaviour, IPointerClickHandler, IScrollHandler
    {
        public RoomView Room;

        public void OnPointerClick(PointerEventData eventData)
        {
            var view = GetComponentInParent<VillaView>();
            if (view != null && view.RoomClicked != null) view.RoomClicked(Room.Index);
        }

        public void OnScroll(PointerEventData eventData)
        {
            var view = GetComponentInParent<VillaView>();
            if (view != null && view.Rig != null) view.Rig.ZoomBy(eventData.scrollDelta.y > 0 ? 1.12f : 1f / 1.12f);
        }
    }

    public sealed class GroundCatcher : MonoBehaviour, IPointerClickHandler, IScrollHandler
    {
        public VillaView View;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (View != null && View.BackgroundClicked != null) View.BackgroundClicked();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (View != null && View.Rig != null) View.Rig.ZoomBy(eventData.scrollDelta.y > 0 ? 1.12f : 1f / 1.12f);
        }
    }
}
