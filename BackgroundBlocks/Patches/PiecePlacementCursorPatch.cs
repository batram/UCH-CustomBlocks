using CustomBlocks.Backgrounds.UI;
using HarmonyLib;
using UnityEngine;

namespace CustomBlocks.Backgrounds.Patches
{
    [HarmonyPatch(typeof(PiecePlacementCursor), nameof(PiecePlacementCursor.SetPiece))]
    static class PiecePlacementCursorSetPiecePatch
    {
        // A block that already exists in the level keeps what it is; only a fresh
        // one out of the inventory takes the player's current choice.
        //
        // PickupPiece hands SetPiece the very instance that was standing in the
        // level (PiecePlacementCursor.cs:1384), so this used to rewrite it: with
        // background mode off it destroyed the BackgroundBlock outright, turning a
        // background block solid just for nudging it, and with the mode on it
        // moved the block to whatever layer the player happened to be on. Picking
        // a block up and putting it back now leaves the level as it was.
        //
        // The cursor adopts a background block's layer instead, which doubles as an
        // eyedropper: nudge a block on some layer and the next fresh block
        // continues there. A solid block has nothing to adopt and leaves the
        // player's settings alone (see LayerState.AdoptFrom).
        //
        // Placed is the discriminator the game itself uses to tell these apart (it
        // picks "Stash" over "Inventory" on it in UIUpdate). Placeable.PickUp does
        // not clear it, so it still reads true here.
        static void Postfix(PiecePlacementCursor __instance, Placeable piece)
        {
            if (piece == null || piece.gameObject == null)
            {
                return;
            }

            LayerState state = LayerState.For(__instance);

            if (piece.Placed)
            {
                if (state.AdoptFrom(piece.gameObject.GetComponent<BackgroundBlock>()))
                {
                    CursorLayerHints.RefreshAll();

                    // The selected layer decides what the highlight dims, so the
                    // whole view has to be recomputed, not just this block.
                    PlaceableHighlighter.HighlightUpdateAll();
                    return;
                }
            }
            else if (state.IsBackground)
            {
                BackgroundBlock mbi = CustomBlocksMod.EnableBackgroundBlock(piece.gameObject);
                mbi.layer = state.LayerName();
            }
            else if (CustomBlocksMod.IsBackgroundBlock(piece.gameObject))
            {
                CustomBlocksMod.DisableBackgroundBlock(piece.gameObject);
            }

            PlaceableHighlighter.HighlightUpdateBlock(piece);
        }
    }
}
