using System.Collections.Generic;
using Alibi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Alibi.Unity
{
    public sealed class GameManager : MonoBehaviour
    {
        const float BarH = 52f;
        const float ProximityRange = 5f;
        const float ProximityInterval = 0.15f;

        public GameSession Session { get; private set; }
        public Question Draft = new Question();
        public AskOutcome LastOutcome;
        public int AccPerson = -1, AccRoom = -1, AccTime = -1;
        public int TutorialIndex;

        CaseDefinition caseDef;
        CameraRig rig;
        VillaView villa;
        // ScreenLabels labels;  // REMOVED — floating neon letters disabled
        PropagationAnimator animator;
        Canvas canvas;

        // --- immersive UI ---
        RectTransform overlayRoot, villaFrame, roomCardHost, hudHost, barHost, cameraControlsHost;
        RectTransform deductionPanel;       // toggled with Tab
        RectTransform rightOverlayRoot;     // suspect interaction panel (right side)
        RectTransform rightContent, tabsRow;
        ScrollRect rightScroll;
        RectTransform promptHost;           // bottom-center "Press E" prompt

        DeductionBoardView board;
        TimelineController timeline;
        InterrogationPanel interrogation;
        GamePanels panels;
        ProgressiveRules progressiveRules;

        int currentTime;
        int selectedRoom = -1;
        int tab;
        bool tutorialVisible;

        // --- immersive state ---
        bool showDeduction;
        bool showRight;
        int nearSuspect = -1;
        float proximityTimer;
        Transform playerBody;

        // --- minimap ---
        RectTransform minimapHost;
        RectTransform minimapDot;

        // --- ceiling hiding for top-down view ---
        List<GameObject> ceilingObjects;

        public Transform CanvasTransform { get { return canvas != null ? canvas.transform : null; } }

        void Start()
        {
            caseDef = AlibiFlow.PendingCase ?? LevelCards.Card(AlibiFlow.NextCard());
            AlibiFlow.PendingCase = null;
            if (caseDef.PathRooms == null) caseDef.Prepare();
            tutorialVisible = caseDef.Tutorial.Count > 0 && AlibiFlow.TutorialEnabled;
            BuildWorld();
            BuildUI();
            StartSession(false);
            ShowBrief();
        }

        void Update()
        {
            if (progressiveRules != null) progressiveRules.Update();
            HandleInput();
            proximityTimer -= Time.deltaTime;
            if (proximityTimer <= 0f)
            {
                proximityTimer = ProximityInterval;
                UpdateProximity();
            }
            UpdateMinimap();
        }

        // ==================== WORLD ====================
        void BuildWorld()
        {
            villa = MapBuilder.Build(transform, caseDef);
            HouseDressing.Build(villa);
            HouseDressing.SetResidentsMode(villa, ResidentsMode.Always);

            // Fully deactivate SuspectMarker GameObjects (capsule body, sphere head, neon dashed ring)
            // Only the ResidentFigure 3D meshes from HouseDressing remain visible
            if (villa.Markers != null)
            {
                int sn = villa.Case.N, rc = villa.Case.RoomCount;
                for (int s = 0; s < sn; s++)
                    for (int r = 0; r < rc; r++)
                    {
                        var m = villa.Markers[s, r];
                        if (m == null) continue;
                        m.gameObject.SetActive(false);
                    }
            }

            SceneSetup.Lights(transform, villa.Bounds);
            rig = CameraRig.Create(transform, Theme.Night);
            rig.Target = villa.Bounds;
            villa.Rig = rig;
            villa.RoomClicked = OnRoomClicked;
            villa.BackgroundClicked = () => OnRoomClicked(-1);
            villa.SuspectClicked = s =>
            {
                tab = 0;
                InteractWithSuspect(s);
            };
            AlibiFlow.DisableForeignCamerasAndLights(transform);
            animator = gameObject.AddComponent<PropagationAnimator>();

            // --- start in first person inside the hall ---
            RoomLayout hallLayout = default;
            bool foundHall = false;
            foreach (var kv in villa.Rooms)
            {
                if (kv.Value.Info.Id == "HO") { hallLayout = kv.Value.Layout; foundHall = true; break; }
            }
            if (foundHall)
            {
                rig.EnterFirstPerson(hallLayout.Center);
            }
            else
            {
                rig.SetMode(CameraMode.FirstPerson);
            }

            BuildPlayerBody();
        }

        void BuildPlayerBody()
        {
            var go = new GameObject("PlayerBody");
            go.transform.SetParent(transform, false);
            playerBody = go.transform;

            var model = new GameObject("Model").transform;
            model.SetParent(go.transform, false);

            var coatMat = Theme.Lit(Theme.Hex("#3A4256"), 0.35f);
            var skinMat = Theme.Lit(Theme.Hex("#F5D4B0"), 0.55f);
            var hairMat = Theme.Lit(Theme.Hex("#2A1E16"), 0.2f);
            var shoeMat = Theme.Lit(Theme.Hex("#1E1A18"), 0.4f);
            HouseDressing.BuildHumanoidBody(model, coatMat, skinMat, hairMat, shoeMat);

            var hatMat = Theme.Lit(Theme.Hex("#2B2622"), 0.25f);
            var bandMat = Theme.Lit(Theme.Hex("#8C6A3A"), 0.3f);
            HouseDressing.Part(model, PrimitiveType.Cylinder, "HatBrim", new Vector3(0, 1.76f, 0), new Vector3(0.40f, 0.008f, 0.40f), hatMat);
            HouseDressing.Part(model, PrimitiveType.Cylinder, "HatCrown", new Vector3(0, 1.83f, 0), new Vector3(0.29f, 0.07f, 0.29f), hatMat);
            HouseDressing.Part(model, PrimitiveType.Cylinder, "HatBand", new Vector3(0, 1.785f, 0), new Vector3(0.295f, 0.018f, 0.295f), bandMat);

            go.AddComponent<PlayerWalkAnimator>().Setup(model);

            var lightGo = new GameObject("PlayerLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 2.2f, 0.5f);
            var pl = lightGo.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = new Color(1f, 0.95f, 0.85f);
            pl.intensity = 2.2f;
            pl.range = 4.5f;
            pl.shadows = LightShadows.None;

            rig.PlayerBody = playerBody;

            // first-person: hide the body
            go.SetActive(rig.Mode != CameraMode.FirstPerson);
        }

        void CacheCeilings()
        {
            if (ceilingObjects != null) return;
            ceilingObjects = new List<GameObject>();
            if (villa == null) return;
            foreach (var t in villa.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Ceiling") ceilingObjects.Add(t.gameObject);
            }
        }

        void SetCeilingsVisible(bool visible)
        {
            CacheCeilings();
            foreach (var c in ceilingObjects)
                if (c != null) c.SetActive(visible);
        }

        // ==================== UI ====================
        void BuildUI()
        {
            canvas = AlibiFlow.CreateCanvas(transform, "GameCanvas", 10);
            var root = (RectTransform)canvas.transform;

            // === VILLA FRAME = FULL SCREEN ===
            villaFrame = UI.Rect(root, "VillaFrame");
            UI.Stretch(villaFrame, 0, 0, 0, 0);
            hudHost = UI.Rect(villaFrame, "Hud");
            UI.Stretch(hudHost, 0, 0, 0, 0);
            roomCardHost = UI.Rect(villaFrame, "RoomCard");
            UI.PlaceRight(roomCardHost, 14, 14, 300, 200);
            BuildCameraControls();
            rig.Frame = villaFrame;

            // ScreenLabels REMOVED — floating neon letter labels disabled
            // Name labels above suspects are shown via proximity (see UpdateProximity)

            // === FLOATING TOP BAR ===
            barHost = UI.Rect(root, "TopBar");
            UI.StretchTop(barHost, 0, 0, 0, BarH);

            // === DEDUCTION PANEL (toggled with Tab, left side overlay) ===
            deductionPanel = UI.Rect(root, "DeductionPanel");
            deductionPanel.anchorMin = Vector2.zero;
            deductionPanel.anchorMax = new Vector2(0.55f, 1f);
            deductionPanel.offsetMin = new Vector2(16, 16);
            deductionPanel.offsetMax = new Vector2(-16, -(BarH + 12));
            deductionPanel.gameObject.SetActive(false);

            float boardH = DeductionBoardView.PreferredHeight(caseDef);
            var boardRt = UI.Rect(deductionPanel, "Board");
            UI.StretchBottom(boardRt, 0, 0, 0, boardH);
            board = gameObject.AddComponent<DeductionBoardView>();
            board.Animator = animator;
            board.OnHourClicked = t => SetTime(t);
            board.OnSuspectClicked = s =>
            {
                tab = 0;
                InteractWithSuspect(s);
            };

            var tlRt = UI.Rect(deductionPanel, "Timeline");
            UI.StretchBottom(tlRt, 0, boardH + 12, 0, 56);
            timeline = gameObject.AddComponent<TimelineController>();
            timeline.Build(tlRt, caseDef);
            timeline.OnTimeChanged = t => SetTime(t);

            board.Build(boardRt, new GameSession(caseDef), villa);

            // === RIGHT OVERLAY (suspect interaction) ===
            rightOverlayRoot = UI.Rect(root, "RightOverlay");
            rightOverlayRoot.anchorMin = new Vector2(0.6f, 0);
            rightOverlayRoot.anchorMax = new Vector2(1, 1);
            rightOverlayRoot.offsetMin = new Vector2(0, 16);
            rightOverlayRoot.offsetMax = new Vector2(-16, -(BarH + 12));
            rightOverlayRoot.gameObject.SetActive(false);

            tabsRow = UI.Rect(rightOverlayRoot, "Tabs");
            UI.StretchTop(tabsRow, 0, 0, 0, 44);
            rightScroll = UI.Scroll(rightOverlayRoot, "RightScroll", out rightContent);
            UI.Stretch((RectTransform)rightScroll.transform, 0, 54, 0, 0);

            // === PROMPT HOST (bottom center) ===
            promptHost = UI.Rect(root, "PromptHost");
            promptHost.anchorMin = new Vector2(0.3f, 0);
            promptHost.anchorMax = new Vector2(0.7f, 0);
            promptHost.pivot = new Vector2(0.5f, 0);
            promptHost.anchoredPosition = new Vector2(0, 80);
            promptHost.sizeDelta = new Vector2(0, 50);

            // === MINIMAP (bottom-right) ===
            BuildMinimap(root);

            // === OVERLAYS (modals) ===
            overlayRoot = UI.Rect(root, "Overlays");
            UI.Stretch(overlayRoot, 0, 0, 0, 0);

            interrogation = new InterrogationPanel(this);
            panels = new GamePanels(this);
            progressiveRules = new ProgressiveRules(this);
        }

        void BuildCameraControls()
        {
            if (cameraControlsHost != null) Object.Destroy(cameraControlsHost.gameObject);
            var host = UI.Rect(villaFrame, "CameraControls");
            cameraControlsHost = host;
            host.anchorMin = host.anchorMax = new Vector2(0, 0);
            host.pivot = new Vector2(0, 0);
            host.anchoredPosition = new Vector2(14, 14);
            host.sizeDelta = new Vector2(400, 40);

            bool isFP = rig != null && rig.Mode == CameraMode.FirstPerson;
            bool isTP = rig != null && rig.Mode == CameraMode.ThirdPerson;
            bool isOV = rig != null && rig.Mode == CameraMode.Overview;
            bool isTopDown = rig != null && rig.Mode == CameraMode.TopDown;

            float x = 0;

            // Person view toggle: FirstPerson <-> ThirdPerson
            string personLabel = isFP ? "Pamje e tretë" : "Pamje e parë";
            var togglePerson = UI.Button(host, personLabel, isFP ? BtnStyle.ChipOn : (isTP ? BtnStyle.ChipOn : BtnStyle.Ghost), () =>
            {
                bool wasTopDown = rig.Mode == CameraMode.TopDown;
                if (rig.Mode == CameraMode.FirstPerson || rig.Mode == CameraMode.ThirdPerson)
                {
                    rig.TogglePersonView();
                }
                else
                {
                    rig.SetMode(CameraMode.FirstPerson);
                }
                if (wasTopDown) SetCeilingsVisible(true);
                // Show/hide player body
                if (playerBody != null) playerBody.gameObject.SetActive(rig.Mode != CameraMode.FirstPerson);
                BuildCameraControls();
            }, 14, 8);
            togglePerson.Background.color = new Color(0.05f, 0.06f, 0.08f, 0.8f);
            UI.Place(togglePerson.Rect, x, 0, 140, 40);
            x += 146;

            // Top-down player-tracking overhead view
            var overviewBtn = UI.Button(host, "Pamje nga lart", isTopDown ? BtnStyle.ChipOn : BtnStyle.Ghost, () =>
            {
                if (rig.Mode == CameraMode.TopDown)
                {
                    rig.SetMode(CameraMode.FirstPerson);
                    if (playerBody != null) playerBody.gameObject.SetActive(false);
                    SetCeilingsVisible(true);
                }
                else
                {
                    rig.SetMode(CameraMode.TopDown);
                    if (playerBody != null) playerBody.gameObject.SetActive(true);
                    SetCeilingsVisible(true);
                }
                BuildCameraControls();
            }, 14, 8);
            overviewBtn.Background.color = new Color(0.05f, 0.06f, 0.08f, 0.8f);
            UI.Place(overviewBtn.Rect, x, 0, 140, 40);
            x += 146;

            // Hint text
            string hint1;
            if (isFP || isTP || isTopDown)
                hint1 = "W/S ec · A/D kthehu · Tab deduksion · E pyet";
            else
                hint1 = "Kliko një dhomë ose një figurë";
            var hint = UI.LabelAt(host, hint1, Theme.Sans, 11, Theme.Muted, x + 6, 0, 350, 40, TextAnchor.MiddleLeft);
            hint.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        // ==================== INPUT ====================
        void HandleInput()
        {
            bool tabDown = false;
            bool eDown = false;
            bool escDown = false;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                tabDown = kb.tabKey.wasPressedThisFrame;
                eDown = kb.eKey.wasPressedThisFrame;
                escDown = kb.escapeKey.wasPressedThisFrame;
            }
#else
            tabDown = Input.GetKeyDown(KeyCode.Tab);
            eDown = Input.GetKeyDown(KeyCode.E);
            escDown = Input.GetKeyDown(KeyCode.Escape);
#endif
            if (escDown)
            {
                if (showRight) { ToggleRight(false); return; }
                if (showDeduction) { ToggleDeduction(); return; }
            }
            if (tabDown)
            {
                ToggleDeduction();
            }
            if (eDown && nearSuspect >= 0 && !Session.IsOver)
            {
                InteractWithSuspect(nearSuspect);
            }
        }

        // ==================== MINIMAP ====================
        void BuildMinimap(RectTransform root)
        {
            const float mapW = 150f, mapH = 120f;
            minimapHost = UI.Rect(root, "Minimap");
            // anchor bottom-left, above camera controls
            minimapHost.anchorMin = new Vector2(0, 0);
            minimapHost.anchorMax = new Vector2(0, 0);
            minimapHost.pivot = new Vector2(0, 0);
            minimapHost.anchoredPosition = new Vector2(14, 94);
            minimapHost.sizeDelta = new Vector2(mapW, mapH);

            // Black background with grey border
            var bg = minimapHost.gameObject.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.02f, 0.02f, 0.04f, 0.85f);
            var outline = minimapHost.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0.35f, 0.35f, 0.38f, 0.9f);
            outline.effectDistance = new Vector2(1, 1);

            // Calculate world bounds for all rooms
            float wMin = float.MaxValue, wMax = float.MinValue;
            float zMin = float.MaxValue, zMax = float.MinValue;
            foreach (var l in MapBuilder.Layouts)
            {
                if (l.X0 < wMin) wMin = l.X0;
                if (l.X1 > wMax) wMax = l.X1;
                if (l.Z0 < zMin) zMin = l.Z0;
                if (l.Z1 > zMax) zMax = l.Z1;
            }
            float worldW = wMax - wMin;
            float worldD = zMax - zMin;
            float pad = 4f;
            float drawW = mapW - pad * 2;
            float drawH = mapH - pad * 2;
            float scale = Mathf.Min(drawW / worldW, drawH / worldD);

            // Draw each room as an outlined rectangle
            foreach (var l in MapBuilder.Layouts)
            {
                float rx = pad + (l.X0 - wMin) * scale;
                float ry = pad + (l.Z0 - zMin) * scale;
                float rw = (l.X1 - l.X0) * scale;
                float rh = (l.Z1 - l.Z0) * scale;

                var room = UI.Rect(minimapHost, "MR_" + l.Id);
                room.anchorMin = Vector2.zero;
                room.anchorMax = Vector2.zero;
                room.pivot = Vector2.zero;
                room.anchoredPosition = new Vector2(rx, ry);
                room.sizeDelta = new Vector2(rw, rh);
                var roomImg = room.gameObject.AddComponent<UnityEngine.UI.Image>();
                roomImg.color = new Color(0.08f, 0.08f, 0.1f, 1f);
                var roomOutline = room.gameObject.AddComponent<UnityEngine.UI.Outline>();

                // Grey outlines — slightly different tint per floor
                if (l.Y > 1f)
                {
                    roomOutline.effectColor = new Color(0.45f, 0.45f, 0.5f, 0.7f); // upper floor — lighter grey
                }
                else if (l.Y < -1f)
                {
                    roomOutline.effectColor = new Color(0.3f, 0.3f, 0.33f, 0.5f); // basement — dim grey
                }
                else
                {
                    roomOutline.effectColor = new Color(0.4f, 0.4f, 0.42f, 0.9f); // ground floor — standard grey
                }
                roomOutline.effectDistance = new Vector2(1, 1);

                // Room label
                var lbl = UI.Label(room, l.Id, Theme.Mono, 8,
                    new Color(0.55f, 0.55f, 0.5f, 0.7f), TextAnchor.MiddleCenter);
                UI.Stretch(lbl.rectTransform, 0, 0, 0, 0);
            }

            // Draw door connections as thin lines between rooms
            foreach (var d in MapBuilder.Doors)
            {
                RoomLayout a = default, b = default;
                bool fa = false, fb = false;
                foreach (var l in MapBuilder.Layouts)
                {
                    if (l.Id == d.A) { a = l; fa = true; }
                    if (l.Id == d.B) { b = l; fb = true; }
                }
                if (!fa || !fb) continue;

                // Calculate center of each room on minimap
                float ax = pad + ((a.X0 + a.X1) * 0.5f - wMin) * scale;
                float ay = pad + ((a.Z0 + a.Z1) * 0.5f - zMin) * scale;
                float bx = pad + ((b.X0 + b.X1) * 0.5f - wMin) * scale;
                float by = pad + ((b.Z0 + b.Z1) * 0.5f - zMin) * scale;

                // Find the edge midpoint between the two rooms (not full center-to-center)
                // Use the door position to draw a short connector at the shared wall
                float cx = (ax + bx) * 0.5f;
                float cy = (ay + by) * 0.5f;
                float dx = bx - ax;
                float dy = by - ay;
                float len = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

                // Short connecting line (8px long, 2px wide)
                float lineLen = Mathf.Min(len * 0.4f, 8f);
                var line = UI.Rect(minimapHost, "Door_" + d.A + d.B);
                line.anchorMin = Vector2.zero;
                line.anchorMax = Vector2.zero;
                line.pivot = new Vector2(0.5f, 0.5f);
                line.anchoredPosition = new Vector2(cx, cy);
                line.sizeDelta = new Vector2(lineLen, 2f);
                line.localRotation = Quaternion.Euler(0, 0, angle);
                var lineImg = line.gameObject.AddComponent<UnityEngine.UI.Image>();
                // Stairs connections (different floors) in dotted/dimmer style
                bool sameFloor = Mathf.Abs(a.Y - b.Y) < 0.5f;
                lineImg.color = sameFloor
                    ? new Color(0.5f, 0.5f, 0.45f, 0.6f)
                    : new Color(0.4f, 0.4f, 0.5f, 0.4f);
            }

            // Player dot (gold)
            minimapDot = UI.Rect(minimapHost, "PlayerDot");
            minimapDot.anchorMin = Vector2.zero;
            minimapDot.anchorMax = Vector2.zero;
            minimapDot.pivot = new Vector2(0.5f, 0.5f);
            minimapDot.sizeDelta = new Vector2(6, 6);
            var dotImg = minimapDot.gameObject.AddComponent<UnityEngine.UI.Image>();
            dotImg.color = Theme.Gold;
        }

        void UpdateMinimap()
        {
            if (minimapDot == null || rig == null || minimapHost == null) return;
            if (rig.Mode != CameraMode.FirstPerson && rig.Mode != CameraMode.ThirdPerson && rig.Mode != CameraMode.TopDown)
            {
                minimapHost.gameObject.SetActive(false);
                return;
            }
            minimapHost.gameObject.SetActive(true);

            // Map world coords to minimap
            float wMin = float.MaxValue, wMax = float.MinValue;
            float zMin = float.MaxValue, zMax = float.MinValue;
            foreach (var l in MapBuilder.Layouts)
            {
                if (l.X0 < wMin) wMin = l.X0;
                if (l.X1 > wMax) wMax = l.X1;
                if (l.Z0 < zMin) zMin = l.Z0;
                if (l.Z1 > zMax) zMax = l.Z1;
            }
            float worldW = wMax - wMin;
            float worldD = zMax - zMin;
            float mapW = minimapHost.sizeDelta.x;
            float mapH = minimapHost.sizeDelta.y;
            float pad = 4f;
            float drawW = mapW - pad * 2;
            float drawH = mapH - pad * 2;
            float scale = Mathf.Min(drawW / worldW, drawH / worldD);

            Vector3 p = rig.FpPosition;
            float dx = pad + (p.x - wMin) * scale;
            float dy = pad + (p.z - zMin) * scale;
            dx = Mathf.Clamp(dx, 2, mapW - 2);
            dy = Mathf.Clamp(dy, 2, mapH - 2);
            minimapDot.anchoredPosition = new Vector2(dx, dy);
        }

        // ==================== PROXIMITY ====================
        void UpdateProximity()
        {
            if (rig == null || villa == null) return;
            if (rig.Mode != CameraMode.FirstPerson && rig.Mode != CameraMode.ThirdPerson && rig.Mode != CameraMode.TopDown) { SetNearSuspect(-1); return; }

            Vector3 playerPos = rig.FpPosition;
            float bestDist = ProximityRange;
            int best = -1;
            const float NameLabelRange = 6f; // show name labels within 6 units

            // Check ResidentFigure objects
            var residents = FindObjectsByType<ResidentFigure>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var res in residents)
            {
                if (res == null || !res.gameObject.activeInHierarchy) continue;
                if (res.Visual != null && !res.Visual.activeSelf) continue;
                float dist = Vector3.Distance(playerPos, res.transform.position);

                // Show/hide name label based on proximity
                if (res.NameLabel != null)
                {
                    bool showName = dist < NameLabelRange;
                    if (res.NameLabel.activeSelf != showName)
                        res.NameLabel.SetActive(showName);
                }

                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = res.Suspect;
                }
            }

            // Also check SuspectMarker objects
            if (villa.Markers != null)
            {
                for (int s = 0; s < caseDef.N; s++)
                {
                    for (int r = 0; r < caseDef.RoomCount; r++)
                    {
                        var m = villa.Markers[s, r];
                        if (m == null || !m.IsVisible) continue;
                        float dist = Vector3.Distance(playerPos, m.transform.position);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            best = s;
                        }
                    }
                }
            }

            SetNearSuspect(best);
        }

        void SetNearSuspect(int s)
        {
            if (s == nearSuspect) return;
            // Auto-close interrogation panel when player walks away
            if (s < 0 && showRight) ToggleRight(false);
            nearSuspect = s;
            UpdatePrompt();
        }

        void UpdatePrompt()
        {
            UI.Clear(promptHost);
            // Prompt removed — player uses E to interact without floating text
        }

        void InteractWithSuspect(int s)
        {
            if (Session.IsOver) return;
            Draft.Suspect = s;
            if (Draft.Other == s) Draft.Other = -1;
            tab = 0;
            ToggleRight(true);
        }

        void ToggleDeduction()
        {
            showDeduction = !showDeduction;
            deductionPanel.gameObject.SetActive(showDeduction);
            if (showDeduction)
            {
                board.Refresh(currentTime, Truth);
            }
        }

        void ToggleRight(bool show)
        {
            showRight = show;
            rightOverlayRoot.gameObject.SetActive(showRight);
            if (showRight)
            {
                BuildTabs();
                RebuildRight(false);
            }
        }

        // ==================== SESSION ====================
        void StartSession(bool announce)
        {
            Session = new GameSession(caseDef);
            board.Rebind(Session);
            Draft = new Question();
            LastOutcome = null;
            AccPerson = AccRoom = AccTime = -1;
            TutorialIndex = 0;
            tab = 0;
            selectedRoom = -1;
            villa.SelectRoom(-1);
            currentTime = caseDef.CrimeTimeKnown ? caseDef.CrimeTime : 0;
            timeline.Select(currentTime, false);
            showDeduction = false;
            showRight = false;
            nearSuspect = -1;
            deductionPanel.gameObject.SetActive(false);
            rightOverlayRoot.gameObject.SetActive(false);
            CloseOverlay();
            RefreshAll();
            if (announce) Panels.Toast(canvas.transform, "Rasti u rihap me të njëjtin seed (T11). Buxheti u kthye në " + caseDef.Budget + ".", Theme.Gold, 3.5f);
        }

        bool Truth { get { return Session.IsOver; } }

        public void RefreshAll()
        {
            BuildTopBar();
            BuildHud();
            board.Refresh(currentTime, Truth);
            if (showRight)
            {
                BuildTabs();
                RebuildRight(false);
            }
            BuildRoomCard();
        }

        // ==================== TOP BAR (floating, semi-transparent) ====================
        void BuildTopBar()
        {
            UI.Clear(barHost);
            var bg = UI.Panel(barHost, "Bar", new Color(0.04f, 0.05f, 0.07f, 0.75f), 0, true);
            UI.Stretch(bg.rectTransform, 0, 0, 0, 0);
            var line = UI.Panel(barHost, "Line", Theme.Line, 0, false);
            UI.StretchBottom(line.rectTransform, 0, 0, 0, 1);

            // Left: ALIBI + case title
            UI.LabelAt(barHost, "ALIBI", Theme.Serif, 26, Theme.Gold, 20, 10, 100, 32);
            string title = caseDef.Title;
            string typeName = caseDef.Type == CaseType.Vrasja ? "Vrasja" : caseDef.Type == CaseType.Vjedhja ? "Vjedhja" : "Sabotimi";
            string sub = typeName + " · Niveli " + caseDef.Level;
            var t1 = UI.LabelAt(barHost, title, Theme.SansSemi, 14, Theme.Text, 118, 8, 320, 20);
            t1.horizontalOverflow = HorizontalWrapMode.Overflow;
            var t2 = UI.LabelAt(barHost, sub, Theme.Mono, 11, Theme.Muted, 118, 28, 320, 16);
            t2.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Right side: buttons + budget
            float r = 16;
            var exit = UI.Button(barHost, Session.IsOver ? "Menyja" : "Dil", BtnStyle.Ghost, ExitToMenu, 13, 8);
            UI.PlaceRight(exit.Rect, r, 6, 80, 40);
            r += 86;
            var accuse = UI.Button(barHost, "Akuzo", Session.IsOver ? BtnStyle.Disabled : BtnStyle.Danger, OpenAccusation, 14, 8);
            UI.PlaceRight(accuse.Rect, r, 6, 90, 40);
            accuse.SetInteractable(!Session.IsOver);
            r += 96;
            var howto = UI.Button(barHost, "?", BtnStyle.Ghost, ShowHowTo, 14, 8);
            UI.PlaceRight(howto.Rect, r, 6, 36, 40);
            r += 42;
            if (caseDef.Tutorial.Count > 0)
            {
                var tut = UI.Button(barHost, tutorialVisible ? "Udh: ON" : "Udh: OFF", tutorialVisible ? BtnStyle.ChipOn : BtnStyle.Ghost, ToggleTutorial, 12, 8);
                UI.PlaceRight(tut.Rect, r, 6, 90, 40);
                r += 96;
            }

            // Budget pips
            int remaining = Mathf.Max(0, Session.Remaining);
            var count = UI.LabelAt(barHost, remaining + "/" + caseDef.Budget, Theme.Mono, 14, Theme.Text, 0, 0, 50, BarH, TextAnchor.MiddleRight);
            UI.PlaceRight(count.rectTransform, r, 0, 50, BarH);
            r += 54;
            float pipW = 7, pipGap = 2;
            for (int i = 0; i < caseDef.Budget; i++)
            {
                bool full = i < remaining;
                var pip = UI.Panel(barHost, "Pip", full ? Theme.Gold : Theme.Line, 3, false);
                UI.PlaceRight(pip.rectTransform, r + (caseDef.Budget - 1 - i) * (pipW + pipGap), (BarH - 18) * 0.5f, pipW, 18);
            }
        }

        // ==================== HUD ====================
        void BuildHud()
        {
            UI.Clear(hudHost);
            var chip = UI.Card(hudHost, "HudTitle", new Color(0.05f, 0.06f, 0.08f, 0.85f), Theme.Line, 10);
            string title = Truth ? "E VËRTETA E ZBULUAR" : (caseDef.Level >= 2 ? "VILA · PËRDHESA DHE KATI I PARË" : "VILA · PËRDHESA");
            string sub = Truth ? "Pozicionet reale në " + caseDef.Hours[currentTime] : "Çfarë dihet për " + caseDef.Hours[currentTime];
            float w = Mathf.Max(UI.TextWidth(title, Theme.Mono, 13), UI.TextWidth(sub, Theme.Sans, 14)) + 30;
            UI.Place(chip.rectTransform, 14, BarH + 14, w, 58);
            UI.LabelAt(chip.transform, title, Theme.Mono, 13, Theme.Gold, 14, 10, w, 18).horizontalOverflow = HorizontalWrapMode.Overflow;
            UI.LabelAt(chip.transform, sub, Theme.Sans, 14, Theme.Text2, 14, 31, w, 20).horizontalOverflow = HorizontalWrapMode.Overflow;

            // Crime room pill
            string crime = caseDef.Rooms[caseDef.CrimeRoom].Name + " · " + (caseDef.CrimeTimeKnown ? caseDef.Hours[caseDef.CrimeTime] : "ora ?");
            float pw = UI.TextWidth(crime, Theme.SansSemi, 13) + 28;
            var pill = UI.Card(hudHost, "CrimePill", Theme.WithAlpha(Theme.Red, 0.12f), Theme.WithAlpha(Theme.Red, 0.7f), 14);
            UI.Place(pill.rectTransform, 14, BarH + 76, pw, 30);
            var pt = UI.Label(pill.transform, crime, Theme.SansSemi, 13, Theme.RedText, TextAnchor.MiddleCenter);
            UI.Stretch(pt.rectTransform, 0, 0, 0, 0);
            pt.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Tab hint
            string tabHint = showDeduction ? "[Tab] Fshih deduksionin" : "[Tab] Hap deduksionin";
            var th = UI.Card(hudHost, "TabHint", new Color(0.05f, 0.06f, 0.08f, 0.7f), Theme.Line, 8);
            float thw = UI.TextWidth(tabHint, Theme.Mono, 12) + 24;
            th.rectTransform.anchorMin = th.rectTransform.anchorMax = new Vector2(0, 0);
            th.rectTransform.pivot = new Vector2(0, 0);
            th.rectTransform.anchoredPosition = new Vector2(14, 60);
            th.rectTransform.sizeDelta = new Vector2(thw, 28);
            UI.Label(th.transform, tabHint, Theme.Mono, 12, Theme.Muted, TextAnchor.MiddleCenter);
        }

        void BuildRoomCard()
        {
            UI.Clear(roomCardHost);
            var card = UI.Card(roomCardHost, "Card", new Color(0.05f, 0.06f, 0.08f, 0.9f), Theme.Line2, 10);
            var root = card.transform;
            float iy = 12;
            if (selectedRoom < 0)
            {
                var t = UI.LabelAt(root, "Kliko një dhomë për të parë kush mund të ketë qenë aty në orën e zgjedhur. Afërohu një figure për ta marrë në pyetje.", Theme.Sans, 13, Theme.Text2, 14, iy, 272, 20);
                iy += UI.FitHeight(t, 272) + 12;
            }
            else
            {
                UI.LabelAt(root, caseDef.Rooms[selectedRoom].Name + " · " + caseDef.Hours[currentTime], Theme.SansBold, 15, Theme.Text, 14, iy, 272, 22);
                iy += 28;
                var dm = Session.Domains;
                for (int s = 0; s < caseDef.N; s++)
                {
                    string mark;
                    Color col;
                    if (Truth)
                    {
                        bool here = caseDef.Truth[s][currentTime] == selectedRoom;
                        mark = here ? "ishte këtu" : "nuk ishte këtu";
                        col = here ? Theme.GoldText : Theme.Muted;
                    }
                    else if (dm.Possible[s, currentTime, selectedRoom] && dm.Count[s, currentTime] == 1)
                    {
                        mark = "ishte këtu (e konfirmuar)";
                        col = Theme.GoldText;
                    }
                    else if (dm.Possible[s, currentTime, selectedRoom])
                    {
                        mark = "mund të ketë qenë këtu";
                        col = Theme.Text;
                    }
                    else
                    {
                        mark = "nuk ishte këtu";
                        col = Theme.Muted;
                    }
                    UI.LabelAt(root, caseDef.Suspects[s].Name, Theme.SansSemi, 13, col, 14, iy, 80, 20);
                    UI.LabelAt(root, mark, Theme.Sans, 13, col, 96, iy, 190, 20);
                    iy += 21;
                }
                if (caseDef.UseGraph)
                {
                    var links = new List<string>();
                    for (int r = 0; r < caseDef.RoomCount; r++)
                    {
                        if (r == selectedRoom) continue;
                        if (caseDef.CanMove(selectedRoom, r) && selectedRoom != r) links.Add(caseDef.Rooms[r].Name);
                    }
                    var lt = UI.LabelAt(root, "Lidhet me: " + (links.Count == 0 ? "—" : string.Join(", ", links.ToArray())), Theme.Sans, 12, Theme.Muted, 14, iy + 4, 272, 20);
                    iy += UI.FitHeight(lt, 272) + 6;
                }
                if (caseDef.HasItem && Session.Eval.ItemPossible != null)
                {
                    bool here = Session.Eval.ItemPossible[currentTime, selectedRoom];
                    var it = UI.LabelAt(root, here ? "Dosja mund të jetë këtu në " + caseDef.Hours[currentTime] + "." : "Dosja nuk është këtu në " + caseDef.Hours[currentTime] + ".", Theme.Sans, 12, here ? Theme.Gold : Theme.Muted, 14, iy + 4, 272, 20);
                    iy += UI.FitHeight(it, 272) + 6;
                }
                if (!Truth && caseDef.PhysicalClues.Count > 0 && Session.Remaining > 0)
                {
                    bool already = false;
                    foreach (var pc in Session.FoundClues) if (pc.Room == selectedRoom) already = true;
                    foreach (var pc in caseDef.PhysicalClues) if (pc.Room == selectedRoom && pc.Found) already = true;
                    var search = UI.Button(root, already ? "E kërkuar" : "Kërko dhomën · −1", already || Session.IsOver ? BtnStyle.Disabled : BtnStyle.Chip, () => SearchSelectedRoom(), 14, 8);
                    UI.Place(search.Rect, 14, iy + 4, 272, 40);
                    search.SetInteractable(!already && !Session.IsOver && Session.Remaining > 0);
                    iy += 50;
                }
                iy += 8;
            }
            UI.Stretch(card.rectTransform, 0, 0, 0, 0);
            roomCardHost.sizeDelta = new Vector2(300, iy);
        }

        public void SearchSelectedRoom()
        {
            if (selectedRoom < 0) return;
            var outcome = Session.SearchRoom(selectedRoom);
            if (!outcome.Accepted)
            {
                Panels.Toast(canvas.transform, outcome.Message, Theme.Red, 3f);
                if (outcome.BudgetExhausted) OpenAccusation();
                RefreshAll();
                return;
            }
            LastOutcome = outcome;
            if (caseDef.SecretEdgeActive && villa != null) villa.SetSecretPassageVisible(true);
            board.Refresh(currentTime, Truth);
            RefreshAll();
            Panels.Toast(canvas.transform, outcome.Message, caseDef.SecretEdgeActive ? Theme.Gold : Theme.Text, 4f);
            if (outcome.BudgetExhausted)
            {
                Panels.Toast(canvas.transform, "Buxheti mbaroi. Duhet të akuzosh.", Theme.Gold, 3.5f);
                OpenAccusation();
            }
        }

        void BuildTabs()
        {
            UI.Clear(tabsRow);
            // Shirit pergamene nën tabet, që teksti të lexohet mbi skenën 3D.
            var tabsBg = Panels.ParchmentCard(tabsRow, "TabsBg");
            UI.Stretch(tabsBg.rectTransform, 0, 0, 0, 0);
            string first = Session.IsOver ? "Rezultati" : "Marrja në pyetje";
            var a = UI.Button(tabsRow, first, tab == 0 ? BtnStyle.TabOn : BtnStyle.Tab, () => SetTab(0), 16, 10);
            UI.Place(a.Rect, 0, 0, 220, 44);
            int flagged = Session.Flagged.Count;
            string second = "Dosja e dëshmive (" + Session.Statements.Count + ")" + (flagged > 0 && !Session.IsOver ? "  <color=#F09A8F>!</color>" : "");
            var b = UI.Button(tabsRow, second, tab == 1 ? BtnStyle.TabOn : BtnStyle.Tab, () => SetTab(1), 16, 10);
            UI.Place(b.Rect, 228, 0, 280, 44);
        }

        void SetTab(int t)
        {
            tab = t;
            BuildTabs();
            RebuildRight(false);
            rightScroll.verticalNormalizedPosition = 1f;
        }

        void RebuildRight(bool scrollToEnd)
        {
            UI.Clear(rightContent);
            float y = 0, w = rightOverlayRoot.rect.width - 16, gap = 12;
            if (w < 200) w = 500;
            if (tab == 0)
            {
                if (Session.IsOver)
                {
                    y += panels.Result(rightContent, y, w) + gap;
                }
                else
                {
                    float h = panels.Coach(rightContent, y, w);
                    if (h > 0) y += h + gap;
                    y += interrogation.Build(rightContent, y, w) + gap;
                    h = panels.LastAnswer(rightContent, y, w);
                    if (h > 0) y += h + gap;
                }
            }
            else
            {
                y += panels.Dossier(rightContent, y, w) + gap;
            }
            rightContent.sizeDelta = new Vector2(0, y + 8);
            if (scrollToEnd) rightScroll.verticalNormalizedPosition = 0f;
        }

        // ==================== TUTORIAL ====================
        public Question TutorialHint
        {
            get
            {
                var step = CurrentTutorialStep;
                return step != null ? step.Fill : null;
            }
        }

        public TutorialStep CurrentTutorialStep
        {
            get
            {
                if (!tutorialVisible || Session.IsOver || TutorialIndex >= caseDef.Tutorial.Count) return null;
                return caseDef.Tutorial[TutorialIndex];
            }
        }

        public void FillTutorial()
        {
            var step = CurrentTutorialStep;
            if (step == null || step.Fill == null) return;
            Draft = step.Fill.Clone();
            if (step.Fill.Time >= 0) SetTime(step.Fill.Time);
            RebuildRight(false);
        }

        public void NextTutorial()
        {
            TutorialIndex++;
            RebuildRight(false);
        }

        public void ToggleTutorial()
        {
            tutorialVisible = !tutorialVisible;
            AlibiFlow.TutorialEnabled = tutorialVisible;
            BuildTopBar();
            RebuildRight(false);
        }

        // ==================== GAME LOGIC (unchanged) ====================
        public void SelectSuspect(int s)
        {
            if (Session.IsOver) return;
            Draft.Suspect = s;
            if (Draft.Other == s) Draft.Other = -1;
            BuildTabs();
            RebuildRight(false);
        }

        public void SelectType(QType t)
        {
            Draft.HasType = true;
            Draft.Type = t;
            RebuildRight(false);
        }

        public void SelectOther(int y)
        {
            Draft.Other = y;
            RebuildRight(false);
        }

        public void SelectTime(int t)
        {
            Draft.Time = t;
            SetTime(t);
            RebuildRight(false);
        }

        public void SelectRoomParam(int r)
        {
            Draft.Room = r;
            RebuildRight(false);
        }

        public string ComposeText()
        {
            if (Draft.Suspect < 0) return "Zgjidh të dyshuarin dhe pyetjen.";
            var sus = caseDef.Suspects[Draft.Suspect];
            if (!Draft.HasType) return "«" + sus.Def + ", …»";
            string phrase = QuestionEngine.Phrase(caseDef, Draft);
            return "«" + sus.Def + ", " + char.ToLowerInvariant(phrase[0]) + phrase.Substring(1) + "»";
        }

        public bool CanAsk(out string message)
        {
            if (Session.IsOver)
            {
                message = "Rasti u mbyll.";
                return false;
            }
            if (Session.Remaining <= 0 || (Session.Phase == Phase.Accusing && Session.ForcedAccusation))
            {
                message = "Buxheti mbaroi. Duhet të akuzosh.";
                return false;
            }
            var missing = QuestionEngine.Missing(Draft);
            if (missing.Count > 0)
            {
                message = "Mungon: " + string.Join(", ", missing.ToArray()) + ". Dërgimi është i bllokuar dhe buxheti nuk preket (D10).";
                return false;
            }
            var invalid = QuestionEngine.InvalidReason(caseDef, Draft);
            if (invalid != null)
            {
                message = invalid;
                return false;
            }
            var budget = Session.BudgetMessage(Draft.Type);
            if (budget != null)
            {
                message = budget;
                return false;
            }
            int dup = Session.DuplicateOf(Draft);
            int cost = QuestionEngine.Cost(Draft.Type);
            message = dup >= 0
                ? "Kjo pyetje u bë te #" + dup + " dhe nuk sjell informacion të ri. Kostoja " + cost + "."
                : "Gati · kostoja " + cost + " · mbeten " + Session.Remaining;
            return true;
        }

        public void Ask()
        {
            string message;
            if (!CanAsk(out message))
            {
                Panels.Toast(canvas.transform, message, Theme.Red, 3f);
                return;
            }
            var asked = Draft.Clone();
            var outcome = Session.Ask(asked);
            if (!outcome.Accepted)
            {
                Panels.Toast(canvas.transform, outcome.Message, Theme.Red, 3f);
                if (outcome.BudgetExhausted) OpenAccusation();
                RefreshAll();
                return;
            }
            LastOutcome = outcome;
            var step = CurrentTutorialStep;
            if (step != null && step.Fill != null && step.Fill.Key == asked.Key) TutorialIndex++;
            if (asked.Time >= 0) currentTime = asked.Time;
            timeline.Select(currentTime, false);
            BuildTopBar();
            BuildHud();
            board.Refresh(currentTime, Truth);
            if (showRight)
            {
                BuildTabs();
                RebuildRight(true);
            }
            BuildRoomCard();
            progressiveRules.OnQuestionAsked(outcome);
            if (outcome.Conflict != null) Panels.Toast(canvas.transform, "Kontraditë: disa deklarata nuk mund të jenë të gjitha të vërteta.", Theme.Red, 3f);
            if (outcome.BudgetExhausted)
            {
                Panels.Toast(canvas.transform, "Buxheti mbaroi. Duhet të akuzosh.", Theme.Gold, 3.5f);
                OpenAccusation();
            }
        }

        public void SetTime(int t)
        {
            currentTime = Mathf.Clamp(t, 0, caseDef.T - 1);
            timeline.Select(currentTime, false);
            board.Refresh(currentTime, Truth);
            BuildHud();
            BuildRoomCard();
        }

        void OnRoomClicked(int r)
        {
            selectedRoom = selectedRoom == r ? -1 : r;
            villa.SelectRoom(selectedRoom);
            BuildRoomCard();
        }

        void ShowOverlay(int kind)
        {
            UI.Clear(overlayRoot);
            var shade = Panels.Overlay(overlayRoot, "Shade");
            if (kind == 1) panels.Brief(shade);
            else if (kind == 2) panels.Accusation(shade);
            else if (kind == 3) Panels.HowTo(shade, CloseOverlay);
        }

        public void CloseOverlay()
        {
            UI.Clear(overlayRoot);
        }

        void ShowBrief()
        {
            if (IntroVideo.ShouldShow() && caseDef.CardNumber <= 1)
            {
                var iv = gameObject.AddComponent<IntroVideo>();
                iv.Play(canvas.transform, caseDef, () =>
                {
                    if (iv != null) Destroy(iv);
                    ShowOverlay(1);
                });
            }
            else
            {
                ShowOverlay(1);
            }
        }

        void ShowHowTo()
        {
            ShowOverlay(3);
        }

        public void EnterCase()
        {
            Session.Begin();
            CloseOverlay();
            RefreshAll();
        }

        public void OpenAccusation()
        {
            if (Session.IsOver) return;
            Session.StartAccusation();
            if (AccTime < 0 && caseDef.CrimeTimeKnown) AccTime = -1;
            ShowOverlay(2);
        }

        public void CancelAccusation()
        {
            Session.CancelAccusation();
            CloseOverlay();
        }

        public void SetAccusation(int person, int room, int time)
        {
            AccPerson = person;
            AccRoom = room;
            AccTime = time;
            ShowOverlay(2);
        }

        public void SubmitAccusation()
        {
            var o = Session.Accuse(AccPerson, AccRoom, AccTime);
            if (!o.Accepted)
            {
                Panels.Toast(canvas.transform, o.Message, Theme.Gold, 3f);
                ShowOverlay(2);
                return;
            }
            CloseOverlay();
            if (o.Verdict.Won && caseDef.CardNumber > 0) AlibiFlow.RecordWin(caseDef.CardNumber, Session.Stars);
            currentTime = caseDef.CrimeTime;
            tab = 0;
            selectedRoom = o.Verdict.Won ? caseDef.CrimeRoom : caseDef.Truth[AccPerson][caseDef.CrimeTime];
            villa.SelectRoom(selectedRoom);
            timeline.Select(currentTime, false);
            showDeduction = true;
            deductionPanel.gameObject.SetActive(true);
            showRight = true;
            rightOverlayRoot.gameObject.SetActive(true);
            RefreshAll();
            rightScroll.verticalNormalizedPosition = 1f;
            Panels.Toast(canvas.transform, o.Verdict.Won ? "Rasti u zgjidh." : "LOST: akuza ra. Shiko kufizimin që e përjashtonte.", o.Verdict.Won ? Theme.Gold : Theme.Red, 3.5f);
        }

        public void Restart()
        {
            StartSession(true);
            Session.Begin();
            RefreshAll();
        }

        public void AfterResult()
        {
            if (Session.Phase == Phase.Won && caseDef.CardNumber > 0 && caseDef.CardNumber < 4)
            {
                AlibiFlow.StartCase(LevelCards.Card(caseDef.CardNumber + 1));
                return;
            }
            string note = Session.Phase == Phase.Won ? caseDef.Title + " u zgjidh me " + Session.Stars + (Session.Stars == 1 ? " yll." : " yje.") : null;
            AlibiFlow.ToMenu(note);
        }

        public void ExitToMenu()
        {
            string note = null;
            if (!Session.IsOver && Session.Statements.Count > 0)
            {
                Session.Abandon();
                note = "Rasti u braktis pa akuzë (ABANDONED): pa yje. Mund ta rifillosh me të njëjtin seed (" + caseDef.Seed + ").";
            }
            else if (Session.IsOver && Session.Phase == Phase.Won)
            {
                note = caseDef.Title + " u zgjidh me " + Session.Stars + (Session.Stars == 1 ? " yll." : " yje.");
            }
            AlibiFlow.ToMenu(note);
        }
    }

    public sealed class PlayerWalkAnimator : MonoBehaviour
    {
        Transform model, legL, legR, armL, armR;
        Vector3 lastPos;
        float phase;
        float moveAmount;

        public void Setup(Transform modelRoot)
        {
            model = modelRoot;
            legL = Group("LegL", new Vector3(-0.11f, 0.80f, 0f), "ThighL", "KneeL", "ShinL", "CalfL", "AnkleL", "FootL", "SoleL");
            legR = Group("LegR", new Vector3(0.11f, 0.80f, 0f), "ThighR", "KneeR", "ShinR", "CalfR", "AnkleR", "FootR", "SoleR");
            armL = Group("ArmL", new Vector3(-0.28f, 1.32f, 0f), "UpperArmL", "ElbowL", "ForearmL", "WristL", "HandL", "ThumbL");
            armR = Group("ArmR", new Vector3(0.28f, 1.32f, 0f), "UpperArmR", "ElbowR", "ForearmR", "WristR", "HandR", "ThumbR");
            lastPos = transform.position;
        }

        Transform Group(string name, Vector3 pivot, params string[] parts)
        {
            var p = new GameObject(name).transform;
            p.SetParent(model, false);
            p.localPosition = pivot;
            foreach (var n in parts)
            {
                var t = model.Find(n);
                if (t != null) t.SetParent(p, true);
            }
            return p;
        }

        void OnEnable()
        {
            lastPos = transform.position;
        }

        void Update()
        {
            if (model == null) return;
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;

            var delta = transform.position - lastPos;
            delta.y = 0f;
            lastPos = transform.position;
            float speed = delta.magnitude / dt;
            if (speed > 30f) speed = 0f;

            moveAmount = Mathf.MoveTowards(moveAmount, Mathf.Clamp01(speed / 3f), dt * 6f);
            if (moveAmount > 0.01f) phase += dt * (5f + speed * 0.9f);

            float s = Mathf.Sin(phase) * moveAmount;
            legL.localRotation = Quaternion.Euler(s * 32f, 0f, 0f);
            legR.localRotation = Quaternion.Euler(-s * 32f, 0f, 0f);
            armL.localRotation = Quaternion.Euler(-s * 26f, 0f, 0f);
            armR.localRotation = Quaternion.Euler(s * 26f, 0f, 0f);
            model.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Cos(phase)) * 0.05f * moveAmount, 0f);
        }
    }
}
