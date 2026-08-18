// #name     customblocks/book-page-stable
// #peers    1
// #describe On a blank level, the mod page's layout survives being turned past: same block positions coming back as going in.

// The bug this pins: BookPageLayout re-arranged whenever a block's measured
// bounds shrank, and a page turn is an ANIMATION — the page root spins about
// the book's spine, so a page the reader has turned PAST sits at y=270 with its
// paper renderer off. Measured edge-on, the paper collapses to a sliver and the
// arrangement is computed against a rectangle that is not there; worse, a
// world-space nudge under that rotation lands in the block's local z and lifts
// it off the page.
//
// Reaching the page from the front never rotates it, which is why this needs a
// page AFTER the mod's: go past it, come back, and compare.

AllowLogErrors("CustomLevelPortal.UpdateAppearanceForClient");
AllowLogErrors("LevelSelectController.SetupLobbyAfterWait");
AllowLogErrors("UnityEngine.Light.set_color");
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");

// A BLANK level, not Farm: on Farm the mod's page is the LAST one, so there is
// nothing to turn past and the whole scenario passes without ever reproducing
// the bug — which is exactly what the first run of it did. The BlankLevelOnly
// customization pages sit behind the mod's page and give the reader somewhere
// to turn to. It is also where the bug was reported.
Step("into blank-level free play");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 60);
for (int p = 0; p < Peers.Count; p++)
    await Peers[p].DoAsync($"Fleet.PickCharacter(\"{(p == 0 ? "SQUIRREL" : "FOX")}\");");
await Task.Delay(1000);

// The treehouse only shows a rotating subset of portals, so an unavailable
// BLANKLEVEL is a skip, not a failure.
if (!await Host.EvalBoolAsync("Fleet.StartGame(\"BLANKLEVEL\", \"FREEPLAY\")"))
    Abort("no BLANKLEVEL portal — Fleet.PortalsJson() shows what the treehouse is offering");

await Require("place phase reached", "Fleet.Scene() != \"TreeHouseLobby\" && Fleet.Phase() == \"PLACE\"", 90);
await Host.DoAsync("FleetCB.SwitchToPlace();");
await Task.Delay(500);

int modIdx = await Host.EvalIntAsync("FleetCB.BookModPageIndex()");
Check("mod page is in the book", modIdx >= 0, $"index {modIdx}");

// Abort, not Check: with no page behind the mod's, every assertion below still
// passes and proves nothing.
int pages = await Host.EvalIntAsync("FleetCB.BookPageCount()");
if (modIdx < 0 || modIdx + 1 >= pages)
    Abort($"no page after the mod's ({modIdx} of {pages}) — nothing to turn past, so the bug cannot show");

Step("the layout as the reader first meets it");
await Host.DoAsync($"FleetCB.OpenBook({modIdx});");
await Require("settled on the mod page", $"FleetCB.BookSettledOn({modIdx})", 15);
// The layout runs off measured artwork bounds and only settles once every
// block has stopped shrinking; give it frames rather than photographing a
// half-arranged page.
await Task.Delay(800);

string first = (await Host.EvalAsync("FleetCB.ModPageItemsJson()")).Trim('"');
Check("every block is on the paper", !first.Contains("\"onPaper\":false"), first);
await Golden("mod page layout", "FleetCB.ModPageItemsJson()");
await SaveScreenshot(Host, "customblocks/book-page-first-visit.png");

Step("turn past the page, then come back");
for (int i = modIdx + 1; i < pages; i++)
{
    await Host.DoAsync($"FleetCB.OpenBook({i});");
    await Require($"settled on page {i}", $"FleetCB.BookSettledOn({i})", 15);
    await Task.Delay(300);
}

await Host.DoAsync($"FleetCB.OpenBook({modIdx});");
await Require("back on the mod page", $"FleetCB.BookSettledOn({modIdx})", 15);
await Task.Delay(800);

string second = (await Host.EvalAsync("FleetCB.ModPageItemsJson()")).Trim('"');
Check("every block is still on the paper", !second.Contains("\"onPaper\":false"), second);
// The whole claim in one line: nothing moved while the page was turned away.
Check("the layout is identical to the first visit", second == first, $"before: {first}\nafter:  {second}");
await SaveScreenshot(Host, "customblocks/book-page-return-visit.png");

await Host.DoAsync("FleetCB.HideBook();");

Step("back to treehouse");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await Require("in the treehouse", "Fleet.Scene() == \"TreeHouseLobby\"", 90);
