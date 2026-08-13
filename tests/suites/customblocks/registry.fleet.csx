// #name     customblocks/registry
// #peers    1
// #describe Golden the custom block registry: stable ids, types, serialize indices.

// The registry is the contract everything else builds on: stable ids are what
// saves record, serialize indices are the session's array slots. A change here
// that is not deliberate is a save-compat break.

Step("registry");

bool loaded = await Host.EvalBoolAsync("FleetCB.ModLoaded()");
if (!loaded) Abort("CustomBlocksMod is not loaded — run with --with-plugins CustomBlocks,PigFarmButton");

await Golden("registry", "FleetCB.RegistryJson()");

// The prefab table the registry extends. Under a profile this coexists with
// the vanilla record of the same table instead of overwriting it.
Step("prefab table");
await Host.DoAsync("Fleet.ReturnToTreehouse();");
await Require("in the treehouse", "Fleet.Scene() == \"TreeHouseLobby\"", 60);

int count = await Host.EvalIntAsync("Fleet.PlaceableCount()");
Check("prefab table extended past vanilla", count > 102, $"{count} placeables");

await Golden("placeable table", "Fleet.PlaceableTable()");
