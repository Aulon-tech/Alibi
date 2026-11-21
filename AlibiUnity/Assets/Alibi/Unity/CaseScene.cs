using System.Collections.Generic;
using Alibi.Core;
using UnityEngine;

namespace Alibi.Unity
{
    // Skena e krimit sipas kartës, mbi mobiljet e zakonshme të HouseDressing.
    //  · Karta 3 (Dorëshkrimi): Biblioteka — vitrina e thyer bosh, konturi me shkumës, faqe të shpërndara,
    //    karrige e rrëzuar, shenja provash dhe shirit policie te dera e Sallonit.
    //  · Karta 4 (Kalimi i fshehtë): Zyra — kasaforta e hapur, letra në dysheme, sirtar i nxjerrë;
    //    Biblioteka — libra të rënë dhe gërvishtje para raftit që duket i lëvizur.
    // Koordinatat janë në metra nga qoshja (X0, Z0) e dhomës; y mbi dyshemenë e saj.
    // Asnjë nga këto nuk tregon fajtorin: janë vetëm pamja e vendit të ngjarjes.
    public static class CaseScene
    {
        public static void Build(VillaView view)
        {
            if (view == null || view.Case == null) return;
            switch (view.Case.Id)
            {
                case "karta3":
                    Manuscript(view);
                    break;
                case "karta4":
                    Office(view);
                    MovedShelfMarks(view);
                    break;
            }
        }

        static RoomView Room(VillaView view, string id)
        {
            foreach (var kv in view.Rooms) if (kv.Value.Info.Id == id) return kv.Value;
            return null;
        }

        // ------------------------------------------------------------------ Karta 3
        static void Manuscript(VillaView view)
        {
            var rv = Room(view, "BI");
            if (rv == null) return;
            var s = new Scene(rv, "CaseScene_Karta3");

            // Vitrina e dorëshkrimit: piedestal, jastëk kadifeje bosh, xhama të hapur
            var wood = s.Mat("#3A2A1C", 0.3f);
            var glass = Theme.Ghost(new Color(0.75f, 0.86f, 0.95f, 0.22f));
            float px = 3.6f, pz = 9.2f;
            s.Box("Pedestal", wood, px, 0.5f, pz, 0.8f, 1.0f, 0.8f, true);
            s.Box("PedestalTop", s.Mat("#5B3F2A", 0.4f), px, 1.02f, pz, 0.86f, 0.04f, 0.86f);
            s.Box("VelvetCushion", s.Mat("#7A1E2A", 0.05f), px, 1.07f, pz, 0.55f, 0.06f, 0.4f);
            s.Box("Plaque", s.Mat("#C9A44C", 0.6f), px, 0.72f, pz - 0.41f, 0.3f, 0.08f, 0.01f);
            s.Box("GlassBack", glass, px, 1.27f, pz + 0.34f, 0.7f, 0.45f, 0.02f);
            s.Box("GlassLeft", glass, px - 0.34f, 1.27f, pz, 0.02f, 0.45f, 0.7f);
            s.Box("GlassRight", glass, px + 0.34f, 1.27f, pz, 0.02f, 0.45f, 0.7f);
            s.Box("GlassLid", glass, px + 0.25f, 1.55f, pz + 0.1f, 0.72f, 0.02f, 0.72f, false, 0f, 22f);
            // xhami i përparmë i thyer, copa në dysheme
            float[,] shards = { { -0.4f, -0.8f, 20f }, { 0.1f, -0.95f, 65f }, { 0.35f, -0.7f, 140f }, { -0.15f, -1.15f, 95f }, { 0.5f, -1.05f, 10f } };
            for (int i = 0; i < shards.GetLength(0); i++)
                s.Box("GlassShard" + i, glass, px + shards[i, 0], 0.012f, pz + shards[i, 1], 0.16f, 0.008f, 0.09f, false, shards[i, 2]);

            // Konturi me shkumës
            ChalkOutline(s, 4.9f, 7.2f, 18f);

            // Faqe të shpërndara nga dorëshkrimi
            var paper = s.Mat("#E6DCC3", 0.05f);
            float[,] pages = { { 2.9f, 8.4f, 15f }, { 4.4f, 8.6f, -30f }, { 3.4f, 7.6f, 60f }, { 5.9f, 8.3f, 5f }, { 2.6f, 9.9f, -70f }, { 6.3f, 7.0f, 40f }, { 4.1f, 6.2f, -10f } };
            for (int i = 0; i < pages.GetLength(0); i++)
                s.Box("Page" + i, paper, pages[i, 0], 0.016f, pages[i, 1], 0.22f, 0.004f, 0.3f, false, pages[i, 2]);

            // Karrige e rrëzuar
            var chair = s.Mat("#6E2F37", 0.2f);
            s.Box("FallenChairSeat", chair, 5.8f, 0.25f, 9.6f, 0.5f, 0.5f, 0.07f, true, 25f);
            s.Box("FallenChairBack", chair, 5.95f, 0.04f, 9.95f, 0.5f, 0.07f, 0.6f, false, 25f);

            // Shenjat e provave
            EvidenceTent(s, 3.0f, 8.1f);
            EvidenceTent(s, 5.7f, 8.0f);
            EvidenceTent(s, 4.1f, 6.5f);
            EvidenceTent(s, 6.0f, 6.6f);

            // Shirit policie te dera Biblioteka–Sallon (muri lindor), pa collider
            var tape = s.Mat("#E8C21A", 0.2f);
            float doorX = rv.Layout.Width - 0.18f;
            s.Box("PoliceTapeHigh", tape, doorX, 1.45f, 6.0f, 0.01f, 0.07f, 4.0f, false, 0f, 3f);
            s.Box("PoliceTapeLow", tape, doorX, 0.95f, 6.0f, 0.01f, 0.07f, 4.0f, false, 0f, -4f);

            // Dritë e ftohtë mbi vitrinë
            s.Spot("ManuscriptSpot", px, 3.8f, pz - 0.6f, new Color(0.85f, 0.92f, 1f), 3.2f, 8f, 55f);
        }

        // ------------------------------------------------------------------ Karta 4
        static void Office(VillaView view)
        {
            var rv = Room(view, "ZY");
            if (rv == null) return;
            var s = new Scene(rv, "CaseScene_Karta4");

            // Kasaforta e HouseDressing është në u 3.36, v 0.27 (x 13.09–13.79, z 0.73–1.43): e hapur, bosh.
            float sx0 = 13.09f, sfront = 1.43f;
            s.Box("SafeOpening", s.Mat("#0A0A0C", 0.05f), 13.44f, 0.55f, sfront + 0.005f, 0.58f, 0.9f, 0.02f);
            var hinge = s.Pivot("SafeDoorPivot", sx0, 0f, sfront + 0.03f, -100f);
            var steel = s.Mat("#4A5058", 0.5f);
            Scene.ChildBox(hinge, "SafeDoor", steel, new Vector3(0.33f, 0.55f, 0.03f), new Vector3(0.66f, 1.0f, 0.06f), true);
            Scene.ChildBox(hinge, "SafeDoorDial", s.Mat("#C9A44C", 0.6f), new Vector3(0.45f, 0.62f, 0.07f), new Vector3(0.12f, 0.12f, 0.02f), false);
            Scene.ChildBox(hinge, "SafeDoorHandle", s.Mat("#C9A44C", 0.6f), new Vector3(0.45f, 0.42f, 0.075f), new Vector3(0.16f, 0.03f, 0.03f), false);

            // Letra të hedhura nga kasaforta
            var paper = s.Mat("#E6DCC3", 0.05f);
            float[,] papers = { { 12.5f, 2.0f, 20f }, { 13.2f, 2.5f, -35f }, { 11.9f, 2.8f, 70f }, { 12.8f, 3.3f, 5f }, { 13.8f, 2.1f, -60f }, { 11.4f, 2.2f, 30f } };
            for (int i = 0; i < papers.GetLength(0); i++)
                s.Box("LoosePaper" + i, paper, papers[i, 0], 0.012f, papers[i, 1], 0.24f, 0.004f, 0.32f, false, papers[i, 2]);

            // Sirtar i tavolinës i nxjerrë (tavolina është në perëndim, x 1.02–2.34)
            var wood = s.Mat("#4A3320", 0.3f);
            s.Box("DeskDrawer", wood, 2.6f, 0.72f, 5.4f, 0.52f, 0.14f, 0.6f);
            s.Box("DrawerPapers", paper, 2.62f, 0.8f, 5.4f, 0.4f, 0.02f, 0.5f);

            EvidenceTent(s, 12.6f, 2.3f);
            EvidenceTent(s, 13.9f, 2.7f);
            EvidenceTent(s, 11.7f, 3.1f);
            EvidenceTent(s, 2.95f, 4.8f);

            s.Spot("SafeSpot", 12.9f, 3.6f, 2.4f, new Color(0.85f, 0.92f, 1f), 3.0f, 7f, 50f);
        }

        // Para raftit të Bibliotekës që fsheh kalimin: libra të rënë dhe gërvishtje në formë harku.
        static void MovedShelfMarks(VillaView view)
        {
            var rv = Room(view, "BI");
            if (rv == null) return;
            var s = new Scene(rv, "CaseScene_Karta4_Shelf");
            float z = MapBuilder.SecretDoorZ_BI;
            s.Box("FallenBookA", s.Mat("#8C3B3B", 0.1f), 0.75f, 0.04f, z - 0.55f, 0.24f, 0.07f, 0.34f, false, 25f);
            s.Box("FallenBookB", s.Mat("#3B5C8C", 0.1f), 1.05f, 0.04f, z + 0.35f, 0.22f, 0.07f, 0.3f, false, -40f);
            // harku që bën skaji i lirë i raftit kur rrotullohet rreth menteshës (në skajin jugor të derës)
            var scuff = s.Mat("#2A2018", 0.02f);
            float hingeV = z - MapBuilder.SecretDoorWidth * 0.5f;
            const float r = 1.9f;
            for (int i = 0; i < 4; i++)
            {
                float a = (12f + i * 20f) * Mathf.Deg2Rad;
                float u = 0.1f + Mathf.Sin(a) * r;
                float v = hingeV + Mathf.Cos(a) * r;
                float yaw = Mathf.Atan2(Mathf.Cos(a), -Mathf.Sin(a)) * Mathf.Rad2Deg;
                s.Box("Scuff" + i, scuff, u, 0.016f, v, 0.035f, 0.004f, 0.5f, false, yaw);
            }
        }

        // ------------------------------------------------------------------ pjesë të përbashkëta
        static void ChalkOutline(Scene s, float cx, float cz, float rotDeg)
        {
            var chalk = s.Mat("#F2F0EA", 0.02f);
            Vector2[] body =
            {
                new Vector2(-0.70f, 0.20f), new Vector2(-0.40f, 0.45f), new Vector2(-0.32f, 0.38f), new Vector2(-0.50f, 0.16f),
                new Vector2(0.15f, 0.17f), new Vector2(0.80f, 0.30f), new Vector2(0.82f, 0.20f), new Vector2(0.25f, 0.06f),
                new Vector2(0.25f, -0.06f), new Vector2(0.82f, -0.20f), new Vector2(0.80f, -0.30f), new Vector2(0.15f, -0.17f),
                new Vector2(-0.50f, -0.16f), new Vector2(-0.30f, -0.40f), new Vector2(-0.38f, -0.47f), new Vector2(-0.70f, -0.20f)
            };
            var pts = new List<Vector2>();
            foreach (var p in body) pts.Add(p * 1.1f);
            Strips(s, chalk, pts, true, cx, cz, rotDeg, "Chalk");
            var head = new List<Vector2>();
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                head.Add(new Vector2(-0.95f + Mathf.Cos(a) * 0.15f, Mathf.Sin(a) * 0.15f));
            }
            Strips(s, chalk, head, true, cx, cz, rotDeg, "ChalkHead");
        }

        static void Strips(Scene s, Material mat, List<Vector2> pts, bool closed, float cx, float cz, float rotDeg, string name)
        {
            var rot = Quaternion.Euler(0f, rotDeg, 0f);
            int count = closed ? pts.Count : pts.Count - 1;
            for (int i = 0; i < count; i++)
            {
                var a3 = rot * new Vector3(pts[i].x, 0f, pts[i].y);
                var b3 = rot * new Vector3(pts[(i + 1) % pts.Count].x, 0f, pts[(i + 1) % pts.Count].y);
                var d = b3 - a3;
                float len = d.magnitude;
                if (len < 0.01f) continue;
                float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                var mid = (a3 + b3) * 0.5f;
                s.Box(name + i, mat, cx + mid.x, 0.018f, cz + mid.z, 0.035f, 0.004f, len + 0.03f, false, yaw);
            }
        }

        static void EvidenceTent(Scene s, float u, float v)
        {
            var yellow = s.Mat("#E8C21A", 0.3f);
            var black = s.Mat("#17130B", 0.2f);
            // kartë e palosur: dy faqe të pjerrëta
            s.Box("EvidenceA", yellow, u, 0.13f, v - 0.05f, 0.2f, 0.24f, 0.01f, true, 0f, 22f);
            s.Box("EvidenceB", yellow, u, 0.13f, v + 0.05f, 0.2f, 0.24f, 0.01f, false, 0f, -22f);
            s.Box("EvidenceMark", black, u, 0.15f, v - 0.105f, 0.08f, 0.1f, 0.004f, false, 0f, 22f);
        }

        // Ndihmës për ndërtimin e sendeve në koordinatat e dhomës.
        sealed class Scene
        {
            readonly RoomView rv;
            readonly Transform root;
            readonly RoomLayout l;
            readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

            public Scene(RoomView rv, string name)
            {
                this.rv = rv;
                l = rv.Layout;
                var go = new GameObject(name);
                go.transform.SetParent(rv.transform, false);
                root = go.transform;
            }

            public Material Mat(string hex, float smooth)
            {
                string key = hex + "|" + smooth;
                Material m;
                if (!mats.TryGetValue(key, out m) || m == null)
                {
                    m = Theme.Lit(Theme.Hex(hex), smooth);
                    mats[key] = m;
                }
                return m;
            }

            Vector3 P(float u, float y, float v)
            {
                return new Vector3(l.X0 + u, l.Y + y, l.Z0 + v);
            }

            // (u, y, v) = qendra; solid = me collider që përcjell klikimin te dhoma.
            public GameObject Box(string name, Material mat, float u, float y, float v, float sx, float sy, float sz,
                bool solid = false, float yaw = 0f, float pitch = 0f, float roll = 0f)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(root, false);
                go.transform.localPosition = P(u, y, v);
                go.transform.localRotation = Quaternion.Euler(pitch, yaw, roll);
                go.transform.localScale = new Vector3(sx, sy, sz);
                go.GetComponent<Renderer>().sharedMaterial = mat;
                var col = go.GetComponent<Collider>();
                if (solid) go.AddComponent<RoomClickTarget>().Room = rv;
                else if (col != null) Object.Destroy(col);
                return go;
            }

            public Transform Pivot(string name, float u, float y, float v, float yaw)
            {
                var go = new GameObject(name);
                go.transform.SetParent(root, false);
                go.transform.localPosition = P(u, y, v);
                go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                return go.transform;
            }

            public static void ChildBox(Transform parent, string name, Material mat, Vector3 pos, Vector3 size, bool solid)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(parent, false);
                go.transform.localPosition = pos;
                go.transform.localScale = size;
                go.GetComponent<Renderer>().sharedMaterial = mat;
                if (!solid) Object.Destroy(go.GetComponent<Collider>());
            }

            public void Spot(string name, float u, float y, float v, Color color, float intensity, float range, float angle)
            {
                var go = new GameObject(name);
                go.transform.SetParent(root, false);
                go.transform.localPosition = P(u, y, v);
                go.transform.localRotation = Quaternion.Euler(78f, 0f, 0f);
                var light = go.AddComponent<Light>();
                light.type = LightType.Spot;
                light.color = color;
                light.intensity = intensity;
                light.range = range;
                light.spotAngle = angle;
                light.shadows = LightShadows.Soft;
            }
        }
    }
}
