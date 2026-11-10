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

        // Çdo shënues i të dyshuarit merr detajet e rolit; shfaqen vetëm kur shënuesi është i konfirmuar
        // (shënuesit "e mundshëm" janë të tejdukshëm dhe nuk duhet të kenë pjesë të forta).
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

        static void SpawnResidents(VillaView view, Transform parent, Kit kit, HouseLife life)
        {
            var c = view.Case;
            var rooms = HouseSpawns.Assign(c);
            var perRoom = new int[c.RoomCount];
            for (int s = 0; s < c.N; s++)
            {
                int r = rooms[s];
                RoomView rv;
                if (!view.Rooms.TryGetValue(r, out rv)) continue;
                var pos = HouseSpawns.PositionIn(rv.Layout);
                pos.x += 0.45f * perRoom[r]++; // vetëm nëse dy persona ndajnë dhomë (më pak dhoma se të dyshuar)
                var info = c.Suspects[s];
                var go = new GameObject("Resident_" + info.Id);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = pos;
                var visual = new GameObject("Visual");
                visual.transform.SetParent(go.transform, false);
                visual.transform.localScale = Vector3.one * 0.92f;
                var lit = kit.Mat(info.ColorHex, 0.35f);
                var body = Part(visual.transform, PrimitiveType.Capsule, "Body", new Vector3(0, 0.4f, 0), new Vector3(0.36f, 0.4f, 0.36f), lit);
                var head = Part(visual.transform, PrimitiveType.Sphere, "Head", new Vector3(0, 0.95f, 0), new Vector3(0.28f, 0.28f, 0.28f), lit);
                RoleLook.Attach(visual.transform, info.Id, kit);
                var res = go.AddComponent<ResidentFigure>();
                res.Life = life;
                res.Suspect = s;
                res.Visual = visual;
                life.Residents.Add(res);
            }
        }

        // Pjesë pa collider (dekor mbi figurë).
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

        // Materialet ndahen mes mobiljeve dhe figurave (Theme.Lit krijon material të ri çdo herë).
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

        // Mobiljet e një dhome. Koordinatat (u, v) maten nga qoshja (X0, Z0) e dhomës; y0 = lartësia mbi dysheme.
        // Mobiljet qëndrojnë te muret (brenda 0.35 m) dhe nuk prekin dyert, kalimin e fshehtë Biblioteka–Zyra,
        // as rreshtat e figurave të Slot(): kështu klikimet dhe shënuesit mbeten të lirë.
        sealed class Dresser
        {
            readonly Kit kit;
            readonly RoomView rv;
            readonly Transform parent;
            readonly RoomLayout l;
            readonly float w, d;

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
                return new Vector3(l.X0 + u, l.Y + y, l.Z0 + v);
            }

            void Finish(GameObject go, bool solid)
            {
                var col = go.GetComponent<Collider>();
                if (solid)
                {
                    // PhysicsRaycaster: mobiljet nuk duhet ta bllokojnë klikimin mbi dhomën.
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
                go.transform.localScale = new Vector3(sx, sy, sz);
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
                go.transform.localScale = new Vector3(dia, h * 0.5f, dia);
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
                go.transform.localScale = new Vector3(dia, dia, dia);
                go.GetComponent<Renderer>().sharedMaterial = mat;
                Finish(go, false);
                return go;
            }

            // Tapet: aq të hollë sa të mos mbulojnë theksimin e dhomës (0.012) as vendin e krimit (0.01).
            void Rug(float u, float v, float sx, float sz, string hex)
            {
                Cube("Rug", M(hex, 0.05f), u, v, 0f, sx, 0.006f, sz, false);
            }

            void Plant(float u, float v)
            {
                Cyl("PlantPot", M("#8A4B32"), u, v, 0f, 0.32f, 0.26f);
                Ball("PlantLeaves", M("#3F7A45"), u, v, 0.46f, 0.4f);
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
                // backSide: -1 / +1 = anën v ku është mbështetësja, 0 = pa mbështetëse
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
                light.range = range;
                light.intensity = intensity;
                light.shadows = LightShadows.None;
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

            // Një dritë e ngrohtë në çdo dhomë, mbi mes të saj.
            void RoomLight()
            {
                Color color = new Color(1f, 0.8f, 0.55f);
                float intensity = 0.9f;
                switch (l.Id)
                {
                    case "KU": color = new Color(1f, 0.86f, 0.64f); break;
                    case "BI": color = new Color(1f, 0.74f, 0.45f); break;
                    case "ZY": color = new Color(1f, 0.87f, 0.68f); break;
                    case "BO": color = new Color(1f, 0.66f, 0.38f); intensity = 0.7f; break;
                    case "SH": intensity = 0.7f; break;
                }
                float range = Mathf.Max(w, d) * 1.1f + 0.7f;
                Lamp(P(w * 0.5f, d * 0.5f, 1.45f), color, range, intensity, "RoomLight_" + l.Id);
            }

            void Hall()
            {
                Rug(2.0f, 1.5f, 1.8f, 1.1f, "#6E2F37");
                Cube("Console", M("#5B3F2A"), 2.0f, 0.22f, 0f, 1.0f, 0.42f, 0.26f);
                Cyl("Vase", M("#B8923F", 0.5f), 2.0f, 0.22f, 0.42f, 0.12f, 0.18f, false);
                Plant(0.3f, 0.3f);
                Plant(3.7f, 0.3f);
                Cyl("CoatStand", M("#5B3F2A"), 3.7f, 2.7f, 0f, 0.14f, 1.0f);
                Cyl("CoatStandBase", M("#2B2B2E"), 3.7f, 2.7f, 0f, 0.3f, 0.05f, false);
                FloorLamp(0.3f, 2.7f);
            }

            void Salon()
            {
                Rug(2.2f, 1.5f, 2.4f, 1.5f, "#2E3F5C");
                // divan në murin veriorë
                Cube("SofaSeat", M("#6E2F37", 0.2f), 2.0f, 2.8f, 0f, 1.7f, 0.22f, 0.24f);
                Cube("SofaBack", M("#6E2F37", 0.2f), 2.0f, 2.885f, 0f, 1.7f, 0.5f, 0.07f);
                Cube("SofaArmL", M("#6E2F37", 0.2f), 1.22f, 2.8f, 0f, 0.14f, 0.32f, 0.24f);
                Cube("SofaArmR", M("#6E2F37", 0.2f), 2.78f, 2.8f, 0f, 0.14f, 0.32f, 0.24f);
                // tavolinë kafeje
                Cube("CoffeeTable", M("#7C5A3A", 0.3f), 1.1f, 1.5f, 0f, 0.8f, 0.3f, 0.45f);
                Cyl("CoffeeCup", M("#D8CBB0"), 1.0f, 1.5f, 0.3f, 0.09f, 0.07f, false);
                // oxhak në murin jugor
                Cube("Fireplace", M("#6E6A64", 0.1f), 3.2f, 0.22f, 0f, 0.9f, 0.8f, 0.28f);
                Cube("Fire", kit.Glow("#FF7A2E", 2.2f), 3.2f, 0.375f, 0.08f, 0.4f, 0.22f, 0.03f, false);
                Lamp(P(3.2f, 0.7f, 0.5f), new Color(1f, 0.5f, 0.2f), 2.2f, 0.5f, "FireGlow");
                // tavolinë anësore me llambë
                Cube("SideTable", M("#5B3F2A"), 3.55f, 2.68f, 0f, 0.4f, 0.45f, 0.4f);
                TableLamp(3.55f, 2.68f, 0.45f);
                Plant(0.3f, 2.7f);
            }

            void Library()
            {
                Rug(1.5f, 1.5f, 1.5f, 1.1f, "#5E2E34");
                // rafte në veri
                Shelf("ShelfN1", 0.9f, 2.79f, 1.1f, 0.26f, 0.85f, 1, true);
                Shelf("ShelfN2", 2.1f, 2.79f, 1.1f, 0.26f, 0.85f, 4, true);
                // rafte në perëndim, me hapësirë për kalimin e fshehtë (v 1.0–2.0)
                Shelf("ShelfW1", 0.22f, 0.55f, 0.28f, 0.8f, 0.85f, 0, false);
                Shelf("ShelfW2", 0.22f, 2.3f, 0.28f, 0.5f, 0.85f, 0, false);
                // rafte në lindje, me hapësirë për derën (v 1–2)
                Shelf("ShelfE1", 2.8f, 0.52f, 0.26f, 0.74f, 0.85f, 0, false);
                Shelf("ShelfE2", 2.8f, 2.3f, 0.26f, 0.5f, 0.85f, 0, false);
                // tavolinë leximi me llambë
                Cube("ReadingTable", M("#7C5A3A", 0.3f), 1.9f, 1.5f, 0f, 0.7f, 0.32f, 0.5f);
                Cube("OpenBook", M("#D8CBB0", 0.1f), 1.78f, 1.5f, 0.32f, 0.2f, 0.02f, 0.14f, false);
                TableLamp(2.1f, 1.5f, 0.32f, "#FFC97A");
            }

            void Dining()
            {
                Rug(1.9f, 1.5f, 1.9f, 1.3f, "#5E2430");
                Cube("DiningTable", M("#7C5A3A", 0.3f), 1.9f, 1.5f, 0f, 1.2f, 0.32f, 0.6f);
                Cube("TableCloth", M("#D8CBB0", 0.05f), 1.9f, 1.5f, 0.32f, 1.3f, 0.01f, 0.5f, false);
                Cyl("Candle1", kit.Glow("#FFE2A8", 2f), 1.6f, 1.5f, 0.33f, 0.05f, 0.13f, false);
                Cyl("Candle2", kit.Glow("#FFE2A8", 2f), 2.2f, 1.5f, 0.33f, 0.05f, 0.13f, false);
                Chair(1.55f, 0.95f, -1f);
                Chair(2.25f, 0.95f, -1f);
                Chair(1.55f, 2.05f, 1f);
                Chair(2.25f, 2.05f, 1f);
                Chair(2.68f, 1.5f, 0f);
                Cube("Sideboard", M("#4A3320"), 1.5f, 2.78f, 0f, 1.4f, 0.5f, 0.26f);
                Cyl("SideboardVase", M("#B8923F", 0.5f), 1.0f, 2.78f, 0.5f, 0.12f, 0.2f, false);
                Plant(0.3f, 2.7f);
            }

            void Kitchen()
            {
                // sobë në lindje
                Cube("Stove", M("#2B2B2E", 0.3f), 2.8f, 0.55f, 0f, 0.26f, 0.45f, 0.8f);
                Cyl("StoveCoil1", kit.Glow("#FF6A2A", 2f), 2.8f, 0.4f, 0.45f, 0.14f, 0.012f, false);
                Cyl("StoveCoil2", kit.Glow("#FF6A2A", 2f), 2.8f, 0.7f, 0.45f, 0.14f, 0.012f, false);
                Cyl("Pot", M("#8A9099", 0.5f), 2.8f, 0.4f, 0.46f, 0.16f, 0.12f, false);
                Lamp(P(2.8f, 0.55f, 0.7f), new Color(1f, 0.45f, 0.2f), 1.8f, 0.4f, "StoveGlow");
                // banak me lavaman në jug
                Cube("Counter", M("#6A6F73", 0.3f), 1.15f, 0.22f, 0f, 1.5f, 0.42f, 0.27f);
                Cube("Basin", M("#3A4650", 0.5f), 1.0f, 0.22f, 0.42f, 0.4f, 0.012f, 0.18f, false);
                Cyl("Faucet", M("#8A9099", 0.6f), 1.0f, 0.12f, 0.42f, 0.03f, 0.15f, false);
                // dollapë në veri, me hapësirë për derën (u 1–2)
                Cube("CupboardL", M("#7C5A3A"), 0.55f, 2.78f, 0f, 0.8f, 0.6f, 0.26f);
                Cube("CupboardR", M("#7C5A3A"), 2.45f, 2.78f, 0f, 0.8f, 0.6f, 0.26f);
            }

            void Stairs()
            {
                // shkallët zënë pjesën majtas; fener në qoshen e lirë
                Cyl("LanternPole", M("#2B2B2E"), 2.75f, 2.75f, 0f, 0.07f, 0.8f);
                Ball("Lantern", kit.Glow("#FFC97A", 2f), 2.75f, 2.75f, 0.9f, 0.22f);
            }

            void Corridor()
            {
                Cube("Console", M("#5B3F2A"), 1.5f, 0.22f, 0f, 1.0f, 0.42f, 0.26f);
                Cyl("Vase", M("#B8923F", 0.5f), 1.5f, 0.22f, 0.42f, 0.12f, 0.18f, false);
                Cube("GrandfatherClock", M("#4A3320"), 0.22f, 1.5f, 0f, 0.28f, 1.0f, 0.3f);
                Cube("ClockFace", kit.Glow("#F0E6C8", 1.2f), 0.22f, 1.5f, 0.82f, 0.29f, 0.14f, 0.16f, false);
                Cube("BenchL", M("#5B3F2A"), 0.7f, 2.78f, 0f, 0.8f, 0.25f, 0.24f);
                Cube("BenchR", M("#5B3F2A"), 2.9f, 2.78f, 0f, 0.8f, 0.25f, 0.24f);
                Plant(3.35f, 0.3f);
            }

            void Office()
            {
                Rug(1.9f, 1.5f, 1.8f, 1.2f, "#2E3F5C");
                // tavolinë pune në perëndim, me karrige dhe llambë
                Cube("Desk", M("#4A3320", 0.3f), 0.42f, 1.5f, 0f, 0.6f, 0.42f, 1.2f);
                TableLamp(0.3f, 1.1f, 0.42f, "#FFE9C4");
                Cube("Papers", M("#D8CBB0", 0.05f), 0.5f, 1.8f, 0.42f, 0.28f, 0.012f, 0.2f, false);
                Cube("Folder", M("#D8B365", 0.1f), 0.5f, 1.45f, 0.42f, 0.26f, 0.02f, 0.2f, false);
                Cube("ChairSeat", M("#2B2B2E", 0.3f), 0.98f, 1.5f, 0f, 0.3f, 0.24f, 0.3f);
                Cube("ChairBack", M("#2B2B2E", 0.3f), 1.12f, 1.5f, 0.24f, 0.05f, 0.24f, 0.3f, false);
                // raft dhe kabinet në veri
                Shelf("Bookshelf", 1.3f, 2.79f, 1.4f, 0.26f, 0.8f, 2, true);
                Cube("FileCabinet", M("#6A6F73", 0.4f), 3.27f, 2.74f, 0f, 0.5f, 0.7f, 0.36f);
                // kasaforte në qoshen juglindore (larg kalimit të fshehtë)
                Cube("Safe", M("#4A5058", 0.5f), 3.36f, 0.27f, 0f, 0.32f, 0.5f, 0.32f);
                Cyl("SafeDial", M("#B8923F", 0.6f), 3.36f, 0.27f, 0.5f, 0.12f, 0.02f, false);
            }

            void Cellar()
            {
                Cube("CrateA", M("#7C5A3A", 0.1f), 0.25f, 0.25f, 0f, 0.32f, 0.32f, 0.32f);
                Cube("CrateB", M("#8A6A44", 0.1f), 0.25f, 0.25f, 0.32f, 0.26f, 0.26f, 0.26f, false, 18f);
                Cyl("BarrelS", M("#5B3F2A"), 2.72f, 0.27f, 0f, 0.38f, 0.5f);
                Cyl("BarrelNW", M("#5B3F2A"), 0.3f, d - 0.28f, 0f, 0.38f, 0.5f);
                Cyl("BarrelNE", M("#5B3F2A"), 2.72f, d - 0.28f, 0f, 0.38f, 0.5f);
                Cube("WineRackW", M("#4A3320"), 0.2f, 1.6f, 0f, 0.24f, 0.7f, 1.0f);
                Cube("WineRackE", M("#4A3320"), 2.8f, 1.6f, 0f, 0.24f, 0.7f, 1.0f);
                for (int i = 0; i < 4; i++)
                {
                    Cyl("BottleW" + i, M("#2F5A3A", 0.5f), 0.2f, 1.25f + i * 0.2f, 0.7f, 0.07f, 0.2f, false);
                    Cyl("BottleE" + i, M("#5A2F3A", 0.5f), 2.8f, 1.25f + i * 0.2f, 0.7f, 0.07f, 0.2f, false);
                }
                Ball("Lantern", kit.Glow("#FFB45A", 2f), 0.2f, 1.6f, 0.82f, 0.18f);
            }
        }
    }

    // Detajet e rolit mbi figurë (shënues ose banor). Koordinatat janë ato të SuspectMarker:
    // trupi y 0–0.8 (rrezja 0.18), koka y 0.81–1.09. Pjesët janë rrathë/kapele që duken nga çdo kënd,
    // pasi kamera rrotullohet.
    static class RoleLook
    {
        public static GameObject Attach(Transform figure, string suspectId, HouseDressing.Kit kit)
        {
            var g = new GameObject("RoleProps");
            g.transform.SetParent(figure, false);
            var t = g.transform;
            switch (suspectId)
            {
                case "A": // Arta, kuzhiniere: kapelë kuzhinieri + përparëse e bardhë
                    Disc(t, "Apron", 0.36f, 0.395f, 0.32f, kit.Mat("#EDE6D3", 0.1f));
                    Disc(t, "ToqueBase", 1.19f, 0.27f, 0.16f, kit.Mat("#F4F1E8", 0.1f));
                    Dome(t, "ToquePuff", 1.32f, 0.36f, 0.2f, 0.36f, kit.Mat("#F4F1E8", 0.1f));
                    break;
                case "B": // Blerimi, shofer: kapele shoferi + rrip lëkure
                    Dome(t, "CapDome", 1.05f, 0.31f, 0.17f, 0.31f, kit.Mat("#1F2A44", 0.2f));
                    Disc(t, "CapBrim", 0.985f, 0.38f, 0.016f, kit.Mat("#141A2C", 0.3f));
                    Disc(t, "CapBand", 1.0f, 0.305f, 0.035f, kit.Mat("#C9A44C", 0.5f));
                    Disc(t, "Belt", 0.38f, 0.375f, 0.035f, kit.Mat("#17130B", 0.3f));
                    break;
                case "D": // Drita, sekretare: dosje në dorë + topuz
                    Slab(t, "Folder", new Vector3(0.25f, 0.5f, 0f), new Vector3(0.05f, 0.3f, 0.22f), kit.Mat("#D8B365", 0.1f));
                    Slab(t, "FolderPaper", new Vector3(0.252f, 0.52f, 0f), new Vector3(0.05f, 0.27f, 0.19f), kit.Mat("#F4F1E8", 0.05f));
                    Dome(t, "Hand", new Vector3(0.23f, 0.34f, 0f), 0.1f, kit.Mat("#E3B58F", 0.2f));
                    Dome(t, "HairBun", 1.12f, 0.13f, 0.13f, 0.13f, kit.Mat("#3B2A22", 0.2f));
                    break;
                case "E": // Elmazi, kopshtar: kapelë kashte + përparëse jeshile
                    Disc(t, "Apron", 0.34f, 0.395f, 0.3f, kit.Mat("#3F6B3A", 0.1f));
                    Disc(t, "StrawBrim", 1.02f, 0.52f, 0.02f, kit.Mat("#D9C27A", 0.1f));
                    Disc(t, "StrawCrown", 1.07f, 0.26f, 0.08f, kit.Mat("#D9C27A", 0.1f));
                    break;
                case "F": // Fatmiri, nipi i pronarit: shall bordo + zinxhir ore ari + flokë
                    Disc(t, "Scarf", 0.7f, 0.34f, 0.09f, kit.Mat("#7A2433", 0.2f));
                    Slab(t, "ScarfTail", new Vector3(0f, 0.52f, -0.17f), new Vector3(0.07f, 0.24f, 0.03f), kit.Mat("#7A2433", 0.2f));
                    Disc(t, "WatchChain", 0.45f, 0.37f, 0.012f, kit.Mat("#C9A44C", 0.6f));
                    Dome(t, "Hair", 1.03f, 0.3f, 0.14f, 0.3f, kit.Mat("#3B2A22", 0.2f));
                    break;
                case "G": // Genta, ekonomiste: libër llogarish + varëse ari + flokë
                    Slab(t, "Ledger", new Vector3(-0.25f, 0.5f, 0f), new Vector3(0.07f, 0.27f, 0.2f), kit.Mat("#1F3D2E", 0.1f));
                    Slab(t, "LedgerSpine", new Vector3(-0.285f, 0.5f, 0f), new Vector3(0.012f, 0.27f, 0.2f), kit.Mat("#C9A44C", 0.5f));
                    Disc(t, "Pendant", 0.72f, 0.3f, 0.012f, kit.Mat("#C9A44C", 0.6f));
                    Dome(t, "Hair", 1.02f, 0.31f, 0.2f, 0.31f, kit.Mat("#5A4630", 0.2f));
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

    // Shënuesit "e mundshëm" janë të tejdukshëm, ndaj detajet e rolit shfaqen vetëm kur janë të konfirmuar.
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

    // Figurë banori që qëndron në dhomën e vet. Nuk ka collider, ndaj nuk pengon klikimet.
    public sealed class ResidentFigure : MonoBehaviour
    {
        public HouseLife Life;
        public int Suspect;
        public GameObject Visual;

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
