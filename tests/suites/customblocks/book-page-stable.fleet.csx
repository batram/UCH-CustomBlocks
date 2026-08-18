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

// Read the layout only once it has stopped moving.
//
// The layout re-flows as artwork settles: the glue-based blocks carry a rig
// that is opaque for the first frames after the page shows and fades out, so
// their measured size keeps shrinking for a while. A fixed sleep photographs
// whatever that process happens to be doing — Acid's y came out -9.7, -8.01
// and -10.38 on three runs of exactly the same build.
Func<Task<string>> settledItems = async () =>
{
    string previous = null;
    for (int i = 0; i < 25; i++)
    {
        string now = (await Host.EvalAsync("FleetCB.ModPageItemsJson()")).Trim('"');
        if (now == previous) return now;
        previous = now;
        await Task.Delay(300);
    }
    Log("layout never stopped moving — goldening the last reading");
    return previous;
};

Step("the layout as the reader first meets it");
await Host.DoAsync($"FleetCB.OpenBook({modIdx});");
await Require("settled on the mod page", $"FleetCB.BookSettledOn({modIdx})", 15);

// Wait for the layout to stop moving, THEN golden it. The page is settled
// before the artwork is: goldening on a fixed delay recorded a different Acid
// position on each of three runs of the same build.
string first = await settledItems();
Check("every block is on the paper", !first.Contains("\"onPaper\":false"), first);
await Golden("mod page layout", "FleetCB.ModPageItemsJson()");
await SaveScreenshot(Host, "customblocks/book-page-first-visit.png");

Step("turn past the page, then come back");
for (int i = modIdx + 1; i < pages; i++)
{
    await Host.DoAsync($"FleetCB.OpenBook({i});");
    await Require($"settled on page {i}", $"FleetCB.BookSettledOn({i})", 15);
    await Task.Delay(300);

    // Nothing from another page may draw in front of this one. Sampled right
    // after the turn ON PURPOSE: the game leaves the previous page's blocks
    // Visible for about a second, and that second is when MultiStart's bar
    // copies and spawn hatch showed through — they kept their authored sorting
    // orders while the rest of the block went behind the paper.
    string over = (await Host.EvalAsync("FleetCB.PageOverdrawJson()")).Trim('"');
    Check($"nothing from another page draws over page {i}", over == "[]", over);

    string leaks = (await Host.EvalAsync("FleetCB.HiddenPickLeaksJson()")).Trim('"');
    Check($"no hidden block is still drawing on page {i}", leaks == "[]", leaks);
}

await Host.DoAsync($"FleetCB.OpenBook({modIdx});");
await Require("back on the mod page", $"FleetCB.BookSettledOn({modIdx})", 15);

string second = await settledItems();
Check("every block is still on the paper", !second.Contains("\"onPaper\":false"), second);
// The whole claim in one line: nothing moved while the page was turned away.
Check("the layout is identical to the first visit", second == first, $"before: {first}\nafter:  {second}");
await SaveScreenshot(Host, "customblocks/book-page-return-visit.png");

Step("turn back off the mod page — the reported repro");
// Backwards off the page, sampled repeatedly across the second that follows.
// The bleed is not instantaneous and it is not permanent: it lasts from the
// turn until the game gets around to hiding the page's blocks, so a single
// sample either side of that window sees nothing wrong.
await Host.DoAsync("FleetCB.PrevPage();");
for (int i = 0; i < 6; i++)
{
    string over = (await Host.EvalAsync("FleetCB.PageOverdrawJson()")).Trim('"');
    Check($"nothing bleeds through {i * 200}ms after turning back", over == "[]", over);
    await Task.Delay(200);
}
await SaveScreenshot(Host, "customblocks/book-page-turned-back.png");

Step("captions survive the round trip legible");
// Back onto the mod page and check what the captions are actually drawn from.
// Hiding a block by switching its Canvas off makes Graphic.canvas null, and a
// text mesh rebuilt in that state bakes its glyphs at a tiny size and keeps
// them — MultiStart's caption dropped from 69 to 6 and re-blurred on every
// page turn. A screenshot at this size cannot show that; the baked size can.
await Host.DoAsync($"FleetCB.OpenBook({modIdx});");
await Require("on the mod page again", $"FleetCB.BookSettledOn({modIdx})", 15);
await Task.Delay(500);

// baked == rebuilt, per caption.
Func<string, bool> captionsAgree = json =>
{
    foreach (string row in json.Split(new[] { "},{" }, StringSplitOptions.None))
    {
        int b = row.IndexOf("\"baked\":");
        int r = row.IndexOf("\"rebuilt\":");
        if (b < 0 || r < 0) continue;
        string baked = new string(row.Substring(b + 8).TakeWhile(char.IsDigit).ToArray());
        string rebuilt = new string(row.Substring(r + 10).TakeWhile(char.IsDigit).ToArray());
        if (baked != rebuilt) return false;
    }
    return true;
};

string captions = (await Host.EvalAsync("FleetCB.CaptionRasterJson()")).Trim('"');
Check("a caption exists to check", captions.Contains("\"text\""), captions);
Check("the caption's canvas is live while the block is on display", !captions.Contains("\"canvasOn\":false"), captions);
Check("captions are rasterised at the size a rebuild would pick", captionsAgree(captions), captions);

await Host.DoAsync("FleetCB.HideBook();");

Step("back to treehouse");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await Require("in the treehouse", "Fleet.Scene() == \"TreeHouseLobby\"", 90);
