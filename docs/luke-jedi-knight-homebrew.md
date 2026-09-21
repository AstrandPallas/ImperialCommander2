# Luke Skywalker, Jedi Knight — as a campaign hero

**This is homebrew.** It is not an official FFG conversion, and nothing here is
printed on any card you own. Luke ships in *Jabba's Realm* as an **ally**
(`A011`, "Luke Skywalker (Jedi)") with a full stat block, **no hero sheet and
no class deck**. Both had to be designed, so both are guesses — informed ones,
but guesses. The numbers below are starting points to be corrected by play, not
settled values.

The *Luke Skywalker Jedi Knight* pack contains a figure, a rulesheet, 2
deployment cards, 1 side mission, 2 skirmish missions and 3 command cards. It
contains no hero sheet and no class cards, which is why none of this can be
copied from the box.

## The problem this solves, and the problem it creates

Ally Luke costs 12 threat and has **16 health** with two strong innate
abilities. Campaign heroes start around 8–12 health with 3–5 endurance and one
modest hero ability, beginning with a single 0-XP class card. Dropping ally
Luke in as a level-0 hero would trivialise the early campaign.

Worse, **allies have no endurance at all** in Imperial Assault — they cannot
strain, so there is no value to port. Endurance had to be invented outright.

The fix is a deliberate **power split**: the level-0 sheet keeps the identity,
and the surplus power moves into the class deck to be bought back with XP.

| Ally card | Level-0 hero sheet | Where the rest went |
|---|---|---|
| health 16 | **health 11** | health returns through items and class cards |
| no endurance | **endurance 4** | invented; enables Rest and surge-for-strain |
| innate `Heroic` | **not innate** | `luke07`, a 4-XP capstone |
| innate `Deflect` | **not innate** | `luke03`, at 2 XP |
| innate melee Blue/Green/Yellow | **from the 0-XP card** | so the item deck upgrades his weapon normally |
| `+1 {H}`, `+1 {F}` keywords | dropped at level 0 | folded into individual class cards |
| speed 4, defense White | **unchanged** | — |

Moving the attack dice onto the 0-XP class card matters more than it looks.
Heroes get weapons from the **item deck**; if Luke's attack were innate he
would either double-dip or be locked out of item progression. Diala's
`Plasteel Staff` works exactly this way, so this follows the existing pattern
rather than inventing one.

## Hero sheet — healthy side

```
LUKE SKYWALKER                                    JEDI KNIGHT
Health 11    Endurance 4    Speed 4    Defense: White
Traits: Force User, Leader, Brawler

Hero ability — Trust the Force
  Once per round, when you would spend a surge, you may instead
  suffer 1 strain to spend that surge twice.
```

## Hero sheet — wounded side

Heroes have a wounded side; allies have no equivalent, so this too is invented.
Thematically a wounded Luke leans harder on the Force rather than hitting
harder, so that being wounded stays a real cost.

```
LUKE SKYWALKER (WOUNDED)                          JEDI KNIGHT
Health 11    Endurance 4    Speed 4    Defense: White

Hero ability — Reach Out
  Once per round, when you defend, you may suffer 1 strain to
  reroll one defense die.
```

## Class deck — 9 cards

Costs follow the convention the shipped decks obey: **exactly two cards at each
of 1, 2, 3 and 4 XP, plus a starting card at 0.** Luke's deck must match that
shape or the XP economy desynchronises from every other hero.

One correction to the original design note, found by checking the data rather
than trusting it: the decks are **not** uniformly nine cards. Verena Talos
(`H11`) ships with **ten** — two of them at 0 XP, `Fighting Knife` and
`Military Blaster`, because she is a melee and ranged hybrid who starts with
both weapons. The data marks the extra one with the id suffix `00a` rather
than inventing a tier, which is what makes it readable as deliberate. The
two-per-tier rule is what actually holds across every deck, and that is what
the test asserts.

The deck deliberately does **not** clone Diala, who is the existing Force hero
and already owns Force Throw, Force Adept, Battle Meditation and Defensive
Stance. Luke leans on his ally card's identity instead: lightsaber melee,
deflecting ranged fire, and leadership.

| id | XP | Name | Text |
|---|---|---|---|
| `luke00` | 0 | **Jedi Knight's Lightsaber** | Melee. Attack pool: Blue, Green, Yellow. Surge: +1 {H}. Surge: Pierce 1. |
| `luke01` | 1 | **Rallying Presence** | At the start of your activation, one adjacent friendly figure may recover 1 strain. |
| `luke02` | 1 | **Sweeping Strike** | When you perform a melee attack, you may suffer 1 strain to also test Strength; if you pass, one other hostile figure adjacent to you suffers 1 {H}. |
| `luke03` | 2 | **Deflect** | After a ranged attack targeting you or an adjacent friendly figure resolves, a hostile figure of your choice in your line of sight suffers 1 {H}. |
| `luke04` | 2 | **Unshakable Resolve** | You may spend 1 {F} during your activation to remove one harmful condition from yourself. |
| `luke05` | 3 | **Lead by Example** | When you defeat a hostile figure, each friendly figure within 3 spaces may move 1 space. Limit once per round. |
| `luke06` | 3 | **Saber Flourish** | Your melee attacks gain: Surge: +1 Pierce. While attacking a figure adjacent to a friendly figure, apply +1 {H}. |
| `luke07` | 4 | **Heroic** | Once during your activation, you may perform an attack without spending an action. |
| `luke08` | 4 | **Master of the Force** | You may exhaust this card when you would suffer {H} from an attack to reduce that damage by 2. |

`Deflect` lands at 2 XP and `Heroic` at 4 XP because those are the two
abilities that make ally Luke overpowered. Everything else fills the tiers from
his `Leader` and `Brawler` traits.

## What this fork changes

| Where | Change |
|---|---|
| `CardData/heroes.json` | `H22`, `isHero: true`, expansion `Jabba`, traits Force User / Leader / Brawler |
| `CardData/herostats-homebrew.json` | health 11, endurance 4, speed 4, defense White. Kept **separate** from `herostats.json` so sourced data stays sourced and homebrew stays labelled |
| `Languages/En/CampaignData/skills.json` | the 9 `CampaignSkill` rows above, owner `H22`. Per-language; only `En` is authored, and the others fall back readably |
| `LukeAsHero` | reports the two setup conflicts below rather than silently fixing them |

**Not done:** a hero portrait (`mugShotPath`) is an art asset and is not
authored here, and the other languages are untranslated.

## Two things that break, and are reported rather than fixed

**He cannot be both.** `A011` must be excluded from the ally pool while Luke is
a hero — he cannot recruit an ally version of himself. `LukeAsHero.Check`
reports this; applying it is left to the campaign, because removing a card the
players chose is not the app's decision.

**His side mission's reward is void.** `OTHER2` — "A Light in the Darkness" —
is already in this repo, and its reward reads *"gain Luke Skywalker (Jedi
Knight) as an ally"*, staging his arrival by shuttle at a `DP Luke` deployment
point. If Luke is already standing on the board, both the narrative and the
reward are meaningless.

The suggested fix is to **re-skin rather than delete**, since the campaign
structure already reserves a personal side mission per hero: keep the map and
the encounter, replace the arrival beat, and change the reward from "recruit
Luke" to a free class card or a bump of Luke-only XP. That preserves content
the player paid for and slots into the existing structure.

The shipped `OTHER2.json` is **deliberately left unmodified**. Rewriting FFG
mission content in place would make the change invisible and hard to undo.

## Balance, and how to check it

- Threat, fame and deployment costs are unaffected — Luke **replaces** a hero
  rather than adding one, so party size scaling is unchanged.
- Reuse the simulation harness: run campaign missions with a Luke party and
  compare win rate and average hero damage taken against the same missions with
  a standard party. If Luke's party is markedly safer at equal XP, the level-0
  sheet is still too strong — pull more power into the deck.
- Check XP pacing: at every total from 0 to 8, Luke's purchasable power should
  sit inside the band the 21 shipped decks occupy, not above it.
- Play the Jabba-wave missions he originally shipped for, which are tuned
  around him being present.

**Physical components do not exist.** The app can model him, but the players
need a printed hero sheet (both sides) and 9 class cards at the table. The
tables above are the source for printing them.
