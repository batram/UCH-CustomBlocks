// #name     customblocks/collider-modes
// #peers    1
// #describe Dynamically replaced custom colliders follow their inherited ColliderModeControl.

AllowLogErrors("CustomLevelPortal.UpdateAppearanceForClient");
AllowLogErrors("LevelSelectController.SetupLobbyAfterWait");
AllowLogErrors("UnityEngine.Light.set_color");
AllowLogErrors("UndergroundComputer.UpdateVisibility");
AllowLogErrors("TreehouseGrow.SetNewState");

Step("enter free play and pick the rounded custom block");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await UntilAll("Fleet.Scene() == \"TreeHouseLobby\"", 60);
await Host.DoAsync("Fleet.PickCharacter(\"SQUIRREL\");");
await Task.Delay(1000);
await Host.DoAsync("Fleet.StartGame(\"Farm\", \"FREEPLAY\");");
await UntilAll("Fleet.Scene() != \"TreeHouseLobby\" && Fleet.Phase() == \"PLACE\"", 90);
await Host.DoAsync("FleetCB.SwitchToPlace();");
await Task.Delay(500);

string picked = await Host.EvalAsync("FleetCB.PickFromBook(\"OneRoundWood\")");
Check("OneRoundWood picked from the real book", picked.Contains("OneRoundWood_Pick"), picked);
await RequireOn("cursor holds OneRoundWood", Host,
    "FleetCB.CursorHolds().StartsWith(\"OneRoundWood\")", 15);

Step("its post-clone circle follows the inherited collider control");
string state = await Host.EvalAsync("FleetCB.HeldNoColliderLeak()");
Check("NoColliders disables every controlled collider",
    state.EndsWith("enabledDuringNone=0"), state);

Step("pick ChickenRoll — its template's ColliderCache entry is stale the same way");
string rollPicked = await Host.EvalAsync("FleetCB.PickFromBook(\"ChickenRoll\")");
Check("ChickenRoll picked from the real book", rollPicked.Contains("ChickenRoll_Pick"), rollPicked);
await RequireOn("cursor holds ChickenRoll", Host,
    "FleetCB.CursorHolds().StartsWith(\"ChickenRoll\")", 15);

Step("ChickenRoll's post-clone circle follows the inherited collider control");
string rollState = await Host.EvalAsync("FleetCB.HeldNoColliderLeak()");
Check("NoColliders disables every controlled ChickenRoll collider",
    rollState.EndsWith("enabledDuringNone=0"), rollState);
