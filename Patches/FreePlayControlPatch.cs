using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using GameEvent;
using HarmonyLib;
using UnityEngine;

namespace CustomBlocks.Patches
{
    [HarmonyPatch(typeof(FreePlayControl), nameof(FreePlayControl.handleEvent))]
    static class FreePlayControlPatch
    {
        static void Prefix(GameEvent.GameEvent e)
        {
            if (e.GetType() == typeof(FreePlayPlayerSwitchEvent))
            {
                FreePlayPlayerSwitchEvent freePlayPlayerSwitchEvent = e as FreePlayPlayerSwitchEvent;
                ToggleLayersAndCollider(freePlayPlayerSwitchEvent.Phase);
            }
            if (e.GetType() == typeof(StartPhaseEvent))
            {
                StartPhaseEvent startPhaseEvent = e as StartPhaseEvent;
                ToggleLayersAndCollider(startPhaseEvent.Phase);
            }
        }

        // switches colliders and sortingLayer depending on GamePhase
        // so pieces are visible in selectable in place phase
        static void ToggleLayersAndCollider(GameControl.GamePhase phase)
        {
            foreach (CustomBlock customblock in GameObject.FindObjectsOfType<CustomBlock>())
            {
                if (phase != GameControl.GamePhase.PLAY)
                {
                    PlaceableHighlighter.HighlightAlpha(customblock.placeable);

                    // collider needs to be active, to be selectable
                    customblock.SetCollide(true);

                    // change sortingLayer to be visible in place phase
                    customblock.SetLayer("Default");
                }
                else
                {
                    PlaceableHighlighter.ResetAlpha(customblock.placeable);
                    customblock.SetCollide(false);
                    customblock.RestoreLayer();
                }
            }

            PlaceableHighlighter.HighlightUpdateAll();
        }
    }
}