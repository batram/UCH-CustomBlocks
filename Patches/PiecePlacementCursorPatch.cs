using HarmonyLib;
using System;
using UnityEngine;

namespace BackgroundBlocks.Patches
{
    [HarmonyPatch(typeof(PiecePlacementCursor), nameof(PiecePlacementCursor.SetPiece))]
    static class PiecePlacementCursorSetPiecePatch
    {
        static void Postfix(Placeable piece)
        {
            if (BackgroundBlocksMod.enableBackgroundMode)
            {
                if (piece != null)
                {
                    BackgroundBlocksMod.EnableBackground(piece.gameObject, false, true);
                }
            }
            else if (piece != null && BackgroundBlocksMod.IsBackground(piece.gameObject))
            {
                BackgroundBlocksMod.DisableBackground(piece.gameObject);
            }
        }
    }
}