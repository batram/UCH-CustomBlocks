// #name     customblocks/rounds
// #peers    2
// #describe Placed custom blocks survive a PLACE -> PLAY phase transition.

// treehouse re-entry noise on a NetTest host: the custom level portals poke
// destroyed network views while the lobby resets (see the fleet knowledge notes)
AllowLogErrors("CustomLevelPortal.UpdateAppearanceForClient");
AllowLogErrors("LevelSelectController.SetupLobbyAfterWait");
AllowLogErrors("UnityEngine.Light.set_color");
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");
AllowLogErrors("Could not attach spawned netsurrogate");
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");
AllowLogErrors("UnityEngine.Light.set_color");

Step("into party mode");
// self-heal: a previous scenario may have died outside the treehouse
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 60);
// every lobby player must pick, whatever size the fleet runs at
for (int p = 0; p < Peers.Count; p++)
    await Peers[p].DoAsync($"Fleet.PickCharacter(\"{(p == 0 ? "SQUIRREL" : "FOX")}\");");
await Task.Delay(1000);
await Host.DoAsync("Fleet.StartGame(\"Farm\", \"PARTY\");");
await UntilAll("Fleet.Scene() != \"TreeHouseLobby\"", 90);
await Require("place phase reached", "Fleet.Phase() == \"PLACE\"", 90);

Step("place custom blocks");
await Host.EvalAsync("FleetCB.PlaceCustom(\"OneRoundWood\", -4f, 6f)");
await Host.EvalAsync("FleetCB.PlaceCustom(\"FloatyCloud\", 4f, 8f)");
await Task.Delay(1000);
await GoldenOn("placed in round 1", Host, "FleetCB.PlacedCustomJson()");

Step("advance to play phase");
// AdvancePlacement bypasses the placement countdown (prelude-documented
// shortcut); the phase change itself is the real game path.
await Host.DoAsync("Fleet.AdvancePlacement();");
await Require("play phase reached", "Fleet.Phase() == \"PLAY\" || Fleet.Phase() == \"SUDDENDEATH\"", 60);

// the blocks must still be there and active in PLAY
await GoldenOn("alive in play phase", Host, "FleetCB.PlacedCustomJson()");
string played = await Host.EvalAsync("FleetCB.PlacedCustomJson()");
Check("OneRoundWood survived the transition", played.Contains("OneRoundWood(Clone)"), "");
Check("FloatyCloud survived the transition", played.Contains("FloatyCloud(Clone)"), "");
await SaveScreenshot(Host, "customblocks/rounds-play-phase.png");

Step("back to treehouse");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 90);
Check("both back in treehouse", true);
