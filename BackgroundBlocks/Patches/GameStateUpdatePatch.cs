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
                if (Input.GetKeyDown(CustomBlocksMod.SwitchLayerKey.Value))
                {
                    bool reverse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    LayerSelectionGUI.CycleLayer(reverse);
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
