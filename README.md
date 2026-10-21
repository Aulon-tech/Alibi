# Alibi

**A procedurally generated murder mystery game powered by a Constraint Satisfaction Problem (CSP) engine using arc consistency and contradiction hunting.**

An algorithmic murder mystery game where logic is your only weapon. Every case is procedurally generated with a guaranteed unique solution and a provable deduction path.

Group 6 — Dea Berisha, Nysret Ukaj, Aulon Miftari, Enis Dapko
Game Development · FSHMN · University of Prishtina

**Status:** design phase. The specification is complete and validated against a paper prototype; the engine is not yet implemented.

## 🧩 Core Concept

In this game, murder mystery meets formal logical reasoning. Suspects don't tell emotional stories—they generate **logical constraints**. Instead of guessing, players question suspects, log statements onto a dynamic **Deduction Board**, and cross-reference constraint sets to catch lies and isolate the single guilty party.

## ⚙️ Algorithmic Engine

* **Procedural Case Generation:** Uses constraint propagation (**Arc Consistency / AC-3**) combined with backtracking search to generate solvable cases on the fly.
* **Dynamic Difficulty Tuning:** Case complexity is determined dynamically by measuring the number of propagation steps required by the solver to reach a unique solution.
* **Verifiable Proof Chains:** The engine verifies the player's final accusation by checking the validity of their contradiction chain.

## 🎮 Core Mechanics & Rules

* **Spatial & Temporal Grid:** Every suspect occupies exactly one room per time slot.
* **Corroboration:** Two people in the same room at the same time confirm each other's presence.
* **Physical Evidence:** Provides immutable hard constraints that cannot be lied about or altered.
* **Lie Detection:** Honest suspects always state true constraints. Exactly one suspect (the culprit) can lie, producing isolated contradictions in the overall constraint matrix.
* **Question Budget:** Players have a fixed question quota that shrinks as difficulty increases, requiring optimal information-gathering strategy.

## 🏆 Victory & Defeat

* **Win Condition:** Correctly identify the culprit alongside the supporting logical contradiction chain.
* **Lose Condition:** Run out of the question budget or accuse the wrong suspect.

## 📐 Specification

| File | Contents |
|---|---|
| `GDD_v0.1.pdf` | Game design document, 9 pages |
| `rules_table.md` | Rules R01–R13 in WHEN/IF … THEN form |
| `state_model.md` | State variables, invariants and transitions |
| `test_cases.md` | 10 test cases with expected results |
| `core_loop.png` | Core gameplay loop diagram |
| `paper_prototype.jpg` | Paper prototype board |
| `case1_answer_key.md` | Verified answer key for Case 1 |
| `playtest_note.md` | Playtest record |

## ✅ Validation

Paper playtest, 19 October 2026. Level 1, Case 2: four suspects, five rooms, three time slots, budget 12, par 6. The player identified the culprit correctly and reconstructed the contradiction chain.

All three prototype cases are verified unique by script — the generator counts solutions and stops at the second; each returned exactly one valid hypothesis.

## 🗺️ Roadmap

- `Alibi.Core` in C#, independent of UnityEngine: case generator, AC-3, RuleEngine, ParPolicy
- Contradiction feedback when two constraints conflict
- Input validation before the question budget is charged
- Unity project: scenes, Deduction Board, question interface
- Unit tests across the 10 cases in `test_cases.md`
