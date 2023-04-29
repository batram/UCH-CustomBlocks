using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace ModBlocks.Patches
{
    [HarmonyPatch(typeof(PlaceableMetadataList), nameof(PlaceableMetadataList.GetPrefabForPlaceableIndex))]
    static class PlaceableMetadataListPatch
    {
        static void Prefix(ref int idx)
        {
            if (idx >= ModBlocksMod.magicBackgroundBlockNumber)
            {
                idx -= ModBlocksMod.magicBackgroundBlockNumber;
            }
            if (idx >= ModBlocksMod.magicCustomBlockNumber)
            {
                idx += CustomBlocks.CustomBlock.OriginalBlockCount;
                idx -= ModBlocksMod.magicCustomBlockNumber;
            }
        }
    }
}