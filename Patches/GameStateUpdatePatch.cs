using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CustomBlocks.Patches
{
    [HarmonyPatch(typeof(GameState), nameof(GameState.Update))]
    static class GameStateUpdatePatch
    {
        static void Prefix(GameState __instance)
        {
            if (CustomBlocksMod.InFreePlace() && !GameState.ChatSystem.ChatMode)
            {
                if (Input.GetKeyDown(KeyCode.G))
                {
                    ToggleBackgroundMode();
                }
                if (Input.GetKeyDown(KeyCode.L))
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
                if (Input.GetKeyDown(KeyCode.H))
                {
                    ToggleLayerHighlight();
                }
            }
            else if (GameSettings.GetInstance().GameMode != GameState.GameMode.FREEPLAY)
            {
                CustomBlocksMod.enableCustomBlockMode = false;
            }
        }

        static void ToggleBackgroundMode()
        {
            CustomBlocksMod.enableCustomBlockMode = !CustomBlocksMod.enableCustomBlockMode;

            LayerSelectionGUI.NotifyChanged("Mod Block Mode", CustomBlocksMod.enableCustomBlockMode);

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