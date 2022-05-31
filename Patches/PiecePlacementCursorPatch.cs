using HarmonyLib;
using System;
using UnityEngine;

namespace ModBlocks.Patches
{
    [HarmonyPatch(typeof(PiecePlacementCursor), nameof(PiecePlacementCursor.SetPiece))]
    static class PiecePlacementCursorSetPiecePatch
    {
        static void Postfix(Placeable piece)
        {
            if (piece && piece.gameObject)
            {
                if (ModBlocksMod.enableModBlockMode)
                {
                    ModBlock mbi = ModBlocksMod.EnableModBlock(piece.gameObject);
                    mbi.layer = SortingLayer.layers[ModBlocksMod.selectedLayer].name;
                }
                else if (ModBlocksMod.IsModBlock(piece.gameObject))
                {
                    ModBlocksMod.DisableModBlock(piece.gameObject);
                }
                PlaceableHighlighter.HighlightUpdateBlock(piece);
            }
        }
    }
}