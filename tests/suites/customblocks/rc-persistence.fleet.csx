// #name     customblocks/rc-persistence
// #peers    1
// #describe A linked RC pair must come back linked after save, clear, reload.

// The pairing is runtime component state (ConnectedTransmitter/-Receiver),
// nothing serializes it: persistence works because a placed transmitter's
// FixedUpdate keeps courting free placed receivers, so after a reload that
// restores BOTH blocks as placed the link re-forms on its own. This pins
// exactly that chain — if either block stops restoring placed, or the
// re-courtship stops, this goes red.

// treehouse re-entry noise on a NetTest host: the custom level portals poke
// destroyed network views while the lobby resets (see the fleet knowledge notes)
AllowLogErrors("CustomLevelPortal.UpdateAppearanceForClient");
AllowLogErrors("LevelSelectController.SetupLobbyAfterWait");
AllowLogErrors("UnityEngine.Light.set_color");
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");
// host-local direct placement leaves the client with nothing for the spawned
// surrogate to attach to
AllowLogErrors("Could not attach spawned netsurrogate");

Step("into free play");
// self-heal: a previous scenario may have died outside the treehouse
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 60);
for (int p = 0; p < Peers.Count; p++)
    await Peers[p].DoAsync($"Fleet.PickCharacter(\"{(p == 0 ? "SQUIRREL" : "FOX")}\");");
await Task.Delay(1000);
await Host.DoAsync("Fleet.StartGame(\"Farm\", \"FREEPLAY\");");
await Require("place phase reached", "Fleet.Scene() != \"TreeHouseLobby\" && Fleet.Phase() == \"PLACE\"", 90);

Step("place and link the pair");
await Host.EvalAsync("FleetCB.PlaceCustom(\"RCReceiver\", -19f, -6f)");
await Host.EvalAsync("FleetCB.PlaceCustom(\"RCTransmitter\", -17f, -6f)");
bool linked = await Until(Host, "FleetCB.ReceiverLinked()", 15);
Check("pair linked before save", linked);

Step("save, clear, reload");
string b64 = (await Host.EvalAsync("FleetCB.SnapshotB64()")).Trim('"');
await Host.DoAsync("FleetCB.ClearLevel();");
await Task.Delay(1000);
bool loaded = await Host.EvalBoolAsync($"FleetCB.LoadSnapshotB64(\"{b64}\")");
Check("snapshot loaded", loaded);

bool bothBack = await Until(Host,
    "FleetCB.IsPlacedCustom(\"RCReceiver(Clone)\") && FleetCB.IsPlacedCustom(\"RCTransmitter(Clone)\")", 15);
Check("both blocks restored placed", bothBack);
bool relinked = await Until(Host, "FleetCB.ReceiverLinked()", 15);
Check("pair re-linked after reload", relinked);

Step("back to treehouse");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await Require("in the treehouse", "Fleet.Scene() == \"TreeHouseLobby\"", 90);
