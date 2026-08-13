// #name     customblocks/geometry-save
// #peers    1
// #describe Moved LEVEL geometry must persist through save/load — the behavior the QuickSaver patch currently destroys.

// The mod's MemorizeInitialLevelPlaceables postfix empties the initial-piece
// list, which is what the game's <moved> save records are built from. This is
// the INTENDED-behavior pin for fixing review finding #4: it stays red until
// the blanket RemoveAll is replaced with a predicate that only drops the
// mod's own prefab clones.

AllowLogErrors("Could not find main block for sub-element GluePiece");
// treehouse re-entry noise on a NetTest host: the custom level portals poke
// destroyed network views while the lobby resets (see the fleet knowledge notes)
AllowLogErrors("CustomLevelPortal.UpdateAppearanceForClient");
AllowLogErrors("LevelSelectController.SetupLobbyAfterWait");
AllowLogErrors("UnityEngine.Light.set_color");
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");
AllowLogErrors("Could not attach spawned netsurrogate");

Step("into free play");
// self-heal: a previous scenario may have died outside the treehouse
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 60);
// every lobby player must pick, whatever size the fleet runs at
for (int p = 0; p < Peers.Count; p++)
    await Peers[p].DoAsync($"Fleet.PickCharacter(\"{(p == 0 ? "SQUIRREL" : "FOX")}\");");
await Task.Delay(1000);
await Host.DoAsync("Fleet.StartGame(\"Farm\", \"FREEPLAY\");");
await Require("place phase reached", "Fleet.Scene() != \"TreeHouseLobby\" && Fleet.Phase() == \"PLACE\"", 90);

Step("move a level piece");
// Farm turned out to have NO placeable level geometry at all, so the moved-
// geometry save path cannot be exercised here. Recorded as an explicit red
// rather than a silent skip: before fixing review finding #4, this scenario
// must move to a level that ships placeable pieces.
string piece;
try
{
    piece = (await Host.EvalAsync("FleetCB.MoveLevelPiece(3f)")).Trim('"');
}
catch (Exception e)
{
    Check("level offers placeable geometry [KNOWN GAP — needs a different level]",
        false, e.Message.Split('\n')[0]);
    await Host.DoAsync("Fleet.ReturnToTreehouse();");
    await Require("in the treehouse", "Fleet.Scene() == \"TreeHouseLobby\"", 90);
    return;
}
Log($"moved level piece: {piece}");
string movedX = (await Host.EvalAsync($"FleetCB.LevelPieceX(\"{piece}\")")).Trim('"');

Step("save, clear, reload");
string b64 = (await Host.EvalAsync("FleetCB.SnapshotB64()")).Trim('"');
await Host.DoAsync("FleetCB.ClearLevel();");
await Task.Delay(1000);
bool loaded = await Host.EvalBoolAsync($"FleetCB.LoadSnapshotB64(\"{b64}\")");
Check("snapshot loaded", loaded);
await Task.Delay(2000);

string restoredX = (await Host.EvalAsync($"FleetCB.LevelPieceX(\"{piece}\")")).Trim('"');
await GoldenOn("level piece position after reload", Host,
    $"FleetCB.LevelPieceX(\"{piece}\")");
// INTENDED — review finding #4 predicts this fails today
Check("moved level geometry persisted [KNOWN DEFECT]", restoredX == movedX,
    $"moved to x={movedX}, after reload x={restoredX}");

Step("back to treehouse");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await Require("in the treehouse", "Fleet.Scene() == \"TreeHouseLobby\"", 90);
