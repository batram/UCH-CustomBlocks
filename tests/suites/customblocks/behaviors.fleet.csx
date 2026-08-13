// #name     customblocks/behaviors
// #peers    1
// #describe Every block's INTENDED behavior, exercised with a real character. Failures document breakage.

// Live-verified recipe: freeplay parks the character until SwitchToPlay; the
// touch blocks react to a character OVERLAPPING their cell (teleport into it,
// not onto it); Farm's ground sits around y = -6.
//
// Ordering matters: blocks that transform or kill the character run last.

AllowLogErrors("Could not find main block for sub-element GluePiece");
// treehouse re-entry noise on a NetTest host: the custom level portals poke
// destroyed network views while the lobby resets (see the fleet knowledge notes)
AllowLogErrors("CustomLevelPortal.UpdateAppearanceForClient");
AllowLogErrors("LevelSelectController.SetupLobbyAfterWait");
AllowLogErrors("UnityEngine.Light.set_color");
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");
AllowLogErrors("Could not attach spawned netsurrogate");

Step("into free play, character in world");
// self-heal: a previous scenario may have died outside the treehouse
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 60);
// every lobby player must pick, whatever size the fleet runs at
for (int p = 0; p < Peers.Count; p++)
    await Peers[p].DoAsync($"Fleet.PickCharacter(\"{(p == 0 ? "SQUIRREL" : "FOX")}\");");
await Task.Delay(1000);
await Host.DoAsync("Fleet.StartGame(\"Farm\", \"FREEPLAY\");");
await Require("place phase reached", "Fleet.Scene() != \"TreeHouseLobby\" && Fleet.Phase() == \"PLACE\"", 90);
await Host.DoAsync("FleetCB.SwitchToPlay();");
await Task.Delay(1000);

Step("OneRoundWood: rounded collider");
await Host.EvalAsync("FleetCB.PlaceCustom(\"OneRoundWood\", -26f, -6f)");
bool round = await Host.EvalBoolAsync("FleetCB.HasCircleCollider(\"OneRoundWood\")");
Check("OneRoundWood has a circle collider", round);

Step("MultiStart: extra spawn position");
int nearBefore = await Host.EvalIntAsync("FleetCB.SpawnsNear(-10f, -6f, 40, 3f)");
await Host.EvalAsync("FleetCB.PlaceCustom(\"MultiStart\", -10f, -6f)");
await Task.Delay(500);
int nearAfter = await Host.EvalIntAsync("FleetCB.SpawnsNear(-10f, -6f, 40, 3f)");
Check("MultiStart adds a spawn position near itself", nearAfter > nearBefore,
    $"spawns near it: {nearBefore} -> {nearAfter}");

Step("RemoteControl: pair connects");
await Host.EvalAsync("FleetCB.PlaceCustom(\"RCReceiver\", -30f, -6f)");
await Host.EvalAsync("FleetCB.PlaceCustom(\"RCTransmitter\", -28f, -6f)");
bool linked = await Until(Host, "FleetCB.ReceiverLinked()", 15);
Check("transmitter connected to receiver", linked);

Step("FloatyCloud: sinks under a character");
await Host.EvalAsync("FleetCB.PlaceCustom(\"FloatyCloud\", -22f, -4f)");
await Task.Delay(500);
double cloudBefore = double.Parse(
    (await Host.EvalAsync("FleetCB.BlockY(\"FloatyCloud\")")).Trim('"'),
    System.Globalization.CultureInfo.InvariantCulture);
await Host.DoAsync("Fleet.PlaceCharacter(-22f, -2.8f);");
await Task.Delay(3000);
double cloudAfter = double.Parse(
    (await Host.EvalAsync("FleetCB.BlockY(\"FloatyCloud\")")).Trim('"'),
    System.Globalization.CultureInfo.InvariantCulture);
Check("cloud sank", cloudAfter < cloudBefore - 0.05, $"y {cloudBefore:F2} -> {cloudAfter:F2}");

Step("PigDirt: flies on touch");
await Host.EvalAsync("FleetCB.PlaceCustom(\"PigDirt\", -12f, -6f)");
await Task.Delay(500);
await Host.DoAsync("Fleet.PlaceCharacter(-12f, -5.9f);");
bool flies = await Until(Host, "FleetCB.LocalFliesCount() > 0", 10);
Check("character got flies", flies);

Step("PigFarmButton: pig-ifies the character");
await Host.EvalAsync("FleetCB.PlaceCustom(\"PigFarmButton\", -16f, -6f)");
await Task.Delay(500);
// the trigger has a sound-driven cooldown; re-enter the cell a few times
bool pigged = false;
for (int attempt = 0; attempt < 3 && !pigged; attempt++)
{
    await Host.DoAsync("Fleet.PlaceCharacter(-16f, -5.9f);");
    pigged = await Until(Host, "FleetCB.LocalAnimal() == \"ELEPHANT\"", 8);
}
Check("character became the pig-elephant", pigged,
    (await Host.EvalAsync("FleetCB.LocalAnimal()")).Trim('"'));

Step("ChickenRoll: kills on touch");
await RequireOn("character alive before chicken roll", Host, "!Fleet.AnyCharacterDead()", 20);
await Host.EvalAsync("FleetCB.PlaceCustom(\"ChickenRoll\", -8f, -6f)");
await Task.Delay(500);
await Host.DoAsync("Fleet.PlaceCharacter(-8f, -5.9f);");
bool rollKill = await Until(Host, "Fleet.AnyCharacterDead()", 10);
Check("chicken roll killed the character", rollKill);

Step("Acid: kills on touch");
// KNOWN DEFECT: Acid.OnPlace never sets the component's own 'placed' flag
// (PigFarmButton does), so its trigger ignores everything; the glue-piece
// gameplay colliders are also absent on a force-placed instance. INTENDED
// behavior asserted anyway — this stays red until the block is fixed.
await RequireOn("character alive before acid", Host, "!Fleet.AnyCharacterDead()", 20);
await Host.EvalAsync("FleetCB.PlaceCustom(\"Acid\", -4f, -6f)");
await Task.Delay(500);
await Host.DoAsync("Fleet.PlaceCharacter(-4f, -5.9f);");
bool acidKill = await Until(Host, "Fleet.AnyCharacterDead()", 10);
Check("acid killed the character [KNOWN DEFECT]", acidKill);
await RequireOn("character alive again", Host, "!Fleet.AnyCharacterDead()", 20);

Step("ReCoin: collectable coin");
await Host.EvalAsync("FleetCB.PlaceCustom(\"ReCoin\", 0f, -6f)");
await Task.Delay(500);
await Host.DoAsync("Fleet.PlaceCharacter(0f, -5.9f);");
await Task.Delay(2000);
// respawn-next-round is round machinery; here the coin at least must exist
// and react to a character. COVERAGE GAP: the actual respawn across rounds.
await GoldenOn("recoin state after touch", Host, "FleetCB.PlacedCustomJson()");

await SaveScreenshot(Host, "customblocks/behaviors-field.png");

Step("back to treehouse");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await Require("in the treehouse", "Fleet.Scene() == \"TreeHouseLobby\"", 90);
