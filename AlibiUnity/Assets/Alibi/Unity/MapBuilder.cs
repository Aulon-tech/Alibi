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
        public float Width { get { return X1 - X0; } }
        public float Depth { get { return Z1 - Z0; } }
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
        public const float WallHeight = 3.0f;
        public const float WallThickness = 0.2f;
        public const float UpperFloor = 3.4f;
        public const float DoorHeight = 2.4f;
        public const float WindowBottom = 1.0f;
        public const float WindowTop = 2.2f;
        public const float Scale = 4f;

        public static readonly RoomLayout[] Layouts =
        {
            new RoomLayout("HO", 3 * Scale, 0, 7 * Scale, 3 * Scale, 0, "marble"),
            new RoomLayout("SA", 3 * Scale, 3 * Scale, 7 * Scale, 6 * Scale, 0, "oak"),
            new RoomLayout("BI", 0, 3 * Scale, 3 * Scale, 6 * Scale, 0, "carpet"),
            new RoomLayout("DN", 7 * Scale, 3 * Scale, 10 * Scale, 6 * Scale, 0, "walnut"),
            new RoomLayout("KU", 7 * Scale, 0, 10 * Scale, 3 * Scale, 0, "tile"),
            new RoomLayout("SH", 0, 0, 3 * Scale, 3 * Scale, 0, "stairs"),
            new RoomLayout("KO", -3.6f * Scale, 0, 0, 3 * Scale, UpperFloor, "runner"),
            new RoomLayout("ZY", -3.6f * Scale, 3 * Scale, 0, 6 * Scale, UpperFloor, "oak"),
            new RoomLayout("BO", 0, -3.2f * Scale, 3 * Scale, 0, -3.0f, "stone")
        };

        public static readonly DoorLayout[] Doors =
        {
            new DoorLayout("HO", "SA", false, 3 * Scale, 4.5f * Scale, 5.5f * Scale),
            new DoorLayout("HO", "KU", true, 7 * Scale, 1 * Scale, 2 * Scale),
            new DoorLayout("HO", "SH", true, 3 * Scale, 1 * Scale, 2 * Scale),
            new DoorLayout("SA", "BI", true, 3 * Scale, 4 * Scale, 5 * Scale),
            new DoorLayout("SA", "DN", true, 7 * Scale, 4 * Scale, 5 * Scale),
            new DoorLayout("KU", "DN", false, 3 * Scale, 8 * Scale, 9 * Scale),
            new DoorLayout("SH", "KO", true, 0, 1 * Scale, 2 * Scale),
            new DoorLayout("KO", "ZY", false, 3 * Scale, -2.4f * Scale, -1.2f * Scale),
            new DoorLayout("SH", "BO", false, 0, 0.8f * Scale, 2.2f * Scale)
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
            else go.GetComponent<Collider>().isTrigger = false;
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

        static GameObject WallQuad(Transform parent, string name, Vector3 center, Vector3 normal, float width, float height, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localRotation = Quaternion.LookRotation(normal);
            go.transform.localScale = new Vector3(width, height, 1f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
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
            var wallMat = Theme.Lit(Theme.Hex("#5A4E42"), 0.08f);
            var wallInnerMat = Theme.Lit(Theme.Hex("#8C7E6E"), 0.06f);
            var ceilingMat = Theme.Lit(Theme.Hex("#6E6358"), 0.04f);
            var capMat = Theme.Lit(Theme.Hex("#C2A673"), 0.2f);
            var plinthMat = Theme.Lit(Theme.Hex("#2B2A2E"), 0.05f);
            var stepMat = Theme.Lit(Theme.Hex("#7B6450"), 0.15f);
            var doorMat = Theme.Lit(Theme.Hex("#CDAA70"), 0.35f);
            var doorFrameMat = Theme.Lit(Theme.Hex("#4A3828"), 0.2f);
            var baseMat = Theme.Lit(Theme.Hex("#1A1D24"), 0.05f);
            var glassMat = GlassMaterial();
            var baseboardMat = Theme.Lit(Theme.Hex("#3A3028"), 0.1f);
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
                float w = l.Width, d = l.Depth;

                // Floor
                var floor = Box(roomGo.transform, "Floor", new Vector3(l.Center.x, l.Y - 0.1f, l.Center.z),
                    new Vector3(w, 0.2f, d), Theme.Floor(l.Floor, w / 4f, d / 4f), true);
                rv.FloorCollider = floor.GetComponent<Collider>();
                floor.AddComponent<RoomClickTarget>().Room = rv;

                // Ceiling
                if (info.Id != "SH")
                {
                    Box(roomGo.transform, "Ceiling", new Vector3(l.Center.x, l.Y + WallHeight + 0.05f, l.Center.z),
                        new Vector3(w - 0.1f, 0.1f, d - 0.1f), ceilingMat);
                }

                // Baseboards along floor edges
                float bbH = 0.12f, bbD = 0.04f;
                Box(roomGo.transform, "BB_N", new Vector3(l.Center.x, l.Y + bbH * 0.5f, l.Z1 - bbD * 0.5f), new Vector3(w - 0.3f, bbH, bbD), baseboardMat);
                Box(roomGo.transform, "BB_S", new Vector3(l.Center.x, l.Y + bbH * 0.5f, l.Z0 + bbD * 0.5f), new Vector3(w - 0.3f, bbH, bbD), baseboardMat);
                Box(roomGo.transform, "BB_E", new Vector3(l.X1 - bbD * 0.5f, l.Y + bbH * 0.5f, l.Center.z), new Vector3(bbD, bbH, d - 0.3f), baseboardMat);
                Box(roomGo.transform, "BB_W", new Vector3(l.X0 + bbD * 0.5f, l.Y + bbH * 0.5f, l.Center.z), new Vector3(bbD, bbH, d - 0.3f), baseboardMat);

                // Support plinth for elevated rooms
                if (l.Y > 0.01f)
                {
                    Box(roomGo.transform, "Plinth",
                        new Vector3(l.Center.x, l.Y * 0.5f - 0.1f, l.Center.z),
                        new Vector3(w, l.Y - 0.2f, d), plinthMat);
                }

                // Room highlight overlay
                rv.Highlight = FlatQuad(roomGo.transform, "Highlight",
                    new Vector3(l.Center.x, l.Y + 0.02f, l.Center.z),
                    new Vector2(w - 0.4f, d - 0.4f), Theme.Ghost(Theme.WithAlpha(Theme.Gold, 0.18f)));
                rv.Highlight.SetActive(false);

                // Staircase steps (SH room)
                if (info.Id == "SH")
                {
                    int numSteps = 14;
                    float stairWidth = l.Width * 0.6f;
                    float stepDepth = l.Depth * 0.7f / numSteps;
                    float stepRise = UpperFloor / numSteps;
                    for (int s = 0; s < numSteps; s++)
                    {
                        float xPos = l.Center.x;
                        float zPos = l.Z0 + 1.5f + s * stepDepth;
                        float yPos = s * stepRise + stepRise * 0.5f;
                        Box(roomGo.transform, "Step" + s,
                            new Vector3(xPos, yPos, zPos),
                            new Vector3(stairWidth, stepRise, stepDepth * 0.95f), stepMat);
                    }
                    // Stair railing (simple box railing on each side)
                    float railH = 0.9f;
                    float railW = 0.06f;
                    float railLen = numSteps * stepDepth;
                    float railCenterZ = l.Z0 + 1.5f + railLen * 0.5f;
                    float railCenterY = UpperFloor * 0.5f + railH * 0.5f;
                    Box(roomGo.transform, "RailL",
                        new Vector3(l.Center.x - stairWidth * 0.5f - railW, railCenterY, railCenterZ),
                        new Vector3(railW, railH, railLen), capMat);
                    Box(roomGo.transform, "RailR",
                        new Vector3(l.Center.x + stairWidth * 0.5f + railW, railCenterY, railCenterZ),
                        new Vector3(railW, railH, railLen), capMat);
                }

                view.Rooms[i] = rv;
                var rb = new Bounds(new Vector3(l.Center.x, l.Y + WallHeight * 0.5f, l.Center.z),
                    new Vector3(w, WallHeight + l.Y, d));
                if (first) bounds = rb;
                else bounds.Encapsulate(rb);
                first = false;
            }

            // Build walls with door gaps, windows, and door frames
            var done = new HashSet<string>();
            foreach (var rv in view.Rooms.Values)
            {
                var l = rv.Layout;
                BuildEdge(root.transform, c, present, done, l, true, l.X0, l.Z0, l.Z1,
                    wallMat, wallInnerMat, capMat, doorMat, doorFrameMat, glassMat);
                BuildEdge(root.transform, c, present, done, l, true, l.X1, l.Z0, l.Z1,
                    wallMat, wallInnerMat, capMat, doorMat, doorFrameMat, glassMat);
                BuildEdge(root.transform, c, present, done, l, false, l.Z0, l.X0, l.X1,
                    wallMat, wallInnerMat, capMat, doorMat, doorFrameMat, glassMat);
                BuildEdge(root.transform, c, present, done, l, false, l.Z1, l.X0, l.X1,
                    wallMat, wallInnerMat, capMat, doorMat, doorFrameMat, glassMat);
            }

            // Ground plane
            var basePlate = Box(root.transform, "Base",
                new Vector3(bounds.center.x, -0.3f, bounds.center.z),
                new Vector3(bounds.size.x + 4f, 0.4f, bounds.size.z + 4f), baseMat);
            basePlate.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            // Ground catcher for click detection
            var catcher = GameObject.CreatePrimitive(PrimitiveType.Cube);
            catcher.name = "GroundCatcher";
            catcher.transform.SetParent(root.transform, false);
            catcher.transform.localPosition = new Vector3(bounds.center.x, -1f, bounds.center.z);
            catcher.transform.localScale = new Vector3(200f, 0.2f, 200f);
            UnityEngine.Object.Destroy(catcher.GetComponent<Renderer>());
            catcher.AddComponent<GroundCatcher>().View = view;

            // Crime scene markers
            var crime = view.Rooms[c.CrimeRoom].Layout;
            view.CrimeDecal = FlatQuad(root.transform, "CrimeScene",
                new Vector3(crime.Center.x, crime.Y + 0.015f, crime.Center.z),
                new Vector2(crime.Width - 1f, crime.Depth - 1f),
                Theme.Ghost(new Color(0.85f, 0.25f, 0.2f, 0.14f)));

            var lightGo = new GameObject("CrimeLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(crime.Center.x, crime.Y + WallHeight - 0.3f, crime.Center.z);
            view.CrimeLight = lightGo.AddComponent<Light>();
            view.CrimeLight.type = LightType.Point;
            view.CrimeLight.color = new Color(1f, 0.35f, 0.28f);
            view.CrimeLight.range = 12f;
            view.CrimeLight.intensity = 0f;
            view.CrimeLight.shadows = LightShadows.Soft;

            view.Bounds = bounds;
            view.BuildMarkers();
            view.SecretPassage = BuildSecretPassage(root.transform, present);
            if (view.SecretPassage != null) view.SecretPassage.SetActive(c.SecretEdgeActive);
            return view;
        }

        static Material GlassMaterial()
        {
            var m = Theme.Lit(new Color(0.6f, 0.72f, 0.82f, 0.25f), 0.85f);
            if (m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", 1f); // Transparent
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.renderQueue = 3000;
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.renderQueue = 3000;
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_ALPHABLEND_ON");
            }
            return m;
        }

        static GameObject BuildSecretPassage(Transform root, HashSet<string> present)
        {
            if (!present.Contains("BI") || !present.Contains("ZY")) return null;
            var bi = Layout("BI");
            var zy = Layout("ZY");
            var go = new GameObject("SecretPassage_BI_ZY");
            go.transform.SetParent(root, false);
            var gold = Theme.Lit(Theme.Hex("#E2C27A"), 0.65f);
            var glow = Theme.Ghost(Theme.WithAlpha(Theme.Gold, 0.45f));
            float zMid = (Mathf.Max(bi.Z0, zy.Z0) + Mathf.Min(bi.Z1, zy.Z1)) * 0.5f;
            float x = bi.X0;
            const int steps = 14;
            float stepW = 2.4f;
            for (int s = 0; s < steps; s++)
            {
                float t = (s + 0.5f) / steps;
                float y = Mathf.Lerp(bi.Y + 0.15f, zy.Y + 0.15f, t);
                float xOff = Mathf.Lerp(1.2f, -1.2f, t);
                Box(go.transform, "GoldStep" + s,
                    new Vector3(x + xOff, y, zMid),
                    new Vector3(stepW, 0.18f, 1.8f), gold);
            }
            FlatQuad(go.transform, "GoldGlow",
                new Vector3(x, (bi.Y + zy.Y) * 0.5f + 0.5f, zMid),
                new Vector2(1.2f, 2.5f), glow);
            var lightGo = new GameObject("SecretLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(x, (bi.Y + zy.Y) * 0.5f + 1.5f, zMid);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Theme.Gold;
            light.range = 8f;
            light.intensity = 1.6f;
            light.shadows = LightShadows.Soft;
            return go;
        }

        static bool IsExteriorEdge(HashSet<string> present, RoomLayout room, bool vertical, float at)
        {
            foreach (var l in Layouts)
            {
                if (l.Id == room.Id || !present.Contains(l.Id)) continue;
                bool shares = vertical
                    ? (Mathf.Approximately(l.X0, at) || Mathf.Approximately(l.X1, at))
                    : (Mathf.Approximately(l.Z0, at) || Mathf.Approximately(l.Z1, at));
                if (shares) return false;
            }
            return true;
        }

        static void BuildEdge(Transform root, CaseDefinition c, HashSet<string> present, HashSet<string> done,
            RoomLayout room, bool vertical, float at, float from, float to,
            Material wallMat, Material wallInnerMat, Material capMat, Material doorMat, Material doorFrameMat, Material glassMat)
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

            bool exterior = (other == null);
            Material useMat = exterior ? wallMat : wallInnerMat;

            // Collect door cuts
            var cuts = new List<Vector2>();
            if (other != null)
            {
                foreach (var d in Doors)
                {
                    bool match = (d.A == room.Id && d.B == other) || (d.B == room.Id && d.A == other);
                    if (match && d.Vertical == vertical && Mathf.Approximately(d.At, at))
                        cuts.Add(new Vector2(d.From, d.To));
                }
            }
            cuts.Sort((a, b) => a.x.CompareTo(b.x));

            // Window cuts for exterior walls (not basement, not staircase)
            var windowCuts = new List<Vector2>();
            if (exterior && room.Id != "BO" && room.Id != "SH")
            {
                float edgeLen = to - from;
                if (edgeLen > 6f)
                {
                    float winW = 2.0f;
                    float center1 = from + edgeLen * 0.3f;
                    float center2 = from + edgeLen * 0.7f;
                    windowCuts.Add(new Vector2(center1 - winW * 0.5f, center1 + winW * 0.5f));
                    if (edgeLen > 10f)
                        windowCuts.Add(new Vector2(center2 - winW * 0.5f, center2 + winW * 0.5f));
                }
                else if (edgeLen > 3f)
                {
                    float winW = 1.6f;
                    float center = (from + to) * 0.5f;
                    windowCuts.Add(new Vector2(center - winW * 0.5f, center + winW * 0.5f));
                }
            }

            // Build solid wall segments (with door gaps)
            float cur = from;
            var segs = new List<Vector2>();
            foreach (var cut in cuts)
            {
                if (cut.x > cur + 0.01f) segs.Add(new Vector2(cur, cut.x));
                cur = cut.y;
            }
            if (cur < to - 0.01f) segs.Add(new Vector2(cur, to));

            foreach (var s in segs)
            {
                float len = s.y - s.x;
                float mid = (s.x + s.y) * 0.5f;

                // Check if this segment contains any windows
                var segWindows = new List<Vector2>();
                foreach (var w in windowCuts)
                {
                    if (w.x >= s.x - 0.01f && w.y <= s.y + 0.01f) segWindows.Add(w);
                }

                if (segWindows.Count == 0)
                {
                    // Solid wall segment, full height
                    Vector3 center = vertical
                        ? new Vector3(at, baseY + WallHeight * 0.5f, mid)
                        : new Vector3(mid, baseY + WallHeight * 0.5f, at);
                    Vector3 size = vertical
                        ? new Vector3(WallThickness, WallHeight, len + WallThickness)
                        : new Vector3(len + WallThickness, WallHeight, WallThickness);
                    Box(root, "Wall", center, size, useMat, true);
                }
                else
                {
                    // Wall with window openings: build parts around windows
                    BuildWallWithWindows(root, vertical, at, baseY, s, segWindows, useMat, glassMat);
                }

                // Wall cap / crown molding
                Vector3 capCenter = vertical
                    ? new Vector3(at, baseY + WallHeight + 0.03f, mid)
                    : new Vector3(mid, baseY + WallHeight + 0.03f, at);
                Vector3 capSize = vertical
                    ? new Vector3(WallThickness + 0.08f, 0.06f, len + WallThickness + 0.08f)
                    : new Vector3(len + WallThickness + 0.08f, 0.06f, WallThickness + 0.08f);
                Box(root, "WallCap", capCenter, capSize, capMat);
            }

            // Door frames and thresholds
            foreach (var cut in cuts)
            {
                float mid = (cut.x + cut.y) * 0.5f;
                float doorW = cut.y - cut.x;

                // Threshold
                Vector3 thresholdCenter = vertical
                    ? new Vector3(at, baseY + 0.02f, mid)
                    : new Vector3(mid, baseY + 0.02f, at);
                Vector3 thresholdSize = vertical
                    ? new Vector3(WallThickness + 0.3f, 0.04f, doorW)
                    : new Vector3(doorW, 0.04f, WallThickness + 0.3f);
                Box(root, "Threshold", thresholdCenter, thresholdSize, doorMat);

                // Door frame sides
                float frameW = 0.1f;
                float frameD = WallThickness + 0.06f;
                if (vertical)
                {
                    Box(root, "DoorFrameL", new Vector3(at, baseY + DoorHeight * 0.5f, cut.x + 0.02f), new Vector3(frameD, DoorHeight, frameW), doorFrameMat);
                    Box(root, "DoorFrameR", new Vector3(at, baseY + DoorHeight * 0.5f, cut.y - 0.02f), new Vector3(frameD, DoorHeight, frameW), doorFrameMat);
                }
                else
                {
                    Box(root, "DoorFrameL", new Vector3(cut.x + 0.02f, baseY + DoorHeight * 0.5f, at), new Vector3(frameW, DoorHeight, frameD), doorFrameMat);
                    Box(root, "DoorFrameR", new Vector3(cut.y - 0.02f, baseY + DoorHeight * 0.5f, at), new Vector3(frameW, DoorHeight, frameD), doorFrameMat);
                }

                // Door frame lintel (top bar)
                Vector3 lintelCenter = vertical
                    ? new Vector3(at, baseY + DoorHeight + 0.05f, mid)
                    : new Vector3(mid, baseY + DoorHeight + 0.05f, at);
                Vector3 lintelSize = vertical
                    ? new Vector3(frameD, 0.1f, doorW + 0.1f)
                    : new Vector3(doorW + 0.1f, 0.1f, frameD);
                Box(root, "DoorLintel", lintelCenter, lintelSize, doorFrameMat);

                // Wall above door (from door top to wall top)
                float aboveDoorH = WallHeight - DoorHeight - 0.1f;
                if (aboveDoorH > 0.1f)
                {
                    Vector3 aboveCenter = vertical
                        ? new Vector3(at, baseY + DoorHeight + 0.1f + aboveDoorH * 0.5f, mid)
                        : new Vector3(mid, baseY + DoorHeight + 0.1f + aboveDoorH * 0.5f, at);
                    Vector3 aboveSize = vertical
                        ? new Vector3(WallThickness, aboveDoorH, doorW + WallThickness)
                        : new Vector3(doorW + WallThickness, aboveDoorH, WallThickness);
                    Box(root, "WallAboveDoor", aboveCenter, aboveSize, useMat);
                }
            }
        }

        static void BuildWallWithWindows(Transform root, bool vertical, float at, float baseY,
            Vector2 seg, List<Vector2> windows, Material wallMat, Material glassMat)
        {
            // Sort windows by position
            windows.Sort((a, b) => a.x.CompareTo(b.x));

            // Build wall below windows (from floor to windowBottom)
            float belowH = WindowBottom;
            if (belowH > 0.05f)
            {
                float mid = (seg.x + seg.y) * 0.5f;
                float len = seg.y - seg.x;
                Vector3 center = vertical
                    ? new Vector3(at, baseY + belowH * 0.5f, mid)
                    : new Vector3(mid, baseY + belowH * 0.5f, at);
                Vector3 size = vertical
                    ? new Vector3(WallThickness, belowH, len + WallThickness)
                    : new Vector3(len + WallThickness, belowH, WallThickness);
                Box(root, "WallBelow", center, size, wallMat, true);
            }

            // Build wall above windows (from windowTop to ceiling)
            float aboveH = WallHeight - WindowTop;
            if (aboveH > 0.05f)
            {
                float mid = (seg.x + seg.y) * 0.5f;
                float len = seg.y - seg.x;
                Vector3 center = vertical
                    ? new Vector3(at, baseY + WindowTop + aboveH * 0.5f, mid)
                    : new Vector3(mid, baseY + WindowTop + aboveH * 0.5f, at);
                Vector3 size = vertical
                    ? new Vector3(WallThickness, aboveH, len + WallThickness)
                    : new Vector3(len + WallThickness, aboveH, WallThickness);
                Box(root, "WallAbove", center, size, wallMat, true);
            }

            // Build pillars between windows and wall segments at window height
            float cur = seg.x;
            float windowH = WindowTop - WindowBottom;
            foreach (var w in windows)
            {
                // Pillar before window
                if (w.x > cur + 0.1f)
                {
                    float pillarLen = w.x - cur;
                    float pillarMid = (cur + w.x) * 0.5f;
                    Vector3 center = vertical
                        ? new Vector3(at, baseY + WindowBottom + windowH * 0.5f, pillarMid)
                        : new Vector3(pillarMid, baseY + WindowBottom + windowH * 0.5f, at);
                    Vector3 size = vertical
                        ? new Vector3(WallThickness, windowH, pillarLen)
                        : new Vector3(pillarLen, windowH, WallThickness);
                    Box(root, "WallPillar", center, size, wallMat, true);
                }

                // Window glass
                float winMid = (w.x + w.y) * 0.5f;
                float winW = w.y - w.x;
                Vector3 glassCenter = vertical
                    ? new Vector3(at, baseY + WindowBottom + windowH * 0.5f, winMid)
                    : new Vector3(winMid, baseY + WindowBottom + windowH * 0.5f, at);
                Vector3 glassSize = vertical
                    ? new Vector3(WallThickness * 0.3f, windowH - 0.1f, winW - 0.1f)
                    : new Vector3(winW - 0.1f, windowH - 0.1f, WallThickness * 0.3f);
                Box(root, "Glass", glassCenter, glassSize, glassMat);

                // Window frame (sill + top + sides)
                var frameMat = Theme.Lit(Theme.Hex("#3A3028"), 0.15f);
                float frameT = 0.06f;
                // Sill
                Vector3 sillCenter = vertical
                    ? new Vector3(at, baseY + WindowBottom - frameT * 0.5f, winMid)
                    : new Vector3(winMid, baseY + WindowBottom - frameT * 0.5f, at);
                Vector3 sillSize = vertical
                    ? new Vector3(WallThickness + 0.15f, frameT, winW + 0.1f)
                    : new Vector3(winW + 0.1f, frameT, WallThickness + 0.15f);
                Box(root, "WindowSill", sillCenter, sillSize, frameMat);
                // Top
                Vector3 topCenter = vertical
                    ? new Vector3(at, baseY + WindowTop + frameT * 0.5f, winMid)
                    : new Vector3(winMid, baseY + WindowTop + frameT * 0.5f, at);
                Box(root, "WindowTop", topCenter, sillSize, frameMat);

                cur = w.y;
            }

            // Pillar after last window
            if (cur < seg.y - 0.1f)
            {
                float pillarLen = seg.y - cur;
                float pillarMid = (cur + seg.y) * 0.5f;
                Vector3 center = vertical
                    ? new Vector3(at, baseY + WindowBottom + windowH * 0.5f, pillarMid)
                    : new Vector3(pillarMid, baseY + WindowBottom + windowH * 0.5f, at);
                Vector3 size = vertical
                    ? new Vector3(WallThickness, windowH, pillarLen)
                    : new Vector3(pillarLen, windowH, WallThickness);
                Box(root, "WallPillar", center, size, wallMat, true);
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
        public GameObject SecretPassage;
        public Action<int> RoomClicked;
        public Action<int> SuspectClicked;
        public Action BackgroundClicked;
        public CameraRig Rig;
        float crimeTarget;

        public void SetSecretPassageVisible(bool on)
        {
            if (SecretPassage != null) SecretPassage.SetActive(on);
        }

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
            float pad = 1.5f;
            float x0 = l.X0 + pad, x1 = l.X1 - pad, z0 = l.Z0 + pad, z1 = l.Z1 - pad;
            if (l.Id == "SH")
            {
                z0 = l.Z0 + l.Depth * 0.75f;
                z1 = l.Z1 - 1f;
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
            crimeTarget = strong ? 3.5f : 0.5f;
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

        public Vector3 LabelPoint { get { return new Vector3(Layout.Center.x, Layout.Y + 0.1f, Layout.Z0 + 0.5f); } }
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
