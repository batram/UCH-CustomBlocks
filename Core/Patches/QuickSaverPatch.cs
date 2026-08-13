using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace CustomBlocks.Core.Patches
{
    [HarmonyPatch(typeof(QuickSaver), nameof(QuickSaver.MemorizeInitialLevelPlaceables))]
    static class QuickSaverPatch
    {
        // Only the mod's own hidden prefab clones must be dropped from the
        // level's initial-piece list; real level geometry stays memorized so
        // moved pieces keep their positions through save/load.
        private static bool IsModPrefabClone(QuickSaver.SaveablePiece s)
        {
            if (s == null || s.placeable == null)
            {
                return false;
            }
            GameObject go = s.placeable.gameObject;
            if ((go.hideFlags & HideFlags.DontSave) != 0)
            {
                return true;
            }
            foreach (Placeable prefab in CustomBlockRegistry.Prefabs)
            {
                if (s.placeable == prefab)
                {
                    return true;
                }
            }
            return false;
        }

        static void Postfix(QuickSaver __instance)
        {
            __instance.initialLevelPlaceables.RemoveAll(IsModPrefabClone);
        }
    }
}
