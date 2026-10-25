# Feedback Matrix dhe situatat e dështimit — Enis Dapko

Alibi · Grupi 6 · PBT 4 · docs/feedback_matrix.md

## 1. Feedback Matrix

| Veprimi ose ngjarja | Reagimi i sistemit | Feedback-u i dukshëm | Rregulli |
|---|---|---|---|
| Veprim i vlefshëm — Q1 “Ku ishe në orën 2?” te S2 (buxheti 12) | Përgjigjja shtohet si kufizim. Sistemi heq mundësitë që nuk mbahen më (AC-3). Buxheti zbret nga 12 në 11. | Deklarata del në listë pa shenjë dyshimi. Dhomat që nuk mundën më shënohen si të përjashtuara. Figurat ndryshojnë menjëherë (e plotë = e sigurt, e tejdukshme = ende e mundshme). Numëruesi tregon 11. | R03, R02, R12 (T01) |
| Veprim i vlefshëm me kosto 2 — Q4 “Kush ishte me ty në orën 1?” te S1 (buxheti 11) | Shtohen kufizime për të gjithë ata që ishin aty. Buxheti zbret me 2, pra nga 11 në 9. | Njerëzit që ishin bashkë shfaqen në tabelë. Numëruesi ndryshon menjëherë nga 11 në 9. | R03, R12 (T02) |
| Veprim i vlefshëm, por pa përgjigje — I dyshuari refuzon të përgjigjet (niveli 5 e lart) | Nuk shtohet kufizim i ri. Buxheti zbret njësoj sikur të kishte përgjigjur. | “Nuk përgjigjem për këtë”. Numëruesi i buxhetit zbret. | R11 |
| Veprim i refuzuar — Pyetje me kosto 2 kur buxheti është 1 | Pyetja nuk pranohet. Buxheti dhe gjendja mbeten njësoj. Lojtari mund të zgjedhë vetëm pyetje me kosto 1. | Pyetjet me kosto 2 bëhen gri. Mesazhi: “Buxheti nuk mjafton: kjo pyetje kushton 2, të ka mbetur 1.” (propozim) | R12 (T08) |
| Veprim i refuzuar — Q2 me dhomë që nuk ekziston në këtë nivel | Pyetja nuk pranohet para se të preket buxheti. Gjendja nuk ndryshon. | Mesazhi: “dhomë e pavlefshme”. Numëruesi mbetet njësoj. | R12 (T05) |
| Veprim i refuzuar — Pyetje kur buxheti është 0 | Pyetja nuk pranohet. Loja kalon te akuza (ACCUSING) dhe lojtari duhet të akuzojë. | Butonat e pyetjeve çaktivizohen. Mesazhi: “Buxheti mbaroi. Duhet të akuzosh.” (propozim) | R12, D2 (T04) |
| Veprim i refuzuar — Akuzë e paplotë (person dhe dhomë, pa orë) | Akuza nuk dërgohet. Buxheti nuk preket. Loja mbetet në të njëjtën fazë. | Fusha që mungon theksohet. Mesazhi: “Plotëso edhe orën para se të akuzosh.” (propozim) | R10 (T06) |
| Veprim i refuzuar — Dy të dyshuar thonë se ishin bashkë, por asnjëri nuk është i verifikuar si i ndershëm | Konfirmimi nuk pranohet si i sigurt. | Pranë deklaratës del “konfirmim i paverifikuar”. | R08 (T09) |
| Sukses — Akuzë me person, dhomë dhe orë të sakta | Sistemi kontrollon që nuk ka asnjë zgjidhje tjetër. Loja kalon në WON. | Mesazhi: “Rasti u zgjidh.” (propozim). Shfaqen pyetjet e përdorura krahas par-it dhe yjet: 3 yje nëse ≤ par+1, 2 yje deri në par+3, përndryshe 1 yll. | R10, §10, §12 |
| Dështim — Akuzë ku të paktën një nga tri fushat është e gabuar | Loja kalon në LOST dhe rasti mbyllet. | Del “LOST” dhe kufizimi që e përjashtonte akuzën. Lojtari mund të shtypë “Provo përsëri”. | R10 (T07), D1 |
| Ngjarje e re: kontradikta — Një kufizim i ri bie ndesh me një ekzistues (p.sh. Drita thotë se ishte në Sallon, por një dëshmitar i ndershëm thotë të kundërtën) | Sistemi e kupton që dy deklarata nuk mund të jenë të dyja të vërteta. | Të dyja deklaratat theksohen menjëherë në tabelë. | E propozuar: D9 (ende nuk është në R01–R13) |

## 2. Dy situata dështimi

| Rasti | Shkaku | Rezultati i sistemit | Mesazhi | Rikuperimi |
|---|---|---|---|---|
| 1 — humbje e lojtarit | Niveli 3 (Sabotimi: dhoma dihet, ora jo). Lojtari zgjedh saktë personin dhe dhomën, por orën e gabuar, sepse nuk e ka ngushtuar orën me pyetje. Kjo ndodhi edhe në playtest (Evidenca 3). | Sipas R10, akuza nuk pranohet dhe loja kalon në LOST. Rasti është në rregull dhe ka vetëm një zgjidhje të saktë. Gabimi është i lojtarit. | “Akuza është e gabuar: ora. Ja kufizimi që e përjashtonte: [deklarata].” (propozim; shfaqja e kufizimit vjen nga R10) | Lojtari shtyp “Provo përsëri”. Rasti hapet me të njëjtën caseSeed (D1) dhe buxheti kthehet në vlerën e plotë. Ose mund të kalojë te niveli tjetër. |
| 2 — defekt i nivelit / konfigurimit | Gjeneratori e krijon rastin gabim. Për shembull, rasti ka dy ose më shumë zgjidhje të sakta, ose një dhomë mbetet pa asnjë mundësi (seksioni 9 i GDD-së kërkon që rasti të ketë vetëm një zgjidhje dhe asnjë qelizë bosh). Lojtari nuk ka bërë asnjë gabim. | Para se loja të nisë, zgjidhësi e kontrollon rastin (loja është ende në CASE_LOADED). Rasti i dëmtuar nuk i shfaqet lojtarit; krijohet një rast tjetër me seed tjetër. Nuk ka LOST, nuk zbritet buxhet, nuk jepen yje. Nëse defekti zbulohet vetëm te akuza (zgjidhësi gjen një përgjigje tjetër që pajtohet me gjithçka), akuza nuk llogaritet si humbje. | “Ky rast ka një defekt dhe nuk llogaritet si humbje. Po hapet një rast i ri.” (propozim) | E bën sistemi vetë: hap rast të ri me seed tjetër dhe lojtari nuk ka asgjë për të bërë. Seed-i i dëmtuar ruhet. Meqë i njëjti seed jep gjithmonë të njëjtin rast, zhvilluesit mund ta provojnë përsëri dhe ta rregullojnë gjeneratorin. |
