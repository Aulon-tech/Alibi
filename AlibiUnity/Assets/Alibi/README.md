# Alibi – Unity demo (Grupi 6)

Ky folder e permban demon e plote te lojes Alibi ne Unity: menyja kryesore, tri kartat e niveleve nga PBT4, gjeneratori i rasteve, vila ne 3D dhe i gjithe gameplay-i (pyetjet, tabela e deduksionit, kontradiktat, akuza dhe rezultati). Logjika eshte ne `Alibi.Core` (C# i paster, pa UnityEngine), kurse pamja dhe UI jane ne `Alibi.Unity`, ashtu si ne planin e projektit.

## Kerkesat

Unity 2022.3 LTS ose Unity 6, me nje projekt 3D (URP ose Built-in, te dyja punojne). Nuk duhen TextMeshPro, Cinemachine apo asete te jashtme. Funksionon me Input Manager (Old) dhe me Input System.

## Si me e nis

1. Krijo nje projekt te ri 3D. Me Unity Hub: New project > 3D (URP). Me Unity CLI: `unity projects create Alibi --no-cloud` dhe pastaj `unity open ./Alibi`.
2. Kur hapet projekti, bone double-click ne `Alibi_Unity_Demo.unitypackage` (ose Assets > Import Package > Custom Package) dhe kliko Import.
3. Kur del dialogu "Alibi", kliko "Po, krijoji". Krijohen skenat `Alibi_MainMenu` dhe `Alibi_Game` dhe shtohen te Build Settings. Nese dialogu nuk del, perdor menyne lart: Alibi > Krijo skenat dhe Build Settings.
4. Shtyp Play, ose Alibi > Luaj demon. Per pamje te mire vendose Game view ne 1920x1080 (16:9).

Nese e shtyp Play ne `SampleScene` pa i krijuar skenat, demo hapet prape aty.

## Cka permban demo

Menyja ka Vazhdo, Kartat e niveleve, Rast i ri, Si luhet dhe Dil. Kartat 1–4 jane te verifikuara (te dyshuarit, dhomat, oret, buxheti, krimi, par-i) dhe hapen njera pas tjetres; per demo ka butonin "hap te gjitha kartat". Karta 1 ka udhezuesin hap pas hapi. Karta 4 (Kalimi) shton Vjedhjen, Zyre/Bodrum, Gentën dhe kalimin e fshehtë Biblioteka–Zyra (R06, butoni «Kërko dhomën»). Rast i ri gjeneron raste me seed per nivelin 1, nivelin 2 dhe Sabotimin; Karta 4 hapet si rast fiks (seed 404).

Ne loje: vila 3D me figurat (e plote = e konfirmuar, e tejdukshme = e mundshme, zhduket = e perjashtuar), slider-i i kohes, pyetjet Q1–Q5 me kosto, buxheti, tabela e deduksionit qe perditesohet vete (AC-3), dosja e deshmive, kontradikta e theksuar (D9), input i strukturuar (D10), akuza me person + dhome + ore, fitorja me yje dhe zinxhirin e kufizimeve, humbja me kufizimin qe e perjashtonte akuzen dhe pozicionet e verteta ne vile, "Provo perseri" me te njejtin seed (T11) dhe dalja pa akuze si ABANDONED (T10).

## Struktura e kodit

- `Core/` – Model, QuestionEngine, CspSolver (AC-3 + backtracking per cdo hipoteze fajtori, edhe me deshmitar qe gabon nje here), DomainMatrix, GameSession (gjendjet CASE_LOADED deri ABANDONED), CaseGenerator, LieSelector, ParPolicy, LevelCards.
- `Unity/` – GameManager, MapBuilder, RoomView, SuspectMarker, DeductionBoardView, PropagationAnimator, TimelineController, InterrogationPanel, CameraRig, MainMenuController dhe panelet e UI.
- `Editor/` – AlibiSetup (krijon skenat, Build Settings dhe materialin per URP ose Built-in).
- `Tests/` – EditMode: kartat 1–4 sipas zgjidhjes se dokumentuar (përfshi R06 / TC-10), TC-01 deri TC-10, yjet, unikesia e gjeneratorit dhe zbulimi i rastit me defekt.
- `Resources/AlibiFonts/` – Inter, Cormorant Garamond dhe JetBrains Mono (licenca OFL).

## Testet

Window > General > Test Runner > EditMode > Run All. Me CLI: `unity test` brenda folderit te projektit.

## Shenime

Sabotimi eshte variant eksperimental (4 te dyshuar, 3 ore, buxhet 12). Me parametrat e plote te nivelit 3 (5 te dyshuar, 5 ore, buxhet 11) par-i i llogaritur del 18–25 pyetje, sepse cdo i dyshuar duhet te perjashtohet ne cdo ore. Kjo eshte pike per vendim te ekipit, njesoj si divergjencat ne PBT4.

Akuza e sakte por e paprovuar llogaritet si humbje: loja ndjek parimin e GDD, fitohet vetem kur zgjidhesi konfirmon qe asnje kombinim tjeter nuk qendron.

Nese del ndonje gabim ne Console pas importit, bone screenshot te gabimit te pare dhe dergoje.
