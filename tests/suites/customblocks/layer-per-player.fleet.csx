// #name     customblocks/layer-per-player
// #peers    2
// #describe One player's layer choice leaves the other player's alone, and the block still carries its layer across.

// The regression this pins: the mod used to apply a layer change to EVERY
// PiecePlacementCursor in the scene, so one player choosing a layer rewrote the
// piece held by everyone else — remote players included. Layer state is per
// player and deliberately not networked; what crosses the wire is the placed
// block's layer, and that half is asserted too so this cannot pass by the mod
// simply doing nothing.

// treehouse re-entry noise on a NetTest host: the custom level portals poke
// destroyed network views while the lobby resets (see the fleet knowledge notes)
AllowLogErrors("CustomLevelPortal.UpdateAppearanceForClient");
AllowLogErrors("LevelSelectController.SetupLobbyAfterWait");
AllowLogErrors("UnityEngine.Light.set_color");
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");
AllowLogErrors("Could not attach spawned netsurrogate");
AllowLogErrors("cursor' is inactive");

Step("into free play");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 60);
for (int p = 0; p < Peers.Count; p++)
    await Peers[p].DoAsync($"Fleet.PickCharacter(\"{(p == 0 ? "SQUIRREL" : "FOX")}\");");
await Task.Delay(1000);

// A blank level, not Farm: this scenario has to actually place a block, and
// Farm has terrain in the way — the drop was rejected as cannot-place there.
// The treehouse only shows a rotating subset of portals, so an unavailable
// BLANKLEVEL is a skip, not a failure.
if (!await Host.EvalBoolAsync("Fleet.StartGame(\"BLANKLEVEL\", \"FREEPLAY\")"))
    Abort("no BLANKLEVEL portal — Fleet.PortalsJson() shows what the treehouse is offering");

await UntilAll("Fleet.Scene() != \"TreeHouseLobby\" && Fleet.Phase() == \"PLACE\"", 90);
Check("both peers in place phase", true);

for (int p = 0; p < Peers.Count; p++)
{
    await Peers[p].DoAsync("FleetCB.SwitchToPlace();");
    await Peers[p].DoAsync("FleetCB.SetBackgroundMode(false);");
}
await Task.Delay(500);

Step("both peers start on the same footing");
string clientStart = (await Clients[0].EvalAsync("FleetCB.LayerStateJson()")).Trim('"');
Check("client starts solid with the tool off",
    clientStart.Contains("\"mode\":false") && clientStart.Contains("\"layer\":\"Default\""), clientStart);

Step("the host picks a layer");
string hostLayer = (await Host.EvalAsync("FleetCB.SetLayer(\"Haze\")")).Trim('"');
Check("host is on Haze", hostLayer == "Haze", hostLayer);
await Task.Delay(500);

// The assertion that matters: the host's choice is the host's own.
string clientAfter = (await Clients[0].EvalAsync("FleetCB.LayerStateJson()")).Trim('"');
Check("the client's own state is untouched", clientAfter == clientStart, clientAfter);
Check("the client did not follow the host onto Haze", !clientAfter.Contains("Haze"), clientAfter);

Step("what the host places still carries its layer to the client");
await Host.DoAsync("FleetCB.SetBackgroundMode(true);");
await Host.DoAsync("FleetCB.SetLayer(\"Haze\");");
await Host.EvalAsync("FleetCB.PickFromBook(\"01_1x1 Box\")");

string dropped = "cannot-place";
for (int attempt = 0; attempt < 5 && dropped.Contains("cannot-place"); attempt++)
{
    await Task.Delay(500);
    dropped = (await Host.EvalAsync("FleetCB.CursorDropAt(-3f, 6f)")).Trim('"');
}
Check("background box dropped by host", !dropped.Contains("cannot-place"), dropped);

bool clientSeesLayer = await Until(Clients[0],
    "FleetCB.BackgroundJson().Contains(\"Haze\")", 15);
Check("the client's copy carries the host's layer", clientSeesLayer,
    (await Clients[0].EvalAsync("FleetCB.BackgroundJson()")).Trim('"'));

// ...and the block crossing did not drag the host's state onto the client
string clientEnd = (await Clients[0].EvalAsync("FleetCB.LayerStateJson()")).Trim('"');
Check("client state still its own after the block arrived", clientEnd == clientStart, clientEnd);

Step("back to treehouse");
await Host.DoAsync("FleetCB.SetBackgroundMode(false);");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 90);
Check("both back in treehouse", true);
