using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace BackgroundBlocks.Patches
{
    [HarmonyPatch(typeof(PlaceableMetadataList), nameof(PlaceableMetadataList.GetPrefabForPlaceableIndex))]
    static class PlaceableMetadataListPatch
    {
        static void Prefix(ref int idx)
        {
            if(idx > BackgroundBlocksMod.magicBackgroundNumber)
            {
                idx -= BackgroundBlocksMod.magicBackgroundNumber;
            }
        }
    }
}