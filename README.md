# Cube Blast — Dream Games Software Engineering Case Study

A level-based mobile blast puzzle game built with **Unity 6000.3.10f1**, C# and
Unity's built-in renderer, in portrait (9:16) orientation.

## Getting started

1. Open the project folder with Unity **6000.3.10f1**.
2. Open `Assets/Scenes/MainScene.unity` and press Play (a 9:16 portrait Game
   view resolution is recommended, e.g. 1080x1920).
3. Tap the LevelButton to play the current level.

Both scenes are registered in the build settings automatically on first load
(`Assets/Editor/ProjectBootstrap.cs`), and every sprite import setting is
enforced by an asset postprocessor, so the project works from a clean checkout
with no manual setup.

### Editor tools

The **Dream Games** menu in the editor toolbar contains the level progress
tools required by the case study:

* **Set Last Played Level...** window with a slider to set the persisted
  last played level number (setting it to 11 marks all levels finished).
* **Reset Progress (Level 1)** and **Mark All Levels Finished** shortcuts.

Progress is persisted with `PlayerPrefs`, so the LevelButton shows the same
level after Unity is restarted.

## Architecture

The project is split into an engine-independent simulation and a Unity view
layer that only animates what the simulation reports.

```
Assets/Scripts
├── Core/    Pure C# game simulation (its asmdef has noEngineReferences: true)
│   ├── Items/           BoardItem hierarchy: cubes, rocket/TNT, vase/stone/chalice box
│   ├── Board.cs         Cell grid with multi-cell item support (2x2 Chalice Box)
│   ├── BoardEngine.cs   Tap processing, move/win/lose state, hints, deadlock shuffle
│   ├── ExplosionResolver.cs  Rockets, TNTs, combos and chain reactions
│   ├── GravitySystem.cs Coded fall + top spawning rules
│   └── TurnSteps.cs     Ordered turn steps consumed by the view layer
├── Game/    Scene controllers, level loading/progress, view classes per item type
├── UI/      Runtime-built HUD, fail popup, win celebration, UI factory
├── Fx/      Coded sprite-particle bursts and camera shake
└── Tween/   Small in-house tween engine (no third-party dependencies)
```

**How a turn flows:** the scene controller converts a tap to a grid cell and
calls `BoardEngine.Tap`. The engine resolves the entire turn synchronously and
returns a `TurnResult`, an ordered list of steps (blast, combo gather,
explosions with chain waves, gravity/spawns, shuffle) describing exactly what
happened. The controller then plays those steps back as animations. This keeps
every gameplay rule in one testable place and makes the visuals a pure
function of the simulation.

**OOP notes:** items form a polymorphic hierarchy (`BoardItem` →
`CubeItem` / `SpecialItem` / `ObstacleItem`). Each obstacle implements its own
damage rules (`ApplyBlastDamage` / `ApplyExplosionDamage`), so adding a new
obstacle or special item means adding one core class and one view class, there are no switch statements. All tuning values live in `GameRules`.

## Gameplay rules implemented

* Tap 2+ adjacent same-colored cubes to blast them; a move is spent only on
  valid taps. Groups of 4–5 create a Rocket (random orientation), 6+ create a
  TNT, at the tapped cell and eligible groups display the matching item logo
  as a hint.
* All falling is implemented in code (no physics, no Unity animations):
  items fall straight down, never through other items, and new random cubes
  enter from the top of each open column.
* Rockets split into two parts that damage cells one by one as they travel;
  TNT explodes 5x5. Special items explode when tapped or when damaged by
  another special item (chain reactions).
* Tapping adjacent special items merges them at the tapped cell into a combo:
  rocket+rocket → perpendicular rockets in a + shape; 2+ TNTs → 7x7; TNT +
  rocket(s) → a 3x3 array of rockets clearing three rows and three columns.
* Obstacles: **Vase** (2 hits, max one per blast, falls), **Stone** (special
  item damage only, static), **Chalice Box** (static 2x2; door phase takes
  exactly 1 damage per source; afterwards every point of damage, explosion
  cells covered or adjacent blast cubes, collects one chalice; 10 chalices
  clear it).
* Win → celebration particles/animation, then back to the MainScene with the
  next level persisted. Lose → fail popup with Close (main menu) and Try Again
  (replay) options. After level 10 the LevelButton shows **Finished**.

## Design assumptions

The case study leaves a Chalice Box detail open, which is kept as named
constant in `GameRules.cs`:

* `ChaliceBoxDoorHealth = 1` — the door takes exactly 1 damage per source;
  its durability is not specified, so it is tunable. The value was chosen by
  observing the Royal Kingdom gameplay.

For combos, each primitive explosion (every rocket of a combo, every TNT
area) counts as its own damage source, matching the reference games.

As a small extra, the engine reshuffles the cubes if the board ever has no
valid move left.

## Testing

The core simulation is fully covered by a standalone test harness (see
`Tests~/README.md`): targeted tests for every mechanic above plus 300 random
playthroughs of the shipped 10 levels with board-consistency invariants
verified after every move, around 100,000 assertions in total.
