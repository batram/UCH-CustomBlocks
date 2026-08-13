// #name     customblocks/party-draw
// #peers    2
// #describe With every vanilla block at frequency 0, the party box can only draw custom blocks.

AllowLogErrors("Could not find main block for sub-element GluePiece");
// treehouse re-entry noise on a NetTest host: the custom level portals poke
// destroyed network views while the lobby resets (see the fleet knowledge notes)
AllowLogErrors("CustomLevelPortal.UpdateAppearanceForClient");
AllowLogErrors("LevelSelectController.SetupLobbyAfterWait");
AllowLogErrors("UnityEngine.Light.set_color");
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");
// party -> treehouse -> party transition noise on Farm (see party-box)
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");
AllowLogErrors("UnityEngine.Light.set_color");
// the box refills during scene exit while everything vanilla is still zeroed
AllowLogErrors("Failed to grab random index");

Step("zero out vanilla, into party mode");
// self-heal: a previous scenario may have died outside the treehouse
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 60);
// every lobby player must pick, whatever size the fleet runs at
for (int p = 0; p < Peers.Count; p++)
    await Peers[p].DoAsync($"Fleet.PickCharacter(\"{(p == 0 ? "SQUIRREL" : "FOX")}\");");
await Task.Delay(1000);

int zeroed = await Host.EvalIntAsync("FleetCB.ZeroAllVanillaFrequencies()");
Check("vanilla frequencies zeroed", zeroed > 100, $"{zeroed} blocks");

await Host.DoAsync("Fleet.StartGame(\"Farm\", \"PARTY\");");
await UntilAll("Fleet.Scene() != \"TreeHouseLobby\"", 90);
await Require("place phase reached", "Fleet.Phase() == \"PLACE\"", 90);

Step("only custom blocks drawable");
int vanillaSum = await Host.EvalIntAsync("FleetCB.VanillaPartyWeightSum()");
Check("vanilla weight sum is 0", vanillaSum == 0, $"sum {vanillaSum}");
await GoldenOn("custom-only party weights", Host, "FleetCB.PartyWeightsJson()");
await SaveScreenshot(Host, "customblocks/party-draw.png");

Step("restore vanilla frequencies");
// later scenarios in the same session share this GameSettings state
int restored = await Host.EvalIntAsync("FleetCB.RestoreVanillaFrequencies()");
Check("vanilla frequencies restored", restored > 100, $"{restored} blocks");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 90);
Check("both back in treehouse", true);
