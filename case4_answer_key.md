# Rasti 4 — çelësi i përgjigjeve (Niveli 4 · Kalimi)

> Vetëm për drejtuesin e playtest-it. Mos ia trego lojtarit.

Konfigurimi: 6 të dyshuar, 9 dhoma, 5 orë, **buxhet 16**, **par 14** (seed 404).
Krimi: **Zyra**, ora **22:00** (ora nuk dihet lojtarit). Lloji: **Vjedhja**. Fajtori: **Fatmiri**, me cover në Sallon.
Kalimi i fshehtë: **Biblioteka–Zyra** (zbulohet me **«Kërko»** në Bibliotekë, kosto 1 / R06 — jo me Q6).
Q6: «A e ke parë dosjen në orën X?» (sendi lëviz me hajnin, R10).

### Balancimi

Pas Vjedhjes së plotë (send + orë e panjohur + Q6 dosja), ParPolicy me kërkim jep **par 14**; buxheti u ngrit në **16**. Rruga e verifikuar në `tools/AlibiVerify` mbyll rastin me **spent = 14** (3 yje).

Verifikuar me program: zgjidhja është e vetme kur kalimi është i hapur.

## E vërteta

| I dyshuari | 20:00 | 21:00 | 22:00 | 23:00 | 00:00 |
|---|---|---|---|---|---|
| Arta | Kuzhina | Holli | Salloni | Salloni | Holli |
| Blerimi | Dhoma e ngrënies | Salloni | Salloni | Salloni | Dhoma e ngrënies |
| Drita | Holli | Shkallët | Korridori | Korridori | Shkallët |
| Elmazi | Salloni | Biblioteka | Salloni | Holli | Shkallët |
| Fatmiri | Salloni | Biblioteka | **Zyra** | Korridori | Shkallët |
| Genta | Zyra | Zyra | Korridori | Shkallët | Holli |

## Cover i Fatmirit

Pretendon Sallonin në çdo orë.

## Kontradikta që e hap rastin

Elmazi ishte në Bibliotekë në 21:00 dhe thotë se Fatmiri ishte me të.
Fatmiri pretendon Sallonin — kontradiktë. Kalimi i fshehtë e bën të mundur që Fatmiri të arrijë në Zyrë në 22:00.

## Rruga e verifikuar (par = 14 / 3 yje)

Nga `AlibiVerify` (pas «Kërko» në Bibliotekë):

1. **Kërko** Bibliotekën (−1) → zbulohet kalimi Biblioteka–Zyra (R06); shfaqet shkalla e artë
2. Q1 te Arta për 22:00 → Salloni
3. Q4 te Fatmiri për 22:00 → liston Artën, Blerimin, Elmazin (cover)
4. Q3 te Blerimi për Fatmirin në 22:00 → Jo
5. Q3 te Arta për Fatmirin në 22:00 → Jo
6. Q1 te Drita për 22:00 → Korridori
7. Q3 te Drita për Gentën në 22:00 → Po
8. Q1 te Elmazi për 21:00 → Biblioteka
9. Q3 te Elmazi për Fatmirin në 21:00 → Po
10. Q6 te Genta për 21:00 → Po, e pa dosjen (ende në Zyrë para vjedhjes)
11. Q3 te Arta për Blerimin në 22:00 → Po
12. Q1 te Elmazi për 00:00 → Shkallët
13. Q6 te Elmazi për 00:00 → Po, e pa dosjen (me hajnin)
14. Akuza: **Fatmiri, Zyra, 22:00**

Kosto totale: **14** (përfshin kërkimin).
