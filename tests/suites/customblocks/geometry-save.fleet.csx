// #name     customblocks/geometry-save
// #peers    1
// #describe A level's own pieces must keep a moved position through save/load — the path the QuickSaver patch destroys.

// Farm ships no placeable geometry, so this loads a minimal fixture level
// (two 1x1 boxes) through the arena path — the pieces arrive during level
// setup, exactly like a custom level's own geometry. This is the INTENDED-
// behavior pin for fixing review finding #4: the mod's
// MemorizeInitialLevelPlaceables postfix empties the initial-piece list the
// game's moved-geometry save records are built from.

AllowLogErrors("Could not find main block for sub-element GluePiece");
// treehouse re-entry noise on a NetTest host: the custom level portals poke
// destroyed network views while the lobby resets (see the fleet knowledge notes)
AllowLogErrors("CustomLevelPortal.UpdateAppearanceForClient");
AllowLogErrors("LevelSelectController.SetupLobbyAfterWait");
AllowLogErrors("UnityEngine.Light.set_color");
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");
AllowLogErrors("Could not attach spawned netsurrogate");

Step("into the fixture level");
// self-heal: a previous scenario may have died outside the treehouse
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 60);
// every lobby player must pick, whatever size the fleet runs at
for (int p = 0; p < Peers.Count; p++)
    await Peers[p].DoAsync($"Fleet.PickCharacter(\"{(p == 0 ? "SQUIRREL" : "FOX")}\");");
await Task.Delay(1000);

string arena = SuiteFileB64("customblocks/fixtures/minimal-geometry.xml");
await Host.DoAsync($"Fleet.StartArena(\"FREEPLAY\", \"{arena}\");");
await Require("place phase reached", "Fleet.Scene() != \"TreeHouseLobby\" && Fleet.Phase() == \"PLACE\"", 90);
await Task.Delay(1000);

Step("move a level piece");
string piece = (await Host.EvalAsync("FleetCB.MoveLevelPiece(3f)")).Trim('"');
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
