using HarmonyLib;

namespace CustomBlocks.Core.Patches
{
    // Keep a custom block's clickable area on top of its artwork.
    //
    // The pick collider arrives from the cloned base block — that block's
    // footprint, sitting at the pickable's origin — while the artwork is moved
    // and resized independently by each block's CreatePickableBlock. Measured
    // on the book page, Acid's and RCReceiver's hitboxes had drifted further
    // than their own height from their art, so the thing you see and the thing
    // you can pick up did not overlap at all.
    //
    // Why here and not straight after CreatePickableBlock: the artwork's
    // extents are not settled at creation. PickableBlock's own refresh
    // multiplies each renderer's alpha by DisabledObjectAlpha, so the glue
    // rig's StickingBlock and RotatingBlock fade to fully transparent over
    // several frames. Measured at creation they are still opaque and get
    // counted, which inflated Acid's box to the whole 1.79x2.91 rig; measured
    // once the page is actually showing the block they are invisible and
    // correctly ignored. Enable(true) is the game's own "this is now on
    // display" signal, so it is the honest place to look.
    //
    // Re-running on every Enable is deliberate: it costs a bounds calculation
    // per block per page-show and it is self-correcting if anything later
    // moves the art.
    [HarmonyPatch(typeof(PickableBlock), nameof(PickableBlock.Enable), new System.Type[] { typeof(bool) })]
    static class PickableBlockEnablePatch
    {
        static void Postfix(PickableBlock __instance, bool enable)
        {
            if (!enable || __instance == null) return;

            CustomBlock owner;
            if (!CustomBlock.OwnerOf.TryGetValue(__instance, out owner) || owner == null) return;

            PickColliderAligner aligner = __instance.GetComponent<PickColliderAligner>();
            if (aligner == null)
            {
                aligner = __instance.gameObject.AddComponent<PickColliderAligner>();
                aligner.Owner = owner;
                aligner.Pick = __instance;
            }
            // Enable(true) has just reset every renderer's alpha to 1, so this
            // is the start of a settle, not the end of one. Hand it to the
            // aligner rather than measuring here.
            aligner.Rearm();
        }
    }
}
