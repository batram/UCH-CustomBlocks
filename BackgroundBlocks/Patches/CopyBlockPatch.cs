using CustomBlocks.Backgrounds.UI;
using HarmonyLib;
using UnityEngine;

namespace CustomBlocks.Backgrounds.Patches
{
    // Copying a block copies its layer.
    //
    // Copy hands the player a fresh piece built from the inventory prefab, not a
    // clone of the block under the cursor, so it arrives with no BackgroundBlock
    // and SetPiece would give it whatever layer the player happened to be on. A
    // copy that differs from its source is just a bug with extra steps, so the
    // cursor adopts the source first — the same rule picking a block up follows,
    // reusing the same AdoptFrom.
    //
    // PickBlockEvent.ReuseTransformPlaceable is the block being copied. It is null
    // for an ordinary pick out of the book, which makes it an exact discriminator
    // rather than a guess: the game populates it precisely when it means "reuse
    // that block's transform", which only Copy does.
    //
    // A prefix, because GameEventManager.SendEvent dispatches synchronously
    // (GameEvent/GameEventManager.cs:58) — the piece is handed over and SetPiece
    // runs inside this call, so the state has to be right before it proceeds.
    [HarmonyPatch(typeof(GameEvent.GameEventManager), nameof(GameEvent.GameEventManager.SendEvent))]
    static class CopyBlockPatch
    {
        static void Prefix(GameEvent.GameEvent e)
        {
            var pick = e as GameEvent.PickBlockEvent;
            if (pick == null || pick.ReuseTransformPlaceable == null)
            {
                return;
            }

            PiecePlacementCursor cursor = CursorFor(pick.PlayerNumber);
            if (cursor == null)
            {
                return;
            }

            LayerState state = LayerState.For(cursor);
            if (state.AdoptFrom(pick.ReuseTransformPlaceable.GetComponent<BackgroundBlock>()))
            {
                CursorLayerHints.RefreshAll();

                // The selected layer decides what the highlight dims, so the whole
                // view has to be recomputed, not just the copied block.
                PlaceableHighlighter.HighlightUpdateAll();
            }
        }

        // PlayerNumber is the network number; only this client's own cursors are
        // ours to touch, and matching the number picks the right one when two
        // local players are building at once.
        static PiecePlacementCursor CursorFor(int networkNumber)
        {
            foreach (PiecePlacementCursor cursor in Object.FindObjectsOfType<PiecePlacementCursor>())
            {
                if (cursor != null && cursor.hasAuthority && cursor.networkNumber == networkNumber)
                {
                    return cursor;
                }
            }

            return null;
        }
    }
}
