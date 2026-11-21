using System.Collections.Generic;
using Alibi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Alibi.Unity
{
    // Shtepia e gjalle: detektivi ecen neper vile me tastiere dhe pyet te dyshuarit nga afer.
    // Figurat jane SuspectFigure (Aulon), dhomat dhe kostumet vijne nga HouseSpawns / HouseDressing (Nysret).
    public sealed class ExploreMode : MonoBehaviour
    {
        const float NearDist = 1.3f;
        const float FarDist = 1.8f;
        const float PassageDist = 0.9f;
        const float DrawerHidden = 760f;
        const float Pitch = 50f;
        const float Distance = 4.6f;
        const float FigureScale = 2.6f;
        const float BannerSeconds = 2.2f;

        sealed class Live
        {
            public SuspectFigure Fig;
            public int Suspect;
            public Vector3 Pos;
            public Transform Mark;
        }

        GameManager gm;
        CaseDefinition c;
        VillaView villa;
        Camera cam;
        PlayerController player;
        HouseDressing.Kit kit;
        readonly List<Live> figures = new List<Live>();
        RectTransform layer, rightCol, leftCol;
        Vector2 rightBase;
        float drawerOffset = DrawerHidden;
        RectTransform nameRt, bubbleRt;
        Text nameText, nameSub, bubbleText, hintText, bannerText;
        Transform ring;
        int near = -1;
        int bubbleFor = -1;
        float bubbleUntil;
        int lastRoom = -2;
        float bannerUntil;
        bool boardOpen;
        float yaw;
        bool camReady;
        bool hasPassage;
        Vector3 passageBI, passageZY;
        GameSession lastSession;

        public void Init(GameManager manager)
        {
            gm = manager;
            c = gm.Case;
            villa = gm.Villa;
            cam = gm.Rig.Cam;
            kit = new HouseDressing.Kit();
            gm.Rig.enabled = false;
            cam.orthographic = false;
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.05f;
            var catcher = villa.GetComponentInChildren<GroundCatcher>();
            if (catcher != null)
            {
                var col = catcher.GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
            HouseDressing.SetResidentsMode(villa, ResidentsMode.Never);
            BuildRamps();
            SpawnFigures();
            SpawnPlayer();
            BuildRing();
            SetupPassage();
            BuildUI();
            gm.Answered += OnAnswered;
        }

        bool Has(string roomId)
        {
            return c.RoomIndex(roomId) >= 0;
        }

        Vector3 World(Vector3 local)
        {
            return villa.transform.TransformPoint(local);
        }

        GameObject Solid(string name, Vector3 center, Vector3 size, Quaternion rot)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(villa.transform, false);
            go.transform.localPosition = center;
            go.transform.localRotation = rot;
            go.transform.localScale = size;
            Object.Destroy(go.GetComponent<Renderer>());
            return go;
        }

        void BuildRamps()
        {
            if (Has("SH") && Has("KO"))
            {
                float run = 2.73f, rise = MapBuilder.UpperFloor;
                float angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
                float len = Mathf.Sqrt(run * run + rise * rise);
                Solid("RampaShkallet", new Vector3(run * 0.5f, rise * 0.5f, 1.5f), new Vector3(len, 0.06f, 1.3f), Quaternion.Euler(0, 0, -angle));
                for (int s = 1; s < 6; s++)
                {
                    float h = 0.27f * (s + 1) - 0.35f;
                    Solid("NenShkalle" + s, new Vector3(2.31f - s * 0.42f, h * 0.5f, 1.5f), new Vector3(0.42f, h, 1.3f), Quaternion.identity);
                }
            }
            if (Has("SH") && Has("BO"))
            {
                var bo = MapBuilder.Layout("BO");
                float run = 2.4f, drop = -bo.Y;
                float angle = Mathf.Atan2(drop, run) * Mathf.Rad2Deg;
                float len = Mathf.Sqrt(run * run + drop * drop);
                Solid("RampaBodrumi", new Vector3(1.5f, -drop * 0.5f, -run * 0.5f), new Vector3(1.4f, 0.06f, len), Quaternion.Euler(-angle, 0, 0));
                float wallH = drop - 0.12f;
                Solid("MuriNenShkalle", new Vector3(1.5f, bo.Y + wallH * 0.5f, 0.07f), new Vector3(3f, wallH, 0.1f), Quaternion.identity);
            }
        }

        // Detektivi nis ne Holl, ne piken me larg nga te dyshuarit (te Karta 1 Drita qendron ne Holl).
        void SpawnPlayer()
        {
            string start = Has("HO") ? "HO" : c.Rooms[0].Id;
            var l = MapBuilder.Layout(start);
            Vector3[] spots =
            {
                new Vector3(l.Center.x, l.Y, l.Center.z - 0.4f),
                new Vector3(l.X0 + 0.6f, l.Y, l.Z0 + 2.2f),
                new Vector3(l.X1 - 0.6f, l.Y, l.Z0 + 2.2f),
                new Vector3(l.X0 + 0.7f, l.Y, l.Z0 + 1.0f)
            };
            Vector3 best = spots[0];
            float bestD = -1f;
            foreach (var sp in spots)
            {
                var w = World(sp);
                float d = float.MaxValue;
                foreach (var f in figures)
                {
                    float dy;
                    d = Mathf.Min(d, Flat(w, f.Pos, out dy));
                }
                if (d >= 1.9f)
                {
                    best = sp;
                    break;
                }
                if (d > bestD)
                {
                    bestD = d;
                    best = sp;
                }
            }
            player = PlayerController.Create(villa.transform, World(best));
            player.CameraYaw = () => yaw;
        }

        // Nje person per dhome (HouseSpawns), figura e animuar e Aulonit e zmadhuar ne masen e viles,
        // me kostumin e rolit (RoleLook) dhe nje pengese te padukshme qe detektivi mos te kaloje neper te.
        void SpawnFigures()
        {
            var rooms = HouseSpawns.Assign(c);
            var perRoom = new int[c.RoomCount];
            for (int s = 0; s < c.N; s++)
            {
                RoomView rv;
                if (!villa.Rooms.TryGetValue(rooms[s], out rv)) continue;
                var local = HouseSpawns.PositionIn(rv.Layout);
                local.x += 0.45f * perRoom[rooms[s]]++;
                var info = c.Suspects[s];
                var fig = SuspectFigure.Build(villa.transform, info);
                fig.name = "Figura_" + info.Id;
                fig.transform.position = World(local);
                fig.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                fig.transform.localScale = Vector3.one * FigureScale;
                var anchor = new GameObject("Kostumi").transform;
                anchor.SetParent(fig.transform, false);
                anchor.localScale = Vector3.one * (1f / FigureScale);
                RoleLook.Attach(anchor, info.Id, kit);
                Solid("Pengesa_" + info.Id, local + new Vector3(0f, 0.5f, 0f), new Vector3(0.36f, 1f, 0.36f), Quaternion.identity);
                figures.Add(new Live { Fig = fig, Suspect = s, Pos = World(local), Mark = NewMark(info.Id) });
            }
        }

        // Diamant i vogel i arte mbi koken e atij qe nuk e ke pyetur ende.
        Transform NewMark(string id)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "PaPyetur_" + id;
            go.transform.SetParent(villa.transform, false);
            go.transform.localScale = Vector3.one * 0.09f;
            go.GetComponent<Renderer>().sharedMaterial = kit.Glow("#E2C27A", 1.8f);
            Object.Destroy(go.GetComponent<Collider>());
            return go.transform;
        }

        void UpdateMarks()
        {
            var asked = new bool[c.N];
            foreach (var st in gm.Session.Statements) asked[st.Suspect] = true;
            for (int i = 0; i < figures.Count; i++)
            {
                var f = figures[i];
                bool on = !asked[f.Suspect] && !gm.Session.IsOver;
                if (f.Mark.gameObject.activeSelf != on) f.Mark.gameObject.SetActive(on);
                if (!on) continue;
                float bob = Mathf.Sin(Time.time * 2.5f + i) * 0.04f;
                f.Mark.position = f.Fig.HeadTop.position + Vector3.up * (0.42f + bob);
                f.Mark.rotation = Quaternion.Euler(45f, Time.time * 90f + i * 30f, 45f);
            }
        }

        // Rreth i arte ne dysheme nen personin qe mund te pyesh.
        void BuildRing()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "RrethiIPyetjes";
            go.transform.SetParent(villa.transform, false);
            go.GetComponent<Renderer>().sharedMaterial = kit.Glow("#E2C27A", 1.4f);
            Object.Destroy(go.GetComponent<Collider>());
            ring = go.transform;
            go.SetActive(false);
        }

        void SetupPassage()
        {
            hasPassage = c.SecretEdgeExists && Has("BI") && Has("ZY");
            if (!hasPassage) return;
            var bi = MapBuilder.Layout("BI");
            var zy = MapBuilder.Layout("ZY");
            float z = (Mathf.Max(bi.Z0, zy.Z0) + Mathf.Min(bi.Z1, zy.Z1)) * 0.5f;
            passageBI = World(new Vector3(bi.X0 + 0.5f, bi.Y, z));
            passageZY = World(new Vector3(zy.X1 - 0.5f, zy.Y, z));
        }

        void BuildUI()
        {
            leftCol = gm.LeftColumn;
            rightCol = gm.RightColumn;
            if (gm.VillaFrameRect != null) gm.VillaFrameRect.gameObject.SetActive(false);
            if (gm.LabelLayer != null) gm.LabelLayer.gameObject.SetActive(false);
            var labels = gm.GetComponent<ScreenLabels>();
            if (labels != null) labels.enabled = false;
            leftCol.gameObject.SetActive(false);
            rightBase = rightCol.anchoredPosition;
            rightCol.anchoredPosition = rightBase + new Vector2(DrawerHidden, 0);

            var root = (RectTransform)gm.UiCanvas.transform;
            layer = UI.Rect(root, "ExploreLayer");
            UI.Stretch(layer, 0, 0, 0, 0);
            layer.SetSiblingIndex(gm.OverlayRoot.GetSiblingIndex());

            var tag = UI.Card(layer, "NameTag", new Color(0.05f, 0.06f, 0.08f, 0.92f), Theme.Gold, 10);
            nameRt = tag.rectTransform;
            nameRt.anchorMin = nameRt.anchorMax = new Vector2(0.5f, 0.5f);
            nameRt.pivot = new Vector2(0.5f, 0f);
            nameRt.sizeDelta = new Vector2(260, 58);
            nameText = UI.Label(tag.transform, "", Theme.SansSemi, 19, Theme.Text, TextAnchor.UpperCenter);
            UI.Stretch(nameText.rectTransform, 8, 7, 8, 26);
            nameSub = UI.Label(tag.transform, "", Theme.Sans, 13, Theme.Muted, TextAnchor.LowerCenter);
            UI.Stretch(nameSub.rectTransform, 8, 28, 8, 8);
            tag.gameObject.SetActive(false);

            var bub = UI.Card(layer, "Bubble", Theme.Parchment, Theme.ParchMuted, 12);
            bubbleRt = bub.rectTransform;
            bubbleRt.anchorMin = bubbleRt.anchorMax = new Vector2(0.5f, 0.5f);
            bubbleRt.pivot = new Vector2(0.5f, 0f);
            bubbleRt.sizeDelta = new Vector2(380, 60);
            bubbleText = UI.Label(bub.transform, "", Theme.SansSemi, 16, Theme.ParchInk, TextAnchor.MiddleCenter);
            UI.Stretch(bubbleText.rectTransform, 14, 10, 14, 10);
            bub.gameObject.SetActive(false);

            bannerText = UI.Label(layer, "", Theme.Serif, 40, Theme.Parchment, TextAnchor.MiddleCenter);
            var br = bannerText.rectTransform;
            br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.8f);
            br.pivot = new Vector2(0.5f, 0.5f);
            br.sizeDelta = new Vector2(700, 60);
            bannerText.horizontalOverflow = HorizontalWrapMode.Overflow;
            bannerText.raycastTarget = false;

            var hint = UI.Card(layer, "Hint", new Color(0.05f, 0.06f, 0.08f, 0.88f), Theme.Line, 10);
            var hr = hint.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(0f, 0f);
            hr.pivot = new Vector2(0f, 0f);
            hr.anchoredPosition = new Vector2(16, 16);
            hr.sizeDelta = new Vector2(1100, 64);
            hintText = UI.Label(hint.transform, "", Theme.Sans, 14, Theme.Text2, TextAnchor.MiddleLeft);
            UI.Stretch(hintText.rectTransform, 14, 6, 14, 6);
        }

        int CurrentRoom()
        {
            var p = villa.transform.InverseTransformPoint(player.transform.position);
            foreach (var kv in villa.Rooms)
            {
                var l = kv.Value.Layout;
                if (p.x > l.X0 && p.x < l.X1 && p.z > l.Z0 && p.z < l.Z1 && Mathf.Abs(p.y - l.Y) < 0.8f) return kv.Key;
            }
            return -1;
        }

        static float Flat(Vector3 a, Vector3 b, out float dy)
        {
            dy = Mathf.Abs(a.y - b.y);
            a.y = 0;
            b.y = 0;
            return Vector3.Distance(a, b);
        }

        int PassageSide()
        {
            if (!hasPassage || !c.SecretEdgeActive) return 0;
            float dy;
            var p = player.transform.position;
            if (Flat(p, passageBI, out dy) < PassageDist && dy < 0.6f) return 1;
            if (Flat(p, passageZY, out dy) < PassageDist && dy < 0.6f) return 2;
            return 0;
        }

        void UpdateNear()
        {
            var p = player.transform.position;
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < figures.Count; i++)
            {
                float dy;
                float d = Flat(p, figures[i].Pos, out dy);
                if (dy > 0.9f) continue;
                if (d < bestD)
                {
                    bestD = d;
                    best = i;
                }
            }
            int next = near;
            if (near >= 0)
            {
                float dy;
                float d = Flat(p, figures[near].Pos, out dy);
                if (d > FarDist || dy > 0.9f) next = -1;
            }
            if (next < 0 && best >= 0 && bestD < NearDist) next = best;
            if (next == near) return;
            if (near >= 0) figures[near].Fig.ClearLook();
            near = next;
            if (near >= 0)
            {
                int s = figures[near].Suspect;
                gm.FocusSuspect(s);
                var sus = c.Suspects[s];
                nameText.text = sus.Name;
                nameSub.text = sus.Role + " · " + (gm.Session.IsCleared(s) ? "jashtë dyshimit" : "kandidat");
                float w = Mathf.Max(UI.TextWidth(sus.Name, Theme.SansSemi, 19), UI.TextWidth(nameSub.text, Theme.Sans, 13)) + 40;
                nameRt.sizeDelta = new Vector2(Mathf.Max(160, w), 58);
            }
            else
            {
                gm.FocusSuspect(-1);
            }
        }

        void OnAnswered(AskOutcome o)
        {
            if (o == null || o.Statement == null) return;
            for (int i = 0; i < figures.Count; i++)
            {
                if (figures[i].Suspect != o.Statement.Suspect) continue;
                bubbleFor = i;
                bubbleUntil = Time.time + 6f;
                bubbleText.text = o.Statement.Text;
                int lines = Mathf.Max(1, Mathf.CeilToInt(UI.TextWidth(o.Statement.Text, Theme.SansSemi, 16) / 340f));
                bubbleRt.sizeDelta = new Vector2(380, lines * 22 + 26);
                figures[i].Fig.Talk(2.5f);
            }
            if (near >= 0)
            {
                int s = figures[near].Suspect;
                nameSub.text = c.Suspects[s].Role + " · " + (gm.Session.IsCleared(s) ? "jashtë dyshimit" : "kandidat");
            }
        }

        void Update()
        {
            if (gm == null || player == null) return;
            bool overlay = gm.OverlayOpen;
            if (!overlay && ExploreInput.Pressed(ExploreKey.Board))
            {
                boardOpen = !boardOpen;
                leftCol.gameObject.SetActive(boardOpen);
            }
            if (boardOpen && ExploreInput.Pressed(ExploreKey.Close))
            {
                boardOpen = false;
                leftCol.gameObject.SetActive(false);
            }
            bool blocked = overlay || boardOpen;
            player.InputBlocked = blocked;
            float turn = 0;
            if (!blocked && ExploreInput.Held(ExploreKey.TurnLeft)) turn -= 1;
            if (!blocked && ExploreInput.Held(ExploreKey.TurnRight)) turn += 1;
            yaw += turn * 90f * Time.deltaTime;

            if (gm.Session != lastSession)
            {
                lastSession = gm.Session;
                if (near >= 0) figures[near].Fig.ClearLook();
                near = -1;
                lastRoom = -2;
            }
            UpdateNear();
            if (near >= 0) figures[near].Fig.LookAt(player.transform.position);
            UpdateRing();
            UpdateMarks();

            if (!blocked && near >= 0 && !gm.Session.IsOver)
            {
                var types = QuestionEngine.TypesFor(c);
                ExploreKey[] keys = { ExploreKey.Q1, ExploreKey.Q2, ExploreKey.Q3, ExploreKey.Q4, ExploreKey.Q5, ExploreKey.Q6 };
                for (int k = 0; k < keys.Length && k < types.Length; k++)
                {
                    if (ExploreInput.Pressed(keys[k])) gm.SelectType(types[k]);
                }
                if (ExploreInput.Pressed(ExploreKey.Ask)) gm.Ask();
            }

            int room = CurrentRoom();
            if (overlay) bannerText.color = Color.clear;
            else UpdateBanner(room);
            if (!blocked && room >= 0 && c.PhysicalClues.Count > 0 && !gm.Session.IsOver && ExploreInput.Pressed(ExploreKey.Search)) gm.SearchRoom(room);

            int side = PassageSide();
            if (!blocked && side != 0 && ExploreInput.Pressed(ExploreKey.Passage))
            {
                var dir = passageZY - passageBI;
                dir.y = 0;
                dir = dir.normalized * 0.3f;
                if (side == 1) player.Teleport(passageZY + dir, 270f);
                else player.Teleport(passageBI - dir, 90f);
            }

            bool drawer = near >= 0 || gm.Session.IsOver || gm.CurrentTutorialStep != null;
            drawerOffset = Mathf.Lerp(drawerOffset, drawer ? 0f : DrawerHidden, Mathf.Clamp01(Time.deltaTime * 9f));
            rightCol.anchoredPosition = rightBase + new Vector2(drawerOffset, 0);

            hintText.text = HintFor(room, side);
        }

        void UpdateRing()
        {
            bool on = near >= 0 && !gm.Session.IsOver;
            if (ring.gameObject.activeSelf != on) ring.gameObject.SetActive(on);
            if (!on) return;
            float d = 0.78f + Mathf.Sin(Time.time * 4f) * 0.05f;
            ring.position = figures[near].Pos + Vector3.up * 0.02f;
            ring.localScale = new Vector3(d, 0.004f, d);
        }

        // Emri i dhomes shfaqet per pak sekonda sa here hyn ne nje dhome te re.
        void UpdateBanner(int room)
        {
            if (room != lastRoom && room >= 0)
            {
                bannerText.text = c.Rooms[room].Name;
                bannerUntil = Time.time + BannerSeconds;
            }
            if (room >= 0) lastRoom = room;
            float left = bannerUntil - Time.time;
            float a = Mathf.Clamp01(left / 0.6f) * Mathf.Clamp01((BannerSeconds - left) / 0.3f);
            var col = Theme.Parchment;
            col.a = a;
            bannerText.color = col;
        }

        string HintFor(int room, int side)
        {
            if (boardOpen) return "Tab ose Esc: mbyll tabelën";
            var parts = new List<string>();
            if (room >= 0) parts.Add("<color=#E2C27A>" + c.Rooms[room].Name + "</color>");
            parts.Add("WASD: ec");
            parts.Add("Shift: vrap");
            parts.Add("Q / E: kthe kamerën");
            parts.Add("Tab: tabela");
            if (near >= 0 && !gm.Session.IsOver) parts.Add("<color=#E2C27A>1–" + QuestionEngine.TypesFor(c).Length + ": zgjidh pyetjen · Enter: pyet</color>");
            else if (!gm.Session.IsOver) parts.Add("shko afër dikujt për ta pyetur");
            if (room >= 0 && c.PhysicalClues.Count > 0 && !gm.Session.IsOver) parts.Add("K: kërko dhomën (1 pikë)");
            if (side != 0) parts.Add("<color=#E2C27A>F: kalimi i fshehtë</color>");
            return string.Join("  ·  ", parts.ToArray());
        }

        bool ToLocal(Vector3 world, out Vector2 local)
        {
            local = Vector2.zero;
            var sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0) return false;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, sp, null, out local);
        }

        void LateUpdate()
        {
            if (player == null || cam == null) return;
            var target = player.transform.position + Vector3.up * 0.7f;
            var rot = Quaternion.Euler(Pitch, yaw, 0f);
            var desired = target - rot * Vector3.forward * Distance;
            float k = camReady ? Mathf.Clamp01(Time.deltaTime * 8f) : 1f;
            cam.transform.position = Vector3.Lerp(cam.transform.position, desired, k);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(target - cam.transform.position), camReady ? k : 1f);
            camReady = true;

            Vector2 local = Vector2.zero;
            bool showName = near >= 0 && ToLocal(figures[near].Fig.HeadTop.position + Vector3.up * 0.15f, out local);
            nameRt.gameObject.SetActive(showName);
            if (showName) nameRt.anchoredPosition = local;

            bool showBubble = bubbleFor >= 0 && Time.time < bubbleUntil;
            if (showBubble) showBubble = ToLocal(figures[bubbleFor].Fig.HeadTop.position + Vector3.up * 0.4f, out local);
            bubbleRt.gameObject.SetActive(showBubble);
            if (showBubble)
            {
                bool stacked = near == bubbleFor && showName;
                bubbleRt.anchoredPosition = local + new Vector2(0, stacked ? 64 : 0);
            }
        }

        void OnDestroy()
        {
            if (gm != null) gm.Answered -= OnAnswered;
        }
    }
}
