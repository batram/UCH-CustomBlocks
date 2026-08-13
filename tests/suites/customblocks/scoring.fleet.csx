// #name     customblocks/scoring
// #peers    2
// #describe A vanilla suicide point must stay a suicide point with the mod loaded (review finding #1).

// PigDirt's ScoreKeeper patch intercepts ALL PointAwarded messages of type
// suicide and rewrites them into negative "Pig Dirt" coin points. This feeds
// one genuine suicide point through the real message loop and asserts the
// vanilla intent. If it fails, the corruption is real and now pinned.

AllowLogErrors("Could not find main block for sub-element GluePiece");
// treehouse re-entry noise on a NetTest host: the custom level portals poke
// destroyed network views while the lobby resets (see the fleet knowledge notes)
AllowLogErrors("CustomLevelPortal.UpdateAppearanceForClient");
AllowLogErrors("LevelSelectController.SetupLobbyAfterWait");
AllowLogErrors("UnityEngine.Light.set_color");
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");

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

Step("into play phase");
// points are only tallied for players registered at round start
await Host.DoAsync("Fleet.AdvancePlacement();");
await Require("play phase reached", "Fleet.Phase() == \"PLAY\" || Fleet.Phase() == \"SUDDENDEATH\"", 60);

Step("award a vanilla suicide point");
await Host.EvalAsync("FleetCB.AwardSuicideAndReport(1)");
await Task.Delay(2000);

await GoldenOn("scorekeeper state", Host, "FleetCB.ScoreBlocksJson()");
string blocks = await Host.EvalAsync("FleetCB.ScoreBlocksJson()");
// INTENDED behavior — review finding #1 predicts this fails today
Check("suicide point kept its type", blocks.Contains("\\\"type\\\":\\\"suicide\\\"") || blocks.Contains("\"type\":\"suicide\""), "");
Check("no smuggled pig-dirt point", !blocks.Contains("suicideValue\":-1") && !blocks.Contains("suicideValue\\\":-1"), "");

Step("back to treehouse");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 90);
Check("both back in treehouse", true);
