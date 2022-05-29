using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ModBlocks.Patches
{
    [HarmonyPatch(typeof(GameState), nameof(GameState.Update))]
    static class GameStateUpdatePatch
    {
        static void Prefix(GameState __instance)
        {
            if (ModBlocksMod.InFreePlace() && !GameState.ChatSystem.ChatMode)
            {
                if (Input.GetKeyDown(KeyCode.G))
                {
                    ToggleBackgroundMode();
                }
                if (Input.GetKeyDown(KeyCode.L))
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        Debug.Log("Shifty");
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
        }

        static void ToggleBackgroundMode()
        {
            ModBlocksMod.enableModBlockMode = !ModBlocksMod.enableModBlockMode;

            LayerSelectionGUI.NotifyChanged("Mod Block Mode", ModBlocksMod.enableModBlockMode);

            LayerSelectionGUI.UpdatePicked();
            PlaceableHighlighter.UpdateAll();
        }

        static void ToggleLayerHighlight()
        {
            var toggle = GameObject.Find("HighlightToggle")?.GetComponent<Toggle>();
            toggle.isOn = !toggle.isOn; ;
        }

        static void SwitchLayer(bool reverse = false)
        {
            ModBlocksMod.selectedLayer = (ModBlocksMod.selectedLayer + (reverse ? -1 : 1)) % SortingLayer.layers.Length;
            if (ModBlocksMod.selectedLayer < 0)
            {
                ModBlocksMod.selectedLayer = SortingLayer.layers.Length - 1;
            }

            var dropy = GameObject.Find("LayerDropdown")?.GetComponent<Dropdown>();
            dropy.value = ModBlocksMod.selectedLayer;
        }
    }
}