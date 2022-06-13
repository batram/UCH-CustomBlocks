using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ModBlocks.CustomBlocks
{
    [HarmonyPatch(typeof(Placeable), nameof(Placeable.Place), new Type[] { typeof(int), typeof(bool), typeof(bool) })]
    static class PlaceablePlacePatch
    {
        static void Postfix(Placeable __instance, int playerNumber, bool sendEvent, bool force = false)
        {
            __instance.gameObject.GetComponent<CustomBlock>()?.OnPlace(__instance, playerNumber, sendEvent, force);
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