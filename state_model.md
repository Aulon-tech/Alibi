# Alibi – Modeli i gjendjeve dhe tranzicioneve (v0.1)

Teknologjia: Unity (C#). Ky dokument përshkruan gjendjen e lojës në mënyrë të pavarur nga UI-ja.

## 1. Game state (gjendja e një rasti)

| Variabla | Tipi | Vlera fillestare | Kufizime |
|---|---|---|---|
| `level` | int | 1–6 (zgjedhet nga lojtari) | 1 ≤ level ≤ 6 |
| `caseSeed` | int | fiks për nivelet 1–5, i rastësishëm për nivelin 6 | së bashku me `level` rikrijon rastin |
| `suspects` | listë | 4 (N1), 5 (N2–3), 6 (N4–6) | secili ka ID, inicial, ngjyrë |
| `rooms` | listë | 5 / 7 / 7 / 9 / 9 / 9 (N1…N6) | nga vila me 9 dhoma + bodrum |
| `T` (intervale kohore) | int | 3 / 4 / 5 / 5 / 6 / 6 | T ≥ 3 |
| `edges` | bashkësi çiftesh dhomash | grafi i vilës | simetrik; pa kalimin e fshehtë |
| `secretEdgeActive` | bool | `false` | mund të bëhet `true` vetëm nga N4 e lart, pas provës fizike |
| `crimeRoom`, `crimeTime` | dhomë, int | të gjeneruara | `0 ≤ crimeTime < T` |
| `culprit` | i dyshuar | i fshehur nga lojtari | saktësisht 1 |
| `solution[s][t]` | dhomë | e vërteta e plotë | çdo hap `t→t+1` është qëndrim ose skaj i `edges` |
| `honestSet` | bashkësi | të gjithë pa fajtorin | |
| `errorWitness` | i dyshuar ose null | null (N1–2), 1 i ndershëm (N3+) | |
| `errorUsed` | bool | `false` | |
| `refusers[s]` | bashkësi llojesh pyetjesh | bosh (N1–4), jo bosh (N5+) | |
| `budgetTotal` | int | 12, 12, 11, 10, 9, min+2 | > 0 |
| `budgetRemaining` | int | = `budgetTotal` | 0 ≤ budgetRemaining ≤ budgetTotal |
| `domain[s][t]` | bashkësi dhomash | të gjitha `rooms` | nëse bëhet bosh → kontradiktë |
| `statements` | listë | bosh | (i dyshuari, lloji, parametrat, përgjigja/REFUZIM, kostoja) |
| `physicalClues` | listë | bosh | kufizime të forta, nuk genjehen |
| `confirmedHonest` | bashkësi | bosh | |
| `excludedCulprits` | bashkësi | bosh | perjashtuar nga rregulli R08 |
| `phase` | enum | `CASE_LOADED` | shih seksionin 2 |
| `forcedAccusation` | bool | `false` | `true` kur buxheti arrin 0 |
| `result`, `stars` | enum, int | null, 0 | `stars` ∈ {0,1,2,3} |

**Rikrijimi i situatës:** `(level, caseSeed)` + lista `statements` + `physicalClues` janë të mjaftueshme për të rikrijuar saktësisht çdo gjendje (gjeneratori është deterministik sipas `caseSeed`).

### Invariantët (duhet të vlejnë gjithmonë)
1. Çdo i dyshuar është në saktësisht një dhomë për interval në `solution`.
2. `0 ≤ budgetRemaining ≤ budgetTotal` dhe `questionsUsed = budgetTotal − budgetRemaining`.
3. Fajtori ndodhet në `crimeRoom` në `crimeTime`.
4. Fajtori genjen të paktën një herë gjatë rastit (kur lojtari i pyet mjaftueshëm).
5. Provat fizike nuk kontradiktojnë `solution`.

## 2. Gjendjet e lojës (`phase`)

`CASE_LOADED`, `QUESTIONING`, `ACCUSING`, `WON`, `LOST`, `ABANDONED`.

## 3. Tabela e tranzicioneve

| # | Nga | Ngjarja / kushti | Në | Efekti |
|---|---|---|---|---|
| T1 | – | `loadCase(level, seed)` | `CASE_LOADED` | inicializohen të gjitha variablat |
| T2 | `CASE_LOADED` | lojtari shtyp Start | `QUESTIONING` | shfaqet rasti (dhoma, ora, të dyshuarit) |
| T3 | `QUESTIONING` | pyetje e vlefshme, `budgetRemaining − kosto > 0` | `QUESTIONING` | zbritet buxheti, regjistrohet deklarata, ekzekutohet AC-3 |
| T4 | `QUESTIONING` | pyetje e vlefshme, `budgetRemaining − kosto = 0` | `ACCUSING` | `forcedAccusation = true`; pyetjet çaktivizohen |
| T5 | `QUESTIONING` | pyetje me `kosto > budgetRemaining` | `QUESTIONING` | refuzohet, asnjë ndryshim gjendjeje |
| T6 | `QUESTIONING` | lojtari shtyp Akuzo | `ACCUSING` | hapet forma (person, dhomë, orë) |
| T7 | `ACCUSING` | Anulo dhe `forcedAccusation = false` | `QUESTIONING` | kthim pa pasoja |
| T8 | `ACCUSING` | dorëzim i plotë, i saktë | `WON` | llogariten yjet |
| T9 | `ACCUSING` | dorëzim i plotë, me të paktën një gabim | `LOST` | shfaqet kufizimi që e perjashtonte kombinimin |
| T10 | `QUESTIONING` ose `ACCUSING` | lojtari del nga rasti | `ABANDONED` | pa yje |
| T11 | `WON` ose `LOST` | Provo përsëri | `CASE_LOADED` | i njëjti `caseSeed`, buxheti rivendoset |
| T12 | `WON` | Niveli tjetër | `CASE_LOADED` | `level + 1` |

Gjendjet përfundimtare: `WON`, `LOST`, `ABANDONED`. Nga to nuk ka tranzicion tjetër përveç T11/T12.
