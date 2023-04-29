using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ModBlocks.CustomBlocks
{
    [HarmonyPatch(typeof(TabletBlockList), nameof(TabletBlockList.Initialize))]
    static class TabletBlockListPatch
    {
        static void Postfix(TabletBlockList __instance, bool isDisabled)
        {
            if (CustomBlock.Blocks == null)
            {
                Debug.Log("Blocks null");
                return;
            }
            var c = __instance.tabletBlocks.Length;
            var shrink = 0;

            Array.Resize(ref __instance.tabletBlocks, __instance.tabletBlocks.Length + CustomBlock.Blocks.Count);

            foreach (Placeable go in CustomBlock.Blocks.Values)
            {
                if (go)
                {
                    var cb = go.gameObject.GetComponent<CustomBlock>();

                    if (cb)
                    {
                        var basePick = __instance.tabletBlocksByIndex[cb.BasedId];

                        if (basePick)
                        {
                            var clone = GameObject.Instantiate(__instance.tabletBlocksByIndex[cb.BasedId]);
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
                    if (blockSerializeIndex >= 102)
                    {
                        __instance.tabletBlocksByIndex[blockSerializeIndex] = tabletBlock;
                    }
                }
            }

            __instance.ReorderList();
        }
    }
}