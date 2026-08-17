using CustomBlocks.Backgrounds.UI;
using UnityEngine;

namespace CustomBlocks.Backgrounds.Patches
{
    // Every background-layer state change funnels through here, whether it came
    // from a keybind or anywhere else, so the paths cannot disagree about what a
    // change entails. The cursor hint rows are told what to display; they never
    // decide.
    //
    // Each call acts on one player's state and one player's cursor. The keyboard
    // shortcuts resolve that player through LayerState.ControllingCursor.
    static class LayerSelectionGUI
    {
        public static void NotifyChanged(string name, bool value)
        {
            UserMessageManager.Instance.UserMessage($"{name} {(value ? "Enabled" : "Disabled")}");
        }

        // Switches the background tool on or off: shows or hides the layer and
        // highlight controls, and moves between the solid layer and the background
        // layer that was being worked on.
        public static void ToggleBackgroundMode(PiecePlacementCursor cursor)
        {
            LayerState state = LayerState.For(cursor);
            int layer = state.ToggleMode();

            NotifyChanged("Background Block Mode", state.ModeEnabled);

            SetLayer(cursor, layer);
        }

        public static void ToggleHighlight(PiecePlacementCursor cursor)
        {
            LayerState state = LayerState.For(cursor);
            SetHighlight(cursor, !state.HighlightLayer);
        }

        public static void SetHighlight(PiecePlacementCursor cursor, bool on)
        {
            LayerState.For(cursor).HighlightLayer = on;

            NotifyChanged("Highlight Layer", on);

            Apply(cursor);
        }

        public static void CycleLayer(PiecePlacementCursor cursor, bool reverse)
        {
            int count = LayerState.CycleCount;
            if (count <= 1)
            {
                return;
            }

            int position = LayerState.PositionOf(LayerState.For(cursor).SelectedLayer);
            position = ((position + (reverse ? -1 : 1)) % count + count) % count;

            SetLayer(cursor, LayerState.LayerAtPosition(position));
        }

        public static void SetLayer(PiecePlacementCursor cursor, int layer)
        {
            if (layer != LayerState.Solid && (layer < 0 || layer >= SortingLayer.layers.Length))
            {
                return;
            }

            LayerState.For(cursor).Select(layer);

            UserMessageManager.Instance.UserMessage(
                "Layer selected: " + LayerState.LayerName(layer).PadLeft(20, ' '));

            Apply(cursor);
        }

        static void Apply(PiecePlacementCursor cursor)
        {
            CursorLayerHints.RefreshAll();
            UpdatePicked(cursor);
            PlaceableHighlighter.HighlightUpdateAll();
        }

        // Applies a player's selection to the piece that player is holding.
        //
        // This used to walk every PiecePlacementCursor in the scene, which meant
        // one player's layer choice rewrote the piece held by every other player,
        // remote ones included.
        public static void UpdatePicked(PiecePlacementCursor cursor)
        {
            if (cursor == null || cursor.Piece == null)
            {
                return;
            }

            LayerState state = LayerState.For(cursor);
            if (state.IsBackground)
            {
                BackgroundBlock mbi = CustomBlocksMod.EnableBackgroundBlock(cursor.Piece.gameObject);
                mbi.layer = state.LayerName();
            }
            else
            {
                CustomBlocksMod.DisableBackgroundBlock(cursor.Piece.gameObject);
            }
        }
    }
}
