# Alibi – Raste testi (v0.1)

Raste të bazuara në Rastin 1 të nivelit 1 (shih `prototype/case1_answer_key.md`): 4 të dyshuar (A, B, D, E), 5 dhoma, 3 orë (20:00, 21:00, 22:00), dhoma e krimit = Biblioteka, ora e krimit = 21:00, buxhet 12.

| ID | Lloji | Gjendja fillestare | Input | Rezultati i pritshëm |
|---|---|---|---|---|
| TC-01 | Normal | Niveli 1, buxhet 12 | Q1 te A: "Ku ishe në 20:00?" | A përgjigjet "Kuzhina"; buxheti = 11; `domain[A][20:00] = {Kuzhina}`; A hiqet nga kandidatët për fajtor (R08); `phase = QUESTIONING` |
| TC-02 | Normal | Niveli 1, buxhet 12 | Q4 te B: "Kush ishte me ty në 21:00?" | B përgjigjet "Askush"; buxheti = 10; `domain[B][21:00] = {Living}`; Living hiqet nga `domain` e A, D, E në 21:00 |
| TC-03 | E pavlefshme | Buxhet 1 | Q4 (kosto 2) te çdo i dyshuar | Pyetja refuzohet; buxheti mbetet 1; asnjë deklaratë e re; mesazh "Buxhet i pamjaftueshëm" |
| TC-04 | E pavlefshme | `phase = ACCUSING` | Akuzë (D, –, 21:00) pa dhomë | Dorëzimi refuzohet; `phase` mbetet `ACCUSING`; asnjë `result`; mesazh "Plotëso personin, dhomën dhe orën" |
| TC-05 | E pavlefshme (lëvizje) | Niveli 2 (pa `errorWitness`) | X thotë "Biblioteka" në 20:00 dhe "Kuzhina" në 21:00 | Biblioteka lidhet vetëm me Living, ndaj çifti është i pamundur (R02); `domain` bëhet bosh; shfaqet kontradiktë dhe X shënohet si i dyshimtë (gënjeshtar) |
| TC-06 | Kufitar | Niveli 3, `errorUsed = false` | Dy deklarata bien ndesh sepse `errorWitness` gaboi | Shfaqet "⚠ Kontradiktë"; asnjë i dyshuar nuk shënohet automatikisht si fajtor (R05); mbeten dy kandidatë deri në verifikim |
| TC-07 | Kufitar | `phase = ACCUSING`, e vërteta = (D, Biblioteka, 21:00) | Akuzë (D, Biblioteka, 22:00) | `phase = LOST`; shfaqet kufizimi që e perjashton orën 22:00; yje = 0 |
| TC-08 | Kufitar | `budgetRemaining = 2`, `phase = QUESTIONING` | Dy pyetje Q1 radhazi | Pas të dytës buxheti = 0; `phase = ACCUSING` me `forcedAccusation = true`; pyetjet dhe Anulo janë të çaktivizuara |
| TC-09 | Kufitar | Niveli 2, `confirmedHonest` bosh | Dy të dyshuar të paverifikuar thonë se ishin bashkë në Hall në 20:00 | Konfirmimi shpërfillet (R07b); nuk shtohet kufizim; mesazh "Konfirmim i pamjaftueshëm" |
| TC-10 | Kufitar (nivel 4) | `secretEdgeActive = false` | Deklaratë "Biblioteka në 21:00, Zyra në 22:00"; pastaj gjendet prova fizike e kalimit | Para provës çifti është i pamundur sipas grafit; pas provës `secretEdgeActive = true`, `domain` rillogaritet dhe deklarata bëhet e pajtueshme |
