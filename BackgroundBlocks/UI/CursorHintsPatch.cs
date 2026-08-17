using CustomBlocks.Backgrounds.UI;
using HarmonyLib;

namespace CustomBlocks.Backgrounds.Patches
{
    // Cursors arrive by network spawn, respawn and free-play player switching, so
    // rather than pick one lifecycle hook and hope it covers every path, the hint
    // rows are attached from the cursor's own tick. Ensure() returns immediately
    // once a cursor already has them.
    [HarmonyPatch(typeof(PiecePlacementCursor), "FixedUpdate")]
    static class CursorLayerHintsAttachPatch
    {
        static void Postfix(PiecePlacementCursor __instance)
        {
            CursorLayerHints.Ensure(__instance);
        }
    }
}
