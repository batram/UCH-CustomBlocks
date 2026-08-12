using HarmonyLib;
using System;
using UnityEngine;

namespace CustomBlocks.Backgrounds.Patches
{
    [HarmonyPatch(typeof(PiecePlacementCursor), nameof(PiecePlacementCursor.SetPiece))]
    static class PiecePlacementCursorSetPiecePatch
    {
        static void Postfix(Placeable piece)
        {
            if (piece && piece.gameObject)
            {
                if (CustomBlocksMod.enableBackgroundMode)
                {
                    BackgroundBlock mbi = CustomBlocksMod.EnableBackgroundBlock(piece.gameObject);
                    mbi.layer = SortingLayer.layers[CustomBlocksMod.selectedLayer].name;
                }
                else if (CustomBlocksMod.IsBackgroundBlock(piece.gameObject))
                {
                    CustomBlocksMod.DisableBackgroundBlock(piece.gameObject);
                }
                PlaceableHighlighter.HighlightUpdateBlock(piece);
            }
        }
    }
}