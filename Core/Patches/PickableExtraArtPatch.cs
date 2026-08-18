using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace CustomBlocks.Core.Patches
{
    // Show, hide and re-order the parts of a custom pickable the game does not
    // know it owns. See CustomBlock.ExtraArtOf for why they exist and what the
    // game misses.
    //
    // Both patches are postfixes on the game's own decisions rather than a
    // per-frame watcher: the extras change exactly when the artwork they belong
    // to changes, and nothing runs on the frames in between.
    static class ExtraArt
    {
        internal static List<CustomBlock.ExtraArt> For(PickableBlock pick)
        {
            List<CustomBlock.ExtraArt> extras;
            if (pick == null || !CustomBlock.ExtraArtOf.TryGetValue(pick, out extras)) return null;

            extras.RemoveAll(e => e == null || e.Part == null);
            return extras.Count > 0 ? extras : null;
        }
    }

    // PickableBlock.Enable hides ArtSprites, crossOut, buttonText and
    // twitchLogo. Everything else a block brought with it keeps drawing over
    // whichever page the reader turned to.
    //
    // Hiding a caption needs the Canvas switched off, and sorting order is not
    // a substitute: pages further into the book get LOWER numbers (paper at -54
    // and -64 against our -40), so a canvas left enabled draws over every page
    // ahead of its own.
    //
    // But switching a Canvas off is also what ruined the caption. A UI Text with
    // resizeTextForBestFit bakes its glyphs at a size it computes when its mesh
    // rebuilds, and Graphic.canvas is null while no live canvas is above it — so
    // a rebuild in that state settles on 6 where the honest answer is 69, and
    // keeps those glyphs. The caption is first baked during CreatePickableBlock,
    // off-page, which is exactly that state.
    //
    // So: hide it, and re-measure it on the way back in. Enable(true) is the one
    // moment everything a Text needs is true at once — on a page, live canvas
    // above it, rect finished. Both halves live in ONE postfix because the order
    // matters: the canvas has to be back on before the rebuild, and two separate
    // Harmony patches on the same method have no guaranteed order between them.
    [HarmonyPatch(typeof(PickableBlock), nameof(PickableBlock.Enable), new System.Type[] { typeof(bool) })]
    static class PickableExtraArtEnablePatch
    {
        static void Postfix(PickableBlock __instance, bool enable)
        {
            List<CustomBlock.ExtraArt> extras = ExtraArt.For(__instance);
            if (extras != null)
            {
                foreach (CustomBlock.ExtraArt extra in extras)
                {
                    Renderer r = extra.Part as Renderer;
                    if (r != null)
                    {
                        if (r.enabled != enable) r.enabled = enable;
                        continue;
                    }

                    Canvas c = extra.Part as Canvas;
                    if (c != null && c.enabled != enable) c.enabled = enable;
                }
            }

            List<UnityEngine.UI.Text> captions;
            if (!enable || !CustomBlock.CaptionsOf.TryGetValue(__instance, out captions)) return;

            for (int i = captions.Count - 1; i >= 0; i--)
            {
                UnityEngine.UI.Text text = captions[i];
                if (text == null) { captions.RemoveAt(i); continue; }

                // Only with a live canvas above it — a rebuild without one is
                // the bad bake this exists to undo.
                if (text.canvas != null) text.SetAllDirty();
            }
        }
    }

    // InventoryPage.setPageLayer re-orders each pickable through its SortOrder,
    // and SortOrder is a snapshot of the renderers that existed when the
    // pickable awoke. Anything added afterwards keeps the order of the page
    // state it was born into and draws in front of the page being turned to —
    // for as long as it takes something else to hide it.
    [HarmonyPatch(typeof(InventoryPage), nameof(InventoryPage.setPageLayer))]
    static class InventoryPageSetPageLayerPatch
    {
        static void Postfix(InventoryPage __instance, int num)
        {
            foreach (IPickable pickable in __instance.pickableOnPage)
            {
                List<CustomBlock.ExtraArt> extras = ExtraArt.For(pickable as PickableBlock);
                if (extras == null) continue;

                foreach (CustomBlock.ExtraArt extra in extras)
                {
                    // Same arithmetic as SortOrder.SpriteInfo.newSortOrder: the
                    // page's number plus the order the piece was authored with.
                    Renderer r = extra.Part as Renderer;
                    if (r != null)
                    {
                        r.sortingOrder = num + extra.AuthoredOrder;
                        continue;
                    }

                    Canvas c = extra.Part as Canvas;
                    if (c != null) c.sortingOrder = num + extra.AuthoredOrder;
                }
            }
        }
    }
}
