// #name     customblocks/inventory-book
// #peers    1
// #describe Free play inventory book: page structure golden + a screenshot per page.

// The structural golden (page names, pickables per page) is what the run is
// judged on. The screenshots are per-run evidence for eyeballing layout —
// pixels are too animation-noisy to golden.

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

Step("book structure");
await Host.DoAsync("FleetCB.SwitchToPlace();");
await Golden("pages", "FleetCB.BookJson()");

Step("page screenshots");
int pages = await Host.EvalIntAsync("FleetCB.BookPageCount()");
Check("book has pages", pages > 0, $"{pages} pages");

for (int i = 0; i < pages; i++)
{
    await Host.DoAsync($"FleetCB.OpenBook({i});");
    await Task.Delay(1500); // page-turn animation settles
    string? saved = await SaveScreenshot(Host, $"customblocks/book-page-{i}.png");
    Check($"screenshot page {i}", saved is not null, saved ?? "no screenshot (headless?)");
}
await Host.DoAsync("FleetCB.HideBook();");

Step("back to treehouse");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await Require("in the treehouse", "Fleet.Scene() == \"TreeHouseLobby\"", 90);
