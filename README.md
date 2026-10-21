# Alibi

Lojë deduktive misteri ku lojtari gjen fajtorin duke pyetur të dyshuarit dhe duke kapur kontradiktat mes deklaratave të tyre. Rasti ndërtohet si Constraint Satisfaction Problem me zgjidhje të vetme.

Grupi 6 — Dea Berisha, Nysret Ukaj, Aulon Miftari, Enis Dapko
Lënda: Zhvillimi i lojërave, FSHMN, Universiteti i Prishtinës

## Statusi

Faza e dizajnit. Prototipi është në letër; motori ende nuk është i implementuar.

## Çfarë ekziston

Dokumentacion dizajni, i verifikuar:

- `GDD_v0.1.pdf` — game design document, 9 faqe
- `rules_table.md` — rregullat R01–R13 në formë WHEN/IF … THEN
- `state_model.md` — variablat e gjendjes, invariantët dhe tranzicionet
- `test_cases.md` — 10 raste testi me rezultatin e pritshëm
- `prototype/paper_prototype.jpg` — tabela e prototipit në letër
- `playtest_note.md` — shënimi i playtest-it të 19 tetorit
- `core_loop.png` — diagrami i loop-it bazë

Kod burimor: nuk ka ende. Asnjë skedar `.cs`, asnjë projekt Unity në repo.

## Çfarë u provua

Playtest i prototipit në letër, 19 tetor 2026, rreth 10 minuta. Rasti 2 i nivelit 1: katër të dyshuar, pesë dhoma, tri orë, buxhet 12, par 6.

Lojtarja e gjeti fajtoren dhe e arsyetoi drejt, por shpenzoi tërë buxhetin — 12 pyetje kundrejt par-it 6. Dolën tri gjetje:

1. Katër nga trembëdhjetë inputet ishin të paplota ose me parametër të panevojshëm.
2. Kontradikta nuk sinjalizohet. Kur ajo u plotësua logjikisht, lojtarja nuk e vuri re dhe shpenzoi dy pyetje të tjera.
3. Buxheti shkoi edhe te orë që nuk ishin ora e krimit; loja nuk e ndihmon lojtarin ta dallojë informacionin që ngushton hapësirën.

Rastet janë verifikuar edhe me program: gjeneratori numëron zgjidhjet dhe ndalon te e dyta, dhe të tri rastet e prototipit dolën me zgjidhje të vetme.

## Çfarë mbetet

Backlog, sipas rendit:

- Shtresa `Alibi.Core` në C# pa varësi nga UnityEngine: gjeneruesi i rastit, AC-3, RuleEngine, ParPolicy
- Feedback kur dy kufizime bien ndesh — mungon te rregullat, doli nga playtest-i
- Validim i inputit para se të zbritet buxheti
- Projekti Unity: skena, Deduction Board, ndërfaqja e pyetjeve
- Testet njësi mbi 10 rastet e `test_cases.md`

## Verifikimi

Kalimi i fundit i QA-së: `docs/qa_kalimi_verifikimit.pdf`.
