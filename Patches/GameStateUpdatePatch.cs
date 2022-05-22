using HarmonyLib;
using UnityEngine;

namespace ModBlocks.Patches
{
    [HarmonyPatch(typeof(GameState), nameof(GameState.Update))]
    static class GameStateUpdatePatch
    {
        static void Prefix(GameState __instance)
        {
            if(ModBlocksMod.InFreePlace())
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

            NotifyChanged("Mod Block Mode", ModBlocksMod.enableModBlockMode);

            UpdatePicked();
            PlaceableHighlighter.UpdateAll();
        }

        static void ToggleLayerHighlight()
        {
            ModBlocksMod.highlightSelectedLayer = !ModBlocksMod.highlightSelectedLayer;

            NotifyChanged("Highlight Layer", ModBlocksMod.highlightSelectedLayer);

            PlaceableHighlighter.UpdateAll();
        }

        static void UpdatePicked()
        {
            foreach (PiecePlacementCursor cur in UnityEngine.Object.FindObjectsOfType<PiecePlacementCursor>())
            {
                if (cur.Piece)
                {
                    if (ModBlocksMod.enableModBlockMode)
                    {
                        ModBlock mbi = ModBlocksMod.EnableModBlock(cur.Piece.gameObject);
                        mbi.layer = SortingLayer.layers[ModBlocksMod.selectedLayer].name;
                    }
                    else
                    {
                        ModBlocksMod.DisableModBlock(cur.Piece.gameObject);
                    }
                }
            }
        }

        static void SwitchLayer(bool reverse = false)
        {
            ModBlocksMod.selectedLayer = (ModBlocksMod.selectedLayer + (reverse ? -1 : 1)) % SortingLayer.layers.Length;
            if (ModBlocksMod.selectedLayer < 0)
            {
                ModBlocksMod.selectedLayer = SortingLayer.layers.Length - 1;
            }
            UserMessageManager.Instance.UserMessage("Layer selected: " + SortingLayer.layers[ModBlocksMod.selectedLayer].name.PadLeft(20, ' '));
            UpdatePicked();
            PlaceableHighlighter.UpdateAll();
         }

        static void NotifyChanged(string name, bool value)
        {
            UserMessageManager.Instance.UserMessage($"{name} {(value ? "Enabled" : "Disabled")}");
        }
    }
}