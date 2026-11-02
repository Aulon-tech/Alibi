# Rasti 4 — çelësi i përgjigjeve (Niveli 4 · Kalimi)

> Vetëm për drejtuesin e playtest-it. Mos ia trego lojtarit.

Konfigurimi: 6 të dyshuar, 9 dhoma, 5 orë, buxhet 10, par 7.
Krimi: **Zyra**, ora **22:00**. Lloji: **Vjedhja**. Fajtori: **Fatmiri**, me gënjeshtër cover në Sallon.
Kalimi i fshehtë: **Biblioteka–Zyra** (fillimisht i fshehtë; zbulohet me «Kërko» në Bibliotekë).

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

## Rruga e shkurtër (par ≈ 7, demonstrja e verifikuar = 9 / 2 yje)

1. Kërko Bibliotekën (−1) → zbulohet kalimi Biblioteka–Zyra (R06)
2. Q1 te Elmazi për 21:00 → Biblioteka
3. Q4 te Elmazi për 21:00 → Fatmiri
4. Q1 te Fatmiri për 21:00 → Salloni (gënjeshtër, kontradiktë)
5. Q1 te Arta për 22:00 → Salloni
6. Q4 te Arta për 22:00 → Blerimi, Drita, Elmazi, Genta
7. Q2 te Arta për Zyrën → kurrë → përjashton të gjithë shoqëruesit
8. Akuza: Fatmiri, Zyra, 22:00
