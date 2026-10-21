# Alibi — Shënim playtest-i

## Seanca 1 — 19 tetor 2026

| | |
|---|---|
| Data | 19 tetor 2026 |
| Kohëzgjatja | rreth 10 minuta |
| Lojtare | Dea Berisha |
| Drejtues | Nysret Ukaj, me çelësin e përgjigjeve |
| Prototipi | `prototype/paper_prototype.jpg`, Rasti 2 i nivelit 1 |
| Konfigurimi | 4 të dyshuar, 5 dhoma, 3 orë, buxhet 12, par 6 |
| Rasti | Krimi: Biblioteka, ora 3. Fajtorja: Drita, me një gënjeshtër të vetme — pretendimi se ishte në Sallon në orën 3. |

### Protokolli

Lojtares iu dha vetëm tabela dhe lista e llojeve të pyetjeve, pa shpjegim verbal shtesë. Drejtuesi u përgjigj sipas çelësit dhe nuk ndihmoi gjatë lojës.

### Rezultati

Fajtorja u identifikua saktë dhe arsyetimi ishte i drejtë: një dëshmitar i ndershëm ishte në Sallon në orën 3 dhe nuk e kishte parë Dritën atje, ndërsa Drita pretendonte se ishte aty. Buxheti u shpenzua i tëri — 12 nga 12 njësi kundrejt par-it 6, pra vlerësimi 1 yll.

### Evidenca

| # | Vëzhgimi | Prova | Statusi |
|---|---|---|---|
| 1 | Inpute të paplota | 4 nga 13 inpute të paplota ose me parametër të panevojshëm: një pyetje pa kod lloji, një Q4 pa orë, një Q2 me orë që ajo pyetje nuk e merr, një Q2 pa dhomë. Dy u refuzuan pa prekur buxhetin (R12, T06); dy i interpretoi drejtuesi. | Validimi i inputit shkon te backlog-u |
| 2 | Kontradikta nuk sinjalizohet | Pas pyetjes së nëntë kontradikta ishte logjikisht e plotë. Lojtarja nuk e vuri re dhe shpenzoi dy pyetje të tjera për ta rikonfirmuar. Tabela R01–R13 nuk parashikon asnjë feedback për momentin kur dy kufizime bien ndesh. | Pranuar për GDD v1.1 |
| 3 | Buxhet i shpenzuar jashtë orës së krimit | Dy pyetje iu kushtuan orës 2, ndërsa krimi ndodhi në orën 3. Loja nuk e ndihmon lojtarin ta dallojë informacionin që ngushton hapësirën nga ai që vetëm konfirmon. | Në diskutim |

### Konfirmim

Rregulli D2 funksionoi siç ishte specifikuar: kur buxheti arriti 0, pyetjet u çaktivizuan dhe akuza u detyrua.

---


