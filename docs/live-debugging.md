# Debugging the mod against a running game

The game exposes a localhost HTTP bridge (UltimateGlorpExplorer) that runs
arbitrary C# inside the live process and returns screenshots. For UI work this
beats the test suite: you can measure and adjust a transform, look at the
result, and iterate without a rebuild.

For getting a freshly launched game into free play in the first place, see
`glorpy_knowledge/driving-a-live-uch-from-the-main-menu.md` in the UCH
development directory.

## tools/draw-bounds.ps1

Overlays two boxes on every item of the inventory book's open page or the Block
Probability grid:

- **green** — the bounds of what is actually drawn
- **red** — the bounds of what can actually be clicked

```powershell
tools\draw-bounds.ps1 -Target book   -Shot shot.png
tools\draw-bounds.ps1 -Target tablet -Shot shot.png
tools\draw-bounds.ps1 -Clear
```

It also prints a table of art size, hitbox size and the offset between their
centres, which is usually the faster read:

```
  Acid_Pick              art 0.61x0.45  hit 0.75x0.75  offset 0.36,0.77
  RCReceiver_Pick        art 0.54x0.55  hit 0.75x0.75  offset 0.24,0.57
```

Nothing is installed and nothing persists — the overlay is throwaway
`LineRenderer` objects named `DBG_*`, cleared by the next run or by `-Clear`.

### Why it exists

"The icon looks right" and "you can click the icon" are different claims, and
only the second matters to a player. Every custom block's tablet artwork was
wrong for two sessions while every structural golden stayed green; the same
class of defect hides in hitboxes, where there is nothing to look at at all.
The overlay makes both visible at once.

## Two traps worth knowing before writing your own probe

**Draw on layer 5 (UI), or you draw nothing.** The book and tablet render
through `InventoryBook.UiCamera`, whose culling mask excludes the default
layer. A `LineRenderer` on layer 0 is created successfully, reports no error,
and is simply never rasterised. This looks exactly like "my code didn't run".

**A renderer can be enabled, hold a sprite, contribute to bounds, and draw
nothing.** The glue rig's `StickingBlock` and `RotatingBlock` sit at colour
alpha 0. Counting them inflated Acid's measured height from 0.45 to 2.91, which
in turn made a layout pass reserve three times the space it needed. Filter on
`sr.color.a` as well as `sr.enabled` and `sr.sprite != null`.

## Measuring, not eyeballing

Read a number back before believing a change landed. Two specific reasons in
this codebase:

- `TabletBlock.Update` only recomputes `spriteHolder.localScale` while
  `scaleAlpha` is changing, i.e. during a hover transition. Setting
  `BlockProbabilityScale` and screenshotting immediately shows the *old* size,
  and the new one appears minutes later when something happens to hover the
  tile. A screenshot taken in between is actively misleading.
- `Renderer.bounds` can still describe the transform as it was before the line
  that just moved it. Where that matters, compute from `sprite.bounds` through
  `localToWorldMatrix` instead.

## Do not let hand-testing reach the goldens

`--isolate` gives each fleet instance a private plugins directory but
**junctions the game data**. Block frequencies changed by clicking around in
the normal Steam install are the same frequencies the shadow install reads, and
they persist. Goldens must therefore record only state the mod controls — see
the golden rules in `AGENTS.md`.
