// #name     customblocks/rules-tablet
// #peers    1
// #describe Rules screen block list (percentages/disable menu): structure golden + screenshot.

// This is the menu where block frequencies are set and blocks disabled. The
// mod's TabletBlockList patch appends the custom blocks; the golden records
// exactly what ends up in the list and under which serialize index. The screen
// is a tablet page of the free-play inventory book, so this drives free play.

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

await SaveScreenshot(Host, "customblocks/rules-tablet.png");

Step("block probability page");
string probScreen = await Host.EvalAsync("FleetCB.ShowBlockProbability()");
Log($"probability screen: {probScreen}");
await RequireOn("block list visible", Host, "FleetCB.BlockProbabilityVisible()", 15);
// The grid lerps to a page over several frames — a shot taken mid scroll is
// two half pages. Settle on the strip's offset, not on a fixed delay.
await RequireOn("grid settled on page 1", Host, "FleetCB.BlockPageSettled(0)", 15);
await SaveScreenshot(Host, "customblocks/block-probability.png");

Step("custom blocks page");
// The custom blocks are appended, so they land on the last page of the grid.
int lastPage = await Host.EvalIntAsync("FleetCB.BlockPageCount()") - 1;
await Host.DoAsync("FleetCB.GotoLastBlockPage();");
await RequireOn($"grid settled on page {lastPage + 1}", Host,
                $"FleetCB.BlockPageSettled({lastPage})", 20);
await SaveScreenshot(Host, "customblocks/block-probability-custom.png");

// What the tiles actually RENDER. TabletJson above records
// pickableBlockPrefab — a field the mod assigns itself — so it stays green
// while a tile displays the base block's artwork at the base block's scale.
// This golden is the one that can see that.
// Host-only: the tablet is the host's UI, and a client's copy legitimately
// carries different per-tile transform state — Golden() compares peers first
// and would report a disagreement that means nothing.
await GoldenOn("tablet tile visuals", Host, "FleetCB.TabletVisualJson()");

Step("back to treehouse");
await Host.DoAsync("FleetCB.HideBook();");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await Require("in the treehouse", "Fleet.Scene() == \"TreeHouseLobby\"", 90);
