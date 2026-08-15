using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace CustomBlocks.Core.Patches
{
    [HarmonyPatch(typeof(InventoryBook), nameof(InventoryBook.Awake))]
    static class InventoryBookAwakePatch
    {
        static void Postfix(InventoryBook __instance)
        {
            Debug.Log("InventoryBook Awake: " + __instance.InventoryPages.Length);
            if (__instance.InventoryPages.Length >= 4)
            {
                Array.Resize(ref __instance.InventoryPages, __instance.InventoryPages.Length + 1);

                if (__instance.InventoryPages[3])
                {
                    InventoryPage inventoryPage = UnityEngine.Object.Instantiate<InventoryPage>(__instance.InventoryPages[3], __instance.InventoryPages[3].transform.parent);
                    inventoryPage.name = "Inventory Mod Blocks";
                    var items = inventoryPage.transform.Find("Items");

                    foreach (Transform child in items)
                    {
                        GameObject.Destroy(child.gameObject);
                    }
                    // Destroy is deferred; drop the cloned page's references to
                    // the dying items now, or setPageLayer trips over them
                    // later with a MissingReferenceException (review finding #8)
                    inventoryPage.pickableOnPage.RemoveAll(p =>
                        p == null || (p is Component && (((Component)p) == null || ((Component)p).transform.IsChildOf(items))));
                    inventoryPage.textOnPage.RemoveAll(t =>
                        t == null || t.transform.IsChildOf(items));
                    // InventoryPage.Awake fills FIVE lists from the same
                    // children, not two. setPageLayer walks spriteRenders and
                    // sortingGroups with no null guard, so leaving the dead
                    // ones behind is a MissingReferenceException waiting for a
                    // page turn (measured: 113 of 115 renderers dead).
                    inventoryPage.imagesOnPage.RemoveAll(i =>
                        i == null || i.transform.IsChildOf(items));
                    inventoryPage.spriteRenders.RemoveAll(s =>
                        s == null || s.transform.IsChildOf(items));
                    inventoryPage.sortingGroups.RemoveAll(g =>
                        g == null || g.transform.IsChildOf(items));

                    CustomBlockRegistry.InitBlocks();

                    foreach (Placeable cblock in CustomBlockRegistry.Prefabs)
                    {
                        cblock.GetComponent<CustomBlock>()?.AddToInventoryPage(inventoryPage);
                    }

                    // Lay the page out instead of leaving each block wherever
                    // its prefab's world position happened to put it.
                    var layout = inventoryPage.gameObject.AddComponent<BookPageLayout>();
                    layout.Items = items;
                    layout.Paper = inventoryPage.pagePaper;

                    var text = inventoryPage.transform.Find("TextCanvas/Moving Things");
                    if (text)
                    {
                        text.name = "Custom Mod Blocks";
                        var text_field = text.GetComponent<Text>();
                        if (text_field)
                        {
                            text_field.text = text.name;
                        }
                    }

                    // Insert after the last real BLOCK inventory page, in front
                    // of the trailing special pages — the position the
                    // pre-refactor code hardcoded as "before background
                    // selection".
                    //
                    // pageType alone is not the discriminator (review #9 got
                    // this wrong): on a blank level the book also carries the
                    // BlankLevelOnly customization pages, and those reuse
                    // InventoryPage4/5. The last of them ("Inventory G",
                    // Select Background) has NO Next arrow, because vanilla
                    // ends the book there — so anything appended behind it is
                    // reachable by GotoPage and unreachable by a player.
                    // BlankLevelOnly is what actually separates the two kinds.
                    int insertAt = __instance.InventoryPages.Length - 1;
                    for (int i = __instance.InventoryPages.Length - 2; i >= 0; i--)
                    {
                        InventoryPage p = __instance.InventoryPages[i];
                        if (p != null
                            && !p.BlankLevelOnly
                            && p.pageType >= InventoryPage.PageTypes.InventoryPage1
                            && p.pageType <= InventoryPage.PageTypes.InventoryPage9)
                        {
                            insertAt = i + 1;
                            break;
                        }
                    }
                    for (int i = __instance.InventoryPages.Length - 1; i > insertAt; i--)
                    {
                        __instance.InventoryPages[i] = __instance.InventoryPages[i - 1];
                    }
                    __instance.InventoryPages[insertAt] = inventoryPage;
                    // keep the transform order in step with the page order
                    if (insertAt > 0 && __instance.InventoryPages[insertAt - 1] != null
                        && __instance.InventoryPages[insertAt - 1].transform.parent == inventoryPage.transform.parent)
                    {
                        inventoryPage.transform.SetSiblingIndex(
                            __instance.InventoryPages[insertAt - 1].transform.GetSiblingIndex() + 1);
                    }
                }
            }
        }
    }
}