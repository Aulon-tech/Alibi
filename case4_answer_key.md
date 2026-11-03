# Rasti 4 — çelësi i përgjigjeve (Niveli 4 · Kalimi)

> Vetëm për drejtuesin e playtest-it. Mos ia trego lojtarit.

Konfigurimi: 6 të dyshuar, 9 dhoma, 5 orë, **buxhet 12**, **par 6**.
Krimi: **Zyra**, ora **22:00**. Lloji: **Vjedhja**. Fajtori: **Fatmiri**, me gënjeshtër cover në Sallon.
Kalimi i fshehtë: **Biblioteka–Zyra** (fillimisht i fshehtë; zbulohet me **Q6 «Kërko»** në Bibliotekë, kosto 1 / R06).

### Balancimi

Me rregullat e reja (Q6/Kërko hyn në kosto, R06 rillogarit grafin), par-i i vjetër i llogaritur «me dorë» dilte ~12 ndërsa buxheti ishte 10. **Buxheti u ngrit në 12**; rruga optimale e verifikuar me ParPolicy + Q6 kushton **6**. Rrugët e të dyshuarve mbeten të njëjtat (zgjidhja mbetet unike kur kalimi është i hapur).

Verifikuar me program: zgjidhja është e vetme kur kalimi është i hapur.

## E vërteta

| I dyshuari | 20:00 | 21:00 | 22:00 | 23:00 | 00:00 |
|---|---|---|---|---|---|
| Arta | Kuzhina | Holli | Salloni | Salloni | Holli |
| Blerimi | Dhoma e ngrënies | Salloni | Salloni | Salloni | Dhoma e ngrënies |
| Drita | Dhoma e ngrënies | Dhoma e ngrënies | Salloni | Holli | Kuzhina |
| Elmazi | Salloni | Biblioteka | Salloni | Holli | Shkallët |
| Fatmiri | Salloni | Biblioteka | **Zyra** | Korridori | Shkallët |
| Genta | Kuzhina | Holli | Salloni | Salloni | Holli |

## Cover i Fatmirit

Pretendon Sallonin në çdo orë.

## Kontradikta që e hap rastin

Elmazi ishte në Bibliotekë në 21:00 dhe thotë se Fatmiri ishte me të.
Fatmiri pretendon Sallonin — kontradiktë.

## Rruga e shkurtër (par = 6 / 3 yje)

1. **Q6** Kërko Bibliotekën (−1) → zbulohet kalimi Biblioteka–Zyra (R06)
2. Q1 te Elmazi për 21:00 → Biblioteka
3. Q1 te Arta për 22:00 → Salloni
4. Q4 te Fatmiri për 22:00 → i liston të gjithë të tjerët në Sallon (cover)
5. Q1 te Drita për 21:00 → Dhoma e ngrënies → mbetet vetëm Fatmiri
6. Akuza: Fatmiri, Zyra, 22:00

Kosto: 1+1+1+2+1 = **6**.

## Rruga pedagogjike (≈ 8, ende 3 yje me par 6)

E dobishme për playtest / udhëzues: e ekspozon gënjeshtrën para akuzës.

1. Q6 Kërko Bibliotekën (−1)
2. Q1 te Elmazi për 21:00 → Biblioteka
3. Q4 te Elmazi për 21:00 → Fatmiri
4. Q1 te Fatmiri për 21:00 → Salloni (gënjeshtër, kontradiktë)
5. Q1 te Arta për 22:00 → Salloni
6. Q4 te Arta për 22:00 → Blerimi, Drita, Elmazi, Genta
7. Akuza: Fatmiri, Zyra, 22:00

Kosto: 1+1+2+1+1+2 = **8** (≤ par+1 → 3 yje; me Q2 shtesë për Zyrën bëhet 9 → 2 yje).
