// #name     customblocks/arena-load
// #peers    1
// #describe Load a committed level fixture containing every custom block through the real level-entry path.

// place-save-load round-trips a snapshot inside a running level. This loads
// one the way the game enters a custom level (QuickSaver.levelPortalXml before
// setup), against a COMMITTED fixture — so save compatibility with files
// written by earlier builds is enforced forever, not just within one session.
// Re-capture the fixture only for a deliberate, understood format change:
// place-save-load exports each run's snapshot as artifacts/customblocks/snapshot.xml.

AllowLogErrors("Could not find main block for sub-element GluePiece");

Step("load fixture through the level-entry path");
// every lobby player must pick, whatever size the fleet runs at
for (int p = 0; p < Peers.Count; p++)
    await Peers[p].DoAsync($"Fleet.PickCharacter(\"{(p == 0 ? "SQUIRREL" : "FOX")}\");");
await Task.Delay(1000);

string arena = SuiteFileB64("customblocks/fixtures/all-custom-blocks.xml");
await Host.DoAsync($"Fleet.StartArena(\"FREEPLAY\", \"{arena}\");");
await Require("place phase reached", "Fleet.Scene() != \"TreeHouseLobby\" && Fleet.Phase() == \"PLACE\"", 90);

Step("restored blocks");
await Task.Delay(2000);
await Golden("restored", "FleetCB.PlacedCustomJson()");
await SaveScreenshot(Host, "customblocks/arena-load.png");

Step("back to treehouse");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await Require("in the treehouse", "Fleet.Scene() == \"TreeHouseLobby\"", 90);
