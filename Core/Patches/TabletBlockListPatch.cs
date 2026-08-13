using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace CustomBlocks.Core.Patches
{
    [HarmonyPatch(typeof(TabletBlockList), nameof(TabletBlockList.Initialize))]
    static class TabletBlockListPatch
    {
        static void Postfix(TabletBlockList __instance, bool isDisabled)
        {
            if (!CustomBlockRegistry.Initialized || CustomBlockRegistry.Count == 0)
            {
                Debug.Log("No custom blocks initialized yet");
                return;
            }
            var c = __instance.tabletBlocks.Length;
            var shrink = 0;

            Array.Resize(ref __instance.tabletBlocks, __instance.tabletBlocks.Length + CustomBlockRegistry.Count);

            foreach (Placeable go in CustomBlockRegistry.Prefabs)
            {
                if (go)
                {
                    var cb = go.gameObject.GetComponent<CustomBlock>();

                    if (cb)
                    {
                        var basePick = BaseTabletBlock(__instance, cb);

                        if (basePick)
                        {
                            var clone = GameObject.Instantiate(basePick);
                            GameObject.DontDestroyOnLoad(clone);
                            clone.pickableBlockPrefab = cb.PickableBlock;
                            cb.FixSprite(clone.gameObject.transform.Find("SpriteBox/PickableBlockPivot/" + cb.BasePickableBlockName + "(Clone)/ArtHolder/Sprite"));
                            clone.transform.parent = __instance.tabletBlocks[0].transform.parent;
                            clone.transform.localScale = new Vector3(1, 1, 1);
                            __instance.tabletBlocks[c] = clone;
                            c += 1;
                        }
                        else
                        {
                            shrink += 1;
                        }
                    }
                }
            }

            if (shrink != 0)
            {
                Array.Resize(ref __instance.tabletBlocks, __instance.tabletBlocks.Length - shrink);
            }

            for (int j = 0; j < __instance.tabletBlocks.Length; j++)
            {
                TabletBlock tabletBlock = __instance.tabletBlocks[j];
                if (tabletBlock)
                {
                    tabletBlock.disabled = isDisabled;

                    int blockSerializeIndex = tabletBlock.pickableBlockPrefab.blockSerializeIndex;
                    if (blockSerializeIndex >= CustomBlockRegistry.OriginalBlockCount
                        && blockSerializeIndex < __instance.tabletBlocksByIndex.Length)
                    {
                        __instance.tabletBlocksByIndex[blockSerializeIndex] = tabletBlock;
                    }
                }
            }

            __instance.ReorderList();
        }

        // Resolve the vanilla tablet entry to clone by the base block's NAME;
        // the BasedId literal is only a bounds-checked fallback (review #6).
        static TabletBlock BaseTabletBlock(TabletBlockList list, CustomBlock cb)
        {
            GameObject[] prefabs = PlaceableMetadataList.Instance ? PlaceableMetadataList.Instance.allBlockPrefabs : null;
            if (prefabs != null && !string.IsNullOrEmpty(cb.BasePlaceableName))
            {
                int limit = Math.Min(CustomBlockRegistry.OriginalBlockCount, Math.Min(prefabs.Length, list.tabletBlocksByIndex.Length));
                for (int i = 0; i < limit; i++)
                {
                    if (prefabs[i] && prefabs[i].name == cb.BasePlaceableName && list.tabletBlocksByIndex[i])
                    {
                        return list.tabletBlocksByIndex[i];
                    }
                }
            }
            if (cb.BasedId >= 0 && cb.BasedId < list.tabletBlocksByIndex.Length)
            {
                return list.tabletBlocksByIndex[cb.BasedId];
            }
            return null;
        }
    }
}