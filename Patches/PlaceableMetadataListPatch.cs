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
                int serializeIndex;
                if (Core.CustomBlockRegistry.TryGetSerializeIndexForSaveId(idx - CustomBlocksMod.magicCustomBlockNumber, out serializeIndex))
                {
                    idx = serializeIndex;
                }
                else
                {
                    Debug.LogError("CustomBlocks: no block registered for save id " + (idx - CustomBlocksMod.magicCustomBlockNumber));
                }
            }
        }
    }
}