# Known open work

Planned changes and known defects, with enough context to pick one up cold.
Behavioural bugs that a scenario already pins belong in `tests/`, not here —
this is for work that is not yet expressed as a failing check.

`CustomBlock-notes.txt` at the repo root holds three older one-line items
("all blocks disabled in creative", "networking", "load and store"). They
predate this file and have not been triaged into it.

---

## Custom blocks on their own page(s) in Block Probability

**Wanted:** the mod's blocks should not share a page with vanilla ones. They
should start a fresh page after the last vanilla block, and the screen's title
should change while one of those pages is showing, so it reads as a distinct
section rather than an overflow.

**Where it stands today.** The tablet holds 69 tiles at 16 per page (4 columns
x `gridHeight` 4). Custom blocks occupy indices 61-68, so they straddle the
page-4/page-5 boundary: three on page 4, five on page 5. Nothing is wrong with
them individually since `a3a0d31` — they render their own artwork at the right
size — they are just mixed in with vanilla blocks.

**The decision to make.** Two ways to force the page break:

1. *Pad the list* with spacer tiles so the first custom block lands on a page
   boundary. Simple, but invents tiles that have to be invisible without
   disturbing layout, and leaves gaps on the preceding vanilla page.
2. *Patch the grid maths* so `GenerateColumnInfo` starts a new column group at
   the custom boundary. No fake tiles, but touches `TabletBlockList`'s
   pagination directly.

**Measure this before choosing.** `TabletBlockList` sizes itself two different
ways depending on a user toggle:

- `NumPages` and `GenerateColumnInfo` use `tabletBlocks.Length` when
  `hidingDisabledBlocks` is false, and `CountDisplayedBlocks()` when it is true.
- `GetNextDisplayedBlock` always skips entries whose `displayedInList` is false.

A spacer tile that is not "displayed" is therefore counted by one path and
skipped by the other. That mismatch is what would produce phantom pages the
moment somebody presses "Hide Disabled", and it is the thing to check with the
toggle both on and off before committing to approach 1.

**Title.** `RefreshPageNumber()` runs on every page change and after the scroll
settles, so it is the natural hook. The label object still needs identifying —
it is somewhere under `TabletRulesScreen.blockSettingsSubdialog`.

**Code:** `Core/Patches/TabletBlockListPatch.cs`.

---

## Book page: clickable areas do not match the artwork

Separate from the layout below, and worse — a misplaced hitbox is invisible
until someone tries to pick the block up. Measured with
`tools\draw-bounds.ps1 -Target book` (green = art, red = collider):

```
  OneRoundWood_Pick      art 0.81x0.81  hit 0.75x0.75  offset -0.01,-0.01
  ReCoin_Pick            art 0.8x0.86   hit 0.75x0.75  offset -0.03,-0.03
  MultiStart_Pick        art 3.4x0.82   hit 3x0.75     offset  0.11, 0.02
  RCReceiver_Pick        art 0.54x0.55  hit 0.75x0.75  offset  0.24, 0.57
  RCTransmitter_Pick     art 1x1.93     hit 0.75x1.87  offset -0.73, 0.14
  FloatyCloud_Pick       art 4.22x1.28  hit 2.75x0.55  offset  0,   -0.05
  PigDirt_Pick           art 0.41x0.41  hit 0.75x0.75  offset -0.24,-0.24
  ChickenRoll_Pick       art 1.2x1.2    hit 1x1        offset  0,   -0.25
  Acid_Pick              art 0.61x0.45  hit 0.75x0.75  offset  0.36, 0.77
```

Acid and RCReceiver are offset by more than their own height, so their hitbox
and their artwork do not overlap at all. PigDirt's offset is over half its size.

Cause: `PickColliders` come from the cloned base block and stay where the base
put them, while the artwork is moved independently — by each block's
`CreatePickableBlock` (Acid sets `BaseSprite.localPosition` to
`(-0.88, -1.33)`, for instance) and by the sprite swap changing the art's size.
Nothing keeps the two in step.

Fix direction: after building a pickable, drive the collider from the measured
visible bounds so the clickable area is the artwork by construction. Worth
doing at the same time as the layout work below, since both need the same
measurement.

## Book page layout: blocks clip off the edge

`CustomBlock.AddToInventoryPage` assigns `transform.parent` directly, which
preserves *world* position, so each block's placement depends on where its
prefab happened to be. The per-block constants compensating for that
(`pb.transform.localPosition -= new Vector3(20.08f, 20.5f, 1)` and friends in
each `CreatePickableBlock`) are hand-fitted to that accident. The visible
result is the chicken and pig hanging off the left edge of the Custom Mod
Blocks page, and the cloud overlapping the page title.

`SetParent(items, false)` plus real local positions is the honest fix. It will
need per-block eyeballing, and it also changes the tablet's starting point:
that same call currently rewrites the shared prefab's `localScale`, which is
why `FitTile` has to normalise before measuring.

**Code:** `Core/CustomBlock.cs`, plus the `CreatePickableBlock` override in each
block under `Blocks/`.

---

## Smaller known issues

- **MultiStart never appears on the Block Probability page.** Its base,
  `StartPlank`, has no tablet entry of its own, so `BaseTabletBlock` returns
  null and the patch shrinks it out of the list. Arguably correct — a spawn
  plank has no frequency to set — but it happens silently. Nine blocks are
  registered and eight are listed.
- **RCTransmitter's tablet fit matches on the wrong axis.** Its art is portrait
  (1.04 x 2.01) while its base, BoxingGlove, is landscape (2.01 x 1.03), so
  "match the base's longest axis" equates our height to their width. It reads
  acceptably, but it is the one place the auto-fit rule is arbitrary rather
  than right. `CustomBlock.TabletOffset`/`TabletScale` can override it.
- **Acid and RCReceiver sit ~24 left and ~33 up of vanilla Glue.** Auto-centring
  works as designed; vanilla Glue is itself deliberately off-centre in its
  tile. If they should line up with it, override `TabletOffset` to
  `Vector2.zero` on both.

---

## Worth deciding: how much should blocks inherit from their base?

Cloning a vanilla block buys a working `Placeable` — UNet identity, save
serialization, `PlaceableMetadataList` registration, party-box weighting, item
filter membership — plus genuine behaviour reuse (Acid wants glue's attach
mechanics, FloatyCloud wants a plank's collider). That part is earning its
keep.

Presentation is where it costs. Every tablet defect fixed in `a3a0d31` came
from inheriting the base's visuals implicitly: base artwork, base
`BlockProbabilityScale`, base crossout sizing, base `ArtSprites` (which can
contain nulls — vanilla Thwomp does), and pickable prefabs arriving in their
`Disable()`d state. Two structural costs beyond that: base identity is
version-fragile (Thwomp shipping in a later release is why `BasedId` literals
broke and resolution moved to names), and you inherit sub-elements you never
asked for (GluePiece, and the save-sweep guards it needed).

Two directions if this is picked up:

1. Separate "based on for behaviour" from "based on for looks". Today
   `BasePlaceableName` means both. `TabletScale`/`TabletOffset` are a first
   step in that direction.
2. Default to a minimal base. Blocks needing no specific mechanics (PigDirt,
   ChickenRoll, OneRoundWood) could clone one known-simple block instead of a
   semantically similar one — one predictable base, no surprise sub-elements.
