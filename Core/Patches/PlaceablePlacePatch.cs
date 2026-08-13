using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace CustomBlocks.Core.Patches
{
    [HarmonyPatch(typeof(Placeable), nameof(Placeable.Place), new Type[] { typeof(int), typeof(bool), typeof(bool) })]
    static class PlaceablePlacePatch
    {
        static void Postfix(Placeable __instance, int playerNumber, bool sendEvent, bool force = false)
        {
            CustomBlock cb = __instance.gameObject.GetComponent<CustomBlock>();
            if (cb != null)
            {
                // clones inherit the prefab's HideAndDontSave; a placed block
                // must be visible to the save sweeps
                CustomBlocksMod.UnhideInstance(__instance.gameObject);
                cb.OnPlace(__instance, playerNumber, sendEvent, force);
            }
        }
    }

    [HarmonyPatch(typeof(HoneyPiece), nameof(HoneyPiece.Place), new Type[] { typeof(int), typeof(bool), typeof(bool) })]
    static class HoneyPlacePatch
    {
        static void Postfix(HoneyPiece __instance, int playerNumber, bool sendEvent, bool force = false)
        {
            __instance.MainBlock.gameObject.GetComponent<CustomBlock>()?.OnPlace(__instance, playerNumber, sendEvent, force);
        }
    }
}