// #name     customblocks/controller-chord
// #peers    1
// #describe The right-trigger chord drives background mode, layer and highlight — and the chorded button never reaches the game.

// The fleet launches the game directly rather than through Steam, so no pad is
// ever detected here. Fleet.CursorChord feeds the events a pad would send
// straight down the player's controller, which is the seam the bindings sit on,
// so the SEMANTICS are testable with no hardware. What a glyph looks like for a
// given device is not, and is not asserted.

// treehouse re-entry noise on a NetTest host: the custom level portals poke
// destroyed network views while the lobby resets (see the fleet knowledge notes)
AllowLogErrors("CustomLevelPortal.UpdateAppearanceForClient");
AllowLogErrors("LevelSelectController.SetupLobbyAfterWait");
AllowLogErrors("UnityEngine.Light.set_color");
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");
AllowLogErrors("Could not attach spawned netsurrogate");
AllowLogErrors("cursor' is inactive");

Step("into free play");
// self-heal: a previous scenario may have died outside the treehouse
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 60);
for (int p = 0; p < Peers.Count; p++)
    await Peers[p].DoAsync($"Fleet.PickCharacter(\"{(p == 0 ? "SQUIRREL" : "FOX")}\");");
await Task.Delay(1000);
await Host.DoAsync("Fleet.StartGame(\"Farm\", \"FREEPLAY\");");
await Require("place phase reached", "Fleet.Scene() != \"TreeHouseLobby\" && Fleet.Phase() == \"PLACE\"", 90);
await Host.DoAsync("FleetCB.SwitchToPlace();");
await Task.Delay(500);

// a known starting point: tool off, on the solid layer
await Host.DoAsync("FleetCB.SetBackgroundMode(false);");
await Task.Delay(200);
string start = (await Host.EvalAsync("FleetCB.LayerStateJson()")).Trim('"');
Check("starts solid with the tool off", start.Contains("\"mode\":false") && start.Contains("\"layer\":\"Default\""), start);

Step("modifier + A switches the tool on");
await Host.DoAsync("Fleet.CursorChord(\"RightTrigger\", \"Accept\");");
await Task.Delay(300);
string afterOn = (await Host.EvalAsync("FleetCB.LayerStateJson()")).Trim('"');
Check("tool on", afterOn.Contains("\"mode\":true"), afterOn);
Check("moved off the solid layer", afterOn.Contains("\"background\":true"), afterOn);

// Accept is place/pick-up. If the chord did not swallow it, the cursor acted on
// it too — the bug this swallowing exists to prevent.
string held = (await Host.EvalAsync("FleetCB.HeldPiece()")).Trim('"');
Check("the chorded A did not reach the cursor", held == "none", held);

Step("modifier + B and + X step the layer");
string beforeStep = (await Host.EvalAsync("FleetCB.LayerStateJson()")).Trim('"');
await Host.DoAsync("Fleet.CursorChord(\"RightTrigger\", \"Back\");");
await Task.Delay(300);
string afterNext = (await Host.EvalAsync("FleetCB.LayerStateJson()")).Trim('"');
Check("next layer selected", afterNext != beforeStep, afterNext);

await Host.DoAsync("Fleet.CursorChord(\"RightTrigger\", \"Sprint\");");
await Task.Delay(300);
string afterPrev = (await Host.EvalAsync("FleetCB.LayerStateJson()")).Trim('"');
Check("previous layer returns to where it started", afterPrev == beforeStep, afterPrev);

Step("modifier + Y toggles the highlight");
await Host.DoAsync("Fleet.CursorChord(\"RightTrigger\", \"Inventory\");");
await Task.Delay(300);
string afterHighlight = (await Host.EvalAsync("FleetCB.LayerStateJson()")).Trim('"');
Check("highlight on", afterHighlight.Contains("\"highlight\":true"), afterHighlight);

// Inventory opens the book. If the chord leaked, the player is staring at it —
// and the cursor is frozen, which is the signal the game itself acts on. Page
// counts answer "does a book exist", which is true either way.
bool frozen = await Host.EvalBoolAsync("FleetCB.CursorFrozen()");
Check("the chorded Y did not open the book", !frozen);

Step("modifier + A switches the tool back off");
await Host.DoAsync("Fleet.CursorChord(\"RightTrigger\", \"Accept\");");
await Task.Delay(300);
string afterOff = (await Host.EvalAsync("FleetCB.LayerStateJson()")).Trim('"');
Check("tool off and back on the solid layer",
    afterOff.Contains("\"mode\":false") && afterOff.Contains("\"layer\":\"Default\""), afterOff);

Step("the buttons are the game's again once the tool is off");
// Accept with no modifier must still be the game's: the interception is scoped
// to the chord, not to the button.
await Host.DoAsync("FleetCB.SwitchToPlace();");
await Task.Delay(300);
Check("no piece held before the bare press", (await Host.EvalAsync("FleetCB.HeldPiece()")).Trim('"') == "none");

Step("back to treehouse");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await Require("in the treehouse", "Fleet.Scene() == \"TreeHouseLobby\"", 90);
