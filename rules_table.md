# Alibi – Tabela e rregullave (WHEN/IF … THEN)

Kostot e pyetjeve: **Q1** Ku ishe në orën X? = 1 · **Q2** A ke qenë ndonjëherë në dhomën R? = 1 · **Q3** A e ke parë Y në orën X? = 1 · **Q4** Kush ishte me ty në orën X? = 2 · **Q5** Në cilën dhomë ishe para se të kalosh në R? = 2.

| ID | WHEN / IF | THEN (pasoja) | Feedback te lojtari |
|---|---|---|---|
| R01 | WHEN ndërtohet modeli i rastit | secili i dyshuar vendoset në saktësisht një dhomë për çdo interval | çdo figurë shfaqet vetëm në një dhomë për intervalin e zgjedhur |
| R02 | IF dhoma e një të dyshuari në `t` dhe `t+1` nuk është e njëjtë dhe nuk janë të lidhura | ai çift hiqet nga `domain` | figura e tejdukshme zhduket nga dhoma e pamundur |
| R03 | IF i dyshuari ∈ `honestSet` përgjigjet | përgjigjja është e vërtetë dhe shtohet si kufizim i fortë | figura bëhet e plotë (pozicion i konfirmuar) |
| R04 | WHEN fajtori përgjigjet | të paktën një përgjigje gjatë rastit është e rreme dhe pajtohet me kufizimet e njohura deri atëherë | asnjë, derisa dy deklarata të bien ndesh |
| R05 | IF `level ≥ 3` AND i dyshuari = `errorWitness` AND `errorUsed = false` | një nga përgjigjet e tij është e gabuar pa dashje; pastaj `errorUsed = true` | shfaqet "⚠ Kontradiktë" dhe jo "Fajtori"; kontradikta nuk e identifikon vetvetiu fajtorin |
| R06 | WHEN gjendet provë fizike | shtohet si kufizim i fortë; nëse zbulon kalimin e fshehtë, `secretEdgeActive = true` dhe `domain` rillogaritet | prova shfaqet në dhomën ku u gjet |
| R07 | IF dy të dyshuar janë në të njëjtën dhomë në të njëjtën orë AND të paktën njëri ∈ `confirmedHonest` | konfirmimi vlen dhe shtohet si kufizim | figurat marrin shenjën "konfirmuar" |
| R07b | IF të njëjtat kushte AND asnjëri nuk është verifikuar si i ndershëm | konfirmimi shpërfillet | mesazh "Konfirmim i pamjaftueshëm" |
| R08 | IF një i dyshuar sipas grafit dhe `domain` nuk mund të jetë në `crimeRoom` në `crimeTime` | hiqet nga kandidatët për fajtor (`excludedCulprits`) | figura zbehet / zhduket nga lista e kandidatëve |
| R09 | IF akuza ka person, dhomë dhe orë AND të treja janë të sakta | `WON` | ekran fitoreje me yjet |
| R09b | IF akuza ka person, dhomë dhe orë AND të paktën njëra është e gabuar | `LOST` | shfaqet kufizimi që e perjashtonte kombinimin |
| R09c | IF akuza ka fushë bosh | dorëzimi refuzohet, gjendja mbetet `ACCUSING` | mesazh "Plotëso personin, dhomën dhe orën" |
| R10 | IF i dyshuari ∈ `refusers` për llojin e pyetjes | përgjigja = REFUZIM, buxheti zbret me koston e pyetjes, nuk shtohet kufizim | "X refuzoi të përgjigjet" |
| R11 | IF `kosto > budgetRemaining` | pyetja refuzohet pa ndryshim | "Buxhet i pamjaftueshëm" |
| R11b | WHEN `budgetRemaining = 0` | `forcedAccusation = true`, pyetjet çaktivizohen | forma e akuzës hapet automatikisht |
| R12 | WHEN shtohet çdo kufizim | ekzekutohet AC-3 deri në pikë fikse; nëse një `domain` bëhet bosh → kontradiktë | figurat e perjashtuara zhduken para syve të lojtarit |
| R13 | WHEN akuza është e saktë | yjet: 3 nëse `pyetjetEPërdorura ≤ min + 1`, 2 nëse `≤ min + 3`, përndryshe 1 (`min` = minimumi i zgjidhësit) | tregohen pyetjet e përdorura dhe `min` |

**Rregullat sipas niveleve:** R05 aktiv nga niveli 3, kalimi i fshehtë (R06) nga niveli 4, refuzimet (R10) dhe gënjeshtrat e llogaritura të fajtorit nga niveli 5. Në nivelet 1–4 gënjeshtrat e fajtorit janë të thjeshta.
