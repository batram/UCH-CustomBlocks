// #name     customblocks/missing-mod-save
// #peers    1
// #describe A save containing a block from an uninstalled mod must degrade gracefully, not crash the load.

// Simulated by rewriting one block's stable id in the snapshot to an id no
// mod registered (5399), which is exactly what a save from a machine with an
// extra block mod looks like here.

// treehouse re-entry noise on a NetTest host: the custom level portals poke
// destroyed network views while the lobby resets (see the fleet knowledge notes)
AllowLogErrors("CustomLevelPortal.UpdateAppearanceForClient");
AllowLogErrors("LevelSelectController.SetupLobbyAfterWait");
AllowLogErrors("UnityEngine.Light.set_color");
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");
AllowLogErrors("no block registered for save id");
// the game's own graceful degradation for the unresolvable block record
AllowLogErrors("Could not find prefab for block ID");
AllowLogErrors("Could not set transforms for saveable");
AllowLogErrors("Error while trying to restore saveable");
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

Step("place and snapshot");
await Host.EvalAsync("FleetCB.PlaceCustom(\"OneRoundWood\", -4f, 4f)");
await Host.EvalAsync("FleetCB.PlaceCustom(\"PigFarmButton\", 4f, 4f)");
await Task.Delay(1000);

string b64 = (await Host.EvalAsync("FleetCB.SnapshotB64()")).Trim('"');
string xml = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(b64));
Check("snapshot contains PigFarmButton's id", xml.Contains("5006"), "");

// PigFarmButton (5006) becomes an id no installed mod owns
string mangled = Convert.ToBase64String(
    System.Text.Encoding.UTF8.GetBytes(xml.Replace("\"5006\"", "\"5399\"")));

Step("reload without the 'missing mod'");
await Host.DoAsync("FleetCB.ClearLevel();");
await Task.Delay(1000);
bool loaded = await Host.EvalBoolAsync($"FleetCB.LoadSnapshotB64(\"{mangled}\")");
Check("load call survived", loaded);
await Task.Delay(2000);

// QuickClear UNPLACES pieces rather than destroying them, and restore then
// re-places matching records — so the unresolvable block's ghost stays in the
// scene as 'unplaced'. The golden records exactly that; the checks assert the
// graceful part: the known block survives and nothing crashed.
await GoldenOn("restored without missing mod", Host, "FleetCB.PlacedCustomJson()");
string restored = await Host.EvalAsync("FleetCB.PlacedCustomJson()");
Check("OneRoundWood restored", restored.Contains("OneRoundWood"), "");
Check("missing-mod block did not get re-placed",
    !restored.Contains("PigFarmButton(Clone):113:placed"), "");

Step("back to treehouse");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await Require("in the treehouse", "Fleet.Scene() == \"TreeHouseLobby\"", 90);
