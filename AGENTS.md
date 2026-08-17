# Ultimate Chicken Horse Mod Agent Guide

## Project context

- Projects covered by this guide are mods for *Ultimate Chicken Horse* version
  **1.13**.
- Mods are **BepInEx 5** plugins written in C# for **.NET Framework 4.8**.
- A mod may use **Harmony** for runtime patches, but do not introduce Harmony when
  the requested functionality can use the game's public lifecycle directly.
- Decompiled *Ultimate Chicken Horse* 1.13 source may be available locally as a
  reference; its location is machine-specific (see "Local paths" below).
- Use the decompiled source as the primary reference when investigating game types,
  methods, fields, behavior, initialization order, networking, and suitable patch
  points. Treat it as read-only reference material unless the user explicitly asks
  to edit it.

## Local paths (machine-specific, not in git)

- Machine-specific paths live in `EvenMorePlayers.user.props`, which the csproj
  imports if present and which is gitignored (`*.user.props`). Read it to find:
  - `UCHfolder` — the game installation directory (falls back to the default
    Steam path if the file is missing).
  - `DecompFolder` — the decompiled UCH 1.13 source tree, if available.
- Use **only** `DecompFolder` as the decompiled-source reference. Its parent
  directory may contain decomp trees of other game versions — do not read or
  cite those. If a file or subtree you need (e.g. the game code in
  `Assembly-CSharp\` or the UNet HLAPI in `com.unity.multiplayer-hlapi.Runtime\`)
  is not present under `DecompFolder`, stop and tell the user the decompiled
  source is missing instead of substituting another version's tree.
- The game log is written to `$(UCHfolder)\output_log.txt`.
- Game and BepInEx assembly references resolve through `UCHfolder` in the project
  file; the build also copies the plugin DLL into `$(UCHfolder)\BepInEx\plugins\`.
- Inspect the current project rather than assuming its assembly name, plugin ID,
  output directory, entry point, dependencies, or deployment behavior.

## Access to private members (Krafs.Publicizer)

- The project uses the **Krafs.Publicizer** NuGet package (see `EvenMorePlayers.csproj`)
  to make all members of `Assembly-CSharp` and `InControl` public at compile time:
  ```xml
  <PackageReference Include="Krafs.Publicizer" Version="1.0.1" />
  <Publicize Include="Assembly-CSharp" />
  <Publicize Include="InControl" />
  ```
- This means private/internal/protected game fields, methods, and types can be
  accessed **directly in C#** — no reflection, `AccessTools`, `Traverse`, or
  Harmony `AccessTools.Field` helpers are needed for these assemblies.
- At runtime the game assemblies are unchanged; the publicized references are
  compile-time only, and the IL access works because the .NET runtime does not
  re-verify accessibility here.

## Working conventions

- Compare target method signatures and control flow against the decompiled 1.13
  source before changing Harmony patches or relying on game internals.
- Preserve compatibility with the BepInEx, Harmony, Unity, networking, and game
  assemblies referenced by the current project.
- Prefer focused patches and normal game lifecycle calls over copying substantial
  decompiled game logic.
- Keep mods standalone. Do not add source, project, build, configuration, or runtime
  dependencies between sibling mods unless the user explicitly requests them.
- Preserve unrelated user changes and untracked files in every repository.
- Do not commit decompiled proprietary game source or game assemblies.

## Searching decompiled UCH code (use the MCP index, not grep)

- The 1.13 decompilation at
  `C:\Users\mjb\develop\UCH-dev\decompiled_UCH\UCH-decomp_1.13` is indexed by
  the global codebase-memory-mcp server as project
  `C-Users-mjb-develop-UCH-dev-decompiled_UCH-UCH-decomp_1.13` (~46k nodes).
  Plain grep/rg over that tree takes minutes or times out — use the MCP tools:
  - `search_code` (grep + graph enrichment) for text patterns. Prefer plain
    substring patterns; regex using `\w` classes has returned 0 hits where a
    substring matches, so verify a 0-hit regex with a substring probe before
    trusting it.
  - `search_graph` to find functions/classes/methods by name or
    natural-language query; `get_code_snippet` with the returned
    qualified_name to read a definition.
- Falling back to text tools is fine for a *single* known file (e.g. Read on
  `Assembly-CSharp\<Type>.cs`) — just never a recursive grep over the whole
  tree.

## Shared Glorpy knowledge

- Reusable Ultimate Chicken Horse investigation and troubleshooting knowledge is
  stored in the sibling `glorpy_knowledge` folder under the UCH development
  directory.
- Check `glorpy_knowledge` for relevant notes before diagnosing a known symptom
  or repeating an investigation.
- Add durable findings there when an investigation produces reusable knowledge,
  including symptoms, evidence, diagnosis steps, caveats, and verified fixes.
- Keep `AGENTS.md` focused on project-wide workflow. Put issue-specific details
  in `glorpy_knowledge` rather than embedding them here.



# UltimateGlorpExplorer UCH live game bridge

A running Ultimate Chicken Horse instance exposes a localhost HTTP API (UltimateGlorpExplorer) at
`http://127.0.0.1:7311`. Use this bridge to inspect the live game instead of
guessing about runtime state.

## Start with the live documentation

Fetch `GET /docs` before using the bridge, because it is the authoritative API
reference for the running build:

```powershell
curl.exe -sS http://127.0.0.1:7311/docs
```

All responses are JSON and contain an `ok` boolean. Failed requests include an
`error` string. Calls execute on Unity's main thread and can time out when the
game is paused or frozen.

## Read-first workflow

Prefer read-only probes before executing mutations:

1. Confirm liveness with `GET /`.
2. Orient with a shallow scene query such as `GET /scene?depth=1&max=500`.
3. Inspect interesting objects with `GET /inspect?path=...` or inspect a game
   type's signature with `GET /inspect?type=...`.
4. Use `POST /execute` for targeted C# probes or explicitly requested changes.
5. Read runtime output with `GET /logs?since=N`, carrying the returned `total`
   forward as the next cursor.

Example scene and object queries:

```powershell
curl.exe -sS "http://127.0.0.1:7311/scene?depth=2&max=1000"
curl.exe -sS "http://127.0.0.1:7311/inspect?path=MainMenuControl%2FStart%20Shadow"
curl.exe -sS "http://127.0.0.1:7311/inspect?type=TabletButton"
curl.exe -sS "http://127.0.0.1:7311/logs?since=0"
```

Scene results include both loaded scenes and `DontDestroyOnLoad`. Start with a
small depth, check `childCount` and `truncated`, and drill into relevant paths
rather than requesting an unnecessarily large hierarchy.

## Execute C# in the live game

`POST /execute` accepts raw C#, not JSON. It has REPL semantics: a trailing
expression without a semicolon is returned as `result`, and state persists
between calls until the console is reset. Common namespaces and game assemblies
are already referenced.

On PowerShell, `Invoke-RestMethod` avoids native-command quoting problems:

```powershell
Invoke-RestMethod -Method Post `
  -Uri 'http://127.0.0.1:7311/execute' `
  -ContentType 'text/plain' `
  -Body 'UnityEngine.Time.timeScale'
```

For multi-line C#, write a temporary `.cs` payload and submit it as the raw
request body. Keep mutations narrowly scoped, identify the target from current
live state, and verify the resulting state with a follow-up read.

Useful probing pattern:

```csharp
string.Join(
    Environment.NewLine,
    Resources.FindObjectsOfTypeAll<TabletButton>()
        .Where(button => button.gameObject.activeInHierarchy)
        .Select(button => button.name)
        .ToArray())
```


<!-- ===== Everything above this marker is a verbatim copy of
     UCH-dev/AGENTS.md. Re-sync by replacing it wholesale.
     Everything below is specific to UCH-CustomBlocks. ===== -->

# UCH-CustomBlocks specifics

## Local paths in this repo

- This project has **no** `*.user.props`. `CustomBlocks.csproj` hardcodes
  `UCHfolder` in its first `PropertyGroup` — edit it there, not in a props file.
- The decompiled 1.13 reference tree is the sibling
  `decompiled_UCH\UCH-decomp_1.13\` under the UCH development directory. Prefer
  it over ad-hoc decompilation of `Assembly-CSharp.dll`.

## Build side effects (read before running a build)

`CustomBlocks.csproj` build events are not side-effect free:

- **PreBuild** runs `taskkill /f UltimateChickenHorse.exe`, then flattens every
  `Blocks\**\*.png` and `*.wav` into `assets\` with `copy /y`. It never cleans,
  so `assets\` accumulates files from deleted blocks and filenames that collide
  case-insensitively silently overwrite each other.
- **PostBuild** copies the DLL and `assets\*` into
  `$(UCHfolder)\BepInEx\plugins\CustomBlocks\` — overwriting the live install —
  and then runs `start explorer.exe "steam://rungameid/386940"`, which
  **launches the game on the visible desktop**.

A plain `dotnet build` therefore steals focus. Load the `hidden-desktop` skill
first, or suppress the events (`-p:PostBuildEvent= -p:PreBuildEvent=`) and
deploy by hand.

## Tests

Fleet scenarios live in `tests\` and are run by the sibling
`UCH-HarmonicSheepFleet` harness via `tests\fleet.cmd`; see `tests\README.md`
for the profile and plugin-selection flags.

### Runs are hidden — do not make them visible to "see what happens"

Fleet runs launch the instances on a private desktop by default. `--visible` is
for bringing up a new scenario, and is a debugging aid, not a normal run.

A visible run is not merely rude, it corrupts results: the game window takes
focus and the user's mouse then drags the build cursor, so any scenario that
positions the cursor and drops a block fails as `cannot-place`, unreproducibly.
`cross-peer` went 4/10 visible and 10/10 hidden with nothing else changed (and
41s rather than 106s, for not fighting over focus). Two of those failures were
first misread as a regression in the mod.

Hiding the *harness* does nothing — `uch-mcp-proxy` spawns the game, and a
child's desktop is set by its parent, so the flag has to reach the proxy
(`launch_game`'s `hidden`, default `UCH_LAUNCH_HIDDEN`). Screenshots still work
hidden, because the bridge captures after rendering rather than grabbing the
screen.

### Screenshots are not evidence unless something asserts the view

Run before committing test changes:

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File tests/check-screenshot-guards.ps1
```

This exists because a fully green suite once hid two shipped defects for two
sessions. The rules, and why:

- **Never assert that a screenshot file exists.** `Check("shot", saved is not
  null)` proves the renderer produced bytes. It passes identically when the
  camera is pointed at the wrong page. The guard fails the build on it.
- **Assert the identity of the view, not just that it loaded.** "the block list
  is visible" and "the block list is settled on page 5 of 5" are different
  claims; only the second makes the picture mean anything. Use
  `FleetCB.BookSettledOn(i)`, `BookShownPage()`, `BlockPageSettled(page)`.
- **Never sleep through an animation you can wait on.** `InventoryBook.GotoPage`
  runs a coroutine that steps one page per 0.1s and arrives only at the end;
  the tablet grid lerps its strip over several frames. A fixed `Task.Delay`
  next to either is a race. The previous book loop asked for pages 0-4 and
  photographed 1,2,3,4,3 — nobody noticed for two sessions because the only
  assertion was file-existence.
- **A structural golden of fields the mod itself assigns proves nothing about
  the game accepting them.** `TabletJson()` records `pickableBlockPrefab`, which
  the mod sets directly, and stayed green while every custom tile rendered the
  base block's artwork. `TabletVisualJson()` records what is actually under
  `spriteHolder`. When adding a golden, ask which side of the boundary it sits
  on.
- **Only golden state the mod controls.** `TabletVisualJson` briefly recorded
  each tile's `currentProbStep` — the block's frequency, which any player can
  change from that very screen and which persists in game settings. The suite
  then went red because somebody had clicked two blocks down to 0%. Game data
  is junctioned into the fleet's shadow install, so hand-testing in the normal
  game reaches the goldens. Ask of every recorded field: could a player change
  this without touching the mod?
- **Farm is not the only level.** The whole suite ran `StartGame("Farm", ...)`,
  and Farm is precisely the level where the book's page insertion cannot go
  wrong — `BlankLevelOnly` customization pages only exist on `BLANKLEVEL`. See
  `blank-level-book.fleet.csx`. Guard blank-level scenarios with
  `Fleet.PortalsJson()`/`Abort`: the treehouse rotates its portals.

### Live debugging

`tools/draw-bounds.ps1` overlays artwork bounds and clickable bounds on the
book or tablet against a running game — see `docs/live-debugging.md`, which
also records two traps (draw on layer 5 or the UI camera never rasterises it;
alpha-0 renderers still contribute to bounds).

### Re-recording baselines

`--update-baselines` rewrites goldens to match current behaviour, so it can
turn a regression green. Review `git diff tests/baselines` before committing and
say in the commit message which goldens moved and why.
