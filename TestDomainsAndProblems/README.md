# Test Domains and Problems

PDDL representations of three game-AI planning scenarios, plus one standard planning benchmark, used in benchmarks and unit tests.

---

## Folder structure

```
TestDomainsAndProblems/
├── Crafter/
│   ├── crafter-domain.pddl      action definitions
│   └── crafter-problem.pddl     concrete scenario
├── Gripper/
│   ├── gripper-domain.pddl      IPC 1998 gripper (STRIPS), Round 1
│   └── gripper-problem.pddl     instance 1 (4 balls)
├── Heist/
│   ├── heist-domain.pddl
│   └── heist-problem.pddl
└── Soldier/
    ├── soldier-domain.pddl
    └── soldier-problem.pddl
```

Each folder contains exactly one domain file and one problem file. The domain describes the rules; the problem describes the specific situation and goal. Crafter, Heist, and Soldier are purpose-built game scenarios; Gripper is a standard planning-competition benchmark included to test scaling.

---

## Crafter  (7-step plan)

A crafting-chain scenario. An agent must gather raw materials and process them through a sequence of dependent crafting steps to produce a final item.

**crafter-domain.pddl**

Object types: `agent`, `location`

| Action | What it does |
|--------|-------------|
| `travel` | Move between connected locations |
| `gather-wood` | Collect wood from a location where it is available |
| `mine-ore` | Collect iron ore; requires a pickaxe |
| `craft-planks` | Convert wood into planks at a workbench |
| `build-furnace` | Consume planks to build a furnace at a workbench |
| `smelt-ingot` | Smelt iron ore into an ingot using a furnace |
| `forge-sword` | Craft an iron sword; requires both a workbench and a furnace |

**crafter-problem.pddl**

`crafter1` starts in a `wilderness` area that has wood and ore available, and carries a pickaxe. A `workshop` with a workbench is one travel step away. The goal is to produce an iron sword. `gather-wood` and `mine-ore` are independent and can be done in either order at the wilderness; the remaining five steps at the workshop are strictly sequential.

---

## Heist  (11-step plan)

A stealth scenario. A thief must scout the location, acquire equipment, disable security, infiltrate a vault, crack the safe, steal a jewel, distract a guard, and escape. The domain has a mix of independent preparation actions and a strictly ordered endgame.

**heist-domain.pddl**

Object types: `agent`

| Action | What it does |
|--------|-------------|
| `case-location` | Scout the site; required before disabling the camera |
| `acquire-lockpicks` | Obtain lockpicks |
| `don-disguise` | Put on a disguise |
| `disable-camera` | Disable the security camera; requires the location to be cased |
| `pick-lock` | Unlock the vault door; requires lockpicks and the camera disabled |
| `enter-vault` | Enter the vault; requires disguise and unlocked door |
| `crack-safe` | Break into the safe while inside the vault |
| `steal-jewel` | Take the jewel; requires the safe to be open |
| `use-noise-maker` | Distract the guard; one-time use, consumes the noise-maker |
| `exit-vault` | Leave the vault; requires the guard distracted and the jewel in hand |
| `escape` | Flee the scene |

**heist-problem.pddl**

`thief1` starts with only a noise-maker — no lockpicks, no disguise, location not yet cased. The goal is `escaped thief1`. Three preparation actions are mutually independent: `case-location`, `acquire-lockpicks`, and `don-disguise` have no shared preconditions and can fire in any order. `use-noise-maker` is also independent (its only precondition is possessing the noise-maker, which holds from the start) and can fire anywhere before `exit-vault`. The remaining actions form a strict chain: `disable-camera` → `pick-lock` → `enter-vault` → `crack-safe` → `steal-jewel` → `exit-vault` → `escape`.

---

## Soldier  (3-step plan)

A tactical combat scenario. A soldier must advance through connected locations and eliminate an enemy using direct fire. The domain also models cover and reloading for richer variants.

**soldier-domain.pddl**

Object types: `agent`, `enemy`, `weapon`, `location`

| Action | What it does |
|--------|-------------|
| `move` | Advance to an adjacent location; breaks cover |
| `take-cover` | Use available cover at the current location |
| `reload` | Reload a weapon; requires being in cover |
| `shoot` | Fire at an enemy in the same location; unloads the weapon |
| `throw-grenade` | Throw a grenade to an adjacent location; kills one enemy there |

**soldier-problem.pddl**

`soldier1` starts at `entrance` with a loaded `rifle1`. `enemy1` is at `bunker`, two hops away via `corridor`. The goal is to eliminate `enemy1`. The optimal plan is fully sequential with no independent steps: `move(entrance→corridor)` → `move(corridor→bunker)` → `shoot`.

---

## Gripper  (11-step plan, instance 1)

A standard planning-competition benchmark, included to test how the planners scale, rather than a purpose-built game scenario. A robot with two grippers must carry a number of balls from one room to another. The two interchangeable grippers and interchangeable balls create symmetry that drives an unnecessary combinatorial blow-up in many planners, and the problem size is tunable by the number of balls, which makes it a useful stress test that the small game domains do not reach.

**Provenance**

- Competition: the first International Planning Competition, IPC 1998 (held at AIPS-98).
- Domain: Gripper, Round 1, STRIPS track. Author: Jana Koehler.
- Problem: instance 1 (original name `prob01.pddl`), `strips-gripper-x-1`, with 4 balls.
- Source in this repo: `Benchmarks/IPC1998/gripper-round-1-strips/` (domain plus 20 instances of increasing size).

**gripper-domain.pddl**

The domain is untyped: object types are encoded as unary predicates (`room`, `ball`, `gripper`) rather than declared in `(:objects ...)`.

| Action | What it does |
|--------|-------------|
| `move` | Move the robot from one room to another |
| `pick` | Pick up a ball in the current room with a free gripper |
| `drop` | Drop a carried ball in the current room |

**gripper-problem.pddl**

The robot starts in `rooma` with both grippers free; `ball1` through `ball4` are all in `rooma`. The goal is to have all four balls in `roomb`. The optimal plan is 11 steps: two round trips carrying two balls each, with the last trip needing no return.
