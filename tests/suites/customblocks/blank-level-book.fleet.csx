// #name     customblocks/blank-level-book
// #peers    1
// #describe Blank-level free play: the mod page must sit in front of the customization pages and be reachable by hand.

// Every other scenario in this suite runs on Farm, and Farm is the one level
// where the mod's page insertion cannot go wrong. The BlankLevelOnly "Level
// Customization" pages exist only on BLANKLEVEL; they reuse InventoryPage4/5
// types, and the last of them ("Inventory G", Select Background) ships with no
// Next arrow because vanilla ends the book there. A page appended behind it is
// in InventoryPages, is counted in the printed "x / 7", answers GotoPage — and
// is unreachable by a player, because GotoPage sets GoToPageTurning and skips
// the very guard that stops a manual page turn. That is invisible to every
// structural golden, which is why this scenario checks the arrows.

// treehouse re-entry noise on a NetTest host: the custom level portals poke
// destroyed network views while the lobby resets (see the fleet knowledge notes)
AllowLogErrors("CustomLevelPortal.UpdateAppearanceForClient");
AllowLogErrors("LevelSelectController.SetupLobbyAfterWait");
AllowLogErrors("UnityEngine.Light.set_color");
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");

Step("into blank-level free play");
// self-heal: a previous scenario may have died outside the treehouse
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 60);
for (int p = 0; p < Peers.Count; p++)
    await Peers[p].DoAsync($"Fleet.PickCharacter(\"{(p == 0 ? "SQUIRREL" : "FOX")}\");");
await Task.Delay(1000);

// The treehouse only shows a rotating subset of portals, so an unavailable
// BLANKLEVEL is a skip, not a failure.
if (!await Host.EvalBoolAsync("Fleet.StartGame(\"BLANKLEVEL\", \"FREEPLAY\")"))
    Abort("no BLANKLEVEL portal — Fleet.PortalsJson() shows what the treehouse is offering");

await Require("place phase reached",
              "Fleet.Scene() != \"TreeHouseLobby\" && Fleet.Phase() == \"PLACE\"", 90);

Step("book structure on a blank level");
await Host.DoAsync("FleetCB.SwitchToPlace();");
await Golden("blank-level pages", "FleetCB.BookJson()");

int pages = await Host.EvalIntAsync("FleetCB.BookPageCount()");
int modIdx = await Host.EvalIntAsync("FleetCB.BookModPageIndex()");
Check("mod page present", modIdx >= 0, $"index {modIdx} of {pages}");

// The pre-refactor code hardcoded "before background selection" for exactly
// this reason; the position is the fix, not a detail.
Check("mod page sits in front of the customization pages", modIdx >= 0 && modIdx < pages - 1,
      $"index {modIdx} of {pages} — the BlankLevelOnly pages must come after it");

Check("a player can turn to the mod page",
      await Host.EvalBoolAsync("FleetCB.BookModPageReachable()"),
      "every page before it needs an active Next arrow");

Step("mod page screenshot");
await Host.DoAsync($"FleetCB.OpenBook({modIdx});");
await RequireOn("settled on the mod page", Host, $"FleetCB.BookSettledOn({modIdx})", 15);
await Task.Delay(400); // page-flip animation, after arrival
string shown = (await Host.EvalAsync("FleetCB.BookShownPage()")).Trim('"');
string name = (await Host.EvalAsync("FleetCB.BookCurrentPageName()")).Trim('"');
Check("the page on screen is the mod's", name.Contains("Mod Blocks"), $"{name} — printed \"{shown}\"");
await SaveScreenshot(Host, "customblocks/blank-book-modpage.png");

Step("block probability on a blank level");
string rules = await Host.EvalAsync("FleetCB.ShowRulesScreen()");
Log($"rules screen: {rules}");
await RequireOn("rules screen visible", Host, "FleetCB.RulesScreenVisible()", 15);
await Host.EvalAsync("FleetCB.ShowBlockProbability()");
await RequireOn("block list visible", Host, "FleetCB.BlockProbabilityVisible()", 15);

int lastPage = await Host.EvalIntAsync("FleetCB.BlockPageCount()") - 1;
await Host.DoAsync("FleetCB.GotoLastBlockPage();");
await RequireOn($"grid settled on page {lastPage + 1}", Host,
                $"FleetCB.BlockPageSettled({lastPage})", 20);
await SaveScreenshot(Host, "customblocks/blank-blockprob-custom.png");

Step("back to treehouse");
await Host.DoAsync("FleetCB.HideBook();");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await Require("in the treehouse", "Fleet.Scene() == \"TreeHouseLobby\"", 90);
