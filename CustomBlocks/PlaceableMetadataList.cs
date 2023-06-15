using HarmonyLib;
using System;
using UnityEngine;

namespace CustomBlocks.CustomBlocks
{
    [HarmonyPatch(typeof(PlaceableMetadataList), nameof(PlaceableMetadataList.Awake))]
    static class PlaceableMetadataListAwakePatch
    {
        static void Prefix(PlaceableMetadataList __instance)
        {
            if (__instance && __instance.allBlockPrefabs != null && __instance.allBlockPrefabs.Length != 0)
            {
                CustomBlock.InitBlocks();

                var c = __instance.allBlockPrefabs.Length;
                Array.Resize(ref __instance.allBlockPrefabs, __instance.allBlockPrefabs.Length + CustomBlock.Blocks.Count);
                foreach (Placeable cb in CustomBlock.Blocks.Values)
                {
                    if (cb)
                    {
                        GameObject.DontDestroyOnLoad(cb.gameObject);
                        __instance.allBlockPrefabs[c] = cb.gameObject;
                        c += 1;
                    }
                }

                __instance.nameToIndexMap = null;
                __instance.cachedBlockMap = null;
            }
            else
            {
                Debug.Log("PlaceableMetadataList Awake: noped" + __instance.allBlockPrefabs);
            }
        }
    }

    [HarmonyPatch(typeof(PlaceableMetadataList), "get_NameToIndexMap")]
    static class PlaceableMetadataListNameToIndexMapPatch
    {
        static void Prefix(PlaceableMetadataList __instance)
        {
            if (PlaceableMetadataList.instance.allBlockPrefabs.Length != __instance.allBlockPrefabs.Length)
            {
                __instance.allBlockPrefabs = PlaceableMetadataList.instance.allBlockPrefabs;
                __instance.nameToIndexMap = PlaceableMetadataList.instance.NameToIndexMap;
                __instance.cachedBlockMap = PlaceableMetadataList.instance.CachedBlockMap;
            }
        }
    }

    [HarmonyPatch(typeof(PlaceableMetadataList), "get_CachedBlockMap")]
    static class PlaceableMetadataListCachedBlockMapPatch
    {
        static void Prefix(PlaceableMetadataList __instance)
        {
            if (PlaceableMetadataList.instance.allBlockPrefabs.Length != __instance.allBlockPrefabs.Length)
            {
                __instance.allBlockPrefabs = PlaceableMetadataList.instance.allBlockPrefabs;
                __instance.nameToIndexMap = PlaceableMetadataList.instance.NameToIndexMap;
                __instance.cachedBlockMap = PlaceableMetadataList.instance.CachedBlockMap;
            }
        }
    }
}