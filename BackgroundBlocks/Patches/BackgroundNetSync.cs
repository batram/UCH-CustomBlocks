using CustomBlocks.Core;
using HarmonyLib;
using System;
using UnityEngine;

namespace CustomBlocks.Backgrounds.Patches
{
    // Background-ness is applied by the LOCAL SetPiece patch of the player
    // who picked the block, so remote copies materialize as plain foreground
    // blocks. When a background block finishes placing, the placing peer
    // announces it on CustomBlockNet and every peer applies the same
    // component + layer to its copy of the block.
    [HarmonyPatch(typeof(Placeable), nameof(Placeable.Place), new Type[] { typeof(int), typeof(bool), typeof(bool) })]
    static class BackgroundPlaceSyncPatch
    {
        static void Postfix(Placeable __instance, int playerNumber, bool sendEvent, bool force = false)
        {
            // network echoes re-place with sendEvent=false — only the
            // initiating peer announces
            if (!sendEvent || __instance == null)
            {
                return;
            }
            if (!CustomBlocksMod.IsBackgroundBlock(__instance.gameObject))
            {
                return;
            }
            BackgroundBlock mbi = __instance.GetComponent<BackgroundBlock>();
            if (mbi == null)
            {
                return;
            }
            int layerIndex = -1;
            for (int i = 0; i < SortingLayer.layers.Length; i++)
            {
                if (SortingLayer.layers[i].name == mbi.layer)
                {
                    layerIndex = i;
                    break;
                }
            }
            CustomBlockNet.Send(CustomBlocksMod.netActionBackground,
                -1, __instance.ID, new Vector3(layerIndex, mbi.alpha, 0));
        }
    }

    public static class BackgroundNetSync
    {
        public static void Register()
        {
            CustomBlockNet.RegisterGlobalAction(CustomBlocksMod.netActionBackground, Apply);
        }

        static void Apply(MsgCustomBlockEvent e)
        {
            Placeable p = CustomBlockNet.FindPlaceable(e.TargetID);
            if (p == null)
            {
                return;
            }
            BackgroundBlock mbi = CustomBlocksMod.EnableBackgroundBlock(p.gameObject);
            int layerIndex = (int)e.Payload.x;
            if (layerIndex >= 0 && layerIndex < SortingLayer.layers.Length)
            {
                mbi.layer = SortingLayer.layers[layerIndex].name;
            }
            mbi.alpha = e.Payload.y;
        }
    }
}
