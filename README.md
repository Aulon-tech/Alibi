# Alibi

**A procedurally generated murder mystery game powered by a Constraint Satisfaction Problem (CSP) engine using arc consistency and contradiction hunting.**

An algorithmic murder mystery game where logic is your only weapon. Every case is procedurally generated with a guaranteed unique solution and a provable deduction path.

Group 6 — Dea Berisha, Nysret Ukaj, Aulon Miftari, Enis Dapko  
Extension — Level 4 **Kalimi** (Vjedhja + secret passage Biblioteka–Zyra)

## What's new: Level 4 · Kalimi

| | |
|---|---|
| Card | **Karta 4** (seed 404) |
| Type | **Vjedhja** |
| Setup | 6 suspects, 9 rooms, 5 hours, budget 10, par 7 |
| Unlock | Secret passage **Biblioteka–Zyra** via physical search (R06) |
| Culprit | Fatmiri |

Play the documented route in `case4_answer_key.md`. Cards 1–3 are unchanged and still unique.

## Play in the browser (this repo)

```bash
export DOTNET_ROOT="$HOME/.dotnet"   # if you used the install script
export PATH="$PATH:$DOTNET_ROOT"
cd play
dotnet run -c Release --urls http://127.0.0.1:43123
```

Open [http://127.0.0.1:43123](http://127.0.0.1:43123). The UI talks to `Alibi.Core` (same engine as Unity).

## Unity project

See `AlibiUnity/Assets/Alibi/README.md`. Open in Unity 2022.3 LTS / Unity 6, create scenes via **Alibi > Krijo skenat**, then Play. Karta 4 appears in the level map after Karta 3.

## Verify Core

```bash
cd tools/AlibiVerify
dotnet run -c Release
```

## Spec docs

| File | Contents |
|---|---|
| `rules_table.md` | Rules R01–R13 |
| `state_model.md` | State variables (levels 1–6) |
| `case1_answer_key.md` | Level 1 key |
| `case4_answer_key.md` | Level 4 key |
| `test_cases.md` | TC-01…TC-10 |

## Levels

1. Mësimi — Vrasja, no movement graph  
2. Lëvizja — movement graph  
3. Sabotimi / Card 3 — honest witness error (R05)  
4. **Kalimi — Vjedhja + secret edge (R06)** ← shipped  
5–6. Still locked (refusals / endless generation)

## Statusi

Unity + `Alibi.Core` janë në `AlibiUnity/`. Kartat 1–4 janë të verifikuara (unike + rrugë e dokumentuar). Demo në shfletues: `play/` (porta 43123). Mbetet: nivelet 5–6 dhe vendimi për Sabotimin e plotë.
