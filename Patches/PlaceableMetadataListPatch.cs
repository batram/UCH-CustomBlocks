using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace CustomBlocks.Patches
{
    [HarmonyPatch(typeof(PlaceableMetadataList), nameof(PlaceableMetadataList.GetPrefabForPlaceableIndex))]
    static class PlaceableMetadataListPatch
    {
        static void Prefix(ref int idx)
        {
            if (idx >= CustomBlocksMod.magicBackgroundBlockNumber)
            {
                idx -= CustomBlocksMod.magicBackgroundBlockNumber;
            }
            if (idx >= CustomBlocksMod.magicCustomBlockNumber)
            {
                idx += CustomBlocks.CustomBlock.OriginalBlockCount;
                idx -= CustomBlocksMod.magicCustomBlockNumber;
            }
        }
    }
}