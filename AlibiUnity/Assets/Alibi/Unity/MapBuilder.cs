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
        public const float WallHeight = 4.2f;
        public const float WallThickness = 0.2f;
        public const float UpperFloor = 3.4f;
        public const float DoorHeight = 2.4f;
        public const float WindowBottom = 1.0f;
        public const float WindowTop = 2.2f;
        public const float Scale = 4f;

        // Shkallët (SH) janë kullë dy-katëshe; Bodrumi (BO) mbaron te niveli i tokës (y = 0).
        public static float RoomTop(RoomLayout l)
        {
            if (l.Id == "SH") return UpperFloor + WallHeight;
            if (l.Y < -0.01f) return 0f;
            return l.Y + WallHeight;
        }

        // Lartësia e tavanit mbi dyshemenë e dhomës.
        public static float CeilingHeight(RoomLayout l)
        {
            return RoomTop(l) - l.Y;
        }

        // Shkallët kryesore në SH: nga përdhesa (z0 + StairStart) deri te kati i parë (z0 + StairEnd).
        public const int StairSteps = 14;
        public const float StairStart = 1.2f;
        public static float StairEnd(RoomLayout sh)
        {
            float stepDepth = sh.Depth * 0.7f / StairSteps;
            return 1.5f + (StairSteps - 1) * stepDepth + stepDepth * 0.5f;
        }

        // Shkallët e bodrumit: zbresin nga dera SH–BO (z = Z1 e bodrumit) drejt jugut.
        public const float CellarStairX0 = 4.5f, CellarStairX1 = 7.5f, CellarStairRun = 7.2f;

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

        // --- Cylinder helper for trees, lamp posts etc. ---
        static GameObject Cylinder(Transform parent, string name, Vector3 center, float radius, float height, Material mat, bool addCollider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!addCollider) UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Sphere(Transform parent, string name, Vector3 center, float radius, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = Vector3.one * radius * 2f;
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
            var wallMat = Theme.Lit(Theme.Hex("#7A6E60"), 0.10f);
            var wallInnerMat = Theme.Lit(Theme.Hex("#B8A892"), 0.08f);
            var ceilingMat = Theme.Lit(Theme.Hex("#A89880"), 0.06f);
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
                floor.AddComponent<Walkable>();

                // Ceiling. RoomTop jep nivelin e sakte: kthina e shkalleve dy kate e larte,
                // bodrumi me çati ne nivelin e tokes (me hapje per shkallet), te tjerat nje kat.
                if (info.Id == "BO")
                {
                    BuildCellarStairs(roomGo.transform, rv, stepMat, capMat, baseMat);
                }
                else
                {
                    Box(roomGo.transform, "Ceiling", new Vector3(l.Center.x, RoomTop(l) + 0.05f, l.Center.z),
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
                    // me collider: lojtari nuk hyn më brenda bazamentit të katit të parë nga përdhesa
                    Box(roomGo.transform, "Plinth",
                        new Vector3(l.Center.x, l.Y * 0.5f - 0.1f, l.Center.z),
                        new Vector3(w, l.Y - 0.2f, d), plinthMat, true);
                }

                // Room highlight overlay
                rv.Highlight = FlatQuad(roomGo.transform, "Highlight",
                    new Vector3(l.Center.x, l.Y + 0.02f, l.Center.z),
                    new Vector2(w - 0.4f, d - 0.4f), Theme.Ghost(Theme.WithAlpha(Theme.Gold, 0.18f)));
                rv.Highlight.SetActive(false);

                // Staircase steps (SH room)
                if (info.Id == "SH")
                {
                    // Sheshpushimi dhe hapat ndertohen me poshte ne kete bllok. Nuk thirret
                    // BuildStairLanding: do te mbivendosej mbi to dhe do te krijonte z-fighting.
                    int numSteps = StairSteps;
                    float stairWidth = l.Width * 0.6f;
                    float stepDepth = l.Depth * 0.7f / numSteps;
                    float stepRise = UpperFloor / numSteps;
                    for (int s = 0; s < numSteps; s++)
                    {
                        float xPos = l.Center.x;
                        float zPos = l.Z0 + 1.5f + s * stepDepth;
                        float yPos = s * stepRise + stepRise * 0.5f;
                        // Thellesia pak me e madhe se hapi: hapat mbulohen me njeri-tjetrin dhe
                        // nuk mbetet e çare ku lojtari bie nje kat poshte per nje frame.
                        Box(roomGo.transform, "Step" + s,
                            new Vector3(xPos, yPos, zPos),
                            new Vector3(stairWidth, stepRise, stepDepth * 1.06f), stepMat, true);
                    }

                    // Sheshpushimi lart: lidh majen e shkalleve me deren e Korridorit ne murin perendimor.
                    // Pa te, maja e shkalleve del mbi boshllek dhe kati i siperm nuk arrihet.
                    // Fillon pak para skajit te hapit te fundit, qe te mos mbetet vrime ne mes.
                    float topZ = l.Z0 + 1.5f + (numSteps - 1) * stepDepth + stepDepth * 0.3f;
                    if (l.Z1 - topZ > 0.2f)
                    {
                        Box(roomGo.transform, "LandingN",
                            new Vector3(l.Center.x, UpperFloor - 0.1f, (topZ + l.Z1) * 0.5f),
                            new Vector3(l.Width, 0.2f, l.Z1 - topZ), stepMat, true);
                    }
                    float westW = l.Center.x - stairWidth * 0.5f - l.X0;
                    if (westW > 0.2f)
                    {
                        Box(roomGo.transform, "LandingW",
                            new Vector3(l.X0 + westW * 0.5f, UpperFloor - 0.1f, l.Center.z),
                            new Vector3(westW, 0.2f, l.Depth), stepMat, true);
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

            // Front door panel with proximity open/close
            BuildFrontDoor(root.transform, doorMat, doorFrameMat);

            // Ground plane — small plate under villa kept for floor catcher.
            // Nuk mbulon bodrumin: përndryshe pllaka do ta priste dhomën dhe shkallët poshtë.
            bool hasCellar = present.Contains("BO");
            float bx0 = float.MaxValue, bx1 = float.MinValue, bz0 = float.MaxValue, bz1 = float.MinValue;
            foreach (var rv in view.Rooms.Values)
            {
                var l = rv.Layout;
                if (l.Y < -0.01f) continue;
                bx0 = Mathf.Min(bx0, l.X0); bx1 = Mathf.Max(bx1, l.X1);
                bz0 = Mathf.Min(bz0, l.Z0); bz1 = Mathf.Max(bz1, l.Z1);
            }
            float plateZ0 = hasCellar ? bz0 : bz0 - 2f;
            float plateZ1 = bz1 + 2f;
            var basePlate = Box(root.transform, "Base",
                new Vector3((bx0 + bx1) * 0.5f, -0.3f, (plateZ0 + plateZ1) * 0.5f),
                new Vector3(bx1 - bx0 + 4f, 0.4f, plateZ1 - plateZ0), baseMat);
            basePlate.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            // Ground catcher for click detection (nën dyshemenë e bodrumit, që të mos e presë dhomën)
            var catcher = GameObject.CreatePrimitive(PrimitiveType.Cube);
            catcher.name = "GroundCatcher";
            catcher.transform.SetParent(root.transform, false);
            catcher.transform.localPosition = new Vector3(bounds.center.x, -4.2f, bounds.center.z);
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

            // =========== EXTERIOR ENVIRONMENT ===========
            BuildExterior(root.transform, bounds, hasCellar, hasCellar ? Layout("BO") : default(RoomLayout));

            view.Bounds = bounds;
            view.BuildMarkers();
            view.SecretPassage = BuildSecretPassage(root.transform, view, present);
            view.SecretHint = c.SecretEdgeExists ? BuildSecretHint(root.transform, present) : null;
            view.SetSecretPassageVisible(c.SecretEdgeActive);
            return view;
        }

        // ================================================================
        //  BuildExterior — ground, sky enclosure, hedges, trees, moonlight
        // ================================================================
        static void BuildExterior(Transform root, Bounds villaBounds, bool cellarHole, RoomLayout cellar)
        {
            // Materials
            var grassMat   = Theme.Lit(new Color(0.14f, 0.22f, 0.10f), 0.06f);
            var skyMat     = Theme.Lit(new Color(0.04f, 0.05f, 0.10f), 0.02f);
            var hedgeMat   = Theme.Lit(new Color(0.08f, 0.18f, 0.06f), 0.05f);
            var trunkMat   = Theme.Lit(new Color(0.28f, 0.18f, 0.10f), 0.08f);
            var canopyMat  = Theme.Lit(new Color(0.10f, 0.24f, 0.08f), 0.05f);
            var gravelMat  = Theme.Lit(new Color(0.22f, 0.20f, 0.17f), 0.06f);
            var fenceMat   = Theme.Lit(new Color(0.10f, 0.08f, 0.06f), 0.08f);
            var stoneMat   = Theme.Lit(new Color(0.25f, 0.23f, 0.20f), 0.1f);

            float cx = villaBounds.center.x;
            float cz = villaBounds.center.z;

            // --- Large ground plane (dark grass) ---
            float groundSize = 120f;
            if (!cellarHole)
            {
                var ground = Box(root, "ExtGround",
                    new Vector3(cx, -0.52f, cz),
                    new Vector3(groundSize, 0.04f, groundSize), grassMat);
                ground.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            else
            {
                // Bari me një vrimë mbi bodrum: katër pjesë rreth tij, që shkallët dhe dhoma të mos priten.
                float gx0 = cx - groundSize * 0.5f, gx1 = cx + groundSize * 0.5f;
                float gz0 = cz - groundSize * 0.5f, gz1 = cz + groundSize * 0.5f;
                float cx0 = cellar.X0, cx1 = cellar.X1, cz0 = cellar.Z0, cz1 = cellar.Z1;
                GroundPiece(root, gx0, gx1, gz0, cz0, grassMat);
                GroundPiece(root, gx0, gx1, cz1, gz1, grassMat);
                GroundPiece(root, gx0, cx0, cz0, cz1, grassMat);
                GroundPiece(root, cx1, gx1, cz0, cz1, grassMat);
            }

            // --- Invisible wall colliders to keep player from walking off ---
            float wallDist = 55f;
            float wallH = 35f;
            var invisMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            invisMat.color = Color.clear;
            invisMat.SetFloat("_Surface", 1f);
            invisMat.renderQueue = 4000;

            Box(root, "BoundN", new Vector3(cx, wallH * 0.5f, cz + wallDist), new Vector3(groundSize, wallH, 0.5f), invisMat, true)
                .GetComponent<Renderer>().enabled = false;
            Box(root, "BoundS", new Vector3(cx, wallH * 0.5f, cz - wallDist), new Vector3(groundSize, wallH, 0.5f), invisMat, true)
                .GetComponent<Renderer>().enabled = false;
            Box(root, "BoundE", new Vector3(cx + wallDist, wallH * 0.5f, cz), new Vector3(0.5f, wallH, groundSize), invisMat, true)
                .GetComponent<Renderer>().enabled = false;
            Box(root, "BoundW", new Vector3(cx - wallDist, wallH * 0.5f, cz), new Vector3(0.5f, wallH, groundSize), invisMat, true)
                .GetComponent<Renderer>().enabled = false;

            // --- Sky enclosure (dark blue walls + ceiling so no black void) ---
            float skyDist = 80f;
            float skyH = 32f;

            // North sky wall
            Box(root, "SkyN", new Vector3(cx, skyH * 0.5f, cz + skyDist),
                new Vector3(skyDist * 2.2f, skyH, 0.3f), skyMat);
            // South sky wall
            Box(root, "SkyS", new Vector3(cx, skyH * 0.5f, cz - skyDist),
                new Vector3(skyDist * 2.2f, skyH, 0.3f), skyMat);
            // East sky wall
            Box(root, "SkyE", new Vector3(cx + skyDist, skyH * 0.5f, cz),
                new Vector3(0.3f, skyH, skyDist * 2.2f), skyMat);
            // West sky wall
            Box(root, "SkyW", new Vector3(cx - skyDist, skyH * 0.5f, cz),
                new Vector3(0.3f, skyH, skyDist * 2.2f), skyMat);
            // Sky ceiling
            Box(root, "SkyCeiling", new Vector3(cx, skyH, cz),
                new Vector3(skyDist * 2.2f, 0.3f, skyDist * 2.2f), skyMat);

            // --- Hedge ring around the villa perimeter ---
            float hedgeMargin = 8f;
            float hedgeH = 1.6f;
            float hedgeW = 0.8f;
            float hx0 = villaBounds.min.x - hedgeMargin;
            float hx1 = villaBounds.max.x + hedgeMargin;
            float hz0 = villaBounds.min.z - hedgeMargin;
            float hz1 = villaBounds.max.z + hedgeMargin;
            float hedgeLenX = hx1 - hx0;
            float hedgeLenZ = hz1 - hz0;
            float hedgeY = hedgeH * 0.5f - 0.5f;

            // North hedge
            Box(root, "HedgeN", new Vector3(cx, hedgeY, hz1), new Vector3(hedgeLenX + hedgeW, hedgeH, hedgeW), hedgeMat);
            // South hedge (gap for front gate)
            float gateHalf = 2.5f;
            float gateCx = Layout("HO").Center.x;
            Box(root, "HedgeSW", new Vector3((hx0 + gateCx - gateHalf) * 0.5f, hedgeY, hz0),
                new Vector3(gateCx - gateHalf - hx0, hedgeH, hedgeW), hedgeMat);
            Box(root, "HedgeSE", new Vector3((gateCx + gateHalf + hx1) * 0.5f, hedgeY, hz0),
                new Vector3(hx1 - gateCx - gateHalf, hedgeH, hedgeW), hedgeMat);
            // Gate pillars
            Box(root, "GatePillarL", new Vector3(gateCx - gateHalf, hedgeH * 0.5f, hz0),
                new Vector3(0.5f, hedgeH + 0.6f, 0.5f), stoneMat);
            Box(root, "GatePillarR", new Vector3(gateCx + gateHalf, hedgeH * 0.5f, hz0),
                new Vector3(0.5f, hedgeH + 0.6f, 0.5f), stoneMat);
            // East hedge
            Box(root, "HedgeE", new Vector3(hx1, hedgeY, cz), new Vector3(hedgeW, hedgeH, hedgeLenZ), hedgeMat);
            // West hedge
            Box(root, "HedgeW", new Vector3(hx0, hedgeY, cz), new Vector3(hedgeW, hedgeH, hedgeLenZ), hedgeMat);

            // --- Wrought iron fence on top of hedge (thin dark bars) ---
            float fenceH = 0.9f;
            float fenceT = 0.04f;
            float fenceTopY = hedgeH + fenceH * 0.5f - 0.5f;
            Box(root, "FenceN", new Vector3(cx, fenceTopY, hz1), new Vector3(hedgeLenX, fenceH, fenceT), fenceMat);
            Box(root, "FenceE", new Vector3(hx1, fenceTopY, cz), new Vector3(fenceT, fenceH, hedgeLenZ), fenceMat);
            Box(root, "FenceW", new Vector3(hx0, fenceTopY, cz), new Vector3(fenceT, fenceH, hedgeLenZ), fenceMat);
            Box(root, "FenceSW", new Vector3((hx0 + gateCx - gateHalf) * 0.5f, fenceTopY, hz0),
                new Vector3(gateCx - gateHalf - hx0, fenceH, fenceT), fenceMat);
            Box(root, "FenceSE", new Vector3((gateCx + gateHalf + hx1) * 0.5f, fenceTopY, hz0),
                new Vector3(hx1 - gateCx - gateHalf, fenceH, fenceT), fenceMat);

            // --- Gravel path from gate to front door (HO room south edge) ---
            var hall = Layout("HO");
            float pathZ0 = hz0 - 0.3f;
            float pathZ1 = hall.Z0;
            float pathLen = pathZ1 - pathZ0;
            float pathW = 3.0f;
            Box(root, "GravelPath",
                new Vector3(hall.Center.x, -0.48f, pathZ0 + pathLen * 0.5f),
                new Vector3(pathW, 0.06f, pathLen), gravelMat);
            // Path edge stones
            Box(root, "PathEdgeL", new Vector3(hall.Center.x - pathW * 0.5f - 0.15f, -0.44f, pathZ0 + pathLen * 0.5f),
                new Vector3(0.2f, 0.12f, pathLen), stoneMat);
            Box(root, "PathEdgeR", new Vector3(hall.Center.x + pathW * 0.5f + 0.15f, -0.44f, pathZ0 + pathLen * 0.5f),
                new Vector3(0.2f, 0.12f, pathLen), stoneMat);

            // --- Trees around the property ---
            float treeBaseY = -0.5f;
            Vector3[] treePositions =
            {
                new Vector3(hx0 + 4f, treeBaseY, hz1 - 4f),     // NW corner
                new Vector3(hx1 - 4f, treeBaseY, hz1 - 4f),     // NE corner
                new Vector3(hx1 - 3f, treeBaseY, hz0 + 5f),     // SE
                new Vector3(hx0 + 3f, treeBaseY, hz0 + 5f),     // SW
                new Vector3(hx0 + 5f, treeBaseY, cz),           // W mid
                new Vector3(hx1 - 5f, treeBaseY, cz + 4f),      // E mid
                new Vector3(cx - 12f, treeBaseY, hz1 - 6f),      // N left
                new Vector3(cx + 14f, treeBaseY, hz1 - 5f),      // N right
            };

            for (int i = 0; i < treePositions.Length; i++)
            {
                var tp = treePositions[i];
                // Vary tree sizes
                float trunkH = 3.5f + (i % 3) * 0.8f;
                float trunkR = 0.18f + (i % 2) * 0.06f;
                float canopyR = 2.0f + (i % 3) * 0.5f;

                Cylinder(root, "Trunk" + i, new Vector3(tp.x, tp.y + trunkH * 0.5f, tp.z),
                    trunkR, trunkH, trunkMat);
                Sphere(root, "Canopy" + i, new Vector3(tp.x, tp.y + trunkH + canopyR * 0.6f, tp.z),
                    canopyR, canopyMat);
                // Additional canopy sphere for fuller look
                Sphere(root, "Canopy" + i + "b",
                    new Vector3(tp.x + canopyR * 0.3f, tp.y + trunkH + canopyR * 0.3f, tp.z - canopyR * 0.2f),
                    canopyR * 0.75f, canopyMat);
            }

            // --- Bushes scattered near the villa ---
            Vector3[] bushPositions =
            {
                new Vector3(hall.X0 - 2f, -0.3f, hall.Z0 + 1f),
                new Vector3(hall.X1 + 2f, -0.3f, hall.Z0 + 1f),
                new Vector3(hx1 - 2f, -0.3f, cz + 8f),
                new Vector3(hx0 + 2f, -0.3f, cz - 3f),
            };
            for (int i = 0; i < bushPositions.Length; i++)
            {
                var bp = bushPositions[i];
                float bushR = 0.7f + (i % 2) * 0.3f;
                Sphere(root, "Bush" + i, bp, bushR, hedgeMat);
            }

            // --- Moonlight (directional light for the whole scene) ---
            var moonGo = new GameObject("Moonlight");
            moonGo.transform.SetParent(root, false);
            moonGo.transform.localPosition = new Vector3(cx, 20f, cz);
            moonGo.transform.localRotation = Quaternion.Euler(45f, -30f, 0f);
            var moon = moonGo.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.65f, 0.70f, 0.85f);
            moon.intensity = 0.75f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 0.5f;

            // --- Ambient fill light (soft warm from below/inside) ---
            var ambGo = new GameObject("AmbientFill");
            ambGo.transform.SetParent(root, false);
            ambGo.transform.localPosition = new Vector3(cx, 15f, cz);
            ambGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var amb = ambGo.AddComponent<Light>();
            amb.type = LightType.Directional;
            amb.color = new Color(0.35f, 0.30f, 0.24f);
            amb.intensity = 0.40f;
            amb.shadows = LightShadows.None;

            // --- Porch lamp at front door ---
            var porchLampGo = new GameObject("PorchLamp");
            porchLampGo.transform.SetParent(root, false);
            porchLampGo.transform.localPosition = new Vector3(hall.Center.x, WallHeight - 0.4f, hall.Z0 - 0.3f);
            var porchLight = porchLampGo.AddComponent<Light>();
            porchLight.type = LightType.Point;
            porchLight.color = new Color(1f, 0.85f, 0.55f);
            porchLight.range = 10f;
            porchLight.intensity = 1.5f;
            porchLight.shadows = LightShadows.Soft;
            // Lamp fixture (small box + sphere)
            Box(root, "PorchFixture", new Vector3(hall.Center.x, WallHeight - 0.2f, hall.Z0 - 0.15f),
                new Vector3(0.3f, 0.4f, 0.2f), Theme.Lit(Theme.Hex("#2A2420"), 0.1f));
            Sphere(root, "PorchBulb", new Vector3(hall.Center.x, WallHeight - 0.5f, hall.Z0 - 0.15f),
                0.08f, Theme.Lit(new Color(1f, 0.9f, 0.6f), 0.9f));

            // --- Garden lamp posts along the path ---
            for (int i = 0; i < 2; i++)
            {
                float side = (i == 0) ? -1f : 1f;
                float lampX = hall.Center.x + side * (pathW * 0.5f + 1.2f);
                float lampZ = (pathZ0 + pathZ1) * 0.5f;
                Cylinder(root, "LampPost" + i, new Vector3(lampX, 0.5f, lampZ), 0.06f, 2.2f, fenceMat);
                Sphere(root, "LampGlobe" + i, new Vector3(lampX, 1.7f, lampZ), 0.15f,
                    Theme.Lit(new Color(1f, 0.9f, 0.6f, 0.8f), 0.8f));
                var lpGo = new GameObject("PathLight" + i);
                lpGo.transform.SetParent(root, false);
                lpGo.transform.localPosition = new Vector3(lampX, 1.7f, lampZ);
                var lp = lpGo.AddComponent<Light>();
                lp.type = LightType.Point;
                lp.color = new Color(1f, 0.85f, 0.5f);
                lp.range = 7f;
                lp.intensity = 0.8f;
                lp.shadows = LightShadows.Soft;
            }
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

        // ================================================================
        //  Kalimi i fshehtë Biblioteka–Zyra (Karta 4)
        //  Biblioteka është në përdhesë, Zyra në katin e parë, mbi muret e njëra-tjetrës.
        //  Pas «Kërko» në Bibliotekë, rafti hapet dhe dy dyer të arta lidhin dhomat:
        //  kur lojtari hyn në njërën, del te tjetra (si shkallë të ngushta brenda murit).
        // ================================================================
        public const float SecretDoorZ_BI = 5.2f;   // nga Z0 e Bibliotekës
        public const float SecretDoorZ_ZY = 4.0f;   // nga Z0 e Zyrës
        public const float SecretDoorWidth = 2.0f;

        static GameObject BuildSecretPassage(Transform root, VillaView view, HashSet<string> present)
        {
            if (!present.Contains("BI") || !present.Contains("ZY")) return null;
            var bi = Layout("BI");
            var zy = Layout("ZY");
            var go = new GameObject("SecretPassage_BI_ZY");
            go.transform.SetParent(root, false);
            var gold = Theme.Lit(Theme.Hex("#E2C27A"), 0.65f);
            var dark = Theme.Lit(Theme.Hex("#0B0906"), 0.02f);
            var shelfMat = Theme.Lit(Theme.Hex("#4A3320"), 0.15f);

            // --- Biblioteka: rafti i hapur + hyrja e artë në murin perëndimor ---
            float biFace = bi.X0 + WallThickness * 0.5f;
            float biZ = bi.Z0 + SecretDoorZ_BI;
            SecretDoorFrame(go.transform, "BI", biFace, +1f, bi.Y, biZ, gold, dark);
            // rafti i rrotulluar 90°: qëndron pingul me murin, në anën jugore të derës
            float hingeZ = biZ - SecretDoorWidth * 0.5f;
            var shelf = Box(go.transform, "SwungShelf",
                new Vector3(biFace + 0.95f, bi.Y + 1.15f, hingeZ - 0.16f),
                new Vector3(1.9f, 2.3f, 0.3f), shelfMat, true);
            ShelfBooks(go.transform, new Vector3(biFace + 0.95f, bi.Y, hingeZ - 0.33f), true);

            var toZy = new GameObject("Door_BI_to_ZY");
            toZy.transform.SetParent(go.transform, false);
            toZy.transform.localPosition = new Vector3(biFace + 0.45f, bi.Y, biZ);
            var a = toZy.AddComponent<SecretDoorway>();
            a.View = view;
            a.ToUpper = true;
            a.TargetFeet = new Vector3(zy.X1 - 1.8f, zy.Y, zy.Z0 + SecretDoorZ_ZY);
            a.TargetYaw = 270f;

            // --- Zyra: dera e artë në murin lindor ---
            float zyFace = zy.X1 - WallThickness * 0.5f;
            float zyZ = zy.Z0 + SecretDoorZ_ZY;
            SecretDoorFrame(go.transform, "ZY", zyFace, -1f, zy.Y, zyZ, gold, dark);
            var toBi = new GameObject("Door_ZY_to_BI");
            toBi.transform.SetParent(go.transform, false);
            toBi.transform.localPosition = new Vector3(zyFace - 0.45f, zy.Y, zyZ);
            var b = toBi.AddComponent<SecretDoorway>();
            b.View = view;
            b.ToUpper = false;
            b.TargetFeet = new Vector3(bi.X0 + 1.8f, bi.Y, biZ);
            b.TargetYaw = 90f;
            return go;
        }

        // Para zbulimit: rafti mbyllur mbi derë, pak i shtyrë nga muri, me një fije drite ari.
        static GameObject BuildSecretHint(Transform root, HashSet<string> present)
        {
            if (!present.Contains("BI") || !present.Contains("ZY")) return null;
            var bi = Layout("BI");
            var go = new GameObject("SecretHint_BI");
            go.transform.SetParent(root, false);
            var shelfMat = Theme.Lit(Theme.Hex("#4A3320"), 0.15f);
            float face = bi.X0 + WallThickness * 0.5f;
            float z = bi.Z0 + SecretDoorZ_BI;
            var pivot = new GameObject("ShelfPivot");
            pivot.transform.SetParent(go.transform, false);
            pivot.transform.localPosition = new Vector3(face, bi.Y, z - SecretDoorWidth * 0.5f);
            pivot.transform.localRotation = Quaternion.Euler(0f, -6f, 0f); // pak i hapur: «duket i lëvizur»
            var panel = Box(pivot.transform, "LooseShelf", new Vector3(0.16f, 1.15f, SecretDoorWidth * 0.5f),
                new Vector3(0.3f, 2.3f, SecretDoorWidth - 0.05f), shelfMat, true);
            ShelfBooks(pivot.transform, new Vector3(0.33f, 0f, SecretDoorWidth * 0.5f), false);
            // fija e dritës në skajin verior
            var glow = Theme.Ghost(Theme.WithAlpha(Theme.Gold, 0.55f));
            WallQuad(go.transform, "GoldSliver", new Vector3(face + 0.02f, bi.Y + 1.1f, z + SecretDoorWidth * 0.5f - 0.04f),
                Vector3.right, 0.05f, 2.1f, glow);
            return go;
        }

        static void SecretDoorFrame(Transform parent, string tag, float face, float inward, float floorY, float zc, Material gold, Material dark)
        {
            float half = SecretDoorWidth * 0.5f;
            // hapja e errët
            Box(parent, "SecretOpening_" + tag, new Vector3(face + inward * 0.012f, floorY + DoorHeight * 0.5f, zc),
                new Vector3(0.02f, DoorHeight, SecretDoorWidth - 0.1f), dark);
            // shkëlqim i artë brenda
            var glow = Theme.Ghost(Theme.WithAlpha(Theme.Gold, 0.35f));
            WallQuad(parent, "SecretGlow_" + tag, new Vector3(face + inward * 0.03f, floorY + 1.0f, zc),
                new Vector3(inward, 0, 0), SecretDoorWidth - 0.3f, 1.8f, glow);
            // korniza
            Box(parent, "SecretPostA_" + tag, new Vector3(face + inward * 0.05f, floorY + DoorHeight * 0.5f, zc - half), new Vector3(0.1f, DoorHeight, 0.1f), gold);
            Box(parent, "SecretPostB_" + tag, new Vector3(face + inward * 0.05f, floorY + DoorHeight * 0.5f, zc + half), new Vector3(0.1f, DoorHeight, 0.1f), gold);
            Box(parent, "SecretLintel_" + tag, new Vector3(face + inward * 0.05f, floorY + DoorHeight + 0.05f, zc), new Vector3(0.1f, 0.1f, SecretDoorWidth + 0.1f), gold);
            // drita
            var lightGo = new GameObject("SecretLight_" + tag);
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.localPosition = new Vector3(face + inward * 0.9f, floorY + 1.7f, zc);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Theme.Gold;
            light.range = 5f;
            light.intensity = 2.2f;
            light.shadows = LightShadows.None;
        }

        // Rreshta librash në faqen e raftit (pa collider).
        static void ShelfBooks(Transform parent, Vector3 basePos, bool alongX)
        {
            string[] colors = { "#8C3B3B", "#3B5C8C", "#4C7A4C", "#B08A45", "#6B4A7A", "#9A5F3A" };
            for (int row = 0; row < 4; row++)
            {
                float y = basePos.y + 0.35f + row * 0.5f;
                for (int i = 0; i < 6; i++)
                {
                    float off = -0.75f + i * 0.3f;
                    var mat = Theme.Lit(Theme.Hex(colors[(i + row * 2) % colors.Length]), 0.1f);
                    var pos = alongX ? new Vector3(basePos.x + off, y, basePos.z) : new Vector3(basePos.x, y, basePos.z + off);
                    var size = alongX ? new Vector3(0.24f, 0.36f, 0.04f) : new Vector3(0.04f, 0.36f, 0.24f);
                    Box(parent, "Book", pos, size, mat);
                }
            }
        }

        // ================================================================
        //  Lëvizja vertikale: rampa të padukshme, platforma dhe parmakë
        // ================================================================

        // Rampë e padukshme (vetëm collider) mbi shkallë, që lojtari të ngjitet butë.
        static GameObject Ramp(Transform parent, RoomView rv, string name, float x0, float x1, float zLow, float yLow, float zHigh, float yHigh)
        {
            float dz = zHigh - zLow, dy = yHigh - yLow;
            float len = Mathf.Sqrt(dz * dz + dy * dy);
            float ang = Mathf.Atan2(dy, dz);
            const float t = 0.1f;
            var mid = new Vector3((x0 + x1) * 0.5f, (yLow + yHigh) * 0.5f, (zLow + zHigh) * 0.5f);
            var normal = new Vector3(0f, Mathf.Cos(ang), -Mathf.Sin(ang));
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = mid - normal * (t * 0.5f);
            go.transform.localRotation = Quaternion.Euler(-ang * Mathf.Rad2Deg, 0f, 0f);
            var bc = go.AddComponent<BoxCollider>();
            bc.size = new Vector3(x1 - x0, t, len);
            go.AddComponent<Walkable>();
            if (rv != null) go.AddComponent<RoomClickTarget>().Room = rv;
            return go;
        }

        // Pllakë ku mund të ecet, me sipërfaqen në topY.
        static GameObject WalkSlab(Transform parent, RoomView rv, string name, float x0, float x1, float z0, float z1, float topY, Material mat)
        {
            var go = Box(parent, name, new Vector3((x0 + x1) * 0.5f, topY - 0.1f, (z0 + z1) * 0.5f),
                new Vector3(x1 - x0, 0.2f, z1 - z0), mat, true);
            go.AddComponent<Walkable>();
            if (rv != null) go.AddComponent<RoomClickTarget>().Room = rv;
            return go;
        }

        // Parmak i dukshëm (1 m) + pengesë e padukshme sa lartësia e syve, që lojtari të mos bjerë.
        static void Railing(Transform parent, string name, Vector3 a, Vector3 b, Material mat)
        {
            var dir = b - a;
            float len = dir.magnitude;
            if (len < 0.05f) return;
            var mid = (a + b) * 0.5f;
            var rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            var top = Box(parent, name + "_Bar", mid + Vector3.up * 1.0f, Vector3.one, mat);
            top.transform.localRotation = rot;
            top.transform.localScale = new Vector3(0.06f, 0.06f, len);
            int posts = Mathf.Max(2, Mathf.CeilToInt(len / 1.2f) + 1);
            for (int i = 0; i < posts; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)(posts - 1));
                Box(parent, name + "_Post" + i, p + Vector3.up * 0.5f, new Vector3(0.05f, 1.0f, 0.05f), mat);
            }
            var block = new GameObject(name + "_Block");
            block.transform.SetParent(parent, false);
            block.transform.localPosition = mid + Vector3.up * 1.3f;
            block.transform.localRotation = rot;
            var bc = block.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.1f, 2.6f, len);
        }

        static void GroundPiece(Transform root, float x0, float x1, float z0, float z1, Material mat)
        {
            if (x1 - x0 < 0.01f || z1 - z0 < 0.01f) return;
            var g = Box(root, "ExtGround", new Vector3((x0 + x1) * 0.5f, -0.52f, (z0 + z1) * 0.5f),
                new Vector3(x1 - x0, 0.04f, z1 - z0), mat);
            g.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // Shkallët kryesore (SH): rampa mbi shkallë, sheshpushimi në krye dhe një korridor i ngushtë
        // përgjatë murit perëndimor deri te dera e Korridorit (KO) në katin e parë.
        static void BuildStairLanding(Transform parent, RoomView rv, Material floorMat, Material railMat)
        {
            var l = rv.Layout;
            float stairX0 = l.Center.x - l.Width * 0.3f;
            float stairX1 = l.Center.x + l.Width * 0.3f;
            float zLow = l.Z0 + StairStart;
            float zHigh = l.Z0 + StairEnd(l);
            float top = UpperFloor;
            float walkZ0 = l.Z0 + 0.85f * Scale; // pak para derës SH–KO (z0 + 1·Scale)

            Ramp(parent, rv, "StairRamp", stairX0, stairX1, zLow, 0f, zHigh, top);
            WalkSlab(parent, rv, "StairLanding", l.X0 + 0.1f, l.X1 - 0.1f, zHigh, l.Z1 - 0.1f, top, floorMat);
            WalkSlab(parent, rv, "StairWalkway", l.X0 + 0.1f, stairX0, walkZ0, zHigh, top, floorMat);

            Railing(parent, "WalkwayRail", new Vector3(stairX0, top, walkZ0), new Vector3(stairX0, top, zHigh), railMat);
            Railing(parent, "WalkwayEndRail", new Vector3(l.X0 + 0.15f, top, walkZ0), new Vector3(stairX0, top, walkZ0), railMat);
            Railing(parent, "LandingRail", new Vector3(stairX1, top, zHigh), new Vector3(l.X1 - 0.15f, top, zHigh), railMat);
        }

        // Bodrumi: shkallë guri nga dera SH–BO poshtë në dyshemenë e bodrumit, çati në nivelin e tokës
        // me hapje mbi shkallë dhe parmakë rreth saj.
        static void BuildCellarStairs(Transform parent, RoomView rv, Material stepMat, Material railMat, Material roofMat)
        {
            var l = rv.Layout;
            float sx0 = l.X0 + CellarStairX0, sx1 = l.X0 + CellarStairX1;
            float zTop = l.Z1, zBot = l.Z1 - CellarStairRun;
            const int n = 12;
            float rise = -l.Y / n;
            float dep = CellarStairRun / n;
            for (int s = 0; s < n - 1; s++)
            {
                float tread = -rise * (s + 1);
                float h = tread - l.Y;
                Box(parent, "CellarStep" + s,
                    new Vector3((sx0 + sx1) * 0.5f, l.Y + h * 0.5f, zTop - dep * (s + 0.5f)),
                    new Vector3(sx1 - sx0, h, dep * 0.98f), stepMat);
            }
            Ramp(parent, rv, "CellarRamp", sx0, sx1, zBot, l.Y, zTop, 0f);

            // çatia (mund të ecet sipër nga jashtë), pa pjesën mbi shkallë
            WalkSlab(parent, rv, "CellarRoofW", l.X0, sx0, l.Z0, l.Z1, 0f, roofMat);
            WalkSlab(parent, rv, "CellarRoofE", sx1, l.X1, l.Z0, l.Z1, 0f, roofMat);
            WalkSlab(parent, rv, "CellarRoofS", sx0, sx1, l.Z0, zBot, 0f, roofMat);

            Railing(parent, "CellarRailW", new Vector3(sx0 - 0.05f, 0f, zBot), new Vector3(sx0 - 0.05f, 0f, zTop - 0.2f), railMat);
            Railing(parent, "CellarRailE", new Vector3(sx1 + 0.05f, 0f, zBot), new Vector3(sx1 + 0.05f, 0f, zTop - 0.2f), railMat);
            Railing(parent, "CellarRailS", new Vector3(sx0, 0f, zBot - 0.05f), new Vector3(sx1, 0f, zBot - 0.05f), railMat);
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
            // Maja e murit: kulla e shkallëve shkon deri në katin e parë, bodrumi mbaron te toka.
            float wallTop = RoomTop(room);
            if (other != null) wallTop = Mathf.Max(wallTop, RoomTop(Layout(other)));
            float wallH = wallTop - baseY;

            bool exterior = (other == null);
            Material useMat = exterior ? wallMat : wallInnerMat;

            // Kthina e shkalleve eshte dy kate e larte: xhami i saj shkon nga afer dyshemeja
            // deri nen tavan. RoomTop e mban majen e murit te sakte per çdo dhome.
            bool tall = room.Id == "SH" || other == "SH";

            // Mur i plotë poshtë kur dy dhoma fqinje janë në nivele të ndryshme (p.sh. Biblioteka nën Zyrë,
            // Shkallët nën Korridor, Bodrumi nën Shkallë). Pa të, nga dhoma e poshtme shihej bazamenti bosh.
            if (other != null)
            {
                float lowY = Mathf.Min(room.Y, Layout(other).Y);
                if (baseY - lowY > 0.05f)
                {
                    float lmid = (from + to) * 0.5f;
                    float llen = to - from;
                    float lh = baseY - lowY;
                    Vector3 lc = vertical ? new Vector3(at, lowY + lh * 0.5f, lmid) : new Vector3(lmid, lowY + lh * 0.5f, at);
                    Vector3 ls = vertical ? new Vector3(WallThickness, lh, llen + WallThickness) : new Vector3(llen + WallThickness, lh, WallThickness);
                    Box(root, "WallLower", lc, ls, useMat, true);
                }
            }

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

            // Front door cut on HO south exterior wall
            if (exterior && room.Id == "HO" && !vertical && Mathf.Approximately(at, 0f))
            {
                float doorCx = Layout("HO").Center.x;
                float doorHalfW = 1.0f;
                cuts.Add(new Vector2(doorCx - doorHalfW, doorCx + doorHalfW));
                cuts.Sort((a, b) => a.x.CompareTo(b.x));
            }

            // Window cuts for exterior walls (not basement). Kthina e shkalleve i merr
            // dritaret e saj te larta — xhami nga afer dyshemeja deri nen tavan.
            var windowCuts = new List<Vector2>();
            if (exterior && room.Id != "BO")
            {
                float edgeLen = to - from;
                if (edgeLen > 6f)
                {
                    float winW = tall ? 3.2f : 2.0f;
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
                        ? new Vector3(at, baseY + wallH * 0.5f, mid)
                        : new Vector3(mid, baseY + wallH * 0.5f, at);
                    Vector3 size = vertical
                        ? new Vector3(WallThickness, wallH, len + WallThickness)
                        : new Vector3(len + WallThickness, wallH, WallThickness);
                    Box(root, "Wall", center, size, useMat, true);
                }
                else
                {
                    // Wall with window openings: build parts around windows
                    BuildWallWithWindows(root, vertical, at, baseY, wallH, s, segWindows, useMat, glassMat, tall);
                }

                // Wall cap / crown molding
                Vector3 capCenter = vertical
                    ? new Vector3(at, baseY + wallH + 0.03f, mid)
                    : new Vector3(mid, baseY + wallH + 0.03f, at);
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
                float aboveDoorH = wallH - DoorHeight - 0.1f;
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

        static void BuildWallWithWindows(Transform root, bool vertical, float at, float baseY, float wallH,
            Vector2 seg, List<Vector2> windows, Material wallMat, Material glassMat, bool tall)
        {
            // Sort windows by position
            windows.Sort((a, b) => a.x.CompareTo(b.x));

            // Ne kthinen e shkalleve xhami eshte i larte, nga afer dyshemeja deri nen tavan.
            float winBottom = tall ? 0.6f : WindowBottom;
            float winTop = tall ? wallH - 0.8f : WindowTop;

            // Build wall below windows (from floor to windowBottom)
            float belowH = winBottom;
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
            float aboveH = wallH - winTop;
            if (aboveH > 0.05f)
            {
                float mid = (seg.x + seg.y) * 0.5f;
                float len = seg.y - seg.x;
                Vector3 center = vertical
                    ? new Vector3(at, baseY + winTop + aboveH * 0.5f, mid)
                    : new Vector3(mid, baseY + winTop + aboveH * 0.5f, at);
                Vector3 size = vertical
                    ? new Vector3(WallThickness, aboveH, len + WallThickness)
                    : new Vector3(len + WallThickness, aboveH, WallThickness);
                Box(root, "WallAbove", center, size, wallMat, true);
            }

            // Build pillars between windows and wall segments at window height
            float cur = seg.x;
            float windowH = winTop - winBottom;
            foreach (var w in windows)
            {
                // Pillar before window
                if (w.x > cur + 0.1f)
                {
                    float pillarLen = w.x - cur;
                    float pillarMid = (cur + w.x) * 0.5f;
                    Vector3 center = vertical
                        ? new Vector3(at, baseY + winBottom + windowH * 0.5f, pillarMid)
                        : new Vector3(pillarMid, baseY + winBottom + windowH * 0.5f, at);
                    Vector3 size = vertical
                        ? new Vector3(WallThickness, windowH, pillarLen)
                        : new Vector3(pillarLen, windowH, WallThickness);
                    Box(root, "WallPillar", center, size, wallMat, true);
                }

                // Window glass
                float winMid = (w.x + w.y) * 0.5f;
                float winW = w.y - w.x;
                Vector3 glassCenter = vertical
                    ? new Vector3(at, baseY + winBottom + windowH * 0.5f, winMid)
                    : new Vector3(winMid, baseY + winBottom + windowH * 0.5f, at);
                Vector3 glassSize = vertical
                    ? new Vector3(WallThickness * 0.3f, windowH - 0.1f, winW - 0.1f)
                    : new Vector3(winW - 0.1f, windowH - 0.1f, WallThickness * 0.3f);
                Box(root, "Glass", glassCenter, glassSize, glassMat);

                // Window frame (sill + top + sides)
                var frameMat = Theme.Lit(Theme.Hex("#3A3028"), 0.15f);
                float frameT = 0.06f;
                // Sill
                Vector3 sillCenter = vertical
                    ? new Vector3(at, baseY + winBottom - frameT * 0.5f, winMid)
                    : new Vector3(winMid, baseY + winBottom - frameT * 0.5f, at);
                Vector3 sillSize = vertical
                    ? new Vector3(WallThickness + 0.15f, frameT, winW + 0.1f)
                    : new Vector3(winW + 0.1f, frameT, WallThickness + 0.15f);
                Box(root, "WindowSill", sillCenter, sillSize, frameMat);
                // Top
                Vector3 topCenter = vertical
                    ? new Vector3(at, baseY + winTop + frameT * 0.5f, winMid)
                    : new Vector3(winMid, baseY + winTop + frameT * 0.5f, at);
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

        // ================================================================
        //  BuildFrontDoor — pivoting door panel + proximity controller
        // ================================================================
        static void BuildFrontDoor(Transform root, Material doorMat, Material doorFrameMat)
        {
            var hall = Layout("HO");
            float doorCx = hall.Center.x;
            float doorW = 2.0f;
            float doorZ = hall.Z0; // south wall at Z=0

            // Pivot at left hinge edge of door
            var pivot = new GameObject("FrontDoorPivot");
            pivot.transform.SetParent(root, false);
            pivot.transform.localPosition = new Vector3(doorCx - doorW * 0.5f, 0f, doorZ);

            // Door panel (keeps its collider so it blocks the player when closed)
            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "FrontDoorPanel";
            panel.transform.SetParent(pivot.transform, false);
            panel.transform.localPosition = new Vector3(doorW * 0.5f, DoorHeight * 0.5f, 0f);
            panel.transform.localScale = new Vector3(doorW - 0.12f, DoorHeight - 0.06f, 0.08f);
            panel.GetComponent<Renderer>().sharedMaterial = doorMat;

            // Door handle (front, outside = -Z direction)
            var hf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hf.name = "HandleFront";
            hf.transform.SetParent(pivot.transform, false);
            hf.transform.localPosition = new Vector3(doorW - 0.3f, DoorHeight * 0.45f, -0.07f);
            hf.transform.localScale = new Vector3(0.08f, 0.03f, 0.1f);
            hf.GetComponent<Renderer>().sharedMaterial = doorFrameMat;
            UnityEngine.Object.Destroy(hf.GetComponent<Collider>());

            // Door handle (back, inside = +Z direction)
            var hb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hb.name = "HandleBack";
            hb.transform.SetParent(pivot.transform, false);
            hb.transform.localPosition = new Vector3(doorW - 0.3f, DoorHeight * 0.45f, 0.07f);
            hb.transform.localScale = new Vector3(0.08f, 0.03f, 0.1f);
            hb.GetComponent<Renderer>().sharedMaterial = doorFrameMat;
            UnityEngine.Object.Destroy(hb.GetComponent<Collider>());

            // Proximity controller
            var ctrl = pivot.AddComponent<FrontDoorController>();
            ctrl.DoorCenter = new Vector3(doorCx, 1.0f, doorZ);
            ctrl.TriggerRadius = 3.5f;
        }
    }

    /// <summary>
    /// Smoothly opens/closes the front door when the player walks near it.
    /// Uses distance check against CameraRig.FpPosition each frame.
    /// </summary>
    public sealed class FrontDoorController : MonoBehaviour
    {
        public Vector3 DoorCenter;
        public float TriggerRadius = 3.5f;
        public float OpenAngle = -90f;   // negative = swings inward (into the hall)
        public float Speed = 3.5f;

        float currentAngle;
        CameraRig cachedRig;

        void Update()
        {
            if (cachedRig == null)
            {
                var rigs = FindObjectsByType<CameraRig>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (rigs.Length > 0) cachedRig = rigs[0];
                else return;
            }

            bool inRange = false;
            if (cachedRig.Mode == CameraMode.FirstPerson || cachedRig.Mode == CameraMode.ThirdPerson)
            {
                float dist = Vector3.Distance(cachedRig.FpPosition, DoorCenter);
                inRange = dist < TriggerRadius;
            }

            float target = inRange ? OpenAngle : 0f;
            currentAngle = Mathf.Lerp(currentAngle, target, Time.deltaTime * Speed);
            transform.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
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
        public GameObject SecretHint;
        public Action<int> RoomClicked;
        public Action<int> SuspectClicked;
        public Action BackgroundClicked;
        public Action<bool> SecretPassageUsed; // true = u ngjit në Zyrë, false = zbriti në Bibliotekë
        public CameraRig Rig;
        float crimeTarget;

        public void SetSecretPassageVisible(bool on)
        {
            if (SecretPassage != null) SecretPassage.SetActive(on);
            if (SecretHint != null) SecretHint.SetActive(!on);
        }

        public void BuildMarkers()
        {
            Markers = new SuspectMarker[Case.N, Case.RoomCount];
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

    // Shënon sipërfaqet ku këmbët e lojtarit mund të qëndrojnë (dysheme, rampa shkallësh, sheshpushime).
    // CameraRig ndjek vetëm këto, që lojtari të mos hipë mbi tavolina apo mobilje.
    public sealed class Walkable : MonoBehaviour { }

    // Dera e kalimit të fshehtë: kur lojtari i afrohet, del te dera tjetër (Biblioteka ↔ Zyra).
    public sealed class SecretDoorway : MonoBehaviour
    {
        public VillaView View;
        public Vector3 TargetFeet;   // në koordinatat lokale të vilës
        public float TargetYaw;
        public bool ToUpper;
        public float Radius = 0.8f;
        static float lastUse = -10f;
        CameraRig rig;

        void Update()
        {
            if (rig == null)
            {
                rig = View != null && View.Rig != null ? View.Rig : FindAnyObjectByType<CameraRig>();
                if (rig == null) return;
            }
            if (rig.Mode == CameraMode.Overview) return;
            if (Time.unscaledTime - lastUse < 1.2f) return;
            var feet = rig.FpPosition - Vector3.up * rig.EyeHeight;
            var p = transform.position;
            if (Mathf.Abs(feet.y - p.y) > 1f) return;
            float dx = feet.x - p.x, dz = feet.z - p.z;
            if (dx * dx + dz * dz > Radius * Radius) return;
            lastUse = Time.unscaledTime;
            var world = View != null ? View.transform.TransformPoint(TargetFeet) : TargetFeet;
            rig.Teleport(world, TargetYaw);
            if (View != null && View.SecretPassageUsed != null) View.SecretPassageUsed(ToUpper);
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
