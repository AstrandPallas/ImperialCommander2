# AI adjudication log

Findings from reading simulated-play transcripts, sorted into the three buckets
the plan defines. Only **(a)** and **(b)** are correctness; **(c)** is the
scoring function, which the plan expects to iterate on after real play.

- **(a) terrain data error** — the board says something the cardboard does not
- **(b) rules bug** — the engine breaks a rule in the RRG
- **(c) tactical quality** — legal, but not what a decent Imperial player does

Reproduce any run with `dotnet run -- --sim <seed> <rounds>` from
`tests/BoardTests`.

## Mechanical results

| check | scope | result |
|---|---|---|
| illegal orders | 40 seeds x 8 rounds, CORE1 real map | **0** |
| movement cost mismatch | every path replayed independently against `EnterCost` | **0** |
| illegal orders | 400 random boards, one activation each (fuzzer) | **0** |

Legality is checked by `PlanLegality.Violations`, which the fuzzer and the
simulator both call. One definition, deliberately: two copies of "legal" would
drift, and the drift would surface as the simulator blessing a move the fuzzer
rejects, or the reverse.

The independent cost replay matters more than it looks. The planner reports
`MovementSpent`; the simulator ignores that and re-sums `board.EnterCost` over
the path it was handed. A pathfinder that mis-costed difficult terrain would
agree with itself and be caught here.

## Findings

### 1. The whole group commits to one target, so it overkills — bucket (c)

Seed 1, round 3. All three figures were ordered onto Diala. The Trandoshan took
her to 9/10, Stormtrooper 1 to 11/10 and she was wounded, and Stormtrooper 2
still spent its attack on her — into a hero who had already resolved the wound
and reset to 0 damage.

This is not a rules break. It follows from `ActivationPlanner` choosing
`plan.GroupTarget` **once**, from the figure best placed to judge, and every
figure in the group then working toward that target. A human Imperial player
picks a target per attack and would have switched after the second hit.

Worth fixing in the scoring function, not the rules layer: re-evaluate the
target between figures once damage from earlier figures in the same activation
is known. Note the fix needs the tracker in the loop, since the planner today
is handed a static list of `TargetCandidate`.

### 2. Target churn makes figures reverse across the board — bucket (c)

Seed 1, rounds 5 and 6. Both Stormtroopers closed on Mak and shot him at
required accuracy 1. Mak was wounded at the end of round 5. Rule 1 is *closest
healthy Rebel*, so a wounded Mak left the running, Fenn became the target, and
both troopers spent round 6 running four squares back east — ending on shots of
accuracy 5 and 6.

So the AI gave up two guaranteed hits for two long shots, because the hero it
was standing next to stopped being *healthy*.

The Trandoshan in the same round is the control case: it could not reach Fenn,
so it attacked an adjacent, already-wounded Diala at accuracy 0 and stayed put.
The planner does prefer an available attack over a hopeless chase. The churn
comes specifically from figures that *can* reach the new target and therefore
do, with nothing in the scoring to weigh what they are giving up.

This is the failure the plan predicted almost exactly: legal-but-dumb moves that
read *worse* than IC2's abstract text precisely because they are specific enough
to be visibly wrong. It is faithful to the documented priority chain, so the
chain itself is what needs revisiting — most likely a stickiness or
sunk-position term, so that abandoning an adjacent target has to be paid for.

Flagged rather than fixed: the chain is inherited IC2 behaviour, and changing it
is a design decision about how the Imperials should feel, not a bug fix.

## What simulation cannot tell us

Legality on a real map is now well covered, but two gaps remain open by
construction.

**Terrain data that is wrong but self-consistent.** The engine cannot know the
cardboard. If a tile face is authored with difficult terrain on the wrong
squares, every path over it is costed consistently, every test passes, and the
only symptom is a player at the table saying "that is not what my tile says."
This is what the table-adjudication pass in the plan exists for.

**Whether the orders are any good.** Bucket (c) has no mechanical oracle. Both
findings above came from reading a transcript, which is the only way they could
have come.
