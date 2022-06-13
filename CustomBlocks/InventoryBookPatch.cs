using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModBlocks.CustomBlocks
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
                    GameObject.DontDestroyOnLoad(inventoryPage);
                    inventoryPage.name = "Inventory Mod Blocks";
                    var items = inventoryPage.transform.Find("Items");

                    foreach (Transform child in items)
                    {
                        GameObject.Destroy(child.gameObject);
                    }

                    CustomBlock.InitBlocks();

                    foreach (Placeable cblock in CustomBlock.Blocks.Values)
                    {
                        cblock.GetComponent<CustomBlock>()?.AddToInventoryPage(inventoryPage);
                    }

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

                    // place new Mod Blocks page, before background selection
                    __instance.InventoryPages[__instance.InventoryPages.Length - 1] = __instance.InventoryPages[__instance.InventoryPages.Length - 2];
                    __instance.InventoryPages[__instance.InventoryPages.Length - 2] = __instance.InventoryPages[__instance.InventoryPages.Length - 3];
                    __instance.InventoryPages[__instance.InventoryPages.Length - 3] = inventoryPage;
                }
            }
        }
    }
}