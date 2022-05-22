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
            if (ModBlocksMod.enableModBlockMode)
            {
                if (piece && piece.gameObject)
                {
                    ModBlock mbi = ModBlocksMod.EnableModBlock(piece.gameObject);
                    mbi.layer = SortingLayer.layers[ModBlocksMod.selectedLayer].name;
                }
            }
            else if (piece && piece.gameObject && ModBlocksMod.IsModBlock(piece.gameObject))
            {
                ModBlocksMod.DisableModBlock(piece.gameObject);
            }
        }
    }
}