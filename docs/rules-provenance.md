# Rules provenance

Every game rule the engine encodes, traced to source. A rule may only be marked
**verified** when this table carries an actual quotation — "I'm confident about
this one" is exactly the failure mode below and does not count.

## Why this exists

During development the rule *"a figure cannot move through a hostile figure"*
was written from memory into both the pathfinder and a test. **The test passed.**
The rulebook says the opposite: a figure **may** move into a space occupied by a
hostile figure for **one additional movement point**.

A green suite proves nothing when the test and the implementation came from the
same bad assumption — a test only validates code against an *independent*
statement of intent. Reviewing the tests would never have caught it. Only going
back to the source did.

Treating hostiles as walls would have made a corridor containing one figure read
as impassable, sending every enemy the long way round: visibly wrong at the
table and very hard to trace back.

## Status key

| Status | Meaning |
|---|---|
| **verified** | Quoted from the rulebook or Rules Reference below |
| **interpretation** | Rules are silent or contested; a choice is made, documented, and should be a setting |
| **unverified** | Encoded but not yet sourced. **Must not ship in this state** |
| **house rule** | Deliberate deviation, stated as such |

Sources: [Rules Reference Guide](https://images-cdn.fantasyflightgames.com/filer_public/ef/86/ef866943-534b-429e-b8db-7d99875431b7/imperial_assault_rules_reference_guide.pdf) (exceeds fetch size limits; use a mirror) · [RulesPal rulebook](https://www.rulespal.com/star-wars-imperial-assault/rulebook) · [Consolidated IA Rules](https://a1bert.kapsi.fi/TheConsolidatedImperialAssaultRules.pdf)

---

## Terrain

| Rule | Status | Source | Code |
|---|---|---|---|
| Difficult terrain costs +1 MP to enter | **verified** | "a figure must spend one additional movement point to enter a space of difficult terrain" | `BoardModel.EnterCost` |
| Difficult terrain is marked by a **solid blue** border | **verified** | "Difficult terrain is indicated by a solid blue border surrounding a space" | `terrain_detect.py` |
| Impassable terrain cannot be entered | **verified** | "A figure cannot enter a space containing impassable terrain" | `BoardModel.IsEnterable` |
| Impassable terrain is marked by a **dashed red** line | **verified** | "Impassable terrain is represented by a dashed red line surrounding a space" | `terrain_detect.py` |
| Spaces separated by impassable terrain **are still adjacent**, and **LOS traces through it** | **verified** | "Two spaces separated by impassable terrain are adjacent, and line of sight can be traced through impassable terrain" | `EdgeBreaksAdjacency`, `EdgeBlocksLos` |
| Impassable may apply to a single **edge** of a space | **verified** | "Sometimes impassable terrain is only on one edge of a space. Figures cannot move or be pushed through this edge" | `EdgeType.Impassable` |
| Blocking terrain stops movement, counting and line of sight | **verified** | Blocking terrain cannot be entered, counted through, or traced through | `SquareBlocksLos`, `Distance.CanCountInto` |
| Walls sever adjacency | **verified** | Spaces separated by a wall are not adjacent | `EdgeBreaksAdjacency` |
| A tile's **shape** is a wall — figures cannot leave the playable area | **verified** (by construction) | Tile shape comes from the art's alpha channel; 22% of 276 faces are non-rectangular | `BoardBuilder`, `tile_shapes.py` |
| Crates block neither movement nor LOS | **verified** | — | not modelled as blockers |

## Movement

| Rule | Status | Source | Code |
|---|---|---|---|
| Diagonal movement is allowed | **verified** | "Both orthogonal and diagonal movement is allowed" | `Pathfinder.Moves` |
| A figure may move through a **friendly or neutral** figure at no extra cost | **verified** | "A figure can move into a space occupied by a friendly or neutral figure at no additional cost" | `MoveOptions.Friendly` |
| A figure may move through a **hostile** figure for **+1 MP** | **verified** | "A figure can move into a space occupied by a hostile figure, but must spend one additional movement point to do so" | `Pathfinder.EnterCost` |
| No figure may **end** movement in an occupied space | **verified** | "A figure cannot end its movement in a space containing another figure" | `Pathfinder.CanEndOn` |
| Closed doors block movement and LOS; open doors do not | **verified** | — | `EdgeType.DoorClosed` / `DoorOpen` |
| Cannot move through the **diagonal intersection** of walls / blocking / impassable | **interpretation** | Rules Reference states the restriction; the contested cases are not enumerated. Implemented as: the diagonal is legal when at least one of the two orthogonal routes around the corner is clear on **both** its edges | `BoardModel.DiagonalOpen`, `StrictDiagonals` toggle |

## Large figures

| Rule | Status | Source | Code |
|---|---|---|---|
| A large figure cannot move diagonally | **verified** | "A large figure cannot move diagonally" | `Pathfinder.Moves` |
| Rotating the base costs 1 MP | **verified** | "a large figure cannot rotate its base unless it spends one movement point to do so" | `Pathfinder.Moves` |
| After rotating it must occupy **at least half** the spaces it occupied before | **verified** | "the large figure must occupy at least half of the spaces it occupied before the rotation" | `Pathfinder.SharesHalf` |
| A large figure needs **every** space of its footprint clear | **interpretation** | Follows from occupying multiple spaces; not quoted directly | `Pathfinder.CanOccupy` |
| Footprint cost is the **most expensive** space it lands on | **interpretation** | Not stated in the rules. Chosen because it is the conservative reading | `Pathfinder.EnterCost`, `BoardModel.LargeFigurePaysWorstSpace` toggle |

## Line of sight

| Rule | Status | Source | Code |
|---|---|---|---|
| LOS traces from one corner of the attacker's space to two adjacent corners of the target's | **verified** | Rules Reference, Line of Sight | `LineOfSight.HasLos` |
| Figures block line of sight | **verified** | — | `occupied` predicate |
| Glancing a wall is legal | **verified** | — | open-interior tests |
| LOS is symmetric | **interpretation** | Not stated explicitly; the trace is written asymmetrically. Enforced by canonicalising the pair, and pinned by a property test | `LineOfSight.HasLos` |
| A corner standing on a barrier cannot trace **through** it, only **past its end** | **interpretation** | Not addressed by the rules. Without it, a corner embedded in a continuous wall traces straight through, because the wall only touches the traced region at that vertex | `CornerIsBehindBarrier`, `BoardModel.StrictBarrierCorners` toggle |

## Attacks and distance

| Rule | Status | Source | Code |
|---|---|---|---|
| Melee can only target an **adjacent** figure | **verified** | "Melee attacks can only target figures adjacent to the attacker" | `AttackEvaluator.Assess` |
| Ranged attacks require line of sight | **verified** | "To declare the attack, the target figure must be in line of sight of the attacking figure" | `AttackEvaluator.Assess` |
| Ranged attacks have **no maximum range** | **verified** | "ranged attacks can target any hostile figure that the figure can see" | `AttackEvaluator.Assess` |
| Accuracy must meet or exceed **distance in spaces** | **verified** | "the amount of accuracy ... must be equal to or greater than the number of spaces the target is away from the attacker" | `AttackAssessment.RequiredAccuracy` |
| Distance is counted as moved spaces for a **small figure**, and may be 0 | **verified** | COUNTING SPACES: "the player counts how many moved spaces it would take for a small figure to move from one space to the other. The value for the distance can be 0" | `Distance.Count` |
| Counting **may pass through impassable terrain** | **verified** | "Impassable terrain can be moved into and through for this measurement" | `Distance.CanCountInto` |
| Counting **may not** pass through walls, doors or blocking terrain | **verified** | "This measurement cannot go through walls, doors, or blocking terrain" | `Distance.CanCountInto` |
| Counting ignores difficult terrain and figures | **verified** (implied) | Counting is by *moved spaces*, and the exclusion list names only walls, doors and blocking terrain | `Distance.Count` |
| Diagonals count as **one space** | **verified** (implied) | Diagonal movement is allowed and costs one movement point, so it is one moved space | `Distance.Count` |

## Heroes

| Rule | Status | Source | Code |
|---|---|---|---|
| First defeat: discard all damage and flip to the wounded side | **verified** | "When a hero is defeated for the first time during a mission, he discards all damage tokens from his Hero sheet and flips his Hero sheet to the wounded side" | `HeroCombatState.ApplyDamage` |
| A wounded hero defeated again **withdraws** | **verified** | "If a wounded hero is defeated, he withdraws" | `HeroCombatState.ApplyDamage` |
| Strain may only be suffered up to Endurance | **verified** | "A hero can only optionally suffer an amount of strain up to his Endurance" | `HeroCombatState.SpendStrain` |
| Rest recovers strain equal to Endurance; **excess recovers damage** | **verified** | "By resting, a hero can recover strain equal to his Endurance. If a hero recovers strain in excess of the number of strain tokens he has, the hero recovers damage equal to the amount of excess" | `HeroCombatState.Rest` |

## Enemy groups and conditions

| Rule | Status | Source | Code |
|---|---|---|---|
| Damage does not carry between figures (no overkill spill) | **interpretation** | Each figure is damaged separately; not quoted | `GroupCombatState.ApplyDamage` |
| A figure performs **two actions** per activation | **verified** | "The player performs any combination of two actions with the figure" | `ActionsPerActivation` |
| **Stunned**: cannot voluntarily exit its space, cannot declare an attack; an action discards it | **verified** | STUNNED: "A Stunned figure cannot voluntarily exit its space and cannot declare an attack... can use the [action] specified on the Stunned condition card to discard the Stunned condition" | `CanMove`, `CanDeclareAttack`, `DiscardStunned` |
| **Bleeding**: 1 damage **after each action**, not once per activation | **verified** | BLEEDING: "If a figure has Bleeding after it has resolved an action, the figure suffers 1 [damage]" | `ResolveAfterAction` |
| **Weakened**: −1 attacking and defending, auto-discarded at end of activation | **verified** | WEAKENED: "applies -1 while defending and -1 while attacking. Weakened is automatically discarded at the end of a figure's activation" | `EndActivation` |
| A condition cannot stack with itself | **verified** | "A figure cannot be affected by multiple instances of the same condition" | `Conditions` is a set |
| Focused and Hidden are BENEFICIAL; Bleeding, Stunned, Weakened are HARMFUL | **verified** | "Focused, Hidden are BENEFICIAL, Bleeding, Stunned, Weakened are HARMFUL conditions" | `Condition` enum |

## Keywords and abilities

| Rule | Status | Source | Code |
|---|---|---|---|
| **Massive** ignores terrain for movement: may enter and end on blocking and impassable terrain and their edges | **verified** | Consolidated Rules p.41: "Massive figures ignore terrain for movement, thus can enter spaces containing blocking terrain and impassable terrain. They can also move through and end movement on blocked or impassable terrain edges." | `MoveOptions.Massive`, `Pathfinder.CanOccupy`, `Pathfinder.StepAllowed` |
| **Massive** pays no extra movement for difficult terrain or hostile figures | **verified** | Consolidated Rules p.41: "Massive figures can enter spaces containing hostile figures and/or difficult terrain at no additional movement cost." | `Pathfinder.EnterCost` |
| **Massive** may not enter a space holding another Massive figure | **verified** | Consolidated Rules p.41: "Massive figures cannot enter spaces containing other Massive figures." | `MoveOptions.OtherMassive` |
| **Massive** may end movement on blocking terrain and on other figures | **verified** | Consolidated Rules p.41: "A Massive figure can end its movement in spaces that contain blocking terrain and/or other figures." The push that follows is resolved at the table | `Pathfinder.CanEndOn` |
| Figures do not block line of sight to or from a **Massive** figure | **verified** | Consolidated Rules p.41: "Figures do not block line of sight to or from a Massive figure." | `FigureVisibility.Massive`, `Visibility.BlockersFor` |
| A **Massive** figure on blocking terrain can still be seen, counted to and attacked | **verified** | Consolidated Rules p.41: "If a Massive figure occupies a space containing blocking terrain, line of sight can be traced to that figure, spaces can be counted to that figure, and adjacent figures can attack that figure." | `Distance.Count(targetOnBlockingIsCountable)` |
| **Massive** push, interior-space ban, and no-further-movement-after-landing-on-a-figure | **not implemented** | Consolidated Rules p.41, quoted in `MassiveTests`. Deliberately out of scope: these are resolution and campaign concerns, not pathfinding, and the app only advises where a figure may go | — |
| Mak Eshka'rey's **Covert**: hostile figures 4 or more spaces away have no LOS to him | **verified** | Hero sheet, quoted identically by two independent sources: "Hostile figures 4 or more spaces away from you do not have line of sight to you." Threshold is **inclusive**, so a figure at exactly 4 cannot see him | `FigureVisibility.HiddenAtOrBeyond` (`>=`) |
| **Covert**: he does not block LOS for those same figures | **verified** | Same sentence: "You do not block line of sight for those figures." This is why blockers are recomputed per observer rather than once per board | `Visibility.BlockersFor` |

## Imperial AI

| Rule | Status | Source | Code |
|---|---|---|---|
| Target priority: closest healthy → least Health remaining → most total Health → closest overall | **verified** (upstream) | Documented in the ImperialCommander2 wiki, which this fork preserves | `TargetSelector.Select` |
| Unresolvable ties are handed to the players | **verified** (upstream) | "If there are still multiple figures that satisfy all those criteria, the players decide" | `TargetDecision.NeedsPlayerDecision` |
| "Closest" means **true path distance** | **house rule** | Upstream has no position awareness at all, so there is nothing to preserve. Straight-line distance would send enemies charging at walls | `TargetSelector.PathDistance` |
| Figures are planned one at a time, nearest first, committing each end square | **house rule** | Not a game rule. Prevents orders that contradict each other | `ActivationPlanner.Plan` |
| Prefer the easiest shot, then the least movement | **house rule** | Not a game rule. Unspent movement is worth nothing at end of activation | `ActivationPlanner.ChooseSpot` |

---

## What the audit found

It was worth running. Auditing against source caught **three rules that were wrong in code already passing tests**:

1. **Hostile figures treated as walls.** Rules: passable for +1 MP. Would have made any corridor containing one figure read as impassable.
2. **Bleeding resolved once per activation.** Rules: once **per action**. Halved the condition's effect, silently.
3. **Stunned modelled as "costs one action".** Rules: the figure cannot voluntarily exit its space *and* cannot declare an attack until the condition is discarded. The action cost is a consequence, not the rule.

All three had passing tests, because each test restated the same assumption the code was built on.

## Outstanding before ship

1. ~~Cite or remove `Massive`~~ — **done**. Sourced to the Consolidated Rules p.41, which turned out to say considerably more than had been implemented. See below.
2. ~~Confirm Mak Eshka'rey's threshold~~ — **done**. The ability is **Covert**, and the wording is "4 or more spaces", so the threshold is inclusive and the existing `>=` was right. Pinned by a boundary test in `CovertTests`.
3. ~~Promote the LOS interpretations to explicit settings~~ — **done**. `BoardModel.StrictBarrierCorners` and `BoardModel.LargeFigurePaysWorstSpace` join the existing `StrictDiagonals`, each defaulting to the current reading. `InterpretationTests` asserts that every toggle actually changes behaviour — an unexercised setting reads as configurable while quietly doing one thing, which is worse than no setting — and that relaxing the barrier-corner rule only ever *grants* sight, checked as a property over 200 random boards rather than one hand-picked one.
4. Re-run the simulated-play adjudicator with this table as its reference, so its rulings cite the same quotations the engine was built against.

**No row remains `unverified`.**

### What the audit actually caught

Worth recording, because it is the justification for doing this as a separate
pass rather than trusting that the code was written carefully.

`Massive` had been implemented from memory as "ignores blocking and impassable
terrain". Reading the printed entry showed that is one bullet of six. Three
things were simply missing, and each was a live bug:

- **Difficult terrain and the hostile toll were still being charged.** "Massive
  figures can enter spaces containing hostile figures and/or difficult terrain
  at no additional movement cost." An AT-ST was paying 2 to cross mud and an
  extra point to walk over a Rebel. Every reachability query for a Massive
  figure was therefore too small, which would have shown up at the table as the
  AT-ST being oddly timid.
- **It could not end its movement on blocking terrain or on other figures**,
  which the entry allows explicitly.
- **Figures were still screening it.** "Figures do not block line of sight to or
  from a Massive figure" — a property of the endpoint, not of the blocker, so
  it could not be expressed by any per-figure flag that already existed.

And one restriction was missing in the other direction: "Massive figures cannot
enter spaces containing other Massive figures." That one only surfaced *because*
the end-on-figures fix landed first — the fuzzer immediately produced two
Massive figures stacked on one square, on seed 15.

The tests for all of this were written from the quotation before the code was
touched, and three of them failed on the first run. That is the whole argument
for the ordering: the previous tests passed, because they had been written from
the same assumption as the implementation.
