// #name     customblocks/book-pick-place
// #peers    2
// #describe Pick a custom block from the book and place it — the real player path, verified on both peers.

// The direct-instantiate placement in place-save-load bypasses the network.
// This is the path a player takes: PickBlockEvent -> cursor instantiates and
// sends MsgBookPiecePicked (a PlaceableMetadataList INDEX crosses the wire) ->
// the other peer instantiates from that index. If custom-block indices ever
// disagree between peers, this is the scenario that catches it.

// treehouse re-entry noise on a NetTest host: the custom level portals poke
// destroyed network views while the lobby resets (see the fleet knowledge notes)
AllowLogErrors("CustomLevelPortal.UpdateAppearanceForClient");
AllowLogErrors("LevelSelectController.SetupLobbyAfterWait");
AllowLogErrors("UnityEngine.Light.set_color");
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");
// artifact of the host-local Place shortcut: the client has no placed piece
// for the surrogate to attach to, and the host's cursor object is inactive
// when scene teardown pokes it
AllowLogErrors("Could not attach spawned netsurrogate");
AllowLogErrors("cursor' is inactive");

Step("into free play");
// self-heal: a previous scenario may have died outside the treehouse
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 60);
// every lobby player must pick, whatever size the fleet runs at
for (int p = 0; p < Peers.Count; p++)
    await Peers[p].DoAsync($"Fleet.PickCharacter(\"{(p == 0 ? "SQUIRREL" : "FOX")}\");");
await Task.Delay(1000);
await Host.DoAsync("Fleet.StartGame(\"Farm\", \"FREEPLAY\");");
await UntilAll("Fleet.Scene() != \"TreeHouseLobby\" && Fleet.Phase() == \"PLACE\"", 90);
Check("both peers in place phase", true);

Step("pick from book");
await Host.DoAsync("FleetCB.SwitchToPlace();");
await Task.Delay(500);
// the external mod's block, so the cross-assembly path is the one on the wire
string picked = await Host.EvalAsync("FleetCB.PickFromBook(\"PigFarmButton\")");
Check("picked", picked.Contains("PigFarmButton_Pick"), picked);

// the pick message must materialize an instance on BOTH peers — this is the
// index-over-the-wire assertion the scenario exists for
await RequireOn("host has the picked instance", Host,
    "FleetCB.PlacedCustomJson().Contains(\"PigFarmButton\")", 15);
bool onClient = await Until(Clients[0],
    "FleetCB.PlacedCustomJson().Contains(\"PigFarmButton\")", 15);
Check("client received the pick over the network", onClient);

Step("place it — the real cursor drop");
// MsgPiecePlaced round-trips through the server and places on EVERY peer
string dropped = await Host.EvalAsync("FleetCB.CursorDropAt(2f, 5f)");
Check("drop accepted", !dropped.Contains("cannot-place"), dropped);
await RequireOn("placed on host", Host,
    "FleetCB.IsPlacedCustom(\"PigFarmButton(Clone)\")", 15);
bool placedOnClient = await Until(Clients[0],
    "FleetCB.IsPlacedCustom(\"PigFarmButton(Clone)\")", 15);
Check("placed on client via the network", placedOnClient);

await Agree("custom blocks agree on both peers", "FleetCB.PlacedCustomJson()");
await Golden("after place", "FleetCB.PlacedCustomJson()");
await SaveScreenshot(Host, "customblocks/book-pick-place-host.png");
await SaveScreenshot(Clients[0], "customblocks/book-pick-place-client.png");

Step("back to treehouse");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 90);
Check("both back in treehouse", true);
