using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using GameEvent;
using HarmonyLib;
using UnityEngine;

namespace BackgroundBlocks.Patches
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
            foreach (Placeable placeable in GameObject.FindObjectsOfType<Placeable>())
            {
                if (BackgroundBlocksMod.IsBackground(placeable.gameObject))
                {
                    if (phase != GameControl.GamePhase.PLAY)
                    {
                        BackgroundBlocksMod.HighlightAlpha(placeable);

                        // collider needs to be active, to be selectable
                        BackgroundBlocksMod.SetCollide(placeable.gameObject, true);

                        // change sortingLayer to be visible in place phase
                        BackgroundBlocksMod.SetLayer(placeable, "Default");
                    }
                    else
                    {
                        BackgroundBlocksMod.ResetAlpha(placeable);
                        BackgroundBlocksMod.SetCollide(placeable.gameObject, false);
                        BackgroundBlocksMod.SetLayer(placeable, "Background 1");
                    }

                }
            }
        }
    }
}