// #name     customblocks/rc-network
// #peers    2
// #describe RC pair placed by DIFFERENT players links identically on both peers via the mod's network channel.

// The pairing used to be local component state: each instance ran its own
// courtship with its own random color, so peers could disagree about who is
// linked to whom. Now the HOST decides the pairing and announces it on the
// mod's CustomBlockNet message; every peer (host included) applies the same
// link. This scenario places the receiver with the host's cursor and the
// transmitter with the client's — the real freeplay path, one block per
// player — and requires both peers to agree on the exact pairing.

// treehouse re-entry noise on a NetTest host: the custom level portals poke
// destroyed network views while the lobby resets (see the fleet knowledge notes)
AllowLogErrors("CustomLevelPortal.UpdateAppearanceForClient");
AllowLogErrors("LevelSelectController.SetupLobbyAfterWait");
AllowLogErrors("UnityEngine.Light.set_color");
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");
AllowLogErrors("cursor' is inactive");

Step("into free play");
// self-heal: a previous scenario may have died outside the treehouse
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 60);
for (int p = 0; p < Peers.Count; p++)
    await Peers[p].DoAsync($"Fleet.PickCharacter(\"{(p == 0 ? "SQUIRREL" : "FOX")}\");");
await Task.Delay(1000);
await Host.DoAsync("Fleet.StartGame(\"Farm\", \"FREEPLAY\");");
await UntilAll("Fleet.Scene() != \"TreeHouseLobby\" && Fleet.Phase() == \"PLACE\"", 90);
Check("both peers in place phase", true);

Step("host cursor-places the receiver");
await Host.DoAsync("FleetCB.SwitchToPlace();");
await Task.Delay(500);
string pickedRx = await Host.EvalAsync("FleetCB.PickFromBook(\"RCReceiver\")");
Check("receiver picked", pickedRx.Contains("RCReceiver_Pick"), pickedRx);
// a freshly picked piece can report cannot-place for a few frames — retry
string droppedRx = "cannot-place";
for (int attempt = 0; attempt < 5 && droppedRx.Contains("cannot-place"); attempt++)
{
    await Task.Delay(500);
    droppedRx = (await Host.EvalAsync("FleetCB.CursorDropAt(-19f, -6f)")).Trim('"');
}
Check("receiver dropped", !droppedRx.Contains("cannot-place"), droppedRx);
await RequireOn("receiver placed on host", Host, "FleetCB.IsPlacedCustom(\"RCReceiver(Clone)\")", 15);

Step("client cursor-places the transmitter");
await Clients[0].DoAsync("FleetCB.SwitchToPlace();");
await Task.Delay(500);
string pickedTx = await Clients[0].EvalAsync("FleetCB.PickFromBook(\"RCTransmitter\")");
Check("transmitter picked", pickedTx.Contains("RCTransmitter_Pick"), pickedTx);
// clear of the floor, and retried for the same settle race as above
string droppedTx = "cannot-place";
for (int attempt = 0; attempt < 5 && droppedTx.Contains("cannot-place"); attempt++)
{
    await Task.Delay(500);
    droppedTx = (await Clients[0].EvalAsync("FleetCB.CursorDropAt(-17f, -3f)")).Trim('"');
}
Check("transmitter dropped", !droppedTx.Contains("cannot-place"), droppedTx);
await RequireOn("transmitter placed on client", Clients[0], "FleetCB.IsPlacedCustom(\"RCTransmitter(Clone)\")", 15);
await RequireOn("transmitter reached the host", Host, "FleetCB.IsPlacedCustom(\"RCTransmitter(Clone)\")", 15);

Step("link crosses the network");
bool hostLinked = await Until(Host, "FleetCB.ReceiverLinked()", 15);
Check("host sees the pair linked", hostLinked);
bool clientLinked = await Until(Clients[0], "FleetCB.ReceiverLinked()", 15);
Check("client sees the pair linked", clientLinked);
await Agree("peers agree on the exact pairing", "FleetCB.RCLinkJson()");

Step("back to treehouse");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 90);
Check("both back in treehouse", true);
