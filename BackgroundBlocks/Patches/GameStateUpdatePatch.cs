using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

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
                    ToggleBackgroundMode();
                }
                if (Input.GetKeyDown(CustomBlocksMod.SwitchLayerKey.Value))
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        SwitchLayer(true);
                    }
                    else
                    {
                        SwitchLayer();
                    }
                }
                if (Input.GetKeyDown(CustomBlocksMod.HighlightBlockKey.Value))
                {
                    ToggleLayerHighlight();
                }
            }
            else if (GameSettings.GetInstance().GameMode != GameState.GameMode.FREEPLAY)
            {
                CustomBlocksMod.enableBackgroundMode = false;
            }
        }

        static void ToggleBackgroundMode()
        {
            CustomBlocksMod.enableBackgroundMode = !CustomBlocksMod.enableBackgroundMode;

            LayerSelectionGUI.NotifyChanged("Background Block Mode", CustomBlocksMod.enableBackgroundMode);

            LayerSelectionGUI.UpdatePicked();
            PlaceableHighlighter.HighlightUpdateAll();
        }

        static void ToggleLayerHighlight()
        {
            var toggle = GameObject.Find("HighlightToggle")?.GetComponent<Toggle>();
            toggle.isOn = !toggle.isOn; ;
        }

        static void SwitchLayer(bool reverse = false)
        {
            CustomBlocksMod.selectedLayer = (CustomBlocksMod.selectedLayer + (reverse ? -1 : 1)) % SortingLayer.layers.Length;
            if (CustomBlocksMod.selectedLayer < 0)
            {
                CustomBlocksMod.selectedLayer = SortingLayer.layers.Length - 1;
            }

            var dropy = GameObject.Find("LayerDropdown")?.GetComponent<Dropdown>();
            dropy.value = CustomBlocksMod.selectedLayer;
        }
    }
}