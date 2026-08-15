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
    // GotoPage walks one page per 0.1s of real time and only arrives at the
    // end. Waiting for arrival instead of sleeping a fixed 1.5s is what stops
    // this loop photographing whatever page the turn happened to be passing:
    // the committed artifacts it used to produce read 1,2,3,4,3 of 5.
    if (!await RequireOn($"book settled on page {i}", Host, $"FleetCB.BookSettledOn({i})", 15))
        continue;
    await Task.Delay(400); // page-flip animation, after arrival

    string name = (await Host.EvalAsync("FleetCB.BookCurrentPageName()")).Trim('"');
    string shown = (await Host.EvalAsync("FleetCB.BookShownPage()")).Trim('"');
    Log($"page {i}: {name} — printed \"{shown}\"");
    await SaveScreenshot(Host, $"customblocks/book-page-{i}.png");
}
await Host.DoAsync("FleetCB.HideBook();");

Step("the mod's own page");
// A page the array knows about but a player cannot turn to is not in the book.
// Vanilla's last blank-level page ships without a Next arrow, so where the
// mod inserts itself decides whether the page exists in practice.
int modIdx = await Host.EvalIntAsync("FleetCB.BookModPageIndex()");
Check("mod page is in the book", modIdx >= 0, $"index {modIdx}");
Check("a player can turn to the mod page",
      await Host.EvalBoolAsync("FleetCB.BookModPageReachable()"),
      "every page before it needs an active Next arrow");

Step("back to treehouse");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await Require("in the treehouse", "Fleet.Scene() == \"TreeHouseLobby\"", 90);
