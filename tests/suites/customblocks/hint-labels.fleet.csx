// #name     customblocks/hint-labels
// #peers    1
// #describe The cursor hint labels stay to the LEFT of their key boxes — including while a block is carried and the layer keys are pressed.

// The bug this pins: the rows are cloned from the game's own hint rows, which
// carry a HorizontalLayoutGroup. That group lays children out left to right, so
// it puts the label to the RIGHT of the key boxes. It only re-runs when
// something dirties the layout — and the only label whose text ever changes is
// "Layer: ...". Pressing K/L therefore flipped that one label across to the
// wrong side, and carrying a block kept it there: the hints anchor to the
// Inventory row, which the game hides while the cursor holds a piece, so the
// mod's own Layout stalls and never corrected it.

// treehouse re-entry noise on a NetTest host: the custom level portals poke
// destroyed network views while the lobby resets (see the fleet knowledge notes)
AllowLogErrors("CustomLevelPortal.UpdateAppearanceForClient");
AllowLogErrors("LevelSelectController.SetupLobbyAfterWait");
AllowLogErrors("UnityEngine.Light.set_color");
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");
AllowLogErrors("Could not attach spawned netsurrogate");
AllowLogErrors("cursor' is inactive");

// A label whose right edge is left of the leftmost key box has a positive gap.
// Negative means it has crossed over the keys. keyGap is the same measurement
// between the key boxes themselves: the first fix removed the layout group that
// was ALSO spreading them, and the layer row's K vanished under its L.
Action<string, string> labelsClear = (what, json) =>
    Check(what,
        !json.Contains("\"gap\":-") && !json.Contains("\"keyGap\":-") && !json.Contains("\"keyGap\":0,")
            && json.Contains("\"row\""),
        json);

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

// The layer and highlight rows only exist on screen with the tool on.
Check("background mode on", await Host.EvalBoolAsync("FleetCB.SetBackgroundMode(true)"));
await Task.Delay(500);

Step("no inherited layout group on the cloned rows");
// The direct cause, asserted directly: with the group alive, everything below
// is one dirty layout away from flipping again.
string rows = (await Host.EvalAsync("FleetCB.HintLabelsJson()")).Trim('"');
Check("all three rows are up", rows.Contains("Mode") && rows.Contains("Layer") && rows.Contains("Highlight"), rows);
Check("no row carries a HorizontalLayoutGroup", !rows.Contains("\"layoutGroup\":true"), rows);
// The layer row is the two-key one (prev/next); the other two show one key.
Check("the layer row still shows both of its keys", rows.Contains("\"keys\":2"), rows);
labelsClear("labels start left of their keys", rows);

Step("stepping the layer with an empty cursor");
for (int i = 0; i < 3; i++)
{
    string layer = (await Host.EvalAsync("FleetCB.StepLayer(false)")).Trim('"');
    await Task.Delay(200);
    string after = (await Host.EvalAsync("FleetCB.HintLabelsJson()")).Trim('"');
    Check($"\"{layer}\" is drawn on the layer row", after.Contains("Layer: " + layer), after);
    labelsClear($"labels still left after stepping to {layer}", after);
}

Step("stepping the layer while carrying a block");
// Carrying is the half that made it stick: the Inventory row goes away, so the
// mod's per-frame Layout has nothing to anchor to and stops running.
await Host.EvalAsync("FleetCB.PickFromBook(\"01_1x1 Box\")");
await RequireOn("cursor holds the box", Host, "FleetCB.CursorHolds() != \"nothing\"", 15);
await Task.Delay(300);

for (int i = 0; i < 3; i++)
{
    string layer = (await Host.EvalAsync($"FleetCB.StepLayer({(i % 2 == 0 ? "true" : "false")})")).Trim('"');
    await Task.Delay(200);
    string carried = (await Host.EvalAsync("FleetCB.HintLabelsJson()")).Trim('"');
    Check($"\"{layer}\" is drawn while carrying", carried.Contains("Layer: " + layer), carried);
    labelsClear($"labels still left while carrying, on {layer}", carried);
}

// Evidence for the eye, next to the assertions that make it mean something.
await SaveScreenshot(Host, "customblocks/hint-labels-carrying.png");

Step("back to treehouse");
await Host.DoAsync("FleetCB.SetBackgroundMode(false);");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await Require("in the treehouse", "Fleet.Scene() == \"TreeHouseLobby\"", 90);
