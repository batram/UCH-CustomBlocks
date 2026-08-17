using HarmonyLib;
using UnityEngine;

namespace CustomBlocks.Backgrounds.Patches
{
    [HarmonyPatch(typeof(GameState), nameof(GameState.Update))]
    static class GameStateUpdatePatch
    {
        static void Prefix(GameState __instance)
        {
            if (CustomBlocksMod.InFreePlace() && !GameState.ChatSystem.ChatMode)
            {
                // These are keyboard shortcuts, so they act on the keyboard
                // player's cursor rather than on everybody's.
                PiecePlacementCursor cursor = LayerState.ControllingCursor();
                if (cursor == null)
                {
                    return;
                }

                if (Input.GetKeyDown(CustomBlocksMod.ToggleBackgroundKey.Value))
                {
                    LayerSelectionGUI.ToggleBackgroundMode(cursor);
                }

                // The rest only act while the tool is on, so they cannot change
                // state the player currently has no controls shown for.
                if (!LayerState.For(cursor).ModeEnabled)
                {
                    return;
                }

                if (Input.GetKeyDown(CustomBlocksMod.PrevLayerKey.Value))
                {
                    LayerSelectionGUI.CycleLayer(cursor, true);
                }
                if (Input.GetKeyDown(CustomBlocksMod.SwitchLayerKey.Value))
                {
                    LayerSelectionGUI.CycleLayer(cursor, false);
                }
                if (Input.GetKeyDown(CustomBlocksMod.HighlightBlockKey.Value))
                {
                    LayerSelectionGUI.ToggleHighlight(cursor);
                }
            }
            else if (GameSettings.GetInstance().GameMode != GameState.GameMode.FREEPLAY)
            {
                LayerState.ResetToSolid();
            }
        }
    }
}
