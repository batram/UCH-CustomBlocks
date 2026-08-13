# CustomBlocks tests

Fleet scenarios for this mod, run by the sibling
[UCH-HarmonicSheepFleet](../../UCH-HarmonicSheepFleet) harness. The harness
finds this repo's `tests/` automatically when invoked from anywhere inside the
repo (its own prelude and shared suites stay with the harness checkout).

## Layout

- `suites/customblocks/*.fleet.csx` — the scenarios
- `prelude/customblocks.cs` — game-side façade (`FleetCB`), injected after the
  harness prelude; conservative C# 5, like the prelude
- `baselines/<profile>/` — committed goldens, one profile per plugin selection
- `artifacts/` — per-run evidence (screenshots), not committed

## Running

```bash
tests\fleet.cmd check
```

```bash
tests\fleet.cmd run customblocks --profile customblocks --minimal-plugins --with-plugins CustomBlocks,PigFarmButton --isolate --game-dir "S:\SteamLibrary\steamapps\common\Ultimate Chicken Horse"
```

The shared harness suites also run from here — with a profile so their goldens
don't collide with the harness repo's vanilla ones:

```bash
tests\fleet.cmd run arena/inventory.fleet.csx --profile customblocks --minimal-plugins --with-plugins CustomBlocks,PigFarmButton --isolate --game-dir "S:\SteamLibrary\steamapps\common\Ultimate Chicken Horse"
```

First run records baselines; review the diff before committing them. Screenshots
land in `tests/artifacts/customblocks/` for eyeballing — the goldens compare
structure (page contents, ids), never pixels.

If another fleet is running (e.g. HSF's own suites), pick free ports:
`--proxy-port 7490 --host-port 17878`.
