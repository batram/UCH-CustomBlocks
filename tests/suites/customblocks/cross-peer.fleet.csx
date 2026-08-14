// #name     customblocks/cross-peer
// #peers    2
// #describe Host-placed blocks act on the OTHER peer: acid kills the client's character, background-ness reaches the client.

// Everything in behaviors is host-local: force-placed blocks do not even
// exist on the client. This scenario uses the real cursor path (one block
// per player per phase) so the block exists on both peers, then asserts the
// EFFECT on the peer that did not place it.

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

Step("host places acid, the client walks into it");
await Host.DoAsync("FleetCB.SwitchToPlace();");
await Task.Delay(500);
await Host.EvalAsync("FleetCB.PickFromBook(\"Acid\")");
string droppedAcid = "cannot-place";
for (int attempt = 0; attempt < 5 && droppedAcid.Contains("cannot-place"); attempt++)
{
    await Task.Delay(500);
    droppedAcid = (await Host.EvalAsync("FleetCB.CursorDropAt(-19f, -6f)")).Trim('"');
}
Check("acid dropped by host", !droppedAcid.Contains("cannot-place"), droppedAcid);
await RequireOn("acid reached the client", Clients[0], "FleetCB.IsPlacedCustom(\"Acid(Clone)\")", 15);

// the client's own character, on the client's own copy of the block
await Clients[0].DoAsync("FleetCB.SwitchToPlay();");
await Task.Delay(1000);
await Clients[0].DoAsync("FleetCB.StartDeathWatch();");
bool clientDied = false;
for (int attempt = 0; attempt < 3 && !clientDied; attempt++)
{
    await Clients[0].DoAsync("Fleet.PlaceCharacter(-19f, -5.9f);");
    clientDied = await Until(Clients[0], "FleetCB.DeathsSeen() > 0", 8);
}
Check("host-placed acid killed the client's character", clientDied);

Step("host makes a background block, the client sees it as one");
await Host.DoAsync("FleetCB.SetBackgroundMode(true);");
// picking the box returns the acid (one block per player) — its test is done
await Host.EvalAsync("FleetCB.PickFromBook(\"01_1x1 Box\")");
string droppedBox = "cannot-place";
for (int attempt = 0; attempt < 5 && droppedBox.Contains("cannot-place"); attempt++)
{
    await Task.Delay(500);
    droppedBox = (await Host.EvalAsync("FleetCB.CursorDropAt(-3f, 6f)")).Trim('"');
}
Check("background box dropped by host", !droppedBox.Contains("cannot-place"), droppedBox);
await Host.DoAsync("FleetCB.SetBackgroundMode(false);");

bool clientSeesBg = await Until(Clients[0],
    "FleetCB.BackgroundJson().Contains(\"01_1x1 Box\")", 15);
Check("client's copy became a background block", clientSeesBg,
    (await Clients[0].EvalAsync("FleetCB.BackgroundJson()")).Trim('"'));
bool clientSeesLayer = await Until(Clients[0],
    "FleetCB.BackgroundJson().Contains(\"Background\")", 10);
Check("client's copy carries the layer", clientSeesLayer);

Step("back to treehouse");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 90);
Check("both back in treehouse", true);
