using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace CustomBlocks.CustomBlocks
{
    [HarmonyPatch(typeof(QuickSaver), nameof(QuickSaver.MemorizeInitialLevelPlaceables))]
    static class QuickSaverPatch
    {
        private static bool EndsWithSaurus(QuickSaver.SaveablePiece s)
        {
            return true;
        }

        static void Postfix(QuickSaver __instance)
        {
            __instance.initialLevelPlaceables.RemoveAll(EndsWithSaurus);
        }
    }
}