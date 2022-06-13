using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace ModBlocks.Patches
{
    [HarmonyPatch(typeof(QuickSaver), nameof(QuickSaver.RestoreSaveables))]
    static class RestoreSaveablesPatch
    {
        static void Postfix(Dictionary<int, QuickSaver.SaveablePiece> saveables)
        {
            foreach (QuickSaver.SaveablePiece saveable in saveables.Values)
            {
                if (saveable.placeable && saveable.blockID >= ModBlocksMod.magicBackgroundBlockNumber)
                {
                    saveable.overrideName = saveable.placeable.gameObject.name;
                    ModBlocksMod.EnableModBlock(saveable.placeable.gameObject);
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
                Debug.Log("saveable.blockID: " + saveable.blockID);

                if (saveable.placeable && saveable.blockID >= ModBlocksMod.magicBackgroundBlockNumber)
                {
                    var mbi = saveable.placeable.gameObject.GetComponent<ModBlock>();
                    if (mbi)
                    {
                        mbi.PersistInGOName();
                    }
                    Debug.Log("GetSaveablesFromMetadata: " + mbi.name);
                    saveable.overrideName = mbi.name;
                } 
            }
        }
    }
}