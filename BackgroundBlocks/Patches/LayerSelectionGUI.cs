using CustomBlocks.Backgrounds.UI;
using UnityEngine;

namespace CustomBlocks.Backgrounds.Patches
{
    // Every background-layer state change funnels through here, whether it came
    // from a keybind or anywhere else, so the paths cannot disagree about what a
    // change entails. The cursor hint rows are told what to display; they never
    // decide.
    static class LayerSelectionGUI
    {
        public static void NotifyChanged(string name, bool value)
        {
            UserMessageManager.Instance.UserMessage($"{name} {(value ? "Enabled" : "Disabled")}");
        }

        public static void ToggleBackgroundMode()
        {
            CustomBlocksMod.enableBackgroundMode = !CustomBlocksMod.enableBackgroundMode;

            NotifyChanged("Background Block Mode", CustomBlocksMod.enableBackgroundMode);

            Apply();
        }

        public static void ToggleHighlight()
        {
            SetHighlight(!CustomBlocksMod.highlightSelectedLayer);
        }

        public static void SetHighlight(bool on)
        {
            CustomBlocksMod.highlightSelectedLayer = on;

            NotifyChanged("Highlight Layer", on);

            Apply();
        }

        public static void CycleLayer(bool reverse)
        {
            int count = SortingLayer.layers.Length;
            if (count == 0)
            {
                return;
            }

            SetLayer(((CustomBlocksMod.selectedLayer + (reverse ? -1 : 1)) % count + count) % count);
        }

        public static void SetLayer(int index)
        {
            if (index < 0 || index >= SortingLayer.layers.Length)
            {
                return;
            }

            CustomBlocksMod.selectedLayer = index;

            UserMessageManager.Instance.UserMessage(
                "Layer selected: " + SortingLayer.layers[index].name.PadLeft(20, ' '));

            Apply();
        }

        static void Apply()
        {
            CursorLayerHints.RefreshAll();
            UpdatePickedLocal();
            PlaceableHighlighter.HighlightUpdateAll();
        }

        // Applies the current selection to the cursors this client owns.
        //
        // This used to walk every PiecePlacementCursor in the scene, which meant
        // one player's layer choice rewrote the piece held by every other player,
        // remote ones included.
        public static void UpdatePickedLocal()
        {
            foreach (PiecePlacementCursor cursor in Object.FindObjectsOfType<PiecePlacementCursor>())
            {
                if (cursor != null && cursor.hasAuthority)
                {
                    UpdatePicked(cursor);
                }
            }
        }

        public static void UpdatePicked(PiecePlacementCursor cursor)
        {
            if (cursor == null || cursor.Piece == null)
            {
                return;
            }

            if (CustomBlocksMod.enableBackgroundMode)
            {
                BackgroundBlock mbi = CustomBlocksMod.EnableBackgroundBlock(cursor.Piece.gameObject);
                mbi.layer = SortingLayer.layers[CustomBlocksMod.selectedLayer].name;
            }
            else
            {
                CustomBlocksMod.DisableBackgroundBlock(cursor.Piece.gameObject);
            }
        }
    }
}
