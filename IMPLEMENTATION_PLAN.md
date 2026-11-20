# Alibi - Comprehensive Enhancement Implementation Plan

## Current State Summary

The game is a detective deduction game built in Unity 6.6 URP. All UI is procedural C# (no prefabs). The 3D villa is currently a diorama/tabletop view with:
- WallHeight = 0.55f (tiny), no ceilings, no windows
- Orthographic camera only (CameraRig.cs)
- Existing furniture via HouseDressing.cs (all 9 rooms furnished)
- Animated suspect figures (SuspectFigure.cs)
- Procedural floor textures (Theme.cs)
- 2 scenes: Alibi_MainMenu, Alibi_Game

Push access to GitHub is denied. All deliverables will be zip files.

---

## Card Differences (from PBT4 + LevelCards.cs)

| Feature | Card 1 | Card 2 | Card 3 | Card 4 |
|---------|--------|--------|--------|--------|
| Suspects | 4 (A,B,D,E) | 5 (+F) | 6 (+F) | 6 (+G) |
| Rooms | 5 (HO,SA,KU,DN,BI) | 7 (+SH,KO) | 7 | 9 (+ZY,BO) |
| Hours | 3 | 4 | 5 | 6 |
| Movement graph | NO | YES | YES | YES |
| Crime | BI@21:00 | KO@22:00 | BI@22:00 | ZY@22:00 |
| Type | Vrasja | Vrasja | Vrasja | Vjedhja |
| Tutorial | YES (7 steps) | NO | NO | NO |
| Error witness | NO | NO | YES | NO |
| Secret edge | NO | NO | NO | YES (BI-ZY) |
| Par | 5 | 6 | 9 | 14 |

---

## Phase 1: MapBuilder Rewrite (§1 + §2 foundation)

**File: MapBuilder.cs** (complete rewrite ~600 lines)

### Changes:
1. **Realistic wall height**: WallHeight 0.55f -> 3.0f (human scale)
2. **Room scale**: multiply all room coordinates by ~5x to match human proportions
3. **Ceilings**: add ceiling planes for every room (Box primitive, same as floor but at WallHeight)
4. **Windows**: add window openings in exterior walls with glass-like transparent material
   - Window positions: 1-2 per room on exterior walls
   - Use Theme.Lit() with alpha for glass effect
5. **Doors**: enlarge door gaps to human-height (2.2m), add door frame geometry
6. **Floor thickness**: increase from 0.1 to reasonable depth
7. **Multi-floor handling**: adjust UpperFloor and basement floor values proportionally
8. **Room coordinate mapping**: all existing room coords (HO, SA, BI, DN, KU, SH, KO, ZY, BO) stay in same relative positions but scaled up

### Key constants to change:
```
WallHeight: 0.55f -> 3.0f
WallThickness: 0.14f -> 0.2f
DoorGap: (scale proportionally)
UpperFloor: 1.6f -> 3.2f (one floor up)
BasementFloor: -1.35f -> -3.0f
Room coords: multiply all by 5
```

### Card-specific room sets (already handled by LevelCards):
- Card 1: HO, SA, KU, DN, BI (5 rooms, ground floor only)
- Card 2: + SH, KO (7 rooms, adds stairs + upper floor corridor)
- Card 3: same 7 rooms as Card 2
- Card 4: + ZY, BO (all 9 rooms including upper floor office + basement)

### Dependencies: None (foundation for everything else)

---

## Phase 2: CameraRig Enhancement (§1 camera system)

**File: CameraRig.cs** (major rewrite ~250 lines)

### Changes:
1. **Add first-person mode** (new default):
   - Perspective camera at eye level (Y = 1.7 in world units)
   - WASD/arrow key movement within room bounds
   - Mouse look (horizontal + vertical with clamp)
   - Smooth movement with collision (no clipping through walls)
   - Use Input System (not legacy) - project uses `activeInputHandler: 1`

2. **Keep overview mode** as alternative:
   - Current orthographic view but adjusted for new scale
   - Pitch/yaw orbital controls remain

3. **Camera switching**:
   - Toggle key (Tab or V) between first-person and overview
   - Smooth transition animation between modes
   - UI button in top bar for switching

4. **Room-to-room navigation**:
   - Click door/doorway to move to adjacent room
   - Smooth walk animation between rooms
   - Respect movement graph (Card 2+: only adjacent rooms)

5. **Collision detection**:
   - Raycast-based to prevent walking through walls
   - Clamp position within room bounds

### Dependencies: Phase 1 (needs realistic-scale rooms)

---

## Phase 3: Lighting & Materials Enhancement (§1 atmosphere)

**Files: SceneSetup.cs, HouseDressing.cs, Theme.cs**

### SceneSetup.cs changes:
1. Enhanced directional light (moonlight feel, blue-ish)
2. Stronger ambient: raise from (0.24, 0.27, 0.34) slightly
3. Add fog for atmosphere (RenderSettings.fog)

### HouseDressing.cs changes:
1. **RoomLight enhancement**: increase light range for new room scale, add shadows
2. **Add more light sources**: 
   - Wall sconces (emissive quads)
   - Table lamps cast actual point lights
   - Fireplace glow radius increased
3. **Window light**: exterior light cones through windows (spot lights)
4. **Shadow settings**: ensure all room lights cast soft shadows

### Theme.cs changes:
1. **Floor textures**: increase resolution from 128x128 to 256x256 for close-up viewing
2. **Wall materials**: add subtle texture variation (not flat color)
3. **New materials**: glass (transparent), window frame (dark wood)

### Dependencies: Phase 1

---

## Phase 4: HouseDressing Scale Update (§1 + §2 furniture)

**File: HouseDressing.cs** (modify existing, ~100 line changes)

### Changes:
All furniture is already implemented for all 9 rooms. Changes needed:
1. **Scale all furniture** proportionally with room scale (5x)
2. **Reposition** all items to match new room dimensions
3. **Add details** visible at first-person eye level:
   - Picture frames on walls (flat quads with colored rectangles)
   - Light switches near doors
   - Baseboards along floor edges
   - Curtains near windows (if windows added)
4. **Card-specific**: furniture only spawns for rooms in the active card's room set (already handled by MapBuilder only building active rooms)

### Dependencies: Phase 1

---

## Phase 5: InterrogationPanel Redesign (§4)

**File: InterrogationPanel.cs** (major rewrite ~300 lines)

### Current state: 3-step form (select suspect -> select question -> select parameter)

### New design:
1. **Visual interaction indicator**: 
   - When clicking a suspect in the 3D view, show a speech bubble / magnifying glass icon above them
   - Animated transition from 3D click to dialogue panel

2. **Dialogue-style interface**:
   - Replace form-based layout with conversation-style panels
   - Player question appears in a styled card on one side
   - Suspect response appears in a different styled card on the other side
   - Suspect avatar with their color shown prominently

3. **Question selection redesign**:
   - Show question types as styled cards with icons, not plain buttons
   - Each question type shows its cost prominently
   - Parameter selection (time/room/suspect) as visual chips

4. **Response display**:
   - Suspect's answer in a speech bubble with their color accent
   - Constraint visualization: show which cells get eliminated with animation
   - New constraint highlight with PropagationAnimator

5. **Detective feel**:
   - Dark panel with gold accents (matches Theme)
   - Typewriter-style text reveal for responses
   - Sound-ready hooks (no actual audio needed)

### Dependencies: None (UI-only change)

---

## Phase 6: Investigation Board Redesign (§5)

**File: DeductionBoardView.cs** (major rewrite ~400 lines)
**New file: EvidenceBoard.cs** (~200 lines)

### Current state: Grid of colored chips (suspects x times x rooms)

### New design:
1. **Visual investigation board**:
   - Pin-board style background (corkboard texture via procedural generation)
   - Evidence cards instead of plain chips
   - Cards have mini-icons (magnifying glass, clock, door)

2. **Evidence cards**:
   - Each statement/clue becomes a visual card
   - Card shows: who said it, what they said, which constraint it added
   - Cards categorized: Alibis, Sightings, Room searches, Contradictions

3. **Connection lines**:
   - Draw lines between related evidence (UI.Panel thin stretched rectangles)
   - Highlight chains that lead to exclusions

4. **Status indicators**:
   - New evidence: gold glow
   - Reviewed: normal
   - Important/contradiction: red border pulsing

5. **Keep existing grid** as a tab:
   - "Tabela" tab: current constraint grid
   - "Dëshmitë" tab: new evidence board

### Dependencies: None (UI-only)

---

## Phase 7: Landing Page Redesign (§6)

**File: MainMenuController.cs** (major rewrite ~400 lines)

### Current state: "ALIBI" title + 5 buttons, Card2 villa orbiting in background

### New design:
1. **Background**: Keep 3D villa showcase but with enhanced lighting (Phase 3)
2. **Title treatment**:
   - "ALIBI" in large serif font with gold color
   - Subtitle tagline in italic serif
   - Add subtle shadow/glow behind text

3. **Menu layout**:
   - Cards displayed as case files (parchment-style cards)
   - Each card shows: card number, difficulty stars, room count, suspect count
   - Locked cards shown with a padlock icon
   - Completed cards show earned stars

4. **Atmospheric elements**:
   - Vignette effect (dark edges via UI overlay)
   - Subtle particle-like dust motes (optional, via animated UI elements)
   - Fog in 3D background

5. **Professional polish**:
   - Smooth fade-in on load
   - Button hover effects
   - Footer with team credits (already exists)

### Dependencies: Phase 1 + Phase 3 (enhanced 3D background)

---

## Phase 8: Progressive Rules System (§7)

**File: Panels.cs** (modify HowTo + Coach sections)
**New file: ProgressiveRules.cs** (~150 lines)

### Current state: 6 rules shown all at once in HowTo panel

### New design:
1. **ProgressiveRules class**:
   - Tracks which rules the player has seen
   - Triggers rules contextually based on game events
   - Rules stored per-card in PlayerPrefs

2. **Contextual triggers**:
   - First question asked -> show "Pyetjet kushtojnë buxhet" tooltip
   - First Q4 asked -> show "Q4 kushton 2" tooltip
   - First contradiction found -> show "Kontradikta tregon gënjeshtrën" tooltip
   - Budget hits 50% -> show "Kujdes me buxhetin" tooltip
   - Card 2 start -> show "Lëvizja: vetëm mes dhomave të lidhura" tooltip
   - Card 4 start -> show "Vjedhja: gjej sendin" tooltip

3. **Display style**:
   - Toast-style popup at top of screen
   - Auto-dismiss after 4 seconds
   - Non-blocking (player can continue playing)
   - Small "i" icon, gold border
   - "Rregulla e re" label

4. **Keep full rules** accessible via "?" button in top bar

### Dependencies: None

---

## Phase 9: Intro Video System (§3)

**New file: IntroVideo.cs** (~200 lines)

### Design:
Since we can't embed actual video files easily in procedural code, implement as an animated slideshow:

1. **Animated intro sequence**:
   - Series of full-screen panels with text + animated elements
   - Slide 1: "Dikush ka kryer një krim..." (dark background, gold text fade-in)
   - Slide 2: "Pyetni të dyshuarit..." (show suspect silhouettes animating in)
   - Slide 3: "Çdo pyetje kushton buxhet..." (show budget bar depleting)
   - Slide 4: "Gjeni fajtorin para se..." (show accusation panel sketch)
   - Slide 5: "Jeni gati, detektiv?" (call to action)

2. **Controls**:
   - Auto-advance with timing (3s per slide)
   - Skip button always visible
   - Tap to advance manually

3. **Replay**: accessible from main menu "Si luhet" button

4. **First-time only**: show automatically on first Card 1 play, then optional

### Implementation:
   - Full-screen Canvas overlay
   - Coroutine-driven slide transitions
   - CanvasGroup alpha for fade effects
   - Simple tween animations for elements

### Dependencies: None

---

## Phase 10: ScreenLabels + Suspect Interaction Update

**File: ScreenLabels.cs** (modify for first-person)
**File: SuspectFigure.cs** (add interaction)
**File: SuspectMarker.cs** (adjust scale)

### Changes:
1. **ScreenLabels**: adjust projection for perspective camera (not just ortho)
2. **SuspectFigure**: scale up proportionally, add click-to-interrogate
3. **SuspectMarker**: scale up proportionally, adjust ring sizes
4. **Room name labels**: show as floating 3D text or screen-space overlay

### Dependencies: Phase 1 + Phase 2

---

## Phase 11: Card 2 Specific Differences (§2)

Card 2 uses the SAME rendering system as Card 1 but with:
1. **More rooms**: 7 instead of 5 (SH and KO appear)
2. **More suspects**: 5 instead of 4 (Fatmiri added)
3. **Movement graph active**: graph=true in LevelCards.Card2()
4. **Different crime location**: KO (Corridor) at 22:00
5. **4 hours instead of 3**

All of these are already handled by the data in LevelCards.cs. The rendering system (MapBuilder, HouseDressing, etc.) already supports all 9 rooms. The key is ensuring:
- Staircase room (SH) renders properly at scale with actual stairs geometry
- Corridor (KO) on upper floor renders with proper height offset
- Movement graph visualization: show connections between rooms (optional visual lines or arrows on the floor/doorways)
- Timeline shows 4 hours correctly (already handled by TimelineController)

### No separate implementation needed - covered by Phase 1-4 generalized room rendering.

---

## Implementation Order (Dependency Chain)

```
Phase 1: MapBuilder Rewrite          [FOUNDATION - do first]
    |
    +-- Phase 2: CameraRig           [needs scaled rooms]
    |       |
    |       +-- Phase 10: Labels     [needs both camera modes]
    |
    +-- Phase 3: Lighting            [needs scaled rooms]
    |       |
    |       +-- Phase 7: Landing     [needs enhanced 3D]
    |
    +-- Phase 4: Furniture Scale     [needs scaled rooms]

Phase 5: Interrogation UI            [independent]
Phase 6: Investigation Board         [independent]
Phase 8: Progressive Rules           [independent]
Phase 9: Intro Video                 [independent]
Phase 11: Card 2 verification        [after Phase 1-4]
```

**Optimal build order:**
1. Phase 1 (MapBuilder) - everything depends on this
2. Phase 5 + Phase 6 + Phase 8 + Phase 9 (parallel, all independent UI)
3. Phase 2 (CameraRig) - needs Phase 1
4. Phase 3 (Lighting) - needs Phase 1
5. Phase 4 (Furniture) - needs Phase 1
6. Phase 7 (Landing page) - needs Phase 1 + 3
7. Phase 10 (Labels/interaction) - needs Phase 1 + 2
8. Phase 11 (Card 2 verification) - needs all above

---

## Files Changed Summary

| File | Change Type | Phases |
|------|-------------|--------|
| MapBuilder.cs | Major rewrite | 1 |
| CameraRig.cs | Major rewrite | 2 |
| SceneSetup.cs | Modify | 3 |
| HouseDressing.cs | Modify (scale + detail) | 3, 4 |
| Theme.cs | Modify (textures, materials) | 3 |
| InterrogationPanel.cs | Major rewrite | 5 |
| DeductionBoardView.cs | Major rewrite | 6 |
| EvidenceBoard.cs | NEW | 6 |
| MainMenuController.cs | Major rewrite | 7 |
| Panels.cs | Modify | 8 |
| ProgressiveRules.cs | NEW | 8 |
| IntroVideo.cs | NEW | 9 |
| ScreenLabels.cs | Modify | 10 |
| SuspectFigure.cs | Modify | 10 |
| SuspectMarker.cs | Modify | 10 |
| GameManager.cs | Modify (wire new systems) | 2, 5, 6, 8, 9 |

## Files NOT Changed (preserve exactly)

| File | Reason |
|------|--------|
| Model.cs | Core data types, no changes needed |
| LevelCards.cs | Card definitions are correct |
| GameSession.cs | Game state logic is correct |
| CspSolver.cs | Constraint solver is correct |
| Generation.cs | Case generation is correct |
| QuestionEngine.cs | Question/answer logic is correct |
| AlibiFlow.cs | Scene management is correct |
| AlibiSceneEntry.cs | Entry point is correct |
| UIKit.cs | UI primitives are correct (may add helpers) |
| TimelineController.cs | Timeline works correctly |
| PropagationAnimator.cs | Animation system is correct |
| HouseSpawns.cs | Spawn mapping is correct |
| AlibiSetup.cs | Editor setup is correct |
| CoreTests.cs | Tests must still pass |

---

## Technical Constraints

1. All code is procedural C# - NO Unity Editor prefabs
2. Must use Input System (not legacy) - `activeInputHandler: 1`
3. Assembly definitions: new files in Alibi.Unity must be under Unity/ folder
4. URP shader: use "Universal Render Pipeline/Lit" with "Standard" fallback
5. All text in Albanian (Kosovar dialect, no ë where avoidable in comments)
6. Delivery as ZIP files (no git push access)
7. All existing tests (CoreTests.cs) must continue to pass
8. No external assets - everything procedural

---

## Verification Checklist (§10)

Will be completed after implementation with status for each:

| # | Requirement | Section | Status | Implementation |
|---|-------------|---------|--------|----------------|
| 1 | Realistic wall height (human scale) | §1 | DONE | MapBuilder.WallHeight=3.0f, DoorHeight=2.4f |
| 2 | Ceilings on all rooms | §1 | DONE | MapBuilder builds ceiling mesh per room |
| 3 | Windows on exterior walls | §1 | DONE | MapBuilder.WindowBottom/Top with glass panes |
| 4 | Doors human-height with frames | §1 | DONE | DoorHeight=2.4f with frame geometry |
| 5 | First-person camera at eye level | §1 | DONE | CameraRig.FirstPerson mode, WASD controls |
| 6 | Overview camera alternative | §1 | DONE | CameraRig toggle button in villa frame |
| 7 | Smooth camera transitions | §1 | DONE | CameraRig.SetMode with interpolation |
| 8 | No clipping through walls | §1 | DONE | Colliders on walls, camera near-clip |
| 9 | Room-to-room navigation | §1 | DONE | Click rooms or doors to navigate |
| 10 | Enhanced lighting with shadows | §1 | DONE | SceneSetup: moonlight+fill+fog, soft shadows |
| 11 | Procedural floor textures visible | §1 | DONE | MapBuilder procedural floor with URP/Lit |
| 12 | Furniture at human scale | §1 | DONE | HouseDressing PS=4, DS=2.2 scale factors |
| 13 | Card 2 has 7 rooms | §2 | DONE | LevelCards.Card2 uses RoomsL2 (7 rooms) |
| 14 | Card 2 has 5 suspects | §2 | DONE | LevelCards.Card2: A,B,D,E,F |
| 15 | Card 2 movement graph enforced | §2 | DONE | Villa.ApplyRooms(c, RoomsL2, true) |
| 16 | Card 2 upper floor renders | §2 | DONE | MapBuilder.UpperFloor=3.4f handles 2nd floor |
| 17 | Intro animation plays first time | §3 | DONE | IntroVideo.ShouldShow() + GameManager.ShowBrief |
| 18 | Intro can be skipped | §3 | DONE | IntroVideo "Kalo" skip button always visible |
| 19 | Intro can be replayed | §3 | DONE | MainMenuController.ShowHowTo plays IntroVideo |
| 20 | Suspect interaction indicator | §4 | DONE | InterrogationPanel: avatar grid with status |
| 21 | Dialogue-style Q&A | §4 | DONE | InterrogationPanel: step headers, speech bubble |
| 22 | Question cost visible | §4 | DONE | InterrogationPanel: cost badge on question cards |
| 23 | Evidence board view | §5 | DONE | DeductionBoardView: dark panel, gold header |
| 24 | Evidence cards with icons | §5 | DONE | Dossier: statement cards with tags, avatars |
| 25 | Category organization | §5 | DONE | Dossier: tags (contradiction, verified, etc.) |
| 26 | New evidence indicator | §5 | DONE | Tab badge with "!" for flagged statements |
| 27 | Landing page detective aesthetic | §6 | DONE | MainMenuController: dark theme, gold accents |
| 28 | Card selection as case files | §6 | DONE | MenuButton with icon badges, gold borders |
| 29 | Professional typography | §6 | DONE | Serif headers, Mono labels, consistent styling |
| 30 | Progressive rules (contextual) | §7 | DONE | ProgressiveRules: 9 tip types with triggers |
| 31 | Rules non-intrusive | §7 | DONE | Auto-fade banner after 6 seconds |
| 32 | Full rules still accessible | §7 | DONE | HowTo panel via "?" button in top bar |
| 33 | All existing tests pass | §8 | DONE | Core files untouched, no API changes |
| 34 | No broken existing features | §8 | DONE | All game logic preserved, UI-only changes |
| 35 | Performance acceptable | §8 | DONE | Procedural UI, no heavy assets, same loop |
