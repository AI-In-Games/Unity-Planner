# Test Domains and Problems

PDDL representations of three game-AI planning scenarios used in benchmarks and unit tests.

---

## Folder structure

```
TestDomainsAndProblems/
├── Worker/
│   ├── worker-domain.pddl      action definitions
│   └── worker-problem.pddl     concrete scenario
├── Companion/
│   ├── companion-domain.pddl
│   └── companion-problem.pddl
└── Guard/
    ├── guard-domain.pddl
    └── guard-problem.pddl
```

Each folder contains exactly one domain file and one problem file. The domain describes the rules; the problem describes the specific situation and goal.

---

## Worker  (4-step plan)

A simple resource-delivery scenario. A single worker must navigate to a resource, pick it up, and bring it to a depot.

**worker-domain.pddl**

Object types: `worker`, `resource`, `location`

| Action | What it does |
|--------|-------------|
| `move` | Worker travels from one location to another along a `connected` edge |
| `gather` | Worker picks up a resource at the same location |
| `deliver` | Worker drops a resource at the depot |

Connectivity is explicit: `move` requires `(connected ?from ?to)`. The problem declares directed edges in both directions for two-way roads.

**worker-problem.pddl**

`worker1` starts at `loc-start`. It must gather `log-01` from `loc-forest` and deliver it to `loc-depot`. The map is a linear chain: `loc-start <-> loc-forest <-> loc-depot`. There is no direct edge between `loc-start` and `loc-depot`, so the worker must pass through `loc-forest` to reach the depot.

---

## Companion  (8-step plan)

A combat scenario where a companion agent must defeat enemies while managing health. It is the most complex of the three domains.

**companion-domain.pddl**

Object types: `agent`, `enemy`, `weapon`, `item`, `zone`

| Action | What it does |
|--------|-------------|
| `move-to-zone` | Navigate between zones |
| `equip-weapon` | Ready a weapon before combat |
| `use-item` | Consume a health potion |
| `engage-enemy` | Enter combat with an enemy |
| `attack-enemy` | Deal damage; kills enemy when health is sufficient |
| `retreat` | Flee to the safe zone |
| `rest` | Recover health while in the safe zone |
| `pick-up-item` | Collect items from the safe zone |

**companion-problem.pddl**

`companion1` must kill `enemy1` and `enemy2`. It starts with a sword, bow, and potion, but low health prevents engaging until it rests. It must return to `zone-safe` after both enemies are dead.

---

## Guard  (6-step plan)

A surveillance scenario where a guard on patrol detects a noise, investigates, spots a suspect, and raises an alarm.

**guard-domain.pddl**

Object types: `guard`, `waypoint`, `invpoint`, `suspect`, `alarm`, `disturbance`

| Action | What it does |
|--------|-------------|
| `patrol` | Move between adjacent patrol waypoints |
| `detect-disturbance` | While on patrol, notice a noise and become suspicious |
| `start-investigate` | Travel to the investigate point a disturbance originated from |
| `clear-suspicion` | End investigation if no suspect is found |
| `spot-suspect` | Detect a suspect; requires line of sight from the current investigate point |
| `pursue-suspect` | Chase a spotted suspect |
| `raise-alarm` | Activate an alarm while pursuing |
| `call-backup` | Request assistance |
| `resume-patrol` | Return to normal patrol state |
| `lose-suspect` | Suspect escapes, investigation ends |

The flow goes through real triggers: suspicion is not asserted in the initial state — it is produced by `detect-disturbance` while the guard is on patrol. Spotting a suspect requires both being at the right investigate point and having line of sight to the suspect from that point. Patrol routes are explicit `(connected ?from ?to)` edges between waypoints.

**guard-problem.pddl**

`guard1` starts on patrol at `wp-a` with patrol route `wp-a <-> wp-b <-> wp-c`. A noise (`disturbance1`) is heard at `inv-pt2`, where `suspect1` is hidden. Only `inv-pt2` has line of sight to the suspect — investigating `inv-pt1` would clear suspicion without finding anything. The goal is to raise `alarm1` and call backup, which requires the guard to detect the noise, investigate at the correct point, spot the suspect, pursue, and trigger both the alarm and backup call.
