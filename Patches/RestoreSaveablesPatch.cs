using CustomBlocks.Backgrounds;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace CustomBlocks.Patches
{
    [HarmonyPatch(typeof(QuickSaver), nameof(QuickSaver.RestoreSaveables))]
    static class RestoreSaveablesPatch
    {
        static void Postfix(Dictionary<int, QuickSaver.SaveablePiece> saveables)
        {
            foreach (QuickSaver.SaveablePiece saveable in saveables.Values)
            {
                if (saveable.placeable && saveable.blockID >= CustomBlocksMod.magicBackgroundBlockNumber)
                {
                    saveable.overrideName = saveable.placeable.gameObject.name;
                    CustomBlocksMod.EnableBackgroundBlock(saveable.placeable.gameObject);
                }
                if(saveable.placeable && saveable.blockID >= CustomBlocksMod.magicCustomBlockNumber)
                {
                    saveable.blockID += Core.CustomBlock.OriginalBlockCount;
                    saveable.blockID -= CustomBlocksMod.magicCustomBlockNumber;
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

                Core.CustomBlock cb = saveable.placeable.GetComponentInChildren<Core.CustomBlock>();

                if (cb && saveable.blockID < CustomBlocksMod.magicCustomBlockNumber)
                {
                    saveable.blockID = CustomBlocksMod.magicCustomBlockNumber + cb.CustomId;
                }

                if (saveable.placeable && saveable.blockID >= CustomBlocksMod.magicBackgroundBlockNumber)
                {
                    var mbi = saveable.placeable.gameObject.GetComponent<BackgroundBlock>();
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