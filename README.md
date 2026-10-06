# Procedural CSP Murder Mystery Engine

An algorithmic murder mystery game where logic is your only weapon. Powered by a custom Constraint Satisfaction Problem (CSP) solver, every case is procedurally generated with a guaranteed unique solution and a provable deduction path.

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
