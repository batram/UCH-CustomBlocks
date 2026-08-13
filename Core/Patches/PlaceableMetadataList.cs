using HarmonyLib;
using System;
using UnityEngine;

namespace CustomBlocks.Core.Patches
{
    [HarmonyPatch(typeof(PlaceableMetadataList), nameof(PlaceableMetadataList.Awake))]
    static class PlaceableMetadataListAwakePatch
    {
        static void Prefix(PlaceableMetadataList __instance)
        {
            if (__instance && __instance.allBlockPrefabs != null && __instance.allBlockPrefabs.Length != 0)
            {
                CustomBlockRegistry.InitBlocks();

                var c = __instance.allBlockPrefabs.Length;
                Array.Resize(ref __instance.allBlockPrefabs, __instance.allBlockPrefabs.Length + CustomBlockRegistry.Count);
                foreach (Placeable cb in CustomBlockRegistry.Prefabs)
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
            MetadataListSync.Sync(__instance);
        }
    }

    [HarmonyPatch(typeof(PlaceableMetadataList), "get_CachedBlockMap")]
    static class PlaceableMetadataListCachedBlockMapPatch
    {
        static void Prefix(PlaceableMetadataList __instance)
        {
            MetadataListSync.Sync(__instance);
        }
    }

    // Non-singleton copies (level-scene lists) borrow the extended prefab
    // arrays from the singleton. Guarded: the getters can run before the
    // singleton's Awake (review finding #15).
    static class MetadataListSync
    {
        internal static void Sync(PlaceableMetadataList target)
        {
            PlaceableMetadataList source = PlaceableMetadataList.instance;
            if (source == null || target == null || source == target
                || source.allBlockPrefabs == null || target.allBlockPrefabs == null)
            {
                return;
            }
            if (source.allBlockPrefabs.Length != target.allBlockPrefabs.Length)
            {
                target.allBlockPrefabs = source.allBlockPrefabs;
                target.nameToIndexMap = source.NameToIndexMap;
                target.cachedBlockMap = source.CachedBlockMap;
            }
        }
    }
}