using System.Collections.Generic;
using Alibi.Core;
using UnityEngine;

namespace Alibi.Unity
{
    public enum ResidentsMode
    {
        Always, // figurat banorë qëndrojnë gjithmonë në dhomën e tyre
        Auto,   // fshihen kur tabela e deduksionit e shfaq vetë atë të dyshuar
        Never
    }

    // Vila "e gjallë": mobilje të thjeshta me kod, dritë e ngrohtë për çdo dhomë, detaje sipas rolit
    // dhe nga një person në dhomën e vet (HouseSpawns). Thirret pas MapBuilder.Build.
    public static class HouseDressing
    {
        const string RootName = "HouseDressing";

        public static void Build(VillaView view)
        {
            if (view == null || view.Case == null) return;
            if (view.transform.Find(RootName) != null) return;
            var root = new GameObject(RootName);
            root.transform.SetParent(view.transform, false);
            var kit = new Kit();

            foreach (var kv in view.Rooms)
            {
                var rv = kv.Value;
                var go = new GameObject("Furniture_" + rv.Info.Id);
                go.transform.SetParent(rv.transform, false);
                var d = new Dresser(kit, rv, go.transform);
                d.Dress();
            }

            var life = root.AddComponent<HouseLife>();
            life.View = view;
            AddRoleLooksToMarkers(view, kit);
            SpawnResidents(view, root.transform, kit, life);
        }

        public static void SetResidentsMode(VillaView view, ResidentsMode mode)
        {
            if (view == null) return;
            var life = view.GetComponentInChildren<HouseLife>(true);
            if (life != null) life.Mode = mode;
        }

        static void AddRoleLooksToMarkers(VillaView view, Kit kit)
        {
            if (view.Markers == null) return;
            var c = view.Case;
            for (int s = 0; s < c.N; s++)
            {
                for (int r = 0; r < c.RoomCount; r++)
                {
                    var m = view.Markers[s, r];
                    if (m == null) continue;
                    var group = RoleLook.Attach(m.transform, c.Suspects[s].Id, kit);
                    if (group == null) continue;
                    var toggle = m.gameObject.AddComponent<RoleLookToggle>();
                    toggle.Marker = m;
                    toggle.Group = group;
                    group.SetActive(m.State == MarkerState.Confirmed);
                }
            }
        }

        // ============ HUMANOID FIGURE BUILDER ============
        // Detailed multi-primitive human — 55 parts: sculpted head with facial
        // features, articulated torso with shoulder caps, jointed arms with thumbs,
        // legs with knee-caps and calf muscles, clothing accents (collar, buttons,
        // belt buckle, shoe soles). Still built entirely from Sphere/Cube/Cylinder
        // primitives — no external meshes.
        internal static void BuildHumanoidBody(Transform parent, Material bodyMat, Material skinMat, Material hairMat, Material shoeMat)
        {
            // --- shared accent materials ---
            var urp = Shader.Find("Universal Render Pipeline/Lit");
            Material eyeMat = skinMat, eyeWhiteMat = skinMat, beltMat = skinMat;
            Material buckleMat = skinMat, lipMat = skinMat, browMat = skinMat;
            if (urp != null)
            {
                eyeMat = new Material(urp);
                eyeMat.color = new Color(0.10f, 0.10f, 0.12f);
                eyeMat.SetFloat("_Smoothness", 0.92f);

                eyeWhiteMat = new Material(urp);
                eyeWhiteMat.color = new Color(0.92f, 0.90f, 0.88f);
                eyeWhiteMat.SetFloat("_Smoothness", 0.6f);

                beltMat = new Material(urp);
                beltMat.color = new Color(0.15f, 0.12f, 0.10f);
                beltMat.SetFloat("_Smoothness", 0.5f);

                buckleMat = new Material(urp);
                buckleMat.color = new Color(0.72f, 0.65f, 0.42f);
                buckleMat.SetFloat("_Smoothness", 0.7f);

                lipMat = new Material(urp);
                lipMat.color = new Color(0.78f, 0.56f, 0.50f);
                lipMat.SetFloat("_Smoothness", 0.3f);

                browMat = new Material(urp);
                browMat.color = new Color(0.82f, 0.68f, 0.56f);
                browMat.SetFloat("_Smoothness", 0.4f);
            }

            // ===== HEAD & FACE (17 parts) =====
            // Cranium — slightly oval
            Part(parent, PrimitiveType.Sphere, "Cranium",
                new Vector3(0, 1.62f, 0), new Vector3(0.26f, 0.30f, 0.27f), skinMat);
            // Face plate — flatter sphere pushed forward
            Part(parent, PrimitiveType.Sphere, "Face",
                new Vector3(0, 1.58f, 0.04f), new Vector3(0.22f, 0.24f, 0.14f), skinMat);
            // Jaw — squared lower face
            Part(parent, PrimitiveType.Cube, "Jaw",
                new Vector3(0, 1.50f, 0.02f), new Vector3(0.18f, 0.07f, 0.14f), skinMat);
            // Chin — small sphere
            Part(parent, PrimitiveType.Sphere, "Chin",
                new Vector3(0, 1.47f, 0.08f), new Vector3(0.08f, 0.055f, 0.055f), skinMat);
            // Brow ridge
            Part(parent, PrimitiveType.Cube, "BrowRidge",
                new Vector3(0, 1.665f, 0.10f), new Vector3(0.18f, 0.022f, 0.055f), browMat);
            // Nose bridge
            Part(parent, PrimitiveType.Cube, "NoseBridge",
                new Vector3(0, 1.58f, 0.13f), new Vector3(0.032f, 0.065f, 0.03f), skinMat);
            // Nose tip
            Part(parent, PrimitiveType.Sphere, "NoseTip",
                new Vector3(0, 1.55f, 0.14f), new Vector3(0.055f, 0.04f, 0.04f), skinMat);
            // Ears
            Part(parent, PrimitiveType.Sphere, "EarL",
                new Vector3(-0.13f, 1.59f, 0), new Vector3(0.04f, 0.07f, 0.03f), skinMat);
            Part(parent, PrimitiveType.Sphere, "EarR",
                new Vector3(0.13f, 1.59f, 0), new Vector3(0.04f, 0.07f, 0.03f), skinMat);
            // Eye whites
            Part(parent, PrimitiveType.Sphere, "EyeWhiteL",
                new Vector3(-0.06f, 1.62f, 0.105f), new Vector3(0.055f, 0.038f, 0.02f), eyeWhiteMat);
            Part(parent, PrimitiveType.Sphere, "EyeWhiteR",
                new Vector3(0.06f, 1.62f, 0.105f), new Vector3(0.055f, 0.038f, 0.02f), eyeWhiteMat);
            // Irises / pupils
            Part(parent, PrimitiveType.Sphere, "EyeL",
                new Vector3(-0.06f, 1.62f, 0.12f), new Vector3(0.04f, 0.032f, 0.025f), eyeMat);
            Part(parent, PrimitiveType.Sphere, "EyeR",
                new Vector3(0.06f, 1.62f, 0.12f), new Vector3(0.04f, 0.032f, 0.025f), eyeMat);
            // Mouth
            Part(parent, PrimitiveType.Cube, "Mouth",
                new Vector3(0, 1.515f, 0.12f), new Vector3(0.065f, 0.012f, 0.018f), lipMat);
            // Hair — three overlapping spheres for volume
            Part(parent, PrimitiveType.Sphere, "HairTop",
                new Vector3(0, 1.72f, -0.01f), new Vector3(0.28f, 0.14f, 0.28f), hairMat);
            Part(parent, PrimitiveType.Sphere, "HairBack",
                new Vector3(0, 1.64f, -0.06f), new Vector3(0.27f, 0.20f, 0.22f), hairMat);
            Part(parent, PrimitiveType.Sphere, "HairSides",
                new Vector3(0, 1.66f, -0.02f), new Vector3(0.30f, 0.12f, 0.24f), hairMat);

            // ===== NECK (2 parts) =====
            Part(parent, PrimitiveType.Cylinder, "Neck",
                new Vector3(0, 1.42f, 0), new Vector3(0.10f, 0.04f, 0.10f), skinMat);
            // Trapezius / collarbone ridge
            Part(parent, PrimitiveType.Cube, "Trapezius",
                new Vector3(0, 1.38f, -0.01f), new Vector3(0.34f, 0.028f, 0.14f), skinMat);

            // ===== TORSO (6 parts) =====
            // Shoulder caps
            Part(parent, PrimitiveType.Sphere, "ShoulderL",
                new Vector3(-0.23f, 1.32f, 0), new Vector3(0.14f, 0.11f, 0.14f), bodyMat);
            Part(parent, PrimitiveType.Sphere, "ShoulderR",
                new Vector3(0.23f, 1.32f, 0), new Vector3(0.14f, 0.11f, 0.14f), bodyMat);
            // Upper chest
            Part(parent, PrimitiveType.Cube, "UpperChest",
                new Vector3(0, 1.24f, 0), new Vector3(0.40f, 0.18f, 0.24f), bodyMat);
            // Lower chest / ribcage
            Part(parent, PrimitiveType.Cube, "LowerChest",
                new Vector3(0, 1.08f, 0), new Vector3(0.36f, 0.14f, 0.22f), bodyMat);
            // Abdomen
            Part(parent, PrimitiveType.Cube, "Abdomen",
                new Vector3(0, 0.95f, 0), new Vector3(0.32f, 0.10f, 0.20f), bodyMat);
            // Hip block
            Part(parent, PrimitiveType.Cube, "Hips",
                new Vector3(0, 0.84f, 0), new Vector3(0.34f, 0.08f, 0.20f), bodyMat);

            // ===== CLOTHING ACCENTS (4 parts) =====
            Part(parent, PrimitiveType.Cube, "Collar",
                new Vector3(0, 1.36f, 0.04f), new Vector3(0.16f, 0.035f, 0.10f), skinMat);
            Part(parent, PrimitiveType.Cube, "ButtonLine",
                new Vector3(0, 1.14f, 0.122f), new Vector3(0.022f, 0.28f, 0.006f), beltMat);
            Part(parent, PrimitiveType.Cube, "Belt",
                new Vector3(0, 0.88f, 0), new Vector3(0.36f, 0.035f, 0.22f), beltMat);
            Part(parent, PrimitiveType.Cube, "BeltBuckle",
                new Vector3(0, 0.88f, 0.11f), new Vector3(0.04f, 0.035f, 0.015f), buckleMat);

            // ===== LEFT ARM (6 parts) =====
            Part(parent, PrimitiveType.Cube, "UpperArmL",
                new Vector3(-0.28f, 1.20f, 0), new Vector3(0.12f, 0.22f, 0.13f), bodyMat);
            Part(parent, PrimitiveType.Sphere, "ElbowL",
                new Vector3(-0.28f, 1.06f, 0), new Vector3(0.10f, 0.07f, 0.10f), bodyMat);
            Part(parent, PrimitiveType.Cube, "ForearmL",
                new Vector3(-0.28f, 0.92f, 0), new Vector3(0.10f, 0.20f, 0.11f), skinMat);
            Part(parent, PrimitiveType.Cylinder, "WristL",
                new Vector3(-0.28f, 0.80f, 0), new Vector3(0.06f, 0.018f, 0.06f), skinMat);
            Part(parent, PrimitiveType.Cube, "HandL",
                new Vector3(-0.28f, 0.73f, 0.01f), new Vector3(0.08f, 0.10f, 0.05f), skinMat);
            Part(parent, PrimitiveType.Sphere, "ThumbL",
                new Vector3(-0.235f, 0.74f, 0.03f), new Vector3(0.03f, 0.05f, 0.03f), skinMat);

            // ===== RIGHT ARM (6 parts) =====
            Part(parent, PrimitiveType.Cube, "UpperArmR",
                new Vector3(0.28f, 1.20f, 0), new Vector3(0.12f, 0.22f, 0.13f), bodyMat);
            Part(parent, PrimitiveType.Sphere, "ElbowR",
                new Vector3(0.28f, 1.06f, 0), new Vector3(0.10f, 0.07f, 0.10f), bodyMat);
            Part(parent, PrimitiveType.Cube, "ForearmR",
                new Vector3(0.28f, 0.92f, 0), new Vector3(0.10f, 0.20f, 0.11f), skinMat);
            Part(parent, PrimitiveType.Cylinder, "WristR",
                new Vector3(0.28f, 0.80f, 0), new Vector3(0.06f, 0.018f, 0.06f), skinMat);
            Part(parent, PrimitiveType.Cube, "HandR",
                new Vector3(0.28f, 0.73f, 0.01f), new Vector3(0.08f, 0.10f, 0.05f), skinMat);
            Part(parent, PrimitiveType.Sphere, "ThumbR",
                new Vector3(0.235f, 0.74f, 0.03f), new Vector3(0.03f, 0.05f, 0.03f), skinMat);

            // ===== LEFT LEG (7 parts) =====
            Part(parent, PrimitiveType.Cube, "ThighL",
                new Vector3(-0.11f, 0.66f, 0), new Vector3(0.15f, 0.24f, 0.15f), bodyMat);
            Part(parent, PrimitiveType.Sphere, "KneeL",
                new Vector3(-0.11f, 0.52f, 0.01f), new Vector3(0.11f, 0.065f, 0.11f), bodyMat);
            Part(parent, PrimitiveType.Cube, "ShinL",
                new Vector3(-0.11f, 0.36f, 0), new Vector3(0.11f, 0.24f, 0.12f), bodyMat);
            Part(parent, PrimitiveType.Sphere, "CalfL",
                new Vector3(-0.11f, 0.38f, -0.03f), new Vector3(0.09f, 0.14f, 0.07f), bodyMat);
            Part(parent, PrimitiveType.Sphere, "AnkleL",
                new Vector3(-0.11f, 0.20f, 0), new Vector3(0.07f, 0.05f, 0.07f), skinMat);
            Part(parent, PrimitiveType.Cube, "FootL",
                new Vector3(-0.11f, 0.055f, 0.04f), new Vector3(0.12f, 0.09f, 0.22f), shoeMat);
            Part(parent, PrimitiveType.Cube, "SoleL",
                new Vector3(-0.11f, 0.012f, 0.04f), new Vector3(0.125f, 0.018f, 0.23f), beltMat);

            // ===== RIGHT LEG (7 parts) =====
            Part(parent, PrimitiveType.Cube, "ThighR",
                new Vector3(0.11f, 0.66f, 0), new Vector3(0.15f, 0.24f, 0.15f), bodyMat);
            Part(parent, PrimitiveType.Sphere, "KneeR",
                new Vector3(0.11f, 0.52f, 0.01f), new Vector3(0.11f, 0.065f, 0.11f), bodyMat);
            Part(parent, PrimitiveType.Cube, "ShinR",
                new Vector3(0.11f, 0.36f, 0), new Vector3(0.11f, 0.24f, 0.12f), bodyMat);
            Part(parent, PrimitiveType.Sphere, "CalfR",
                new Vector3(0.11f, 0.38f, -0.03f), new Vector3(0.09f, 0.14f, 0.07f), bodyMat);
            Part(parent, PrimitiveType.Sphere, "AnkleR",
                new Vector3(0.11f, 0.20f, 0), new Vector3(0.07f, 0.05f, 0.07f), skinMat);
            Part(parent, PrimitiveType.Cube, "FootR",
                new Vector3(0.11f, 0.055f, 0.04f), new Vector3(0.12f, 0.09f, 0.22f), shoeMat);
            Part(parent, PrimitiveType.Cube, "SoleR",
                new Vector3(0.11f, 0.012f, 0.04f), new Vector3(0.125f, 0.018f, 0.23f), beltMat);
        }

        // Gjen një pikë të lirë në dhomë: larg mureve, jashtë mobiljeve dhe larg banorëve të tjerë.
        // Mobiljet maten nga Renderer.bounds reale, prandaj nuk ka nevojë për pozicione me dorë.
        static readonly List<Bounds> obstacleBuf = new List<Bounds>();

        static Vector3 FreeSpot(VillaView view, RoomView rv, List<Vector3> used)
        {
            var l = rv.Layout;
            float floorY = view.transform.TransformPoint(new Vector3(l.Center.x, l.Y, l.Center.z)).y;

            // Gjithçka në dhomë: mobiljet, shkallët, parmakët. Filtri i lartësisë lë jashtë
            // dyshemenë, tavanin, suvatimin dhe qilimat.
            obstacleBuf.Clear();
            foreach (var rend in rv.GetComponentsInChildren<Renderer>(true))
            {
                var b = rend.bounds;
                if (b.max.y < floorY + 0.25f) continue;  // qilima, dysheme — nuk pengojnë
                if (b.min.y > floorY + 2.0f) continue;   // llamba tavani — mbi kokë
                obstacleBuf.Add(b);
            }

            const float Margin = 1.2f;   // larg mureve
            const float Radius = 0.55f;  // gjerësia e figurës
            float x0 = l.X0 + Margin, x1 = l.X1 - Margin;
            float z0 = l.Z0 + Margin, z1 = l.Z1 - Margin;
            if (x1 < x0) { x0 = x1 = l.Center.x; }
            if (z1 < z0) { z0 = z1 = l.Center.z; }

            var best = new Vector3(l.Center.x, l.Y, l.Center.z);
            float bestScore = float.NegativeInfinity;
            const int N = 11;

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    float lx = Mathf.Lerp(x0, x1, i / (float)(N - 1));
                    float lz = Mathf.Lerp(z0, z1, j / (float)(N - 1));
                    var world = view.transform.TransformPoint(new Vector3(lx, l.Y + 0.9f, lz));

                    float clear = 4f;
                    foreach (var b in obstacleBuf)
                    {
                        float dx = Mathf.Max(Mathf.Max(b.min.x - world.x, world.x - b.max.x), 0f);
                        float dz = Mathf.Max(Mathf.Max(b.min.z - world.z, world.z - b.max.z), 0f);
                        float d2 = dx * dx + dz * dz;
                        if (d2 < clear * clear) clear = Mathf.Sqrt(d2);
                    }

                    float score = Mathf.Min(clear, 2.0f);
                    if (clear < Radius) score -= 10f;   // brenda mobiljes — shmange
                    foreach (var u in used)
                    {
                        float d = Vector2.Distance(new Vector2(lx, lz), new Vector2(u.x, u.z));
                        if (d < 1.6f) score -= (1.6f - d) * 4f;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = new Vector3(lx, l.Y, lz);
                    }
                }
            }
            return best;
        }

        static void SpawnResidents(VillaView view, Transform parent, Kit kit, HouseLife life)
        {
            var c = view.Case;
            var rooms = HouseSpawns.Assign(c);
            var taken = new Dictionary<int, List<Vector3>>();
            // Standard materials — NO emission/glow, lit by per-figure point light
            var skinMat = kit.Mat("#F5D4B0", 0.55f);   // warm peach skin, moderate sheen
            var shoeMat = kit.Mat("#2A2220", 0.4f);     // dark shoes

            for (int s = 0; s < c.N; s++)
            {
                int r = rooms[s];
                RoomView rv;
                if (!view.Rooms.TryGetValue(r, out rv)) continue;
                List<Vector3> used;
                if (!taken.TryGetValue(r, out used)) { used = new List<Vector3>(); taken[r] = used; }
                var pos = FreeSpot(view, rv, used);
                used.Add(pos);
                var info = c.Suspects[s];
                var go = new GameObject("Resident_" + info.Id);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = pos;
                var visual = new GameObject("Visual");
                visual.transform.SetParent(go.transform, false);

                // Clothing material — standard lit, NO glow/emission
                var bodyMat = kit.Mat(info.ColorHex, 0.4f);
                // Hair color varies by suspect
                string hairHex;
                switch (info.Id)
                {
                    case "D": hairHex = "#3B2A22"; break;  // Drita — dark brown
                    case "G": hairHex = "#5A4630"; break;  // Genta — lighter brown
                    case "A": hairHex = "#2A1A12"; break;  // Arta — very dark
                    case "E": hairHex = "#6B6B6B"; break;  // Elmazi — grey
                    default:  hairHex = "#1A1410"; break;   // dark hair
                }
                var hairMat = kit.Mat(hairHex, 0.2f);

                BuildHumanoidBody(visual.transform, bodyMat, skinMat, hairMat, shoeMat);

                // Add role-specific props on top
                RoleLook.Attach(visual.transform, info.Id, kit);

                // --- KLIKIMI: hap panelin e pyetjeve (më parë e bënte SuspectMarker) ---
                var clickGo = new GameObject("ClickTarget");
                clickGo.transform.SetParent(visual.transform, false);
                clickGo.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                var cap = clickGo.AddComponent<CapsuleCollider>();
                cap.radius = 0.38f;
                cap.height = 1.9f;
                int si = s;
                clickGo.AddComponent<ClickRelay>().OnClick = () =>
                {
                    if (view.SuspectClicked != null) view.SuspectClicked(si);
                };

                // --- POINT LIGHT per figure: real 3D shading in dark rooms ---
                var lightGo = new GameObject("FigureLight");
                lightGo.transform.SetParent(go.transform, false);
                lightGo.transform.localPosition = new Vector3(0, 2.2f, 0.5f);
                var pl = lightGo.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.color = new Color(1f, 0.95f, 0.85f); // warm white
                pl.intensity = 3.0f;
                pl.range = 5.0f;
                pl.shadows = LightShadows.Soft;

                // --- NAME LABEL (world-space, hidden until player is near) ---
                var nameGo = new GameObject("NameLabel");
                nameGo.transform.SetParent(go.transform, false);
                nameGo.transform.localPosition = new Vector3(0, 2.05f, 0);
                var nameCanvas = nameGo.AddComponent<Canvas>();
                nameCanvas.renderMode = RenderMode.WorldSpace;
                nameCanvas.sortingOrder = 100;
                var nameRt = nameGo.GetComponent<RectTransform>();
                nameRt.sizeDelta = new Vector2(240, 40);
                nameRt.localScale = new Vector3(0.008f, 0.008f, 0.008f);
                var txtGo = new GameObject("Text");
                txtGo.transform.SetParent(nameGo.transform, false);
                var txt = txtGo.AddComponent<UnityEngine.UI.Text>();
                txt.text = info.Name;
                txt.font = Theme.SansBold;
                txt.fontSize = 28;
                txt.color = Color.white;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                var txtRt = txtGo.GetComponent<RectTransform>();
                txtRt.sizeDelta = new Vector2(240, 40);
                // Shadow outline for readability
                var shadow = txtGo.AddComponent<UnityEngine.UI.Shadow>();
                shadow.effectColor = new Color(0, 0, 0, 0.8f);
                shadow.effectDistance = new Vector2(1, -1);
                nameGo.SetActive(false); // hidden by default

                var res = go.AddComponent<ResidentFigure>();
                res.Life = life;
                res.Suspect = s;
                res.Visual = visual;
                res.NameLabel = nameGo;
                life.Residents.Add(res);
            }
        }

        internal static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            return go;
        }

        internal sealed class Kit
        {
            readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

            public Material Mat(string hex, float smooth = 0.15f)
            {
                string key = hex + "|" + smooth;
                Material m;
                if (cache.TryGetValue(key, out m) && m != null) return m;
                m = Theme.Lit(Theme.Hex(hex), smooth);
                cache[key] = m;
                return m;
            }

            public Material Glow(string hex, float strength = 1.6f)
            {
                string key = "glow:" + hex + "|" + strength;
                Material m;
                if (cache.TryGetValue(key, out m) && m != null) return m;
                var c = Theme.Hex(hex);
                m = Theme.Lit(c, 0.3f);
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * strength);
                cache[key] = m;
                return m;
            }
        }

        sealed class Dresser
        {
            readonly Kit kit;
            readonly RoomView rv;
            readonly Transform parent;
            readonly RoomLayout l;
            readonly float w, d;
            const float PS = MapBuilder.Scale;
            const float DS = 2.2f;

            public Dresser(Kit kit, RoomView rv, Transform parent)
            {
                this.kit = kit;
                this.rv = rv;
                this.parent = parent;
                l = rv.Layout;
                w = l.X1 - l.X0;
                d = l.Z1 - l.Z0;
            }

            static readonly string[] BookColors = { "#8C3B3B", "#3B5C8C", "#4C7A4C", "#B08A45", "#6B4A7A", "#9A5F3A", "#2F6B6B" };

            Material M(string hex, float smooth = 0.15f) { return kit.Mat(hex, smooth); }

            Vector3 P(float u, float v, float y)
            {
                return new Vector3(l.X0 + u * PS, l.Y + y * DS, l.Z0 + v * PS);
            }

            void Finish(GameObject go, bool solid)
            {
                var col = go.GetComponent<Collider>();
                if (solid)
                {
                    go.AddComponent<RoomClickTarget>().Room = rv;
                }
                else if (col != null)
                {
                    Object.Destroy(col);
                }
            }

            GameObject Cube(string name, Material mat, float u, float v, float y0, float sx, float sy, float sz, bool solid = true, float rotY = 0f)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(parent, false);
                go.transform.localPosition = P(u, v, y0 + sy * 0.5f);
                go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
                go.transform.localScale = new Vector3(sx * DS, sy * DS, sz * DS);
                go.GetComponent<Renderer>().sharedMaterial = mat;
                Finish(go, solid);
                return go;
            }

            GameObject Cyl(string name, Material mat, float u, float v, float y0, float dia, float h, bool solid = true)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = name;
                go.transform.SetParent(parent, false);
                go.transform.localPosition = P(u, v, y0 + h * 0.5f);
                go.transform.localScale = new Vector3(dia * DS, h * 0.5f * DS, dia * DS);
                go.GetComponent<Renderer>().sharedMaterial = mat;
                Finish(go, solid);
                return go;
            }

            GameObject Ball(string name, Material mat, float u, float v, float yCenter, float dia)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = name;
                go.transform.SetParent(parent, false);
                go.transform.localPosition = P(u, v, yCenter);
                go.transform.localScale = new Vector3(dia * DS, dia * DS, dia * DS);
                go.GetComponent<Renderer>().sharedMaterial = mat;
                Finish(go, false);
                return go;
            }

            void Rug(float u, float v, float sx, float sz, string hex)
            {
                Cube("Rug", M(hex, 0.05f), u, v, 0f, sx, 0.006f, sz, false);
            }

            void Plant(float u, float v)
            {
                Cyl("PlantPot", M("#8A4B32"), u, v, 0f, 0.32f, 0.26f);
                Ball("PlantLeaves", M("#3F7A45"), u, v, 0.46f, 0.4f);
            }

            void SmallPlant(float u, float v, float y0)
            {
                Cyl("SmallPot", M("#8A4B32"), u, v, y0, 0.12f, 0.1f, false);
                Ball("SmallLeaves", M("#4A8A52"), u, v, y0 + 0.16f, 0.14f);
            }

            void FloorLamp(float u, float v, string glowHex = "#FFD9A0")
            {
                Cyl("LampPole", M("#B8923F", 0.5f), u, v, 0f, 0.07f, 0.95f);
                Ball("LampShade", kit.Glow(glowHex, 1.8f), u, v, 1.03f, 0.26f);
            }

            void TableLamp(float u, float v, float y0, string glowHex = "#FFD9A0")
            {
                Cyl("LampBase", M("#B8923F", 0.5f), u, v, y0, 0.08f, 0.12f, false);
                Ball("LampShade", kit.Glow(glowHex, 1.8f), u, v, y0 + 0.2f, 0.18f);
            }

            void Chair(float u, float v, float backSide)
            {
                Cube("ChairSeat", M("#5B3F2A"), u, v, 0f, 0.26f, 0.22f, 0.26f);
                if (backSide != 0f) Cube("ChairBack", M("#5B3F2A"), u, v + backSide * 0.115f, 0.22f, 0.26f, 0.22f, 0.04f, false);
            }

            void Books(float u0, float u1, float v, float topY, int seed)
            {
                float u = u0 + 0.05f;
                int i = seed;
                while (u < u1 - 0.05f)
                {
                    float bw = 0.05f + 0.03f * ((i * 7) % 3);
                    float bh = 0.14f + 0.06f * ((i * 5) % 3);
                    Cube("Book", M(BookColors[i % BookColors.Length], 0.1f), u + bw * 0.5f, v, topY, bw, bh, 0.18f, false);
                    u += bw + 0.012f;
                    i++;
                }
            }

            void BookStack(float u, float v, float y0, int count, int seed)
            {
                float y = y0;
                for (int i = 0; i < count; i++)
                {
                    float bh = 0.02f + 0.01f * ((seed + i) % 3);
                    Cube("BookStack" + i, M(BookColors[(seed + i) % BookColors.Length], 0.1f),
                        u, v, y, 0.18f, bh, 0.12f, false, 5f * ((seed + i) % 4));
                    y += bh;
                }
            }

            void Shelf(string name, float u, float v, float sx, float sz, float h, int seed, bool booksAlongU)
            {
                Cube(name, M("#4A3320"), u, v, 0f, sx, h, sz);
                if (booksAlongU) Books(u - sx * 0.5f, u + sx * 0.5f, v, h, seed);
            }

            void Lamp(Vector3 localPos, Color color, float range, float intensity, string name)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = localPos;
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = color;
                light.range = range * DS;
                light.intensity = intensity;
                light.shadows = LightShadows.None;
            }

            void CeilingLamp(Color color, float range, float intensity, string name)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                float lampY = (l.Id == "SH" ? MapBuilder.UpperFloor + MapBuilder.WallHeight : l.Y + MapBuilder.WallHeight) - 0.3f;
                go.transform.localPosition = new Vector3(l.Center.x, lampY, l.Center.z);
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = color;
                light.range = range;
                light.intensity = intensity;
                light.shadows = LightShadows.Soft;
            }

            // =========== CEILING LIGHT FIXTURES ===========
            void CeilingFixture(string style)
            {
                // Kthina e shkalleve e ka tavanin ne nivelin e katit te siperm.
                float ceilY = (l.Id == "SH" ? MapBuilder.UpperFloor + MapBuilder.WallHeight : MapBuilder.WallHeight) / DS;
                float fixtureY = ceilY - 0.08f;
                float cU = (l.Center.x - l.X0) / PS;
                float cV = (l.Center.z - l.Z0) / PS;

                switch (style)
                {
                    case "chandelier":
                        Cyl("ChandelierRod", M("#B8923F", 0.5f), cU, cV, fixtureY - 0.3f, 0.04f, 0.3f, false);
                        Cyl("ChandelierDisc", M("#C2A673", 0.4f), cU, cV, fixtureY - 0.35f, 0.5f, 0.03f, false);
                        float armR = 0.22f;
                        for (int i = 0; i < 4; i++)
                        {
                            float angle = i * 90f * Mathf.Deg2Rad;
                            float au = cU + Mathf.Cos(angle) * armR;
                            float av = cV + Mathf.Sin(angle) * armR;
                            Ball("ChandelierBulb" + i, kit.Glow("#FFE8C4", 2.0f), au, av, fixtureY - 0.42f, 0.08f);
                        }
                        break;
                    case "pendant":
                        Cyl("PendantRod", M("#2B2B2E", 0.3f), cU, cV, fixtureY - 0.22f, 0.02f, 0.22f, false);
                        Cyl("PendantShade", M("#4A3828", 0.2f), cU, cV, fixtureY - 0.3f, 0.28f, 0.08f, false);
                        Ball("PendantBulb", kit.Glow("#FFD9A0", 1.6f), cU, cV, fixtureY - 0.36f, 0.06f);
                        break;
                    case "industrial":
                        Cyl("IndustrialChain", M("#6A6F73", 0.4f), cU, cV, fixtureY - 0.18f, 0.015f, 0.18f, false);
                        Cyl("IndustrialCage", M("#4A5058", 0.3f), cU, cV, fixtureY - 0.26f, 0.2f, 0.14f, false);
                        Ball("IndustrialBulb", kit.Glow("#FFB45A", 2.0f), cU, cV, fixtureY - 0.26f, 0.07f);
                        break;
                    case "lantern":
                        Cyl("LanternHook", M("#2B2B2E", 0.3f), cU, cV, fixtureY - 0.1f, 0.015f, 0.1f, false);
                        Cube("LanternFrame", M("#4A3828", 0.15f), cU, cV, fixtureY - 0.22f, 0.14f, 0.18f, 0.14f, false);
                        Ball("LanternGlow", kit.Glow("#FFB45A", 1.4f), cU, cV, fixtureY - 0.22f, 0.08f);
                        break;
                    case "flush":
                        Cyl("FlushBase", M("#6A6F73", 0.3f), cU, cV, fixtureY - 0.02f, 0.3f, 0.04f, false);
                        Ball("FlushGlobe", kit.Glow("#FFE2C8", 1.4f), cU, cV, fixtureY - 0.08f, 0.12f);
                        break;
                }
            }

            // =========== WALL ART / CLOCKS / MIRRORS ===========
            void WallPainting(float u, float v, float y0, float sx, float sy, string frameHex, string canvasHex)
            {
                Cube("PaintingFrame", M(frameHex, 0.3f), u, v, y0, sx + 0.04f, sy + 0.04f, 0.02f, false);
                Cube("PaintingCanvas", M(canvasHex, 0.05f), u, v, y0 + 0.02f, sx, sy, 0.015f, false);
            }

            void WallClock(float u, float v, float y0)
            {
                Cyl("ClockBody", M("#4A3320", 0.3f), u, v, y0, 0.22f, 0.03f, false);
                Cyl("ClockFace", kit.Glow("#F0E6C8", 0.8f), u, v, y0 + 0.016f, 0.18f, 0.005f, false);
            }

            // Curtain pair flanking a window position
            void Curtains(float u, float v, float sx, string hex)
            {
                float half = sx * 0.15f;
                Cube("CurtainL", M(hex, 0.05f), u - sx * 0.4f, v, 0.3f, half, 0.65f, 0.03f, false);
                Cube("CurtainR", M(hex, 0.05f), u + sx * 0.4f, v, 0.3f, half, 0.65f, 0.03f, false);
                // curtain rod
                Cyl("CurtainRod", M("#B8923F", 0.4f), u, v, 0.94f, 0.02f, sx * 0.5f, false);
            }

            // Decorative bowl / dish
            void Bowl(float u, float v, float y0, string hex)
            {
                Cyl("Bowl", M(hex, 0.3f), u, v, y0, 0.16f, 0.04f, false);
            }

            // Wine glass
            void WineGlass(float u, float v, float y0)
            {
                Cyl("WineGlass", M("#A8B8C8", 0.8f), u, v, y0, 0.04f, 0.09f, false);
            }

            // Candle
            void Candle(float u, float v, float y0, string name)
            {
                Cyl(name, kit.Glow("#FFE2A8", 1.5f), u, v, y0, 0.04f, 0.1f, false);
            }

            // Newspaper / flat paper
            void Paper(float u, float v, float y0, float rotY = 0f)
            {
                Cube("Paper", M("#D8CBB0", 0.05f), u, v, y0, 0.2f, 0.005f, 0.14f, false, rotY);
            }

            public void Dress()
            {
                switch (l.Id)
                {
                    case "HO": Hall(); break;
                    case "SA": Salon(); break;
                    case "BI": Library(); break;
                    case "DN": Dining(); break;
                    case "KU": Kitchen(); break;
                    case "SH": Stairs(); break;
                    case "KO": Corridor(); break;
                    case "ZY": Office(); break;
                    case "BO": Cellar(); break;
                }
                RoomLight();
            }

            void RoomLight()
            {
                Color color = new Color(1f, 0.88f, 0.68f);
                float intensity = 3.5f;
                switch (l.Id)
                {
                    case "HO": color = new Color(1f, 0.90f, 0.70f); intensity = 3.8f; break;
                    case "SA": color = new Color(1f, 0.85f, 0.65f); intensity = 3.5f; break;
                    case "BI": color = new Color(1f, 0.82f, 0.58f); intensity = 3.2f; break;
                    case "DN": color = new Color(1f, 0.88f, 0.66f); intensity = 3.5f; break;
                    case "KU": color = new Color(1f, 0.92f, 0.72f); intensity = 3.8f; break;
                    case "SH": color = new Color(1f, 0.85f, 0.60f); intensity = 3.0f; break;
                    case "KO": color = new Color(1f, 0.85f, 0.62f); intensity = 3.2f; break;
                    case "ZY": color = new Color(1f, 0.90f, 0.72f); intensity = 3.5f; break;
                    case "BO": color = new Color(1f, 0.75f, 0.50f); intensity = 2.5f; break;
                }
                float range = Mathf.Max(w, d) * 0.8f + 6f;
                CeilingLamp(color, range, intensity, "RoomLight_" + l.Id);
                // Add a secondary fill light at floor level for better visibility
                Lamp(new Vector3(l.Center.x, l.Y + 0.8f, l.Center.z),
                    new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f),
                    Mathf.Max(w, d) * 0.4f + 2f, intensity * 0.3f, "FillLight_" + l.Id);
            }

            // =====================================================
            //   ROOM DRESSING — each room packed with objects
            // =====================================================

            void Hall()
            {
                CeilingFixture("chandelier");
                Rug(2.0f, 1.5f, 1.8f, 1.1f, "#6E2F37");
                // Hyrja mbetet e lirë: konsolla, vazoja, kupa e çelësave dhe pasqyra u hoqën
                // sepse binin pikërisht para derës së hyrjes.
                // Large potted plants
                Plant(0.3f, 0.3f);
                Plant(3.7f, 0.3f);
                // Coat stand + umbrella stand
                Cyl("CoatStand", M("#5B3F2A"), 3.7f, 2.7f, 0f, 0.14f, 1.0f);
                Cyl("CoatStandBase", M("#2B2B2E"), 3.7f, 2.7f, 0f, 0.3f, 0.05f, false);
                Cyl("UmbrellaStand", M("#4A3828"), 3.5f, 2.7f, 0f, 0.18f, 0.35f);
                // Floor lamp
                FloorLamp(0.3f, 2.7f);
                // Hall paintings
                WallPainting(0.6f, 0.08f, 0.55f, 0.3f, 0.22f, "#5B3F2A", "#2A3A4A");
                WallPainting(3.4f, 0.08f, 0.5f, 0.25f, 0.2f, "#B8923F", "#3A2E1E");
                // Curtains flanking imagined window
                Curtains(2.0f, 2.92f, 0.9f, "#6E2F37");
                // Small bench by wall
                Cube("HallBench", M("#5B3F2A"), 0.6f, 2.78f, 0f, 0.6f, 0.24f, 0.22f);
                Cube("HallCushion", M("#D2AD62", 0.1f), 0.6f, 2.76f, 0.24f, 0.4f, 0.08f, 0.18f, false);
                // Door mat
                Cube("DoorMat", M("#4A3828", 0.05f), 2.0f, 2.85f, 0f, 0.5f, 0.006f, 0.3f, false);
                // Shoes near door
                Cube("Shoe1", M("#2B1A12", 0.2f), 1.4f, 2.8f, 0f, 0.08f, 0.05f, 0.14f, false, 15f);
                Cube("Shoe2", M("#2B1A12", 0.2f), 1.5f, 2.8f, 0f, 0.08f, 0.05f, 0.14f, false, 5f);
            }

            void Salon()
            {
                CeilingFixture("chandelier");
                Rug(2.2f, 1.5f, 2.4f, 1.5f, "#2E3F5C");
                // Sofa with cushions
                Cube("SofaSeat", M("#6E2F37", 0.2f), 2.0f, 2.8f, 0f, 1.7f, 0.22f, 0.24f);
                Cube("SofaBack", M("#6E2F37", 0.2f), 2.0f, 2.885f, 0f, 1.7f, 0.5f, 0.07f);
                Cube("SofaArmL", M("#6E2F37", 0.2f), 1.22f, 2.8f, 0f, 0.14f, 0.32f, 0.24f);
                Cube("SofaArmR", M("#6E2F37", 0.2f), 2.78f, 2.8f, 0f, 0.14f, 0.32f, 0.24f);
                Cube("CushionL", M("#D2AD62", 0.1f), 1.5f, 2.76f, 0.22f, 0.22f, 0.14f, 0.18f, false, 5f);
                Cube("CushionM", M("#4A5068", 0.1f), 2.0f, 2.76f, 0.22f, 0.2f, 0.12f, 0.16f, false, -3f);
                Cube("CushionR", M("#2E3F5C", 0.1f), 2.5f, 2.76f, 0.22f, 0.22f, 0.14f, 0.18f, false, -8f);
                // Coffee table with items
                Cube("CoffeeTable", M("#7C5A3A", 0.3f), 1.1f, 1.5f, 0f, 0.8f, 0.3f, 0.45f);
                Cyl("CoffeeCup", M("#D8CBB0"), 1.0f, 1.5f, 0.3f, 0.09f, 0.07f, false);
                Cube("CoffeeBook", M("#8C3B3B", 0.1f), 1.25f, 1.65f, 0.3f, 0.16f, 0.025f, 0.12f, false);
                BookStack(1.3f, 1.35f, 0.3f, 3, 2);
                Bowl(0.95f, 1.7f, 0.3f, "#B8923F");
                Paper(1.4f, 1.5f, 0.3f, 12f);
                // Fireplace
                Cube("Fireplace", M("#6E6A64", 0.1f), 3.2f, 0.22f, 0f, 0.9f, 0.8f, 0.28f);
                Cube("Fire", kit.Glow("#FF7A2E", 2.2f), 3.2f, 0.375f, 0.08f, 0.4f, 0.22f, 0.03f, false);
                Lamp(P(3.2f, 0.7f, 0.5f), new Color(1f, 0.5f, 0.2f), 2.2f, 0.5f, "FireGlow");
                Candle(2.85f, 0.22f, 0.8f, "MantelCandleL");
                Candle(3.55f, 0.22f, 0.8f, "MantelCandleR");
                Cube("MantelShelf", M("#5B3F2A"), 3.2f, 0.22f, 0.78f, 1.05f, 0.04f, 0.14f, false);
                // Photo frame on mantel
                Cube("PhotoFrame", M("#5B3F2A", 0.3f), 3.1f, 0.22f, 0.84f, 0.08f, 0.1f, 0.02f, false);
                Ball("MantelOrnament", M("#D2AD62", 0.4f), 3.4f, 0.22f, 0.86f, 0.06f);
                // Painting above fireplace
                WallPainting(3.2f, 0.08f, 0.55f, 0.4f, 0.28f, "#B8923F", "#2A3A4A");
                // Side table with lamp
                Cube("SideTable", M("#5B3F2A"), 3.55f, 2.68f, 0f, 0.4f, 0.45f, 0.4f);
                TableLamp(3.55f, 2.68f, 0.45f);
                SmallPlant(3.55f, 2.5f, 0.45f);
                // Large potted plant
                Plant(0.3f, 2.7f);
                Plant(0.3f, 0.3f);
                // Armchair + ottoman
                Cube("ArmchairSeat", M("#5E4A38", 0.2f), 3.6f, 1.5f, 0f, 0.36f, 0.22f, 0.34f);
                Cube("ArmchairBack", M("#5E4A38", 0.2f), 3.78f, 1.5f, 0f, 0.08f, 0.5f, 0.34f);
                Cube("ArmchairArmL", M("#5E4A38", 0.2f), 3.6f, 1.32f, 0f, 0.08f, 0.3f, 0.08f, false);
                Cube("ArmchairArmR", M("#5E4A38", 0.2f), 3.6f, 1.68f, 0f, 0.08f, 0.3f, 0.08f, false);
                Cube("Ottoman", M("#5E4A38", 0.1f), 3.3f, 1.5f, 0f, 0.24f, 0.14f, 0.24f, false);
                // Floor lamp by armchair
                FloorLamp(3.75f, 2.0f);
                // Curtains
                Curtains(1.5f, 0.08f, 0.8f, "#2E3F5C");
                // Second painting
                WallPainting(0.5f, 2.92f, 0.5f, 0.3f, 0.22f, "#5B3F2A", "#4A5A3A");
                // Small rug by armchair
                Rug(3.5f, 1.5f, 0.6f, 0.5f, "#5E2E34");
                // Wine glass on side table
                WineGlass(3.4f, 2.68f, 0.45f);
            }

            void Library()
            {
                CeilingFixture("pendant");
                Rug(1.5f, 1.5f, 1.5f, 1.1f, "#5E2E34");
                // Bookshelves north wall
                Shelf("ShelfN1", 0.9f, 2.79f, 1.1f, 0.26f, 0.85f, 1, true);
                Shelf("ShelfN2", 2.1f, 2.79f, 1.1f, 0.26f, 0.85f, 4, true);
                // Bookshelves west wall (with gap for passage)
                Shelf("ShelfW1", 0.22f, 0.55f, 0.28f, 0.8f, 0.85f, 0, false);
                Shelf("ShelfW2", 0.22f, 2.3f, 0.28f, 0.5f, 0.85f, 0, false);
                // Bookshelves east wall (with gap for door)
                Shelf("ShelfE1", 2.8f, 0.52f, 0.26f, 0.74f, 0.85f, 0, false);
                Shelf("ShelfE2", 2.8f, 2.3f, 0.26f, 0.5f, 0.85f, 0, false);
                // Reading table with items
                Cube("ReadingTable", M("#7C5A3A", 0.3f), 1.9f, 1.5f, 0f, 0.7f, 0.32f, 0.5f);
                Cube("OpenBook", M("#D8CBB0", 0.1f), 1.78f, 1.5f, 0.32f, 0.2f, 0.02f, 0.14f, false);
                TableLamp(2.1f, 1.5f, 0.32f, "#FFC97A");
                BookStack(1.65f, 1.3f, 0.32f, 4, 7);
                Paper(2.0f, 1.7f, 0.32f, -15f);
                Cyl("InkWell", M("#1A1D24", 0.3f), 2.15f, 1.3f, 0.32f, 0.04f, 0.06f, false);
                // Globe on a stand
                Cyl("GlobeStand", M("#5B3F2A"), 0.4f, 1.5f, 0f, 0.08f, 0.5f, false);
                Ball("Globe", M("#3B5C8C", 0.2f), 0.4f, 1.5f, 0.6f, 0.2f);
                // Reading chair with footstool
                Cube("ReadChairSeat", M("#6E2F37", 0.2f), 1.5f, 0.8f, 0f, 0.3f, 0.22f, 0.3f);
                Cube("ReadChairBack", M("#6E2F37", 0.2f), 1.5f, 0.65f, 0.22f, 0.3f, 0.26f, 0.04f, false);
                Cube("Footstool", M("#6E2F37", 0.1f), 1.5f, 1.0f, 0f, 0.2f, 0.12f, 0.18f, false);
                // Floor lamp by reading chair
                FloorLamp(1.2f, 0.6f, "#FFC97A");
                // Additional book stacks on floor
                BookStack(0.3f, 2.0f, 0f, 5, 10);
                BookStack(2.6f, 2.6f, 0f, 3, 15);
                // Magnifying glass on table
                Cyl("MagnifyHandle", M("#5B3F2A"), 2.2f, 1.65f, 0.32f, 0.025f, 0.08f, false);
                // Ladder leaning against shelf
                Cube("LadderRailL", M("#5B3F2A"), 0.14f, 1.6f, 0f, 0.03f, 0.95f, 0.03f, false);
                Cube("LadderRailR", M("#5B3F2A"), 0.3f, 1.6f, 0f, 0.03f, 0.95f, 0.03f, false);
                // Wall painting
                WallPainting(1.5f, 0.08f, 0.55f, 0.3f, 0.22f, "#4A3320", "#6E4A2E");
                // Small plant on shelf top
                SmallPlant(0.6f, 2.79f, 0.85f);
            }

            void Dining()
            {
                CeilingFixture("chandelier");
                Rug(1.9f, 1.5f, 1.9f, 1.3f, "#5E2430");
                // Dining table with full settings
                Cube("DiningTable", M("#7C5A3A", 0.3f), 1.9f, 1.5f, 0f, 1.2f, 0.32f, 0.6f);
                Cube("TableCloth", M("#D8CBB0", 0.05f), 1.9f, 1.5f, 0.32f, 1.3f, 0.01f, 0.5f, false);
                Candle(1.6f, 1.5f, 0.33f, "Candle1");
                Candle(2.2f, 1.5f, 0.33f, "Candle2");
                // Candle holder/centerpiece
                Cyl("Centerpiece", M("#B8923F", 0.4f), 1.9f, 1.5f, 0.33f, 0.08f, 0.06f, false);
                // 6 place settings: plates, glasses, napkins
                float[] pu = { 1.55f, 2.25f, 1.55f, 2.25f, 1.9f, 1.9f };
                float[] pv = { 1.1f, 1.1f, 1.9f, 1.9f, 1.1f, 1.9f };
                for (int i = 0; i < 6; i++)
                {
                    Cyl("Plate" + i, M("#D8CBB0", 0.1f), pu[i], pv[i], 0.33f, 0.14f, 0.012f, false);
                    // Napkin
                    Cube("Napkin" + i, M("#D8CBB0", 0.05f), pu[i] + 0.1f, pv[i], 0.33f, 0.06f, 0.006f, 0.1f, false);
                }
                WineGlass(1.72f, 1.1f, 0.33f);
                WineGlass(2.42f, 1.1f, 0.33f);
                WineGlass(1.72f, 1.9f, 0.33f);
                WineGlass(2.42f, 1.9f, 0.33f);
                // 6 chairs
                Chair(1.55f, 0.95f, -1f);
                Chair(2.25f, 0.95f, -1f);
                Chair(1.55f, 2.05f, 1f);
                Chair(2.25f, 2.05f, 1f);
                Chair(2.68f, 1.5f, 0f);
                Chair(1.12f, 1.5f, 0f);
                // Sideboard with decorations
                Cube("Sideboard", M("#4A3320"), 1.5f, 2.78f, 0f, 1.4f, 0.5f, 0.26f);
                Cyl("SideboardVase", M("#B8923F", 0.5f), 1.0f, 2.78f, 0.5f, 0.12f, 0.2f, false);
                Cyl("WineBottle", M("#2F3A28", 0.4f), 1.8f, 2.78f, 0.5f, 0.06f, 0.18f, false);
                Cyl("WineBottle2", M("#5A2F3A", 0.4f), 1.6f, 2.78f, 0.5f, 0.06f, 0.18f, false);
                Bowl(1.3f, 2.78f, 0.5f, "#D2AD62");
                SmallPlant(2.0f, 2.78f, 0.5f);
                // Fruit bowl on sideboard (a few spheres)
                Ball("Apple1", M("#8C3B3B", 0.15f), 1.35f, 2.78f, 0.56f, 0.05f);
                Ball("Apple2", M("#4C7A4C", 0.15f), 1.28f, 2.78f, 0.56f, 0.05f);
                Ball("Orange", M("#D2AD62", 0.15f), 1.32f, 2.78f, 0.58f, 0.04f);
                Plant(0.3f, 2.7f);
                Plant(0.3f, 0.3f);
                // Wall painting
                WallPainting(1.9f, 2.92f, 0.55f, 0.5f, 0.3f, "#5B3F2A", "#3A2E22");
                WallPainting(0.5f, 0.08f, 0.5f, 0.3f, 0.25f, "#B8923F", "#4A3828");
                // Wall clock
                WallClock(0.15f, 1.5f, 0.65f);
                // Curtains
                Curtains(2.5f, 0.08f, 0.8f, "#5E2430");
                // Corner cabinet
                Cube("CornerCabinet", M("#4A3320"), 3.5f, 0.3f, 0f, 0.5f, 0.65f, 0.3f);
                Cyl("CabinetDecanter", M("#A8B8C8", 0.7f), 3.5f, 0.3f, 0.65f, 0.08f, 0.14f, false);
            }

            void Kitchen()
            {
                CeilingFixture("flush");
                // Stove with pots and kettle
                Cube("Stove", M("#2B2B2E", 0.3f), 2.8f, 0.55f, 0f, 0.26f, 0.45f, 0.8f);
                Cyl("StoveCoil1", kit.Glow("#FF6A2A", 2f), 2.8f, 0.4f, 0.45f, 0.14f, 0.012f, false);
                Cyl("StoveCoil2", kit.Glow("#FF6A2A", 2f), 2.8f, 0.7f, 0.45f, 0.14f, 0.012f, false);
                Cyl("Pot", M("#8A9099", 0.5f), 2.8f, 0.4f, 0.46f, 0.16f, 0.12f, false);
                Cyl("Kettle", M("#8A9099", 0.5f), 2.8f, 0.7f, 0.46f, 0.12f, 0.1f, false);
                Lamp(P(2.8f, 0.55f, 0.7f), new Color(1f, 0.45f, 0.2f), 1.8f, 0.4f, "StoveGlow");
                // Counter with sink
                Cube("Counter", M("#6A6F73", 0.3f), 1.15f, 0.22f, 0f, 1.5f, 0.42f, 0.27f);
                Cube("Basin", M("#3A4650", 0.5f), 1.0f, 0.22f, 0.42f, 0.4f, 0.012f, 0.18f, false);
                Cyl("Faucet", M("#8A9099", 0.6f), 1.0f, 0.12f, 0.42f, 0.03f, 0.15f, false);
                // Cutting board, knife, spice jars
                Cube("CuttingBoard", M("#B8923F", 0.1f), 1.5f, 0.22f, 0.42f, 0.2f, 0.012f, 0.14f, false);
                Cube("Knife", M("#8A9099", 0.6f), 1.65f, 0.22f, 0.44f, 0.02f, 0.008f, 0.14f, false);
                Cyl("SpiceJar1", M("#9A5F3A", 0.3f), 1.8f, 0.22f, 0.42f, 0.04f, 0.07f, false);
                Cyl("SpiceJar2", M("#4C7A4C", 0.3f), 1.86f, 0.22f, 0.42f, 0.04f, 0.07f, false);
                // Plate stack on counter
                for (int i = 0; i < 4; i++)
                    Cyl("StackPlate" + i, M("#D8CBB0", 0.1f), 0.55f, 0.22f, 0.42f + i * 0.013f, 0.12f, 0.012f, false);
                // Cupboards
                Cube("CupboardL", M("#7C5A3A"), 0.55f, 2.78f, 0f, 0.8f, 0.6f, 0.26f);
                Cube("CupboardR", M("#7C5A3A"), 2.45f, 2.78f, 0f, 0.8f, 0.6f, 0.26f);
                // Hanging pan rack + pans
                Cube("PanRack", M("#2B2B2E"), 1.5f, 1.5f, 0.95f, 0.6f, 0.03f, 0.06f, false);
                Cyl("HangingPan1", M("#4A5058", 0.4f), 1.3f, 1.5f, 0.8f, 0.16f, 0.03f, false);
                Cyl("HangingPan2", M("#4A5058", 0.4f), 1.7f, 1.5f, 0.82f, 0.14f, 0.03f, false);
                Cyl("HangingPan3", M("#6A6F73", 0.4f), 1.5f, 1.5f, 0.78f, 0.12f, 0.03f, false);
                // Kitchen table with items
                Cube("KitchenTable", M("#7C5A3A", 0.2f), 1.5f, 1.5f, 0f, 0.6f, 0.35f, 0.5f);
                Cyl("KitMug", M("#D8CBB0"), 1.3f, 1.5f, 0.35f, 0.06f, 0.07f, false);
                Paper(1.6f, 1.3f, 0.35f, 8f);
                // Stool at kitchen table
                Cyl("Stool1", M("#5B3F2A"), 1.5f, 1.1f, 0f, 0.18f, 0.28f);
                Cyl("Stool2", M("#5B3F2A"), 1.5f, 1.9f, 0f, 0.18f, 0.28f);
                // Broom leaning against wall
                Cyl("BroomHandle", M("#B8923F", 0.1f), 0.15f, 1.0f, 0f, 0.02f, 0.9f, false);
                // Fruit bowl
                Bowl(1.4f, 1.65f, 0.35f, "#D8CBB0");
                Ball("Fruit1", M("#8C3B3B", 0.15f), 1.42f, 1.68f, 0.4f, 0.04f);
                Ball("Fruit2", M("#D2AD62", 0.15f), 1.38f, 1.62f, 0.4f, 0.04f);
                // Towel (thin slab on handle)
                Cube("TowelBar", M("#8A9099", 0.4f), 2.8f, 1.1f, 0.3f, 0.02f, 0.02f, 0.2f, false);
                Cube("Towel", M("#D8CBB0", 0.05f), 2.8f, 1.1f, 0.18f, 0.01f, 0.18f, 0.16f, false);
            }

            void Stairs()
            {
                CeilingFixture("lantern");
                // Lantern on stand
                Cyl("LanternPole", M("#2B2B2E"), 2.75f, 2.75f, 0f, 0.07f, 0.8f);
                Ball("Lantern", kit.Glow("#FFC97A", 2f), 2.75f, 2.75f, 0.9f, 0.22f);
                // Stair railing post
                Cyl("RailPost1", M("#5B3F2A"), 1.3f, 0.5f, 0f, 0.06f, 0.5f, false);
                Cyl("RailPost2", M("#5B3F2A"), 1.3f, 2.0f, 0f, 0.06f, 0.5f, false);
                Cube("Railing", M("#5B3F2A"), 1.3f, 1.25f, 0.48f, 0.04f, 0.04f, 1.5f * PS / DS, false);
                // Wall paintings along staircase
                WallPainting(2.5f, 0.08f, 0.4f, 0.22f, 0.18f, "#5B3F2A", "#3B5C8C");
                WallPainting(2.5f, 0.08f, 0.7f, 0.2f, 0.16f, "#4A3320", "#6E4A2E");
                // Small table with candle
                Cube("StairTable", M("#5B3F2A"), 2.8f, 0.3f, 0f, 0.3f, 0.35f, 0.25f);
                Candle(2.8f, 0.3f, 0.35f, "StairCandle");

                // --- Kthina eshte dy kate e larte: zbukurime qe ngjiten bashke me shkallet ---
                float[] vs = { 0.7f, 1.5f, 2.3f };
                string[] frames = { "#5B3F2A", "#4A3320", "#5B3F2A" };
                string[] arts = { "#3B5C8C", "#6E4A2E", "#2E5A4A" };
                for (int i = 0; i < vs.Length; i++)
                {
                    // Lartesia e hapit perballe kesaj pike, ne metra mbi dyshemene e kthines.
                    float h = (vs[i] * PS - 1.5f) * MapBuilder.UpperFloor / (l.Depth * 0.7f);
                    // Piktura ne murin lindor, 1.1 m mbi hapin perballe tyre.
                    float y0 = (h + 1.1f) / DS;
                    Cube("StairArtFrame" + i, M(frames[i], 0.3f), 2.96f, vs[i], y0, 0.03f, 0.54f, 0.44f, false);
                    Cube("StairArtCanvas" + i, M(arts[i], 0.05f), 2.93f, vs[i], y0 + 0.02f, 0.015f, 0.5f, 0.4f, false);
                    // Llambe muri mes pikturave
                    float sy = (h + 2.0f) / DS;
                    Cube("StairSconce" + i, M("#2B2B2E", 0.3f), 2.95f, vs[i] + 0.45f, sy, 0.06f, 0.1f, 0.08f, false);
                    Ball("StairBulb" + i, kit.Glow("#FFD79A", 1.8f), 2.88f, vs[i] + 0.45f, sy + 0.08f, 0.13f);
                }

                // Flamur i gjate ne murin verior, perballe atij qe ngjitet shkallet
                float bannerY = (MapBuilder.UpperFloor + 0.7f) / DS;
                Cube("StairBanner", M("#6E2F37", 0.1f), 1.5f, 2.96f, bannerY, 0.52f, 1.25f, 0.02f, false);
                Cube("StairBannerRod", M("#B8923F", 0.5f), 1.5f, 2.95f, bannerY + 1.25f, 0.62f, 0.04f, 0.05f, false);

                // Drite e dyte ne mes te kthines: tavani i larte nuk i ndrit dot hapat vete
                var midGo = new GameObject("StairMidLight");
                midGo.transform.SetParent(parent, false);
                midGo.transform.localPosition = new Vector3(l.Center.x, l.Y + MapBuilder.UpperFloor * 0.6f, l.Center.z);
                var midLight = midGo.AddComponent<Light>();
                midLight.type = LightType.Point;
                midLight.color = new Color(1f, 0.87f, 0.66f);
                midLight.intensity = 2.4f;
                midLight.range = Mathf.Max(l.Width, l.Depth) * 0.9f;
                midLight.shadows = LightShadows.None;
            }

            void Corridor()
            {
                CeilingFixture("flush");
                // Console table with vase
                Cube("Console", M("#5B3F2A"), 1.5f, 0.22f, 0f, 1.0f, 0.42f, 0.26f);
                Cyl("Vase", M("#B8923F", 0.5f), 1.5f, 0.22f, 0.42f, 0.12f, 0.18f, false);
                SmallPlant(1.8f, 0.22f, 0.42f);
                // Grandfather clock
                Cube("GrandfatherClock", M("#4A3320"), 0.22f, 1.5f, 0f, 0.28f, 1.0f, 0.3f);
                Cube("ClockFace", kit.Glow("#F0E6C8", 1.2f), 0.22f, 1.5f, 0.82f, 0.29f, 0.14f, 0.16f, false);
                // Benches along north wall
                Cube("BenchL", M("#5B3F2A"), 0.7f, 2.78f, 0f, 0.8f, 0.25f, 0.24f);
                Cube("BenchCushionL", M("#6E2F37", 0.1f), 0.7f, 2.76f, 0.25f, 0.6f, 0.06f, 0.2f, false);
                Cube("BenchR", M("#5B3F2A"), 2.9f, 2.78f, 0f, 0.8f, 0.25f, 0.24f);
                Cube("BenchCushionR", M("#2E3F5C", 0.1f), 2.9f, 2.76f, 0.25f, 0.6f, 0.06f, 0.2f, false);
                Plant(3.35f, 0.3f);
                Plant(0.3f, 0.3f);
                // Wall paintings
                WallPainting(2.0f, 0.08f, 0.5f, 0.35f, 0.25f, "#5B3F2A", "#4A3A28");
                WallPainting(1.0f, 2.92f, 0.5f, 0.3f, 0.2f, "#B8923F", "#2A3A4A");
                WallPainting(2.5f, 2.92f, 0.55f, 0.25f, 0.2f, "#4A3320", "#5A4A3A");
                // Runner rug
                Rug(1.8f, 1.5f, 2.5f, 0.5f, "#5E2E34");
                // Wall sconces (small glowing spheres on brackets)
                Cube("SconceBracketL", M("#B8923F", 0.4f), 0.9f, 0.08f, 0.6f, 0.04f, 0.06f, 0.06f, false);
                Ball("SconceGlowL", kit.Glow("#FFD9A0", 1.2f), 0.9f, 0.11f, 0.64f, 0.06f);
                Cube("SconceBracketR", M("#B8923F", 0.4f), 2.7f, 0.08f, 0.6f, 0.04f, 0.06f, 0.06f, false);
                Ball("SconceGlowR", kit.Glow("#FFD9A0", 1.2f), 2.7f, 0.11f, 0.64f, 0.06f);
            }

            void Office()
            {
                CeilingFixture("pendant");
                Rug(1.9f, 1.5f, 1.8f, 1.2f, "#2E3F5C");
                // Desk with many items
                Cube("Desk", M("#4A3320", 0.3f), 0.42f, 1.5f, 0f, 0.6f, 0.42f, 1.2f);
                TableLamp(0.3f, 1.1f, 0.42f, "#FFE9C4");
                Cube("Papers", M("#D8CBB0", 0.05f), 0.5f, 1.8f, 0.42f, 0.28f, 0.012f, 0.2f, false);
                Cube("Folder", M("#D8B365", 0.1f), 0.5f, 1.45f, 0.42f, 0.26f, 0.02f, 0.2f, false);
                Cyl("PenHolder", M("#2B2B2E"), 0.35f, 1.6f, 0.42f, 0.06f, 0.08f, false);
                Cyl("InkBottle", M("#1A1D24", 0.3f), 0.6f, 1.3f, 0.42f, 0.04f, 0.06f, false);
                // Letter opener
                Cube("LetterOpener", M("#B8923F", 0.5f), 0.35f, 1.75f, 0.42f, 0.015f, 0.005f, 0.12f, false);
                // Stamp pad
                Cube("StampPad", M("#8C3B3B", 0.1f), 0.3f, 1.9f, 0.42f, 0.06f, 0.015f, 0.08f, false);
                // Paperweight
                Ball("Paperweight", M("#3B5C8C", 0.6f), 0.6f, 1.75f, 0.45f, 0.04f);
                BookStack(0.65f, 1.1f, 0.42f, 3, 5);
                // Desk chair
                Cube("ChairSeat", M("#2B2B2E", 0.3f), 0.98f, 1.5f, 0f, 0.3f, 0.24f, 0.3f);
                Cube("ChairBack", M("#2B2B2E", 0.3f), 1.12f, 1.5f, 0.24f, 0.05f, 0.24f, 0.3f, false);
                // Bookshelf and file cabinet
                Shelf("Bookshelf", 1.3f, 2.79f, 1.4f, 0.26f, 0.8f, 2, true);
                Cube("FileCabinet", M("#6A6F73", 0.4f), 3.27f, 2.74f, 0f, 0.5f, 0.7f, 0.36f);
                // Safe
                Cube("Safe", M("#4A5058", 0.5f), 3.36f, 0.27f, 0f, 0.32f, 0.5f, 0.32f);
                Cyl("SafeDial", M("#B8923F", 0.6f), 3.36f, 0.27f, 0.5f, 0.12f, 0.02f, false);
                // Wall clock + paintings
                WallClock(2.0f, 2.92f, 0.6f);
                WallPainting(2.5f, 0.08f, 0.5f, 0.4f, 0.3f, "#4A3320", "#D8CBB0");
                WallPainting(3.0f, 2.92f, 0.5f, 0.25f, 0.2f, "#5B3F2A", "#2A3A4A");
                // Wastebasket
                Cyl("Wastebasket", M("#4A3320", 0.1f), 1.1f, 1.0f, 0f, 0.14f, 0.2f, false);
                // Floor plant
                Plant(3.5f, 2.5f);
                // Guest chair
                Cube("GuestChairSeat", M("#6E2F37", 0.2f), 2.0f, 0.5f, 0f, 0.3f, 0.22f, 0.3f);
                Cube("GuestChairBack", M("#6E2F37", 0.2f), 2.0f, 0.35f, 0.22f, 0.3f, 0.22f, 0.04f, false);
                // Curtains
                Curtains(1.5f, 0.08f, 0.7f, "#2E3F5C");
                // Small rug in front of desk
                Rug(0.9f, 1.5f, 0.4f, 0.5f, "#5E2E34");
                // Coat hook with hat
                Cube("CoatHook", M("#2B2B2E"), 3.6f, 1.5f, 0.75f, 0.06f, 0.04f, 0.04f, false);
            }

            void Cellar()
            {
                CeilingFixture("industrial");
                // Crates
                Cube("CrateA", M("#7C5A3A", 0.1f), 0.25f, 0.25f, 0f, 0.32f, 0.32f, 0.32f);
                Cube("CrateB", M("#8A6A44", 0.1f), 0.25f, 0.25f, 0.32f, 0.26f, 0.26f, 0.26f, false, 18f);
                Cube("CrateC", M("#6A5A3A", 0.1f), 0.6f, 0.25f, 0f, 0.3f, 0.28f, 0.3f);
                Cube("CrateD", M("#9A7A54", 0.1f), 1.5f, 2.5f, 0f, 0.28f, 0.28f, 0.28f);
                // Barrels
                Cyl("BarrelS", M("#5B3F2A"), 2.72f, 0.27f, 0f, 0.38f, 0.5f);
                Cyl("BarrelNW", M("#5B3F2A"), 0.3f, d - 0.28f, 0f, 0.38f, 0.5f);
                Cyl("BarrelNE", M("#5B3F2A"), 2.72f, d - 0.28f, 0f, 0.38f, 0.5f);
                Cyl("BarrelSmall", M("#7C5A3A"), 1.0f, d - 0.3f, 0f, 0.28f, 0.4f);
                // Wine racks with bottles
                Cube("WineRackW", M("#4A3320"), 0.2f, 1.6f, 0f, 0.24f, 0.7f, 1.0f);
                Cube("WineRackE", M("#4A3320"), 2.8f, 1.6f, 0f, 0.24f, 0.7f, 1.0f);
                for (int i = 0; i < 5; i++)
                {
                    Cyl("BottleW" + i, M("#2F5A3A", 0.5f), 0.2f, 1.2f + i * 0.18f, 0.7f, 0.07f, 0.2f, false);
                    Cyl("BottleE" + i, M("#5A2F3A", 0.5f), 2.8f, 1.2f + i * 0.18f, 0.7f, 0.07f, 0.2f, false);
                }
                Ball("Lantern", kit.Glow("#FFB45A", 2f), 0.2f, 1.6f, 0.82f, 0.18f);
                // Cobwebs
                Cube("Cobweb1", M("#A8A898", 0.02f), 2.9f, 2.9f, 0.9f, 0.2f, 0.15f, 0.01f, false);
                Cube("Cobweb2", M("#A8A898", 0.02f), 0.1f, 2.9f, 0.85f, 0.15f, 0.12f, 0.01f, false);
                // Dusty sacks
                Ball("Sack1", M("#7C6A54", 0.05f), 1.5f, 0.3f, 0.15f, 0.22f);
                Ball("Sack2", M("#6A5A44", 0.05f), 1.8f, 0.3f, 0.12f, 0.18f);
                // Old tools
                Cyl("ShovelHandle", M("#B8923F", 0.1f), 2.4f, 0.15f, 0f, 0.02f, 0.75f, false);
                Cube("ShovelBlade", M("#6A6F73", 0.3f), 2.4f, 0.15f, 0.75f, 0.1f, 0.08f, 0.015f, false);
                // Old chair
                Cube("OldChairSeat", M("#6A5A3A", 0.1f), 1.9f, 1.8f, 0f, 0.24f, 0.2f, 0.24f);
                Cube("OldChairBack", M("#6A5A3A", 0.1f), 1.9f, 1.92f, 0.2f, 0.24f, 0.2f, 0.03f, false);
                // Rope coil on floor
                Cyl("RopeCoil", M("#9A8A6A", 0.1f), 2.1f, 2.5f, 0f, 0.18f, 0.06f, false);
                // Dusty shelf with jars
                Cube("CellarShelf", M("#4A3320"), 1.5f, 0.15f, 0.5f, 1.0f, 0.03f, 0.22f, false);
                for (int i = 0; i < 4; i++)
                    Cyl("Jar" + i, M("#A8B8C8", 0.3f), 1.2f + i * 0.22f, 0.15f, 0.53f, 0.06f, 0.1f, false);
            }
        }
    }

    static class RoleLook
    {
        public static GameObject Attach(Transform figure, string suspectId, HouseDressing.Kit kit)
        {
            var g = new GameObject("RoleProps");
            g.transform.SetParent(figure, false);
            var t = g.transform;
            switch (suspectId)
            {
                case "A": // Arta, kuzhiniere
                    Disc(t, "Apron", 0.95f, 0.36f, 0.14f, kit.Mat("#EDE6D3", 0.1f));
                    Disc(t, "ToqueBase", 1.72f, 0.24f, 0.06f, kit.Mat("#F4F1E8", 0.1f));
                    Dome(t, "ToquePuff", 1.82f, 0.28f, 0.16f, 0.28f, kit.Mat("#F4F1E8", 0.1f));
                    break;
                case "B": // Blerimi, shofer
                    Dome(t, "CapDome", 1.66f, 0.24f, 0.1f, 0.24f, kit.Mat("#1F2A44", 0.2f));
                    Disc(t, "CapBrim", 1.62f, 0.3f, 0.016f, kit.Mat("#141A2C", 0.3f));
                    Disc(t, "CapBand", 1.63f, 0.245f, 0.025f, kit.Mat("#C9A44C", 0.5f));
                    Disc(t, "Belt", 0.88f, 0.32f, 0.025f, kit.Mat("#17130B", 0.3f));
                    break;
                case "D": // Drita, sekretare
                    Slab(t, "Folder", new Vector3(0.3f, 0.95f, 0f), new Vector3(0.04f, 0.24f, 0.18f), kit.Mat("#D8B365", 0.1f));
                    Slab(t, "FolderPaper", new Vector3(0.302f, 0.97f, 0f), new Vector3(0.04f, 0.21f, 0.15f), kit.Mat("#F4F1E8", 0.05f));
                    Dome(t, "HairBun", 1.68f, 0.1f, 0.1f, 0.1f, kit.Mat("#3B2A22", 0.2f));
                    break;
                case "E": // Elmazi, kopshtar
                    Disc(t, "Apron", 0.95f, 0.34f, 0.12f, kit.Mat("#3F6B3A", 0.1f));
                    Disc(t, "StrawBrim", 1.68f, 0.4f, 0.02f, kit.Mat("#D9C27A", 0.1f));
                    Disc(t, "StrawCrown", 1.72f, 0.22f, 0.06f, kit.Mat("#D9C27A", 0.1f));
                    break;
                case "F": // Fatmiri, nipi i pronarit
                    Disc(t, "Scarf", 1.38f, 0.26f, 0.06f, kit.Mat("#7A2433", 0.2f));
                    Slab(t, "ScarfTail", new Vector3(0f, 1.2f, -0.14f), new Vector3(0.06f, 0.18f, 0.03f), kit.Mat("#7A2433", 0.2f));
                    Disc(t, "WatchChain", 1.0f, 0.3f, 0.01f, kit.Mat("#C9A44C", 0.6f));
                    break;
                case "G": // Genta, ekonomiste
                    Slab(t, "Ledger", new Vector3(-0.3f, 0.95f, 0f), new Vector3(0.06f, 0.22f, 0.16f), kit.Mat("#1F3D2E", 0.1f));
                    Slab(t, "LedgerSpine", new Vector3(-0.33f, 0.95f, 0f), new Vector3(0.01f, 0.22f, 0.16f), kit.Mat("#C9A44C", 0.5f));
                    Disc(t, "Pendant", 1.38f, 0.2f, 0.01f, kit.Mat("#C9A44C", 0.6f));
                    break;
                default:
                    Object.Destroy(g);
                    return null;
            }
            return g;
        }

        static void Disc(Transform p, string name, float y, float dia, float h, Material m)
        {
            HouseDressing.Part(p, PrimitiveType.Cylinder, name, new Vector3(0, y, 0), new Vector3(dia, h * 0.5f, dia), m);
        }

        static void Dome(Transform p, string name, float y, float sx, float sy, float sz, Material m)
        {
            HouseDressing.Part(p, PrimitiveType.Sphere, name, new Vector3(0, y, 0), new Vector3(sx, sy, sz), m);
        }

        static void Dome(Transform p, string name, Vector3 pos, float dia, Material m)
        {
            HouseDressing.Part(p, PrimitiveType.Sphere, name, pos, new Vector3(dia, dia, dia), m);
        }

        static void Slab(Transform p, string name, Vector3 pos, Vector3 size, Material m)
        {
            HouseDressing.Part(p, PrimitiveType.Cube, name, pos, size, m);
        }
    }

    public sealed class RoleLookToggle : MonoBehaviour
    {
        public SuspectMarker Marker;
        public GameObject Group;

        void Update()
        {
            if (Marker == null || Group == null) return;
            bool on = Marker.State == MarkerState.Confirmed;
            if (Group.activeSelf != on) Group.SetActive(on);
        }
    }

    public sealed class HouseLife : MonoBehaviour
    {
        public VillaView View;
        public ResidentsMode Mode = ResidentsMode.Always;
        public readonly List<ResidentFigure> Residents = new List<ResidentFigure>();
    }

    public sealed class ResidentFigure : MonoBehaviour
    {
        public HouseLife Life;
        public int Suspect;
        public GameObject Visual;
        public GameObject NameLabel;

        void Update()
        {
            if (Life == null || Visual == null) return;
            bool show;
            switch (Life.Mode)
            {
                case ResidentsMode.Never: show = false; break;
                case ResidentsMode.Auto: show = !MarkerShown(); break;
                default: show = true; break;
            }
            if (Visual.activeSelf != show) Visual.SetActive(show);

            // Billboard: make name label always face the camera
            if (NameLabel != null && NameLabel.activeSelf)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    NameLabel.transform.rotation = Quaternion.LookRotation(
                        NameLabel.transform.position - cam.transform.position);
                }
            }
        }

        bool MarkerShown()
        {
            var markers = Life.View != null ? Life.View.Markers : null;
            if (markers == null) return false;
            for (int r = 0; r < markers.GetLength(1); r++)
            {
                var m = markers[Suspect, r];
                if (m != null && m.IsVisible) return true;
            }
            return false;
        }
    }
}
