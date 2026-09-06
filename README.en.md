# Fishing Mod 2026 for GTA V Enhanced

*[Versione italiana → README.md](README.md)*

A fishing mod for Grand Theft Auto V Enhanced built on real data: 239
species with their weights, baits, hooks, feeding hours and rarity; 137 rods,
134 reels, 233 lines, 381 hooks, jig heads, rigs, leaders and sinkers, 25
floats, 527 lures, 205 natural baits, 31 keepnets and stringers, 18 tackle
boxes and bags, 9 rod cases. It is not "press a button and get a fish": where
you are, what time it is, how warm the water is, what you mounted and how you
use it all count.

35 waters between Los Santos and Blaine County, daily licences, a shop, home
and backpack, tackle boxes and rod cases, 51 tournaments with cash prizes,
110 levels of progression, a log with 239 species to fill, a full-screen
Rockstar-style menu, English and Italian, metric or imperial units.

## The video

[![The mod in game](https://img.youtube.com/vi/fE_OSdsgQKs/hqdefault.jpg)](https://www.youtube.com/watch?v=fE_OSdsgQKs)

https://www.youtube.com/watch?v=fE_OSdsgQKs

This file is the manual: it explains how to install, how to play and, above
all, **why a fish bites or not**. Every rule described here is the one
written in the code (`Pesca.cs`); where a number is ours and not from the
reference catalogue, it says so. A shorter version of the same guide is in
the game: menu → SETTINGS → Guide.

---

## Contents

1. [Requirements and installation](#1-requirements-and-installation)
2. [Controls](#2-controls)
3. [The menu, tab by tab](#3-the-menu-tab-by-tab)
4. [The fishing day](#4-the-fishing-day)
5. [Tackle](#5-tackle)
6. [On the water, step by step](#6-on-the-water-step-by-step)
7. [Why it bites: the rules](#7-why-it-bites-the-rules)
8. [Fish size](#8-fish-size)
9. [Junk](#9-junk)
10. [The HUD](#10-the-hud)
11. [Experience, levels, log and money](#11-experience-levels-log-and-money)
12. [Tournaments](#12-tournaments)
13. [Settings and config.ini](#13-settings-and-configini)
14. [The save file](#14-the-save-file)
15. [The data: what is real and what is ours](#15-the-data-what-is-real-and-what-is-ours)
16. [Common problems](#16-common-problems)
17. [Notes](#17-notes)

---

## 1. Requirements and installation

### Dependencies

| | version the mod was developed and tested with |
|---|---|
| **Grand Theft Auto V Enhanced** | build 1.0.1158.13 (v3889) |
| [ScriptHookV](http://www.dev-c.com/gtav/scripthookv/) | build of 15 July 2026, v3889.0/1158.13 |
| [ScriptHookVDotNet](https://github.com/scripthookvdotnet/scripthookvdotnet/releases) | 3.9.0.6 Enhanced (API 3.9.0) |

All three are required. ScriptHookV must be updated at every game patch or
nothing starts; ScriptHookVDotNet compiles `Pesca.cs` at startup, so no
compiler or other program is needed. The mod is written in old-style C# (no
string interpolation, no lambdas) on purpose, for the compiler that
ScriptHookVDotNet ships with.

The mod does not depend on other scripts and needs none. Other mods in the
same `scripts` folder do not interfere, as long as they do not use the same
keys (see [Controls](#2-controls)).

### Installation

1. Download the repository (Code → Download ZIP) and unpack it.
2. Copy the `scripts` folder into the game folder. In the end this must
   exist:

```
...\Grand Theft Auto V Enhanced\scripts\Attivita\Pesca\Pesca.cs
```

3. Start the game. On first start "Fishing module 1.0 ready" appears at the
   bottom.

The mod reads its files (data, config, save) from a fixed path, written at
the top of `Pesca.cs` (`MY_DIR`, line 20):
`C:\Program Files\Rockstar Games\Grand Theft Auto V Enhanced\scripts\Attivita\Pesca`.
If the game is installed elsewhere (Steam, Epic, another drive) change that
line to your path, otherwise the mod starts but finds no data.

What is in `scripts\Attivita\Pesca`:

| | |
|---|---|
| `Pesca.cs` | the whole mod, one file |
| `config.ini` | every setting, commented line by line; re-read while the game runs |
| `*.txt` | the data: fish, tackle, zones, licences, tournaments, temperatures, hours, rarity, hot spots, translations, guide, tips (full list in [chapter 15](#15-the-data-what-is-real-and-what-is-ours)) |
| `img\` | the images: fish, tackle, zone and tournament banners, HUD, wheel, activity graphs |
| `suoni\` | the mod's sounds (wav): cast, menu open and close |
| `gen_attivita.py` | the script (Python 3 + Pillow) that regenerates the activity graphs: only needed if you change the fish or temperature data |

On first start the mod writes its own save file (`stato.txt`) in its folder.
To reload the scripts without restarting the game: **INS**
(ScriptHookVDotNet). If something does not compile, the error with the line
number is in `ScriptHookVDotNet.log` in the game folder.

Warning: ScriptHookVDotNet compiles **every** `.cs` inside `scripts`,
subfolders included. A backup file with the `.cs` extension left in there is
loaded too: keep backups outside `scripts` or rename them `.cs.old`.

### Language and units

The language is chosen from the menu (SETTINGS → Language) and saved in
`config.ini` (`lingua=0` English, `1` Italian; the repository starts in
English). It changes everything: menu, HUD, messages, guide, tips, fish names
(`pesci_it.txt`), zone names (`zone_en.txt`), bait names (`esche_it.txt`) and
colours (`colori_it.txt`). Tackle names are the English ones of the catalogue
in both languages.

Units (SETTINGS → Units, `unita=0` metric, `1` imperial) convert everywhere:
kilos to pounds, grams to ounces, metres to feet, centimetres to inches,
Celsius to Fahrenheit. The data files stay metric.

---

## 2. Controls

The mod is designed for the pad; the keyboard does the same things. At the
bottom centre of the screen there is always the **key bar**, showing only the
keys useful at that moment and switching by itself between pad icons and
keyboard keys. Just above it the mod's **messages** appear (all of them
there: the mod does not use GTA's notifications above the radar).

### Menu

| Pad | Keyboard | |
|---|---|---|
| RB + dpad LEFT | F7 (`menu_tasto` in config.ini) | opens and closes the menu |
| LB / RB | TAB / Q | change tab |
| dpad ▲ ▼ | ▲ ▼ | scroll the list |
| dpad ◄ ► | ◄ ► | move between columns |
| A | ENTER | select / confirm |
| X | SPACE | sell (in TACKLE) |
| Y | Y | throw away (in TACKLE) |
| B | ESC | back / close |

Inside the menu the world stands still: game time does not run. With the
menu open GTA's audio is muted in the Windows mixer and comes back on close.

### With the rod in hand (before the cast)

| Pad | Keyboard | |
|---|---|---|
| LB (hold) | TAB | the **tackle wheel** |
| RT (hold and release) | left click (hold and release) | charge and **cast** |
| dpad ◄ ► | ◄ ► | reel drag |
| dpad ▲ ▼ | ▲ / ALT | bait depth under the float |
| RB | Q | change bait (cycles the ones in the backpack) |
| X | SPACE | put the rod away |
| left stick | WASD | walk, rod in hand |

### With the line in the water

| Pad | Keyboard | |
|---|---|---|
| RT | left click | reel in (and fight the fish) |
| A | ENTER | **set the hook** when it bites |
| dpad ◄ ► | ◄ ► | drag |
| LB | TAB | retrieve the line and open the wheel |
| left stick ◄ ► | A / D | moves the rod: the line sweeps left and right and drags the fish along |
| left stick back | S | the **jerk**: a stroke of the rod towards you, reels in no line (`strappata=0` turns it off) |

### With the fish (or the junk) in hand

| Pad | Keyboard | |
|---|---|---|
| A | ENTER | keep it: it goes in the keepnet |
| B | ESC | release it (junk: throw it away) |

### The tackle wheel

While fishing, **LB** no longer opens GTA's weapon wheel: it opens the tackle
wheel. Hold LB, pick the slice with the right stick, scroll the pieces of that
category you have in the backpack with dpad ◄ ►, release LB and **all** the
choices mount together. Twelve slices: rod, reel, line, leader, sinker, fish
of the lake, backpack, keepnet, hook, float, bait, lure. The first entry of
every slice is **Empty**: it unmounts that piece.

Three slices mount nothing but open the menu: **backpack** opens TACKLE,
**keepnet** opens TACKLE on the keepnet, **fish of the lake** opens the LOG
on the place you are.

If you release LB with a rod mounted and not in hand, you take it in hand; if
you unmount the rod, you put it away.

---

## 3. The menu, tab by tab

The menu is full screen, with six tabs at the top: **SPOTS, TACKLE, SHOP,
LOG, TOURNAMENTS, SETTINGS**. It opens from anywhere, even while fishing. At
the top right are your money, level and experience points.

### SPOTS

On the left the list of the 35 waters, with the level required. Selecting
one, on the right is its card: banner, name, time, weather with air and water
temperature, the graph of fish activity hour by hour with today's weather,
and the list of fish living there. For every fish three boxes, **common,
trophy, unique**, ticked when you caught that size **in that place**. Moving with ► onto the fish
column and scrolling with ▲ ▼ opens the full card of each: photo, weights
of the three sizes, value, family, preferred baits and lures, hook size,
feeding hours, temperature.

At the bottom of the zone card are the licences: **one day** and, where it
exists, **three days**, with their price. A buys the licence. The **Reach
the spot** row places the waypoint on the map. When you are on the spot with
a licence in your pocket **Start fishing** appears; while fishing **Stop fishing** appears,
which ends the day early (see [The fishing day](#4-the-fishing-day)).

### TACKLE

On the left the eight categories that can be moved: rods, reels, lines,
hooks and terminal tackle, baits, lures, floats, keepnets and stringers.
Below, two fixed boxes: the **rod case** and the **tackle box** you own, with
the room they give (they do not move: they make room wherever they are).

On the right three columns: **HOME | list | BACKPACK**. In the middle the
list of pieces of the category; on the left the ones you have at home, on the
right the ones in your backpack (with capacity: "Rods 1/3", "Items 4/10"…).
◄ ► moves between home and backpack, **A** moves the selected piece to the
other side, **X** sells it (half price, asks to confirm), **Y** throws it away
(asks to confirm). Home has unlimited room; the backpack does not.

**Lines** have one more row: cut spools, with the metres left, are shown
under the whole line and move with A too.

Under **keepnets and stringers** is the keepnet, one at a time, and while
fishing under it you see **what is in the net**: today's fish, with kilos.

While fishing, home is closed: what is in the backpack is what you brought,
you cannot move or sell. You can buy (see SHOP).

### SHOP

Three columns: category, type (for rods spinning, casting, bottom, feeder…;
for lines mono, fluorocarbon, braid, sea; for terminal tackle hooks, jig
heads, rigs, leaders, sinkers; and so on) and the items, with photo, data,
**price** in green and **level** required in yellow (red if you are not
there yet). A **green triangle** at the bottom left marks the items you
already own, at home, in the backpack or mounted (every category except
natural baits, which get used up). A buys: from home the piece goes home,
while fishing it goes straight to the backpack.

While fishing the shop stays open but **costs three times** as much: it is
the stall on the spot, handy and expensive. Planning ahead saves money. No
selling while fishing.

### LOG

On the left the 35 waters; on the right, for the selected one, the fish you
caught **there**: how many times, the record weight, the bait and hook you did
it with, and the record's size (common, trophy, unique). It is the mod's
notebook: knowing that the Gudgeon comes at Zancudo at dawn on red worms, and
that in the canyon there is a trout you will not find in the Alamo.

### TOURNAMENTS

On the left the 51 tournaments, on the right the card of the selected one:
banner, fish, zone, duration, start time and weather, the prizes (bronze,
silver, gold, with the kilos to reach and the extra for trophies and
uniques), the rules, your record. At the bottom the **Enter** row with the
ticket price, or in red the reason you cannot (level, money, licence in your
pocket…). On the spot the row becomes **Start the tournament**; during the
tournament **Withdraw**. Everything in [chapter 12](#12-tournaments).

### SETTINGS

| entry | what it does |
|---|---|
| **Float size** | how big the float is drawn on the water (kept in the save file) |
| **Language** | Italian / English, immediately |
| **Units** | metric / imperial, immediately |
| **Clear the log** | deletes catches, records and exploration; press twice |
| **Start over** | deletes everything: level, tackle, log, tournament records; press twice. Money stays: it is GTA's |
| **Guide** | the full in-game guide, by chapters |

---

## 4. The fishing day

### The zones

There are 35 waters between Los Santos and Blaine County
(`aree_livello.txt`). Every zone on the map matches a real water of the
reference simulator and has its fish: the Alamo Sea is an American lake of
catfish and bass, the Zancudo river is a trout stream, the Paleto sea is the
Norwegian fjord of cod and sharks, Vespucci and Chumash are the Florida coasts
(`pesci_aree.txt`).

Every water has a **level**: below it you are not let in. Big waters (Alamo
Sea, Zancudo, Cassidy, Land Act, Paleto) have several stretches of shore, and
different stretches have different levels and fish. The last eight zones ask
for levels 83, 94 and 100.

### The licence

The licence is bought from the SPOTS tab, from anywhere: it stays in your
pocket until you are on the spot and press **Start fishing**. It covers the
whole **water**, not the single stretch: you pay "Alamo Sea" and fish on all
its shores. Two cuts: **one day** and **three days** (2.85 times one day, the
ratio of the reference simulator). There are no other restrictions: no
"basic" or "advanced" licences, you fish at any hour and keep any size
(one exception: at the golf course ponds only commons come,
`aree_livello.txt`, last column).

The price is **ours** (`licenze.txt`): one day costs 25% of what a keepnet full
of the best-paying fish of that stretch is worth, with the best keepnet you
can buy at that level. A water with several stretches has a price for every
stretch level: you pay the highest row that does not exceed your level, that
is you pay for what you can fish. The licence is the real brake of the game,
not the tackle.

To start you need at least a **rod**, a **reel with line** and a **keepnet**
in the backpack. The rest is up to you.

With a licence in your pocket or in progress you cannot enter a tournament,
and with a tournament ticket in your pocket you cannot buy a licence: one
thing at a time.

### The clock

When you press **Start fishing** the GTA clock goes to **05:00** and slows
down: one game minute every 5 real seconds, so a 24-hour day lasts about
**two hours on the clock**. The hour matters for the fish (see the rules), so
the day has its own rhythm: dawn, morning, afternoon, dusk, night. Behind you
the **camp** appears: tackle box, backpack and a spare rod on the ground
(`campo.txt`).

At the end of the day: the clock goes back to normal, the **keepnet empties
and the fish is sold**, everything you had mounted goes back to the backpack,
home reopens. With the three-day licence the day restarts at 5 the next
morning. **Stop fishing** (in the SPOTS tab) does the same early, and the
licence ends there even if it was a three-day one.

If you quit the game mid-day, everything is saved: next start you resume at
the same hour, with the same keepnet and the same licence.

---

## 5. Tackle

### Home, backpack, tackle box and rod case

Everything you buy stays at home. Before going out you load the backpack from
the TACKLE tab. With the day in progress the backpack is what you brought:
what you forgot at home you forgot.

The **backpack** you have always had: one rod, one reel, two lines and ten
**items** (hooks and terminal tackle, floats, lures, baits). Items count **by
type**, not by piece: ten identical hooks take one slot, a second spoon
identical to one you already have takes no room.

The **rod case** and the **tackle box** are fixed: as soon as you own them
they make room, whether at home or on your back, and take no room
themselves. The rod case adds rods, reels and lines (from the two-rod HobbyGear to
the Rodster XL with seven rods and sixteen lines, `rodcase.txt`); the tackle
box adds items and lines (`cassette.txt`). The room adds up to the backpack's. If you own more than one,
the biggest counts.

The **keepnet** is separate, one at a time.

### Rods

Every rod (`canne.txt`) has a length, a **casting weight** (the grams of bait
it casts well), the kilos of line it holds and a power. Nine types: spinning,
casting, bottom, feeder, match, carp, telescopic, sea, spod.

Every fish family has its rods: carp wants carp, feeder, spod or bottom rods;
pike, bass and perch spinning and casting; trout and salmon spinning, casting
or match; the sea wants sea rods and casting; bream and small fish match,
feeder or telescopic; catfish bottom, carp or sea; sturgeon bottom and carp.
With the wrong rod the fish comes less (see the draw).

### Reels and the drag

The reel (`mulinelli.txt`) has a **drag** in kilos, a retrieve ratio and a
**capacity**: how many metres of line fit, by diameter. The thinner the line,
the more metres fit.

The drag has **12 positions** (`friz_posizioni`). Fully closed, the line does
not give and tension rises fast; the more you open it, the more line the fish
takes and the less it pulls on the line. On the HUD it is the ring around the
reel.

### Lines

Four types (`lenze.txt`): mono, fluorocarbon, braid and sea lines. Every line
has a diameter in millimetres, a **load** in kilos and the metres of the
spool.

The line is a **spool** with its metres. Mounting it, the reel cuts as many as
it holds; what is left stays as a separate spool with the remaining metres.
Unmounting it gives a spool back. Cut spools are shown in TACKLE under Lines,
with their metres, and move between home and backpack like the rest.

When the line snaps you lose three metres (`lenza_persa`), the hook or the
lure, the sinker, the float and the bait. If you had a **leader**, the leader
snaps and the main line, sinker and float stay.

### Hooks and terminal tackle

Hooks (`terminali.txt`) go from **#16** (the smallest) to **#1**, then from
**#1/0** to **#18/0** (the largest). Every fish has its range of sizes in the
catalogue. In the same category are the **jig heads** (hook and lead in one
piece, for soft baits), ready **rigs**, **leaders** (the piece of line before
the hook: the titanium one is for fish with teeth) and **sinkers**.

Sinker and float alone do not fish: at the tip you need a hook, a jig head, a
rig or a lure.

A fish **with teeth** (the `denti` column of `pesci.txt`: pike, barracuda,
sharks…) of **1.2 kg** or more (`denti_kg`), hooked without a leader, cuts
your line after a few seconds of fight (`denti_secondi`, 3.5) and leaves.
Small ones are taken on bare line too.

### Floats

The float (`galleggianti.txt`) has a size and a **load**: how much lead it
carries (light, medium, heavy). On the HUD you see it to scale with the real
bottom: if the rig is longer than the depth, the float **lies flat** on the
water. It is the sign the bait touches the bottom.

Bait depth under the float is set with the dpad, from **13 cm to 2.5 metres**
in 13 cm steps. Spoon and float do not go together: spinning, the lure is the
hook.

### Baits

Five families of natural baits (`esche_negozio.txt`): common (bread, cheese,
corn…), worms and insects, fresh, boilies and pellets, sea. Every bait has a
quantity per pack, a **weight** class (light, medium, heavy: a catalogue
indication, the mod does not use it in the cast) and the hook sizes it is
used with. The bait **is used up** at
every bite. With RB (Q) you change bait among the ones in the backpack without
opening the menu.

Every fish has its **preferred baits**, the ones of its page in the catalogue
(columns of `pesci.txt`; `esche_pesci.txt` is the evidence, in clear): see
them in the fish card, inside SPOTS. A fish with no preferred baits in the
catalogue bites any natural bait.

### Lures

Six types (`artificiali.txt`): spoons, spinners, minnows and poppers, bass
jigs, soft plastics, sea. Every lure has a weight, a length and the **size of
its hook**, which counts for the size like a normal hook.

**Predators** (the species that have lures in the catalogue) rarely bite a
natural bait and almost only small ones: for them you need the lure. The
lure counts full with a casting rod (spinning, casting, sea); with any other
(match, telescopic, feeder, bottom, carp, spod) almost nothing.

### Keepnets

The keepnet (`nasse.txt`) has two limits of its own: the biggest **single
fish** it takes and the **total kilos**. With the keepnet full, or with a fish
too big for it, you can only release the fish: the fish card tells you.
Stringers are small, cheap keepnets.

### What you can hold

The fish you can land is the **weakest of the three parts**: the rod holds up
to so many kilos, the reel brakes up to so many, the line snaps at so many.
The lowest number wins, and it is written on the HUD next to the rod. A fish
heavier than that **does not even hook up**.

---

## 6. On the water, step by step

1. **Rod in hand.** From the wheel (or with the piece mounted) take the rod.
   You can walk along the shore with the rod in hand, set the drag and the
   bait depth under the float.
2. **Cast.** Hold RT: the bar charges; release and the bait flies. The bar is
   **curved**: the first taps make a few metres, the last ones are worth a lot
   (`lancio_curva`). How far you can reach at most is decided by the rod
   (length and casting weight), the **weight at the tip** (inside the casting
   weight it counts full; too light makes a few metres, too heavy even
   fewer) and the line (thin runs, thick brakes). The weight at the tip is
   that of lure, jig head and sinker: natural bait weighs nothing. If the bait lands on grass or
   asphalt the line comes back by itself.
3. **Waiting.** The float sits on the water (or the spoon sinks, if you are
   spinning). If the rig is longer than the depth, the float **lies flat** on
   the water: it is the sign the bait touches the bottom, and the dial on the
   HUD shows it. You can reel slowly to bring the bait towards you; with a
   float, pulling hard while the fish tastes scares it and it leaves.
4. **Nibbles.** In the last seconds before the bite the float dances and the
   pad vibrates. Before that the water is really still: fishing is quiet, and
   when the fish comes, it comes.
5. **The bite.** The float goes under, the pad vibrates hard and the bar shows
   **A – Set the hook**. You have **a second and a half**: if you do not set
   it, "It got away".
6. **The fight.** Hold RT to reel in. The bar above the drag is the line
   **tension**: blue, green, yellow, red. The fish makes its runs: it takes
   line, changes direction and loads the line, the more the heavier it is
   compared to what you can hold; as it tires the radius shrinks. If the
   tension stays at the top for more than a moment (`lenza_rottura_ms`,
   0.7 s) the line **snaps**. The last metres are the hardest: under three
   metres the fish sees the shore and digs in.
7. **Teeth.** A fish with teeth of 1.2 kg or more, hooked without a leader,
   cuts your line after a few seconds and leaves.
8. **On the shore.** When the metres are done the fish is yours: the card
   appears with name, photo, size (common, trophy, unique specimen), weight,
   value and points. **A** keeps it, **B** releases it. If the keepnet cannot
   take it, the card says why and only B remains.

---

## 7. Why it bites: the rules

This is the part that makes the difference between fishing and pressing a
button. Every time you cast three things happen, in order: a **wait** starts,
when it expires a fish is **drawn** among those of the place, and that fish
**looks at the bait**.

### 7.1 Water temperature

GTA has no temperature: the mod computes it, and it is shown on the HUD. It
depends on three things:

- the **game hour**: air is 13° at 4 in the morning and 27° at 16, with a
  smooth curve in between;
- the **weather**: full sun, rain, snow shift it a few degrees;
- the **altitude**: above 50 metres air drops 0.6° every 100 metres.

Water is steadier than air: 16° plus 45% of the air's distance from 20°.
These numbers are ours, not the catalogue's.

Every species has its **temperature range** (`temperature_pesci.txt`:
minimum, maximum, optimum). Trout and char are fine between 4 and 16°, carp
and catfish between 15 and 28°, Amazon fish between 24 and 32°, North Sea cod
between 3 and 12°. They are indicative values, taken from general biology:
the reference catalogue does not have them.

At its optimum temperature a fish **counts full**; at the edges of its range
40% (`temp_bordo`); more than 4° outside the range (`temp_fuori`) it **does
not come**. So at the lake, in winter, trout and pike come; in summer carp
and catfish. You will not catch the Paleto cod with the water at 25°.

In the zone card, in SPOTS, there is the graph of fish activity for that
place hour by hour, with today's weather, and the line of the hour you are
at. The same graph is on the HUD at the top left while fishing.

### 7.2 The wait

How long you wait after the cast depends on **how alive the water is now**:
among the fish of the place, the one best suited to the current temperature
is looked at, and the base wait (`attesa_base`, 60 seconds) is divided by
that value.

| the water is | wait (about) |
|---|---|
| alive (a fish at its optimum temperature) | 60–66 seconds |
| so-so (the fish at the edges of their range) | 2 minutes and a half |
| still (nobody in its range) | up to 6–7 minutes |

Casting distance does not count. The bait does not count. Reeling in slowly
while waiting shortens the wait a little.

### 7.3 The draw

When the wait expires every species of the place is looked at. Some are
**discarded**:

- it weighs more than your tackle holds;
- it does not live in this zone;
- your hook is too far from its size (see below);
- the technique is impossible (the spoon with a rod that cannot cast it);
- it is more than 4° outside its temperature range.

The remaining ones get a **weight** in the draw, which is the product of:

| factor | how it counts |
|---|---|
| **rarity** (from the catalogue, column of `pesci.txt`) | very common 100, common 55, normal 28, rare 12, very rare 5 |
| **hook size** (from the catalogue) | inside the fish's range 1; one size out 0.55; two out 0.25; three out 0.08; beyond it does not come |
| **hour of the day** (the species' habit, column of `pesci.txt`) | night feeders: night 1, dawn/dusk 0.45, full day 0.12. Dawn and dusk feeders: 1 in their hours, 0.35 by day, 0.30 at night. Day feeders: 1, 0.45, 0.10. Night = 21–5, dawn/dusk = 5–8 and 18–21. All-day feeders: 0.75 at any hour |
| **technique** | the predator on natural bait bites at 0.25 and almost only small; the lure with a casting rod (spinning, casting, sea) counts 1, with any other rod 0.15 |
| **rod for the family** | with the wrong rod 0.60 if it is a match, bottom or telescopic rod, 0.35 otherwise |
| **hook for the family** | a hook specialised for another family 0.55; a generic hook 0.80; the family's own hook 1 |
| **temperature** | 1 at the optimum, 0.4 at the edges of the range |
| **hot spot** | if the bait sits over that species' spot, × 6 |

The draw is proportional to the weights. If no species is left in play
(wrong hook, hour, water), nothing happens: the line stays in the water and
it tries again, with no warning. If it never bites, the question is always
the same: what time is it, how warm is the water, what hook do you have.

### 7.4 The bait

The bait **discards nobody** from the draw: it decides afterwards. The drawn
fish looks at what is on the hook:

- if it is one of **its own** (the list of its page in the catalogue) → **it
  always bites**;
- if it is **not** its own → it bites **one time in three**
  (`esca_sbagliata_abbocca`, 33 in 100); the other two it leaves, and you wait
  again.

So you catch the bluegill on bread, but on worms you catch it three times as
often. The same goes for lures: a spoon that fish does not chase catches it
one time in three.

With nothing on the hook nobody bites.

### 7.5 Hot spots

Inside every water the fish are not scattered: there are spots (a hole, a
bank, a reed bed, a point offshore) where a species lives. Over that species'
spot the fish weighs six times more in the draw; on deep spots the species
does not change, but the size draw shifts upwards. The spots are ours
(`punti_caldi.txt`) and are not marked: you find them. In big waters and at
sea there are spots one and two hundred metres from the shore: the middle of
the lake is not empty.

### 7.6 Passing fish

While you are on the shore, now and then a fish passes underwater, alone or in
a group of up to three, each with its own shape and swim. They are the
species of that water: they tell you what is there, not what will bite
(`pesci_scena_*` in config.ini).

### 7.7 The passing angler

Now and then (every 1 to 5 real minutes) another angler arrives on the shore,
30 to 80 metres from you: he stays five minutes, now and then pulls up a fish
of the water and shows it for a moment, then leaves. It is scenery, not
competition: he takes no fish from you. Turn it off with `pnj_pescatore=0`.

---

## 8. Fish size

Once the species is drawn, the **weight** is rolled, between a minimum and a
ceiling:

- the minimum is 60% of the catalogue's "common" weight;
- the ceiling is decided by **the hook** (`amo_taglia`), as in the reference
  simulator. Every fish has its hook range: with the hook at the **small**
  size of the range only **commons** come; from the **middle** up
  **trophies** too; with the **large** size **unique specimens** too. Pike
  goes from #1/0 to #8/0: from #1/0 to #4/0 commons, from #5/0 to #7/0
  trophies, with #8/0 uniques. The lure's hook counts the same way. Outside
  the range the nearest size counts. The trophy ceiling is the trophy weight
  plus 10% (`trofeo_extra`);
- the ceiling never exceeds what your tackle holds;
- at the golf course ponds the ceiling is the common weight: only commons
  come there.

The die is **loaded downwards**: the hook opens the ceiling, it does not take
you there. With the wrong technique (predator on natural bait) almost only
small ones come; on a deep spot the roll shifts upwards. The unique can
exceed the catalogue weight by 20% (`unico_extra`).

After a **big catch** there is a pause, per species, in real minutes and
saved: after a unique specimen that species gives no more uniques for 20
minutes (`unico_pausa_min`) nor trophies for 5; after a trophy no trophies
for 5 minutes (`trofeo_pausa_min`). The pike's unique does not block the
carp's unique.

For the 23 species that have a single weight in the catalogue, trophy and
unique are ours: 1.9 and 2.9 times the common weight (`pesci.txt`, at the top
of the file).

---

## 9. Junk

This is Los Santos: now and then an old shoe, a plastic bag, a traffic cone,
a water plant, a bottle, a can, a tyre, a car door, a paint can, a briefcase
hooks up (`robaccia.txt`). They are worth zero to five dollars, give no points
and do not go in the keepnet.

Junk comes **only with the wrong bait**: when the drawn fish finds a bait
that is not its own and leaves, one time in four
(`robaccia_prob_esca_sbagliata`, 25 in 100) junk hooks up instead. With the
right bait it never comes. With nothing on the hook it comes one time in
three (`robaccia_prob_senza_esca`, 35 in 100).

When it happens: "Something took: reel in". The junk follows the line
underwater and, pulled up, hangs from the rod like a fish; the **JUNK** card
appears with what you caught, its weight and the few dollars it is worth.
**A** or **B**: you throw it away and cash in.

---

## 10. The HUD

While fishing, at the **top left**:

- the game **time** and, below, how long the licence has left;
- the **level** and experience points;
- the **tip**: every 5 real minutes (`sugg_ogni_min`) a piece of advice about
  the game appears for 30 seconds (`sugg_dura_sec`), at random and without
  repeating until all have been shown (`suggerimenti_it.txt`,
  `suggerimenti_en.txt`);
- the **place** you are at, with the **exploration** bar (how many species of
  the place you have already caught);
- the weather with the **air temperature** and the **water temperature**;
- the graph of the place's fish **activity** hour by hour, with the dot of
  the current hour.

At the **top right**:

- the **bait** in the circle, with its name, how many you have and the hook
  size;
- the **keepnet**, and below it the kilos inside.

At the **bottom right**, minimum text:

- the **rod** with the kilos it holds (the weakest of the three parts);
- the **rig column**: line, sinker, leader, float (with its load: light,
  medium, heavy), hook, with a box behind the mounted pieces;
- the **water dial**: sea, lake or river with their colour and their bottom
  (sand and coral, mud with weeds and stones, river pebbles); the float to
  scale with the real bottom, lying flat when the bait touches the ground;
  below, "Bait x m" (the depth set), "Bottom x m" (when the line is in the
  water) and "Water x°";
- the **drag**: the 12-notch ring with the reel inside, the percentage, the
  fish's kilos and the metres of line out;
- the **tension bar**, notched, colouring from blue to red.

At the **bottom centre**: the bar of the keys useful now and, above it, the
mod's messages. With the rod put away, the bar reminds you of the two keys
that matter: LB for the tackle, RB + LEFT (F7) for the menu.

Every position and size can be changed in `config.ini`.

---

## 11. Experience, levels, log and money

### Points

Every fish **landed** gives experience and goes in the log, even if you
then release it; only what you keep goes in the keepnet, and so on sale:

| | |
|---|---|
| base | 20 + 15 per kilo |
| how many times you already caught that species | first time × 8, 2nd to 5th × 3, 6th to 20th × 1.5, beyond × 1.3 |
| size | trophy × 2, unique specimen × 3 |

The three factors multiply. None dominates: a new fish caught small is worth
as much as a known one caught big. You climb by catching **different
species**, not by repeating the same one. Bait and hook give no extra points:
they decide whether it bites and how big, not what it is worth.

### Levels

The level unlocks tackle and zones, with the real levels of the catalogue.
How long it takes to climb is ours (`livelli.txt`): about **three outings per
level**, from start to finish, with no walls. There are **110** levels: at 100
you have opened all 35 waters, from there on only the last terminal tackle in
the shop unlocks.

### The log

What you catch goes in the **log** (LOG tab): for every zone the fish you
caught there and how many times, and for every species the record weight, the
bait and hook you did it with. In the zone card the three boxes common,
trophy and unique are ticked when you caught them **in that place**; the
exploration bar says how many species of the place you have found. 239
species to fill.

### Money

Tackle prices are the catalogue's **divided by 10** (`CAMBIO` in the code).
Fish is sold **at the end of the day**, at full price per kilo, the
catalogue's for its size; the money goes to the character's account. Tackle
resells at **half price** (`vendi_percento`, 50) from home or from the
backpack, with X in TACKLE, but not while fishing. The stall on the spot
costs three times the home shop.

---

## 12. Tournaments

51 tournaments, taken from the cards of the single tournaments of the
catalogue (`tornei.txt`), almost all on a single species (some species have
more than one, in different zones; two tournaments count every fish of the
lake): duration, minimum level, entry
fee, prizes, scoring rule, recommended tackle, start time and weather are
theirs. The only thing ours is the **zone**: the catalogue holds them on
lakes we do not have, so every tournament sits in our water where that fish
lives.

How it works:

1. From the TOURNAMENTS tab, from anywhere, you buy the **entry**: the ticket
   stays in your pocket. You need the tournament's minimum level and must not
   have a licence in your pocket or in progress.
2. On the spot press **Start the tournament**: a day of its own starts, with
   the tournament's hour and sky, and the minutes run (30, 45, 60… real
   minutes depending on the tournament).
3. Only fish of **that species** put in the keepnet count (every fish, in the
   two "whole lake" tournaments): the kilos add up.
   **Bronze, silver and gold** are the kilo thresholds to reach, each with its
   prize in dollars; if you caught at least one trophy there is an extra, and
   another if you caught at least one unique specimen. Below bronze nothing
   is won.
4. When time is up the day closes: the fish is sold, the weather is
   released, the prize arrives and your best result stays as the
   tournament's **record** (`tornei_record.txt`). If you press **Withdraw**
   the day closes at once, with no prize and no record.

The tackle written in the tournament rules is the catalogue's, as advice: the
mod does not check it.

---

## 13. Settings and config.ini

`scripts\Attivita\Pesca\config.ini` is re-read **while the game runs**: almost
everything changes without restarting. Every entry has its comment. The most
useful:

| entry | what it does |
|---|---|
| `lingua`, `unita` | 0 English / metric, 1 Italian / imperial (also from the menu) |
| `menu_tasto` | the keyboard key that opens the menu (F7) |
| `attesa_base`, `attesa_caso` | the wait after the cast (ms) and the random part |
| `esca_sbagliata_abbocca` | in 100, how often it bites on a bait not its own (33) |
| `robaccia_prob_esca_sbagliata`, `robaccia_prob_senza_esca` | junk (25, 35) |
| `temp_pesi`, `temp_bordo`, `temp_fuori` | temperature in the draw |
| `amo_taglia` | the hook decides the size (1 on) |
| `unico_pausa_min`, `trofeo_pausa_min` | the pause after a big catch (20, 5) |
| `unico_extra` | how much the unique can exceed the catalogue weight (%) |
| `trofeo_extra` | how much the trophy can exceed the catalogue weight (%) |
| `denti_kg`, `denti_secondi` | from what weight a toothy fish without leader cuts the line (1.2) and after how many seconds (3.5) |
| `lenza_persa`, `lenza_rottura_ms` | metres lost on a snap (3) and how long the tension must stay in the red before it snaps (700) |
| `strappata`, `strappo_metri` | the jerk with the stick back (1 on) and the metres it reels in (0) |
| `lancio_curva`, `lancio_minimo` | the curve of the casting bar and the minimum metres |
| `friz_posizioni` | drag positions (12) |
| `vendi_percento` | tackle resale value (50) |
| `pesci_scena_*` | passing fish: how often, how many, where |
| `pnj_*` | the passing angler: how often, how far, how long |
| `sugg_*` | the tips: position, every how many minutes, for how many seconds |
| `canna_disegnata` | 1 drawn rod that bends, 0 GTA model |
| `menu_*` | sizes and fonts of the menu |
| `orario_*`, `liv_*`, `posto_*`, `esplora_y`, `temp_*`, `attivita_*` | the HUD at the top left |
| `consigli_*`, `colonna_*`, `esca_*`, `nassa_*`, `friz_*`, `barra_*`, `messaggio_*` | the HUD at the bottom and top right |

Some entries (`denti_kg`, `denti_secondi`, `lenza_persa`, `vendi_percento`,
`messaggio_*`) are not in the file: the mod uses the value written above; to
change them just add the line `entry=value` to `config.ini`.

---

## 14. The save file

Everything is in `scripts\Attivita\Pesca\stato.txt`, written by the mod every
game minute and at every important action: level and points, home and
backpack, the mounted rig, cut spools, the log (catches, records, sizes caught
per zone), the licence in progress with the hour of the day and the keepnet,
the ticket or tournament in progress, the pauses after big catches, the
float size. Tournament records are in `tornei_record.txt`.

So you can quit the game mid-day and resume where you were. To start again:
SETTINGS → **Clear the log** (catches only) or **Start over** (everything).
Deleting `stato.txt` and `tornei_record.txt` with the game closed does the
same.

Money is not in the save file: it is the GTA character's.

---

## 15. The data: what is real and what is ours

**From the reference catalogue**: the whole catalogue (fish with common,
trophy and unique weights, prices, preferred baits, lures, hook size, family,
hours and rarity; rods, reels, lines, hooks, terminal tackle, natural baits
with their weight class, lures, floats, tackle boxes, keepnets, rod cases, with
levels and prices), the waters and their fish, the tournaments, the ratio
between the one-day and the three-day licence.

**Ours**, and written as such at the top of the file that holds them:

- water temperature and the ranges per species (`temperature_pesci.txt`);
- the weights of the draw (rarity, hook, hour, technique, rods and hooks per family);
- the wait and its numbers;
- the "one in three" of the wrong bait and the junk;
- the hook steps for the size (the rule is the reference simulator's, the steps are ours) and the pauses;
- the hot spots (`punti_caldi.txt`);
- the level curve (`livelli.txt`), the points formula, licence prices (`licenze.txt`), the credits → dollars exchange;
- the tournament zones;
- trophy and unique of the 23 species that have a single weight in the catalogue.

`regole.txt` is the memo of what we decided and why.

The data files, all text with the `|` separator and a header at the top:

| file | content |
|---|---|
| `pesci.txt` | the 239 species: weights, prices, teeth, hook, family, baits, lures, zones, hours, rarity, predator |
| `pesci_it.txt`, `esche_it.txt`, `colori_it.txt`, `zone_en.txt` | the translations |
| `pesci_aree.txt`, `aree_livello.txt` | the fish of every zone and the level of every zone |
| `temperature_pesci.txt` | at what temperature every species feeds |
| `esche_pesci.txt`, `orari_pesci.txt`, `rarita_pesci.txt` | the evidence in clear of baits, hours and rarity (the same columns as `pesci.txt`; the mod reads `pesci.txt`) |
| `pesci_modello.txt` | which GTA model is shown for every species |
| `canne.txt`, `mulinelli.txt`, `lenze.txt`, `terminali.txt`, `galleggianti.txt`, `artificiali.txt`, `esche.txt`, `esche_negozio.txt`, `nasse.txt`, `cassette.txt`, `rodcase.txt` | the tackle on sale (`portacanne.txt` is an old list of ours, no longer read) |
| `licenze.txt`, `negozi_zona.txt` | licence prices, stall names |
| `tornei.txt` | the 51 tournaments |
| `punti_caldi.txt`, `acque.txt`, `accessi.txt`, `zone_marcate.txt`, `campo.txt` | hot spots, the map of the waters, the accesses, the camp |
| `robaccia.txt` | the junk |
| `guida_it.txt`, `guida_en.txt`, `suggerimenti_it.txt`, `suggerimenti_en.txt` | the in-game guide and the tips |
| `regole.txt` | the memo of decisions |

---

## 16. Common problems

**"Fishing module ready" does not appear.** ScriptHookV is not updated to the
game version, or ScriptHookVDotNet is not the Enhanced one. Check
`ScriptHookVDotNet.log`.

**Compile error after an edit.** The line is in the log. Remember the
compiler is ScriptHookVDotNet's: no `$"..."`, no lambdas.

**The menu does not open with F7.** Another mod uses the same key: change
`menu_tasto` in config.ini.

**It never bites.** In order: what time it is, how warm the water is (the
activity graph on the HUD says whether the place is alive), what hook you
have (the fish card says the size), what bait you have. A place with still
water waits up to seven minutes.

**The fish never hooks up / the line snaps at once.** Look at the kilos next
to the rod on the HUD: it is the weakest of the three parts. Open the drag
during the fish's runs.

**The game is silent in headphones after using the menu.** With the menu
open the mod mutes GTA in the Windows mixer and unmutes it on close and at
every startup. If it stays muted (for example after a crash with the menu
open), unmute GTA in the Windows volume mixer.

**I want to start from scratch.** SETTINGS → Start over, or delete
`stato.txt` with the game closed.

---

## 17. Notes

Personal, non-profit project, not affiliated with Rockstar Games. *Grand
Theft Auto V* is a trademark of Rockstar Games. Tackle and fish data come
from a public reference catalogue and remain the property of their
respective owners.
