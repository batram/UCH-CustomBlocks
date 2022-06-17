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
                if(saveable.placeable && saveable.blockID >= ModBlocksMod.magicCustomBlockNumber)
                {
                    saveable.blockID += CustomBlocks.CustomBlock.OriginalBlockCount;
                    saveable.blockID -= ModBlocksMod.magicCustomBlockNumber;
                }
            }
        }
    }

    [HarmonyPatch(typeof(QuickSaver), nameof(QuickSaver.GetSaveablesFromMetadata))]
    static class GetSaveablesFromMetadataPatch
    {
        static void Prefix(ref List<PlaceableMetadata> allPlaceables)
        {
            //TODO: filter out crap
        }


        static void Postfix(ref List<QuickSaver.SaveablePiece> __result)
        {
            foreach (QuickSaver.SaveablePiece saveable in __result)
            {
                Debug.Log("saveable.blockID: " + saveable.blockID);

                CustomBlocks.CustomBlock cb = saveable.placeable.GetComponentInChildren<CustomBlocks.CustomBlock>();

                if (cb && saveable.blockID < ModBlocksMod.magicCustomBlockNumber)
                {
                    saveable.blockID = ModBlocksMod.magicCustomBlockNumber + cb.CustomId;
                }

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