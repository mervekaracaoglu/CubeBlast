# Core logic test harness

`CoreLogicTests.cs` is a standalone test suite for the engine-independent game
logic in `Assets/Scripts/Core`. Because that assembly has no UnityEngine
references (`noEngineReferences: true` in its asmdef), it can be compiled and
executed with any plain C# compiler, no Unity installation required:

```bash
csc -out:tests.exe Assets/Scripts/Core/*.cs Assets/Scripts/Core/Items/*.cs Tests~/CoreLogicTests.cs
LEVELS_DIR=Assets/Resources/Levels ./tests.exe
```

The suite covers group blasting, special item creation thresholds and hints,
gravity/refill rules, rocket/TNT explosions and chain reactions, all three
combo types (including board-edge clipping), vase/stone/chalice-box damage
rules, win/lose resolution, the deadlock shuffler, and 300 full random
playthroughs of the 10 shipped levels with board-consistency invariants
checked after every move (~100k assertions in total).

This folder ends with `~` so Unity does not import it.
