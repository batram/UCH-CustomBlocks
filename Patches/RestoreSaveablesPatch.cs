using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace BackgroundBlocks.Patches
{
    [HarmonyPatch(typeof(QuickSaver), nameof(QuickSaver.RestoreSaveables))]
    static class RestoreSaveablesPatch
    {
        static void Postfix(Dictionary<int, QuickSaver.SaveablePiece> saveables)
        {
            foreach (QuickSaver.SaveablePiece saveable in saveables.Values)
            {
                if (saveable.placeable && saveable.blockID >= BackgroundBlocksMod.magicBackgroundNumber)
                {
                    BackgroundBlocksMod.EnableBackground(saveable.placeable.gameObject);
                    saveable.overrideName = saveable.placeable.gameObject.name;
                }
            }
        }
    }

    [HarmonyPatch(typeof(QuickSaver), nameof(QuickSaver.GetSaveablesFromMetadata))]
    static class GetSaveablesFromMetadataPatch
    {
        static void Postfix(ref List<QuickSaver.SaveablePiece> __result)
        {
            foreach (QuickSaver.SaveablePiece saveable in __result)
            {
                if (saveable.placeable && saveable.blockID >= BackgroundBlocksMod.magicBackgroundNumber)
                {
                    saveable.overrideName = saveable.placeable.gameObject.name;
                }
            }
        }
    }
}