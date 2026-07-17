using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CubeBlast.Core;

static class T
{
    public static int Failed;
    public static int Passed;
    public static void Assert(bool cond, string name)
    {
        if (cond) { Passed++; }
        else { Failed++; Console.WriteLine("FAIL: " + name); }
    }
    public static void Eq(int a, int b, string name)
    {
        Assert(a == b, name + " (expected " + b + ", got " + a + ")");
    }
}

class FixedRandom : IRandomProvider
{
    private readonly Queue<int> _values;
    public FixedRandom(params int[] values) { _values = new Queue<int>(values); }
    public int Next(int max) { return _values.Count > 0 ? _values.Dequeue() % max : 0; }
}

static class Program
{
    // grid tokens are bottom-left first, row by row
    static BoardEngine Engine(int w, int h, int moves, string tokens, IRandomProvider rng = null)
    {
        var tok = tokens.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return new BoardEngine(new LevelDefinition(1, w, h, moves, tok), rng ?? new SystemRandomProvider(42));
    }

    static void CheckInvariants(Board board, string name)
    {
        foreach (var item in board.Items.ToList())
        {
            foreach (var cell in item.OccupiedCells)
            {
                T.Assert(board.InBounds(cell), name + ": item in bounds");
                T.Assert(board.ItemAt(cell) == item, name + ": cell backref " + cell);
            }
        }
        for (int x = 0; x < board.Width; x++)
        for (int y = 0; y < board.Height; y++)
        {
            var it = board.ItemAt(new GridPosition(x, y));
            if (it != null) T.Assert(board.Items.Contains(it), name + ": stray item at " + x + "," + y);
        }
    }

    static LevelDefinition LoadLevel(string path)
    {
        string s = File.ReadAllText(path);
        Func<string, int> num = key =>
        {
            int i = s.IndexOf("\"" + key + "\""); i = s.IndexOf(':', i) + 1;
            int j = i; while (j < s.Length && (char.IsDigit(s[j]) || char.IsWhiteSpace(s[j]))) j++;
            return int.Parse(s.Substring(i, j - i).Trim());
        };
        int gi = s.IndexOf('[');
        var grid = s.Substring(gi + 1, s.IndexOf(']') - gi - 1)
            .Split(',').Select(x => x.Trim().Trim('"')).ToArray();
        return new LevelDefinition(num("level_number"), num("grid_width"), num("grid_height"), num("move_count"), grid);
    }

    static void Main()
    {
        TestParsingRealLevels();
        TestBlastBasics();
        TestSpecialCreation();
        TestGravity();
        TestRocket();
        TestTnt();
        TestCombos();
        TestVase();
        TestStone();
        TestChaliceBox();
        TestWinLose();
        TestDeadlockShuffle();
        TestRandomPlaythroughs();
        Console.WriteLine("Passed: " + T.Passed + "  Failed: " + T.Failed);
        Environment.Exit(T.Failed == 0 ? 0 : 1);
    }

    static void TestParsingRealLevels()
    {
        var dir = Environment.GetEnvironmentVariable("LEVELS_DIR");
        foreach (var f in Directory.GetFiles(dir, "level_*.json").OrderBy(x => x))
        {
            var def = LoadLevel(f);
            var engine = new BoardEngine(def, new SystemRandomProvider(1));
            CheckInvariants(engine.Board, "load " + Path.GetFileName(f));
            T.Assert(engine.State == LevelState.Playing, "playing " + Path.GetFileName(f));
            T.Assert(engine.HasAnyValidMove(), "has move " + Path.GetFileName(f));
            T.Eq(engine.MovesLeft, def.MoveCount, "moves " + Path.GetFileName(f));
            int cells = engine.Board.Items.Sum(it => it.CellWidth * it.CellHeight);
            T.Eq(cells, def.Width * def.Height, "full board " + Path.GetFileName(f));
        }
        Console.WriteLine("level parsing done");
    }

    static void TestBlastBasics()
    {
        // 3x3: bottom row r r g / middle g g b / top b y y ; stone somewhere to keep playing
        var e = Engine(3, 3, 5, "r r g  g g b  s y y");
        // tap single 'b' at (2,1) -> rejected
        var res = e.Tap(new GridPosition(2, 1));
        T.Assert(!res.IsValidMove && res.RejectedItem != null, "single cube rejected");
        T.Eq(e.MovesLeft, 5, "no move spent on reject");
        // tap obstacle -> rejected
        res = e.Tap(new GridPosition(0, 2));
        T.Assert(!res.IsValidMove, "obstacle tap rejected");
        // tap r pair at (0,0)
        res = e.Tap(new GridPosition(0, 0));
        T.Assert(res.IsValidMove, "pair blast valid");
        T.Eq(e.MovesLeft, 4, "move spent");
        var blast = res.Steps.OfType<BlastStep>().First();
        T.Eq(blast.Cubes.Count, 2, "2 cubes blasted");
        T.Assert(blast.CreatedSpecial == null, "no special from pair");
        T.Assert(res.Steps.OfType<GravityStep>().Any(), "gravity step present");
        CheckInvariants(e.Board, "blast basics");
    }

    static void TestSpecialCreation()
    {
        // vertical column of 4 reds in col 0 -> rocket; random=0 => horizontal
        var e = Engine(3, 4, 5, "r g b  r b g  r g b  r b s", new FixedRandom(0, 1, 2, 3, 0, 1, 2, 3, 0, 1, 2, 3));
        var res = e.Tap(new GridPosition(0, 1));
        var blast = res.Steps.OfType<BlastStep>().First();
        T.Assert(blast.CreatedSpecial is RocketItem, "rocket created from 4");
        // The special is created at the tap cell, then gravity may drop it further down.
        T.Assert(blast.TapPosition.Equals(new GridPosition(0, 1)), "special created at tap cell");
        T.Assert(blast.CreatedSpecial.Origin.Equals(new GridPosition(0, 0)), "special fell to bottom");
        T.Assert(e.Board.ItemAt(blast.CreatedSpecial.Origin) == blast.CreatedSpecial, "special on board");

        // 6 cubes -> TNT
        var e2 = Engine(3, 3, 5, "r r g  r r b  r r s");
        var res2 = e2.Tap(new GridPosition(0, 0));
        var blast2 = res2.Steps.OfType<BlastStep>().First();
        T.Eq(blast2.Cubes.Count, 6, "6 cube group");
        T.Assert(blast2.CreatedSpecial is TntItem, "tnt created from 6");

        // hints
        var e3 = Engine(3, 3, 5, "r r g  r r b  r r s");
        var hints = e3.GetSpecialHints();
        var anyRed = e3.Board.Items.OfType<CubeItem>().First(c => c.Color == CubeColor.Red);
        T.Assert(hints[anyRed.Id] == SpecialHint.Tnt, "tnt hint on 6 group");
        Console.WriteLine("blast/special done");
    }

    static void TestGravity()
    {
        // column: r at top, empty middle created by blasting pair; stone barrier
        // 1 wide impossible (blast needs 2). Use 2x4 grid:
        // bottom: r r / s y / b b / g y   -> blast b pair at row2, items above fall, spawns fill
        var e = Engine(2, 4, 5, "r r  s y  b b  g y", new FixedRandom(1, 1, 1, 1, 1, 1, 1, 1));
        var res = e.Tap(new GridPosition(0, 2));
        var grav = res.Steps.OfType<GravityStep>().First();
        // g at (0,3) falls to (0,2)? col0: y0 r, y1 s(stone barrier), y2 blasted, y3 g -> g falls to y2
        T.Assert(grav.Falls.Any(f => f.From.Equals(new GridPosition(0, 3)) && f.To.Equals(new GridPosition(0, 2))), "fall above stone");
        // col0 spawn: 1 cell (y3). col1: y3 y falls? col1: y0 r, y1 y, y2 blasted, y3 y -> falls to y2, spawn 1
        T.Eq(grav.Spawns.Count, 2, "two spawns");
        T.Assert(grav.Spawns.All(sp => sp.SpawnRow >= 4), "spawn row above board");
        CheckInvariants(e.Board, "gravity");

        // cells below stone are not refilled: blast pair under a stone
        // 1x? need width>=2: grid 2x3: r r / s s / y y ; blast r pair -> cells (0,0),(1,0) empty, stones block, no spawns below
        var e2 = Engine(2, 3, 5, "r r  s s  y y");
        var res2 = e2.Tap(new GridPosition(0, 0));
        var grav2 = res2.Steps.OfType<GravityStep>().First();
        T.Eq(grav2.Spawns.Count, 0, "no spawns below stone");
        T.Assert(e2.Board.IsEmpty(new GridPosition(0, 0)), "cell under stone stays empty");
        Console.WriteLine("gravity done");
    }

    static void TestRocket()
    {
        // 4x3, hro at (1,1); row y1: s hro v r ; obstacles on row
        var e = Engine(4, 3, 5, "r g b y  s hro v r  g b y r");
        var res = e.Tap(new GridPosition(1, 1));
        T.Assert(res.IsValidMove, "rocket tap valid");
        var ex = res.Steps.OfType<ExplosionStep>().Single();
        T.Assert(ex.Kind == ExplosionKind.Rocket && ex.Orientation == RocketOrientation.Horizontal, "h rocket");
        // stone at (0,1) destroyed, vase at (2,1) damaged 1, cube at (3,1) destroyed
        T.Assert(ex.Damages.Any(d => d.Obstacle is StoneItem && d.Destroyed), "rocket kills stone");
        T.Assert(ex.Damages.Any(d => d.Obstacle is VaseItem && !d.Destroyed && d.Damage == 1), "rocket damages vase once");
        T.Eq(ex.DestroyedCubes.Count, 1, "row cube destroyed");
        T.Assert(e.Board.ItemAt(new GridPosition(0, 0)) is CubeItem, "other rows untouched");

        // chain: two rockets in same row, not adjacent (gap), tap one -> chains the other
        var e2 = Engine(5, 2, 5, "hro r g vro s  b b b b b");
        var res2 = e2.Tap(new GridPosition(0, 0));
        var exs = res2.Steps.OfType<ExplosionStep>().ToList();
        T.Eq(exs.Count, 2, "chained explosion count");
        T.Assert(exs[1].Wave > exs[0].Wave, "chain wave order");
        T.Assert(exs[0].TriggeredSpecials.Count == 1, "rocket triggered vro");
        T.Assert(exs[0].Source is RocketItem, "explosion source recorded");
        T.Assert(exs[1].Source is RocketItem, "chained explosion source recorded");
        // vro clears column 3 -> b at (3,1) destroyed; stone at (4,0) destroyed by first rocket
        T.Assert(exs.Any(x2 => x2.Damages.Any(d => d.Obstacle is StoneItem && d.Destroyed)), "stone killed in chain");
        Console.WriteLine("rocket done");
    }

    static void TestTnt()
    {
        // 7x7 grid all blue with tnt in center; stones at corners (out of 5x5) survive
        var tokens = new List<string>();
        for (int y = 0; y < 7; y++)
            for (int x = 0; x < 7; x++)
                tokens.Add((x == 3 && y == 3) ? "t" : ((x == 0 || x == 6) && (y == 0 || y == 6)) ? "s" : "b");
        var e = Engine(7, 7, 5, string.Join(" ", tokens));
        var res = e.Tap(new GridPosition(3, 3));
        var ex = res.Steps.OfType<ExplosionStep>().Single();
        T.Eq(ex.AffectedCells.Count, 25, "tnt 5x5");
        T.Eq(ex.DestroyedCubes.Count, 24, "tnt kills 24 cubes (center is tnt cell)");
        T.Eq(e.Board.Items.OfType<StoneItem>().Count(), 4, "corner stones survive tnt");
        Console.WriteLine("tnt done");
    }

    static void TestCombos()
    {
        // Rocket-Rocket: two adjacent rockets; + shape means full row + full column, one move
        var e = Engine(5, 5, 5, MakeTokens(5, 5, new Dictionary<string, string> { { "2,2", "hro" }, { "2,3", "vro" }, { "0,0", "s" }, { "4,4", "s" }, { "0,4", "s" }, { "0,2", "s" } }));
        var res = e.Tap(new GridPosition(2, 2));
        var gather = res.Steps.OfType<ComboGatherStep>().Single();
        T.Assert(gather.Kind == ComboKind.RocketRocket, "RR combo kind");
        T.Eq(gather.Items.Count, 2, "RR gathers 2");
        var exs = res.Steps.OfType<ExplosionStep>().ToList();
        T.Eq(exs.Count, 2, "RR = 2 rockets");
        T.Assert(exs.All(x => x.Center.Equals(new GridPosition(2, 2))), "RR centered at tap");
        T.Assert(exs.Any(x => x.Orientation == RocketOrientation.Horizontal) && exs.Any(x => x.Orientation == RocketOrientation.Vertical), "perpendicular");
        // stone at (0,2) in row -> destroyed; stone (0,0) not
        T.Assert(!e.Board.Items.OfType<StoneItem>().Any(s => s.Origin.Equals(new GridPosition(0, 2))), "stone in + destroyed");
        T.Assert(e.Board.Items.OfType<StoneItem>().Any(s => s.Origin.Equals(new GridPosition(0, 0))), "stone off + survives");
        T.Eq(e.MovesLeft, 4, "combo costs one move");

        // TNT-TNT: 7x7 (with rocket adjacent too, still TNT-TNT because >=2 TNTs)
        var e2 = Engine(9, 9, 5, MakeTokens(9, 9, new Dictionary<string, string> { { "4,4", "t" }, { "4,5", "t" }, { "5,4", "hro" }, { "0,0", "s" }, { "1,4", "s" } }));
        var res2 = e2.Tap(new GridPosition(4, 4));
        var gather2 = res2.Steps.OfType<ComboGatherStep>().Single();
        T.Assert(gather2.Kind == ComboKind.TntTnt, "TNT-TNT kind (2 tnt + rocket)");
        var ex2 = res2.Steps.OfType<ExplosionStep>().Single();
        T.Eq(ex2.AffectedCells.Count, 49, "7x7 combo");
        T.Assert(ex2.Damages.Any(d => d.Obstacle is StoneItem && d.Destroyed), "stone at dist3 destroyed");
        T.Assert(e2.Board.Items.OfType<StoneItem>().Any(), "far stone survives");

        // TNT-Rocket: 3 rows + 3 cols; check cell coverage
        var e3 = Engine(9, 9, 5, MakeTokens(9, 9, new Dictionary<string, string> { { "4,4", "t" }, { "5,4", "vro" }, { "0,0", "s" }, { "0,3", "s" }, { "3,0", "s" }, { "0,8", "s" } }));
        var res3 = e3.Tap(new GridPosition(4, 4));
        var gather3 = res3.Steps.OfType<ComboGatherStep>().Single();
        T.Assert(gather3.Kind == ComboKind.TntRocket, "TNT-Rocket kind");
        var exs3 = res3.Steps.OfType<ExplosionStep>().ToList();
        T.Eq(exs3.Count, 6, "6 rockets");
        // stone (0,0) not on rows/cols 3..5 -> survives; (0,3) on row 3 -> dies; (3,0) col3 -> dies; (0,8) survives
        var stones = e3.Board.Items.OfType<StoneItem>().Select(s => s.Origin).ToList();
        T.Assert(stones.Contains(new GridPosition(0, 0)) && stones.Contains(new GridPosition(0, 8)) && stones.Count == 2, "TNT-R coverage");
        // TNT-Rocket in the corner: off-board rockets are skipped
        var e4 = Engine(9, 9, 5, MakeTokens(9, 9, new Dictionary<string, string> { { "0,0", "t" }, { "1,0", "hro" }, { "8,8", "s" } }));
        var res4 = e4.Tap(new GridPosition(0, 0));
        var exs4 = res4.Steps.OfType<ExplosionStep>().ToList();
        T.Eq(exs4.Count, 4, "corner TNT-R: 2 rows + 2 cols");
        T.Assert(exs4.All(x => x.AffectedCells.Count > 0), "no empty explosions");
        Console.WriteLine("combos done");
    }

    static string MakeTokens(int w, int h, Dictionary<string, string> special)
    {
        var t = new List<string>();
        string[] colors = { "r", "g", "b", "y" };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                string key = x + "," + y;
                // checkerboard of pairs to avoid accidental groups: use color by (x/1 + y*2) pattern isolated
                t.Add(special.ContainsKey(key) ? special[key] : colors[(x + 2 * y) % 4]);
            }
        return string.Join(" ", t);
    }

    static void TestVase()
    {
        // vase adjacent to blast of 3 cubes takes only 1 damage; second blast kills it
        // 3x2: bottom r r r / top v g b -> blast 3 r's, vase at (0,1) adjacent to 1 cube... make vase adjacent to 2+ cubes:
        // grid 3x2: r r r / v g y : vase adjacent to r(0,0) only. Instead: 3x2 bottom v r r, top r r g? vase(0,0), r group of 4: (1,0),(2,0),(0,1),(1,1)? (0,1) adjacent vase, (1,0) adjacent vase => 2 adjacent cubes, but creates rocket (4 group, FixedRandom 0 -> horizontal at tap)
        var e = Engine(3, 2, 5, "v r r  r r g", new FixedRandom(0, 0, 0, 0, 0, 0));
        var res = e.Tap(new GridPosition(1, 0));
        var blast = res.Steps.OfType<BlastStep>().First();
        var dmg = blast.Damages.Single(d => d.Obstacle is VaseItem);
        T.Eq(dmg.Damage, 1, "vase max 1 dmg per blast");
        T.Assert(!dmg.Destroyed, "vase survives first blast");
        // vase falls: it is at (0,0) bottom already; check vase still on board
        T.Assert(e.Board.Items.OfType<VaseItem>().Any(), "vase alive");

        // vase falling test: vase above blasted pair
        var e2 = Engine(2, 3, 5, "r r  v y  g b");
        var res2 = e2.Tap(new GridPosition(0, 0));
        var grav2 = res2.Steps.OfType<GravityStep>().First();
        T.Assert(grav2.Falls.Any(f => f.Item is VaseItem && f.To.Equals(new GridPosition(0, 0))), "vase falls");
        Console.WriteLine("vase done");
    }

    static void TestStone()
    {
        // blast next to stone does nothing
        var e = Engine(2, 2, 5, "r r  s y");
        var res = e.Tap(new GridPosition(0, 0));
        var blast = res.Steps.OfType<BlastStep>().First();
        T.Eq(blast.Damages.Count, 0, "stone immune to blast");
        T.Assert(e.Board.Items.OfType<StoneItem>().Any(), "stone alive after blast");
        Console.WriteLine("stone done");
    }

    static void TestChaliceBox()
    {
        // 4x4 with box at (1,1)-(2,2); bottom row cubes for blasting
        // tokens: y0: r r r r ; y1: g cbBL cbBR g ; y2: y cbTL cbTR y ; y3: b b b b
        var e = Engine(4, 4, 20, "r r r r  g cbBL cbBR g  y cbTL cbTR y  b b b b");
        var box = e.Board.Items.OfType<ChaliceBoxItem>().Single();
        T.Assert(!box.IsDoorBroken, "door intact");
        
        int hits = 0;
        // blast bottom 4 r's: 2 cubes adjacent to box -> door takes exactly 1
        var res = e.Tap(new GridPosition(0, 0)); // creates rocket too (4 group) but that's fine
        var d1 = res.Steps.OfType<BlastStep>().First().Damages.Single(d => d.Obstacle == box);
        T.Eq(d1.Damage, 1, "door: 1 dmg per source");
        hits++;

        if (hits < GameRules.ChaliceBoxDoorHealth)
        {
            T.Assert(!box.IsDoorBroken, "door needs more hits");
            // blast top 4 b's: second hit
            var resB = e.Tap(new GridPosition(0, 3));
            hits++;
        }

        while (hits < GameRules.ChaliceBoxDoorHealth)
        {
            T.Assert(!box.IsDoorBroken, "door needs more hits");
            box.ApplyBlastDamage(2);
            hits++;
        }

        T.Assert(box.IsDoorBroken, "door broken after required sources");
        T.Eq(box.ChalicesRemaining, GameRules.ChalicesPerBox, "chalices to collect");
        
        // chalice phase: explosion affecting 2 cells (rocket) -> 2 chalices
        var r4 = box.ApplyExplosionDamage(2);
        T.Eq(r4.ChalicesCollected, 2, "rocket collects 2 chalices");
        
        // adjacent blast with remaining neighbour cubes -> collects remaining, destroyed
        int remaining = GameRules.ChalicesPerBox - 2;
        var r5 = box.ApplyBlastDamage(remaining);
        T.Eq(r5.ChalicesCollected, remaining, "blast collects remaining");
        T.Assert(r5.Destroyed && box.IsDestroyed, "box cleared after chalices collected");
        T.Eq(box.RemainingGoalAmount, 0, "goal amount zero");

        // full flow with rocket through box in chalice phase via engine
        var e2 = Engine(4, 4, 20, "hro r r r  g cbBL cbBR g  y cbTL cbTR y  b b b b");
        var box2 = e2.Board.Items.OfType<ChaliceBoxItem>().Single();
        for (int i = 0; i < GameRules.ChaliceBoxDoorHealth; i++) box2.ApplyBlastDamage(1); // break door
        T.Assert(box2.IsDoorBroken, "door broken setup");
        var res2 = e2.Tap(new GridPosition(0, 0)); // hro passes y0 only -> misses box
        
        // vertical rocket up column 1 would cross 2 box cells; craft: vro at (1,0)
        var e3 = Engine(4, 4, 20, "r vro r r  g cbBL cbBR g  y cbTL cbTR y  b b b b");
        var box3 = e3.Board.Items.OfType<ChaliceBoxItem>().Single();
        for (int i = 0; i < GameRules.ChaliceBoxDoorHealth; i++) box3.ApplyBlastDamage(1);
        var res3 = e3.Tap(new GridPosition(1, 0));
        var ex3 = res3.Steps.OfType<ExplosionStep>().Single();
        var dm3 = ex3.Damages.Single(d => d.Obstacle == box3);
        T.Eq(dm3.ChalicesCollected, 2, "vertical rocket collects 2 chalices via engine");

        // goals list
        var goals = e3.GetGoals();
        T.Assert(goals.Any(g => g.Key == ChaliceBoxItem.Key && g.Remaining == GameRules.ChalicesPerBox - 2), "goal remaining correct");
        Console.WriteLine("chalice box done");
    }

    static void TestWinLose()
    {
        // one vase hp2, enough moves: blast twice adjacent -> win
        var e = Engine(2, 2, 5, "r r  v y", new FixedRandom(0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        var res = e.Tap(new GridPosition(0, 0));
        T.Assert(res.State == LevelState.Playing, "still playing");
        // vase now hp1. keep blasting adjacent pairs until vase dies or moves out
        int guard = 0;
        while (e.State == LevelState.Playing && guard++ < 50)
        {
            bool tapped = false;
            for (int x = 0; x < 2 && !tapped; x++)
                for (int y = 0; y < 2 && !tapped; y++)
                {
                    var g = GroupFinder.FindCubeGroup(e.Board, new GridPosition(x, y));
                    if (g.Count >= 2 && g.Any(c => c.Origin.Neighbours().Any(n => e.Board.ItemAt(n) is VaseItem)))
                    { e.Tap(new GridPosition(x, y)); tapped = true; }
                }
            if (!tapped) break;
        }
        T.Assert(e.State == LevelState.Won || e.State == LevelState.Failed, "level ended");

        // lose: stone can't be cleared without specials; 1 move
        var e2 = Engine(2, 2, 1, "r r  s y");
        var res2 = e2.Tap(new GridPosition(0, 0));
        T.Assert(res2.State == LevelState.Failed, "failed when moves exhausted");

        // win priority on last move
        var e3 = Engine(2, 2, 1, "r r  v y");
        var box = e3.Board.Items.OfType<VaseItem>().Single();
        box.ApplyExplosionDamage(1); // hp1 now
        var res3 = e3.Tap(new GridPosition(0, 0));
        T.Assert(res3.State == LevelState.Won, "win priority on last move");
        Console.WriteLine("win/lose done");
    }

    static void TestDeadlockShuffle()
    {
        // 1x4 column r g r g -> no valid move at start, constructor shuffles
        var e = Engine(1, 5, 5, "r g r g s");
        T.Assert(e.HasAnyValidMove(), "initial deadlock resolved");
        CheckInvariants(e.Board, "shuffle");

        // unsolvable: 2 cubes different colors -> no shuffle possible, engine still constructs
        var e2 = Engine(1, 3, 5, "r g s");
        T.Assert(!e2.HasAnyValidMove(), "truly unsolvable stays");
        Console.WriteLine("deadlock done");
    }

    static void TestRandomPlaythroughs()
    {
        var dir = Environment.GetEnvironmentVariable("LEVELS_DIR");
        var rng = new Random(123);
        int wins = 0, fails = 0;
        foreach (var f in Directory.GetFiles(dir, "level_*.json").OrderBy(x => x))
        {
            for (int run = 0; run < 30; run++)
            {
                var def = LoadLevel(f);
                var engine = new BoardEngine(def, new SystemRandomProvider(rng.Next()));
                int guard = 0;
                while (engine.State == LevelState.Playing && guard++ < 500)
                {
                    var pos = new GridPosition(rng.Next(def.Width), rng.Next(def.Height));
                    var res = engine.Tap(pos);
                    if (res.IsValidMove)
                    {
                        CheckSteps(res, engine);
                    }
                }
                CheckInvariants(engine.Board, "playthrough " + Path.GetFileName(f));
                if (engine.State == LevelState.Won) wins++;
                else if (engine.State == LevelState.Failed) fails++;
                else if (engine.HasAnyValidMove()) T.Assert(false, "game did not end but has moves: " + f);
            }
        }
        Console.WriteLine("random playthroughs done (wins=" + wins + " fails=" + fails + ")");
        T.Assert(wins + fails >= 295, "almost all games ended");
        T.Assert(wins > 0, "some games won");
    }

    static void CheckSteps(TurnResult res, BoardEngine engine)
    {
        foreach (var step in res.Steps)
        {
            var ex = step as ExplosionStep;
            if (ex != null)
            {
                T.Assert(ex.AffectedCells.Count > 0, "explosion has cells");
            }
            var grav = step as GravityStep;
            if (grav != null)
            {
                foreach (var sp in grav.Spawns)
                    T.Assert(sp.SpawnRow >= engine.Board.Height, "spawn row sane");
            }
        }
    }
}
