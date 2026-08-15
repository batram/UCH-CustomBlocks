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
                            // Parent BEFORE building the visuals: InitializeSprites
                            // resolves its material and sort order through
                            // GetComponentInParent<Tablet>().
                            clone.transform.parent = __instance.tabletBlocks[0].transform.parent;
                            clone.transform.localScale = new Vector3(1, 1, 1);
                            BuildTileVisuals(clone, cb, basePick);
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

        // Make a cloned tile actually render OUR block.
        //
        // A TabletBlock's visuals are built by TabletBlock.InitializeSprites:
        // it wipes spriteHolder, instantiates the pickable under it, reassigns
        // materials and sort order, caches ArtSprites, and takes the holder's
        // scale/offset from the pickable's BlockProbabilityScale/Offset. The
        // method has NO runtime callers — shipped tiles are baked in the Unity
        // editor — so a clone keeps the BASE block's artwork forever. Assigning
        // pickableBlockPrefab, which is all this patch used to do, changes a
        // reference nothing reads again: every custom tile rendered its base
        // block (RCTransmitter showed a boxing glove, ReCoin and PigDirt both
        // showed the vanilla coin).
        static void BuildTileVisuals(TabletBlock tile, CustomBlock cb, TabletBlock baseTile)
        {
            if (tile.GetComponentInParent<Tablet>() == null)
            {
                Debug.LogError("CustomBlocks: tablet tile for " + cb.Name
                    + " has no Tablet parent; leaving base artwork in place");
                return;
            }

            // The Material parameter is unused by the 1.13 body — it pulls
            // PickableBlockSpriteMaterial off the parent Tablet instead.
            tile.InitializeSprites(cb.PickableBlock, null);

            // Initialize() reads .color on every ArtSprites entry when the
            // pickable has noneDefaultColors. Entries can be dead: vanilla
            // Thwomp ships a null in its array, and InitializeSprites itself
            // DestroyImmediates the pick colliders it found.
            if (tile.ArtSprites != null)
            {
                List<SpriteRenderer> live = new List<SpriteRenderer>();
                foreach (SpriteRenderer sr in tile.ArtSprites)
                {
                    if (sr) live.Add(sr);
                }
                tile.ArtSprites = live.ToArray();
            }

            tile.Initialize();

            // Our pickable prefabs sit in their Disable()d state — the book
            // page calls Enable() when it shows one. InitializeSprites destroys
            // the PickableBlock component off its clone, so nothing will ever
            // enable these renderers and the tile shows correct-but-invisible
            // art. The crossout is included deliberately: vanilla tiles keep
            // its renderer enabled and hide the GameObject instead, so leaving
            // it disabled means the "block turned off" cross never draws.
            if (tile.spriteHolder == null || tile.spriteHolder.childCount == 0) return;
            Transform art = tile.spriteHolder.GetChild(0);
            foreach (SpriteRenderer sr in art.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.enabled = true;
            }

            FitTile(tile, cb, baseTile, art);
        }

        // Size and place the art inside its 300x300 tile.
        //
        // Default is to match the vanilla block this one is based on and centre
        // the result; CustomBlock.TabletScale / TabletOffset opt out per block.
        static void FitTile(TabletBlock tile, CustomBlock cb, TabletBlock baseTile, Transform art)
        {
            RectTransform rect = tile.transform as RectTransform;
            if (rect == null) return;

            // AddToInventoryPage assigns transform.parent, which preserves world
            // position and therefore rewrites the shared prefab's localScale for
            // the BOOK page. Start from a known state instead of inheriting it.
            art.localScale = Vector3.one;
            tile.spriteHolder.localScale = Vector3.one * 100f;
            tile.spriteHolder.localPosition = Vector3.zero;

            float scale;
            if (cb.TabletScale.HasValue)
            {
                scale = cb.TabletScale.Value;
            }
            else
            {
                Bounds mine, theirs;
                if (!ArtBounds(art, tile.crossOut, out mine)
                    || baseTile == null || baseTile.spriteHolder == null
                    || baseTile.spriteHolder.childCount == 0
                    || !ArtBounds(baseTile.spriteHolder.GetChild(0), baseTile.crossOut, out theirs))
                {
                    return;
                }
                float mineMax = Mathf.Max(mine.size.x, mine.size.y);
                if (mineMax <= 0.0001f) return;
                scale = Mathf.Max(theirs.size.x, theirs.size.y) / mineMax;
            }

            tile.pickableBlockPrefab.BlockProbabilityScale = scale;
            // Written directly as well as through the field: TabletBlock.Update
            // only recomputes spriteHolder.localScale while scaleAlpha is
            // changing, i.e. on a hover transition. Setting the field alone
            // leaves the tile at its old size until the player happens to hover
            // it. At rest Update produces exactly this value, so nothing jumps.
            tile.spriteHolder.localScale = Vector3.one * 100f * scale;

            Vector2 offset;
            if (cb.TabletOffset.HasValue)
            {
                offset = cb.TabletOffset.Value * 100f;
            }
            else
            {
                Bounds scaled;
                if (!ArtBounds(art, tile.crossOut, out scaled)) return;
                Vector3 centre = rect.InverseTransformPoint(scaled.center);
                Vector2 target = new Vector2(rect.rect.width * (0.5f - rect.pivot.x),
                                             rect.rect.height * (0.5f - rect.pivot.y));
                offset = new Vector2(target.x - centre.x, target.y - centre.y);
            }

            tile.spriteHolder.localPosition = new Vector3(offset.x, offset.y, 0f);
            tile.pickableBlockPrefab.BlockProbabilityOffset = offset / 100f;

            FitCrossout(tile, baseTile, rect);
        }

        // The "block is off" cross lives inside spriteHolder, so it rides the
        // fit transform above. Vanilla gets away with scaling its cross by the
        // holder because its holder scale tracks the block's footprint; ours
        // tracks the ARTWORK, and our art sits proportionally smaller inside
        // its pickable — so PigDirt's 2.11x fit blew its cross up to 2.2 units
        // against Coin's 1.04 and it spilled across the neighbouring tiles.
        // Size and place it to match the base block's cross instead.
        static void FitCrossout(TabletBlock tile, TabletBlock baseTile, RectTransform rect)
        {
            if (tile.crossOut == null || baseTile == null || baseTile.crossOut == null) return;

            Bounds mine, theirs;
            if (!RendererBounds(tile.crossOut, out mine)) return;
            if (!RendererBounds(baseTile.crossOut, out theirs)) return;

            float mineMax = Mathf.Max(mine.size.x, mine.size.y);
            if (mineMax <= 0.0001f) return;
            float ratio = Mathf.Max(theirs.size.x, theirs.size.y) / mineMax;
            tile.crossOut.transform.localScale *= ratio;

            // Re-measure after scaling and drop it on the tile centre. Done in
            // world space so it does not matter where in the pickable's
            // hierarchy the cross happens to hang.
            if (!RendererBounds(tile.crossOut, out mine)) return;
            Vector3 centre = rect.TransformPoint(new Vector3(
                rect.rect.width * (0.5f - rect.pivot.x),
                rect.rect.height * (0.5f - rect.pivot.y), 0f));
            tile.crossOut.transform.position += centre - mine.center;
        }

        // Analytic bounds for a single renderer. Works on inactive objects,
        // which Renderer.bounds does not — and the cross is inactive whenever
        // the block is enabled, which is most of the time.
        static bool RendererBounds(SpriteRenderer sr, out Bounds bounds)
        {
            bounds = new Bounds();
            if (sr == null || sr.sprite == null) return false;
            Bounds local = sr.sprite.bounds;
            Matrix4x4 m = sr.transform.localToWorldMatrix;
            for (int i = 0; i < 4; i++)
            {
                Vector3 corner = new Vector3((i & 1) == 0 ? local.min.x : local.max.x,
                                             (i & 2) == 0 ? local.min.y : local.max.y, 0f);
                Vector3 world = m.MultiplyPoint3x4(corner);
                if (i == 0) bounds = new Bounds(world, Vector3.zero);
                else bounds.Encapsulate(world);
            }
            return true;
        }

        // World-space bounds of a tile's artwork, excluding the crossout overlay.
        //
        // Deliberately NOT Renderer.bounds: everything here runs inside one
        // Harmony postfix, and a renderer's cached bounds can still describe the
        // transform as it was before the line above moved it. The sprite's own
        // local bounds through the current matrix is always up to date.
        static bool ArtBounds(Transform art, SpriteRenderer crossOut, out Bounds bounds)
        {
            bounds = new Bounds();
            bool any = false;
            foreach (SpriteRenderer sr in art.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.sprite == null) continue;
                if (crossOut && sr.gameObject == crossOut.gameObject) continue;
                Bounds local = sr.sprite.bounds;
                Matrix4x4 m = sr.transform.localToWorldMatrix;
                for (int i = 0; i < 4; i++)
                {
                    Vector3 corner = new Vector3((i & 1) == 0 ? local.min.x : local.max.x,
                                                 (i & 2) == 0 ? local.min.y : local.max.y, 0f);
                    Vector3 world = m.MultiplyPoint3x4(corner);
                    if (!any) { bounds = new Bounds(world, Vector3.zero); any = true; }
                    else bounds.Encapsulate(world);
                }
            }
            return any;
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