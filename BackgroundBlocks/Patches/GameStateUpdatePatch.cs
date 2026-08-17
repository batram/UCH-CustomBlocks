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
                if (Input.GetKeyDown(CustomBlocksMod.ToggleBackgroundKey.Value))
                {
                    LayerSelectionGUI.ToggleBackgroundMode();
                }
                if (Input.GetKeyDown(CustomBlocksMod.PrevLayerKey.Value))
                {
                    LayerSelectionGUI.CycleLayer(true);
                }
                if (Input.GetKeyDown(CustomBlocksMod.SwitchLayerKey.Value))
                {
                    LayerSelectionGUI.CycleLayer(false);
                }
                if (Input.GetKeyDown(CustomBlocksMod.HighlightBlockKey.Value))
                {
                    LayerSelectionGUI.ToggleHighlight();
                }
            }
            else if (GameSettings.GetInstance().GameMode != GameState.GameMode.FREEPLAY)
            {
                CustomBlocksMod.enableBackgroundMode = false;
            }
        }
    }
}
