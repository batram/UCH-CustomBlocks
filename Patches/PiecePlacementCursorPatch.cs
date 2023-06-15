using HarmonyLib;
using System;
using UnityEngine;

namespace CustomBlocks.Patches
{
    [HarmonyPatch(typeof(PiecePlacementCursor), nameof(PiecePlacementCursor.SetPiece))]
    static class PiecePlacementCursorSetPiecePatch
    {
        static void Postfix(Placeable piece)
        {
            if (piece && piece.gameObject)
            {
                if (CustomBlocksMod.enableCustomBlockMode)
                {
                    CustomBlock mbi = CustomBlocksMod.EnableCustomBlock(piece.gameObject);
                    mbi.layer = SortingLayer.layers[CustomBlocksMod.selectedLayer].name;
                }
                else if (CustomBlocksMod.IsCustomBlock(piece.gameObject))
                {
                    CustomBlocksMod.DisableCustomBlock(piece.gameObject);
                }
                PlaceableHighlighter.HighlightUpdateBlock(piece);
            }
        }
    }
}