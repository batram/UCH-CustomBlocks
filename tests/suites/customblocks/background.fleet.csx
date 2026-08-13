// #name     customblocks/background
// #peers    1
// #describe Background mode: real pick path applies it, layer persists through save/load, custom+background combo recorded.

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
// every lobby player must pick, whatever size the fleet runs at
for (int p = 0; p < Peers.Count; p++)
    await Peers[p].DoAsync($"Fleet.PickCharacter(\"{(p == 0 ? "SQUIRREL" : "FOX")}\");");
await Task.Delay(1000);
await Host.DoAsync("Fleet.StartGame(\"Farm\", \"FREEPLAY\");");
await Require("place phase reached", "Fleet.Scene() != \"TreeHouseLobby\" && Fleet.Phase() == \"PLACE\"", 90);
await Host.DoAsync("FleetCB.SwitchToPlace();");
await Task.Delay(500);

Step("background mode via the real pick path");
bool mode = await Host.EvalBoolAsync("FleetCB.SetBackgroundMode(true)");
Check("background mode on", mode);

// vanilla block picked while mode is on -> SetPiece patch makes it background
await Host.EvalAsync("FleetCB.PickFromBook(\"01_1x1 Box\")");
await RequireOn("picked block became background", Host,
    "FleetCB.BackgroundJson().Contains(\"01_1x1 Box\")", 15);
await Host.DoAsync("FleetCB.PlacePicked(\"01_1x1 Box\", -3f, 6f);");
await Task.Delay(500);
await GoldenOn("background vanilla block", Host, "FleetCB.BackgroundJson()");

Step("custom block in background mode");
// KNOWN DEFECT (review #5): a custom block in background mode saves
// 9000 + session slot, not its stable id — the golden records today's raw ids.
await Host.EvalAsync("FleetCB.PickFromBook(\"OneRoundWood\")");
await RequireOn("custom block became background", Host,
    "FleetCB.BackgroundJson().Contains(\"OneRoundWood\")", 15);
await Host.DoAsync("FleetCB.PlacePicked(\"OneRoundWood\", 3f, 6f);");
await Host.DoAsync("FleetCB.SetBackgroundMode(false);");
await Task.Delay(500);
await SaveScreenshot(Host, "customblocks/background-placed.png");

Step("save ids");
string b64 = (await Host.EvalAsync("FleetCB.SnapshotB64()")).Trim('"');
await GoldenOn("background snapshot ids", Host, $"FleetCB.SnapshotBlockIdsB64(\"{b64}\")");

Step("clear and reload");
await Host.DoAsync("FleetCB.ClearLevel();");
await Task.Delay(1000);
bool loaded = await Host.EvalBoolAsync($"FleetCB.LoadSnapshotB64(\"{b64}\")");
Check("snapshot loaded", loaded);
await Task.Delay(2000);

// layer and background-ness must survive the round-trip
await GoldenOn("background after reload", Host, "FleetCB.BackgroundJson()");
string bg = await Host.EvalAsync("FleetCB.BackgroundJson()");
Check("vanilla background block restored", bg.Contains("01_1x1 Box"), "");
Check("restored with its layer", bg.Contains("Background"), "");
await SaveScreenshot(Host, "customblocks/background-reloaded.png");

Step("back to treehouse");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await Require("in the treehouse", "Fleet.Scene() == \"TreeHouseLobby\"", 90);
