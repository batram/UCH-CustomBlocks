using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace CustomBlocks.CustomBlocks
{
    [HarmonyPatch(typeof(TwitchChatController), nameof(TwitchChatController.Awake))]
    static class TwitchChatControllerAwakePatch
    {
        static void Prefix(TwitchChatController __instance)
        {
            Debug.Log("TwitchChatController Awake: " + PlaceableMetadataList.instance);
            if (PlaceableMetadataList.instance)
            {
                __instance.MetaList = PlaceableMetadataList.instance;
            }
        }
    }

    [HarmonyPatch(typeof(TwitchChatController), nameof(TwitchChatController.UpdateAllowedItems))]
    static class TwitchChatControllerPatch
    {
        static void Prefix(TwitchChatController __instance)
        {
            VersusControl component = ((GameObject)__instance.versusControllerPrefab).GetComponent<VersusControl>();
            if (component != null)
            {
                component.MetaList = PlaceableMetadataList.instance;
            }
        }
    }

}