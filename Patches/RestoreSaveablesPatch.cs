using CustomBlocks.Backgrounds;
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
                if (saveable.placeable == null)
                {
                    continue;
                }
                // restore clones the mod's hidden prefabs; the instances must
                // be visible to FindObjectsOfType again or they neither
                // re-save nor get cleared with the level
                CustomBlocksMod.UnhideInstance(saveable.placeable.gameObject);
                if (saveable.blockID >= CustomBlocksMod.magicBackgroundBlockNumber)
                {
                    // the marker json travels in the saved name; Awake parses
                    // it back into layer/alpha
                    CustomBlocksMod.EnableBackgroundBlock(saveable.placeable.gameObject);

                    int baseId = saveable.blockID - CustomBlocksMod.magicBackgroundBlockNumber;
                    if (baseId >= CustomBlocksMod.magicCustomBlockNumber)
                    {
                        int serializeIndex;
                        if (Core.CustomBlockRegistry.TryGetSerializeIndexForSaveId(
                                baseId - CustomBlocksMod.magicCustomBlockNumber, out serializeIndex))
                        {
                            saveable.blockID = CustomBlocksMod.magicBackgroundBlockNumber + serializeIndex;
                        }
                    }
                }
                else if (saveable.blockID >= CustomBlocksMod.magicCustomBlockNumber)
                {
                    int serializeIndex;
                    if (Core.CustomBlockRegistry.TryGetSerializeIndexForSaveId(
                            saveable.blockID - CustomBlocksMod.magicCustomBlockNumber, out serializeIndex))
                    {
                        saveable.blockID = serializeIndex;
                    }
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
                if (saveable.placeable == null)
                {
                    continue;
                }

                // ids in saves must be the stable magic ids, never this
                // session's raw slot; background-ness composes on top:
                //   custom            -> 5000 + CustomId
                //   background        -> 9000 + vanilla index
                //   custom+background -> 9000 + 5000 + CustomId
                bool background = saveable.blockID >= CustomBlocksMod.magicBackgroundBlockNumber;
                int baseId = background
                    ? saveable.blockID - CustomBlocksMod.magicBackgroundBlockNumber
                    : saveable.blockID;

                Core.CustomBlock cb = saveable.placeable.GetComponent<Core.CustomBlock>();
                if (cb && baseId < CustomBlocksMod.magicCustomBlockNumber)
                {
                    baseId = CustomBlocksMod.magicCustomBlockNumber + cb.CustomId;
                }
                saveable.blockID = (background ? CustomBlocksMod.magicBackgroundBlockNumber : 0) + baseId;

                if (background)
                {
                    BackgroundBlock mbi = saveable.placeable.gameObject.GetComponent<BackgroundBlock>();
                    if (mbi)
                    {
                        // layer/alpha ride along in the saved name; the live
                        // object gets its clean name back right away
                        mbi.PersistInGOName();
                        saveable.overrideName = mbi.gameObject.name;
                        mbi.ClearNameData();
                    }
                }
            }
        }
    }
}
