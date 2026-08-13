// #name     customblocks/rules-tablet
// #peers    1
// #describe Rules screen block list (percentages/disable menu): structure golden + screenshot.

// This is the menu where block frequencies are set and blocks disabled. The
// mod's TabletBlockList patch appends the custom blocks; the golden records
// exactly what ends up in the list and under which serialize index. The screen
// is a tablet page of the free-play inventory book, so this drives free play.

// KNOWN DEFECT (baseline): glue-based custom blocks (RCReceiver, Acid) leave a
// GluePiece sub-element the save sweep cannot map to a main block. Allowed here
// so the baseline records it without failing the run; remove once fixed.
AllowLogErrors("Could not find main block for sub-element GluePiece");
// treehouse re-entry noise on a NetTest host: the custom level portals poke
// destroyed network views while the lobby resets (see the fleet knowledge notes)
AllowLogErrors("CustomLevelPortal.UpdateAppearanceForClient");
AllowLogErrors("LevelSelectController.SetupLobbyAfterWait");
AllowLogErrors("UnityEngine.Light.set_color");
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");

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

Step("rules screen");
await Host.DoAsync("FleetCB.SwitchToPlace();");
string screen = await Host.EvalAsync("FleetCB.ShowRulesScreen()");
Log($"rules screen: {screen}");

// The screenshot is only evidence if the screen is actually on camera.
// Host-only: the book is open on the host, not on other peers.
await RequireOn("rules screen visible", Host, "FleetCB.RulesScreenVisible()", 15);
await Task.Delay(1500);

await Golden("tablet block list", "FleetCB.TabletJson()");

string? saved = await SaveScreenshot(Host, "customblocks/rules-tablet.png");
Check("rules screen screenshot", saved is not null, saved ?? "no screenshot (headless?)");

Step("block probability page");
string probScreen = await Host.EvalAsync("FleetCB.ShowBlockProbability()");
Log($"probability screen: {probScreen}");
await RequireOn("block list visible", Host, "FleetCB.BlockProbabilityVisible()", 15);
await Task.Delay(1500);
string? probShot = await SaveScreenshot(Host, "customblocks/block-probability.png");
Check("block probability screenshot", probShot is not null, probShot ?? "no screenshot (headless?)");

// the custom blocks are appended, i.e. on the last page of the grid
string page = await Host.EvalAsync("FleetCB.GotoLastBlockPage()");
Log($"block page {page.Trim('"')}");
await Task.Delay(1500);
string? lastShot = await SaveScreenshot(Host, "customblocks/block-probability-custom.png");
Check("custom blocks page screenshot", lastShot is not null, lastShot ?? "no screenshot (headless?)");

Step("back to treehouse");
await Host.DoAsync("FleetCB.HideBook();");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await Require("in the treehouse", "Fleet.Scene() == \"TreeHouseLobby\"", 90);
