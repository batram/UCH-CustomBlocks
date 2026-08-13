// #name     customblocks/place-save-load
// #peers    1
// #describe Place every custom block, snapshot, clear, reload — ids stable, pieces survive, no errors.

// The log gate is armed for the whole scenario, so any game-side error or
// exception in the place/save/load path fails the run on its own.

// KNOWN DEFECT (baseline): glue-based custom blocks (RCReceiver, Acid) leave a
// GluePiece sub-element the save sweep cannot map to a main block. Allowed here
// so the baseline records it without failing the run; remove once fixed.
AllowLogErrors("Could not find main block for sub-element GluePiece");

Step("into free play");
await Host.DoAsync("Fleet.PickCharacter(\"SQUIRREL\");");
await Task.Delay(1000);
await Host.DoAsync("Fleet.StartGame(\"Farm\", \"FREEPLAY\");");
await Require("place phase reached", "Fleet.Scene() != \"TreeHouseLobby\" && Fleet.Phase() == \"PLACE\"", 90);

Step("place one of each custom block");
string[] blocks =
[
    "OneRoundWood", "ReCoin", "MultiStart", "RCReceiver", "RCTransmitter",
    "FloatyCloud", "PigFarmButton", "PigDirt", "ChickenRoll", "Acid",
];
for (int i = 0; i < blocks.Length; i++)
{
    string placed = await Host.EvalAsync(
        $"FleetCB.PlaceCustom(\"{blocks[i]}\", {(-9 + i * 2)}f, 4f)");
    // the real Instantiate path names clones "<prefab>(Clone)"
    Check($"placed {blocks[i]}", placed.Trim('"').StartsWith(blocks[i]), placed);
}
await Task.Delay(1000);
await Golden("placed before save", "FleetCB.PlacedCustomJson()");
await SaveScreenshot(Host, "customblocks/placed-blocks.png");

Step("snapshot");
string b64 = (await Host.EvalAsync("FleetCB.SnapshotB64()")).Trim('"');
Check("snapshot produced", b64.Length > 0, $"{b64.Length} base64 chars");

// The ids on disk must be the stable magic ids (5000+), not session slots.
await Golden("snapshot block ids", $"FleetCB.SnapshotBlockIdsB64(\"{b64}\")");

Step("clear and reload");
await Host.DoAsync("FleetCB.ClearLevel();");
await Task.Delay(1000);

bool loadOk = await Host.EvalBoolAsync($"FleetCB.LoadSnapshotB64(\"{b64}\")");
Check("snapshot loaded", loadOk);
await Task.Delay(2000);

// The same set of blocks, at the same serialize indices, must come back.
await Golden("placed after load", "FleetCB.PlacedCustomJson()");
await SaveScreenshot(Host, "customblocks/reloaded-blocks.png");

Step("back to treehouse");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await Require("in the treehouse", "Fleet.Scene() == \"TreeHouseLobby\"", 90);
