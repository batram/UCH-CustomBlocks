// #name     customblocks/rules-tablet
// #peers    1
// #describe Rules screen block list (percentages/disable menu): structure golden + screenshot.

// This is the menu where block frequencies are set and blocks disabled. The
// mod's TabletBlockList patch appends the custom blocks; the golden records
// exactly what ends up in the list and under which serialize index.

Step("rules screen");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await Require("in the treehouse", "Fleet.Scene() == \"TreeHouseLobby\"", 60);

string screen = await Host.EvalAsync("FleetCB.ShowRulesScreen()");
Log($"rules screen: {screen}");
await Task.Delay(1000);

await Golden("tablet block list", "FleetCB.TabletJson()");

string? saved = await SaveScreenshot(Host, "customblocks/rules-tablet.png");
Check("rules screen screenshot", saved is not null, saved ?? "no screenshot (headless?)");
