// #name     customblocks/party-box
// #peers    2
// #describe Party mode: custom blocks in the box rotation, and a disabled one stays out of it.

// Party mode is where two review findings predict trouble (PartyBox registers
// every pickable prefab with UNet at Awake; the itemFilter is consulted
// unchecked), so simply reaching the place phase with the log gate armed is
// half the value of this scenario.

// treehouse re-entry noise on a NetTest host: the custom level portals poke
// destroyed network views while the lobby resets (see the fleet knowledge notes)
AllowLogErrors("CustomLevelPortal.UpdateAppearanceForClient");
AllowLogErrors("LevelSelectController.SetupLobbyAfterWait");
AllowLogErrors("UnityEngine.Light.set_color");
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");
// party -> treehouse -> party transition noise on Farm, seen with the mod on;
// whether it is vanilla or ours is an open question the baseline keeps visible
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");
AllowLogErrors("UnityEngine.Light.set_color");

Step("into party mode");
// self-heal: a previous scenario may have died outside the treehouse
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 60);
await Host.EvalAsync("FleetCB.RestoreVanillaFrequencies()");
// every lobby player must pick, whatever size the fleet runs at
for (int p = 0; p < Peers.Count; p++)
    await Peers[p].DoAsync($"Fleet.PickCharacter(\"{(p == 0 ? "SQUIRREL" : "FOX")}\");");
await Task.Delay(1000);
await Host.DoAsync("Fleet.StartGame(\"Farm\", \"PARTY\");");
await UntilAll("Fleet.Scene() != \"TreeHouseLobby\"", 90);
await Require("place phase reached", "Fleet.Phase() == \"PLACE\"", 90);

Step("default weights");
// custom blocks must be in the base rotation with a non-zero weight
await Golden("party weights", "FleetCB.PartyWeightsJson()");
await SaveScreenshot(Host, "customblocks/party-box.png");

Step("disable one custom block");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 90);

int freq = await Host.EvalIntAsync("FleetCB.SetCustomFrequency(\"Acid\", 0)");
Check("Acid frequency set to 0", freq == 0, $"frequency now {freq}");

await Host.DoAsync("Fleet.StartGame(\"Farm\", \"PARTY\");");
await UntilAll("Fleet.Scene() != \"TreeHouseLobby\"", 90);
await Require("place phase reached again", "Fleet.Phase() == \"PLACE\"", 90);

// Host-only: party piece selection is host-authoritative (the host sends
// MsgSetPartyPieceID), so the host's weights are what gameplay draws from.
// COVERAGE GAP: propagating a frequency change to other peers' tablets goes
// through the rules-screen message flow, which this shortcut does not drive.
await GoldenOn("party weights with Acid disabled", Host, "FleetCB.PartyWeightsJson()");
int acidWeight = await Host.EvalIntAsync("FleetCB.PartyWeightOf(\"Acid\")");
Check("Acid weight is 0", acidWeight == 0, $"weight {acidWeight}");

Step("back to treehouse");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 90);
Check("both back in treehouse", true);
