using System.Collections.Generic;
using UnityEngine;

namespace CustomBlocks.Backgrounds
{
    // Background-layer state, held per local player.
    //
    // This was three statics on the plugin, which meant one player's layer choice
    // rewrote the piece held by every other player — remote ones included — and
    // every cursor's hint rows showed the same state no matter whose they were.
    // What a player places is their own business.
    //
    // Keyed on Cursor.localNumber rather than the cursor instance: cursors are
    // respawned across phases and player switches, and the state should survive
    // that. Nothing here is networked — a remote player's choices live in their
    // own client, and what actually crosses the wire is the placed block's layer.
    public class LayerState
    {
        // The ordinary, collidable state, modelled as a layer so that solidity is
        // just another position in the cycle rather than a separate mode bolted
        // beside it. It is not a sorting layer: a solid block is one with no
        // BackgroundBlock at all, drawing wherever the game puts it and keeping
        // its colliders. Picking up a normal block selects this.
        public const int Solid = -1;

        // What Solid is called on screen. There is also a real sorting layer named
        // "Default" (twice) — see the curation item in docs/TODO.md.
        public const string SolidName = "Default";

        // Whether the player has the background tool switched on (the Background
        // key). This is what puts the layer and highlight controls on the cursor,
        // and it is deliberately NOT the same question as IsBackground below:
        // selecting the solid pseudo-layer — by cycling to it, or by picking up an
        // ordinary block — changes where the next block goes without switching the
        // tool off and taking the controls away mid-build.
        public bool ModeEnabled;

        public bool HighlightLayer;

        // Starts solid: building normally is the default, and background layers
        // are the thing you opt into.
        public int SelectedLayer = Solid;

        // Where the Background key goes back to, so toggling out of solid and back
        // returns to the layer you were working on rather than a fixed one.
        int lastBackgroundLayer = DefaultLayer();

        // A background block is anything not on the solid pseudo-layer.
        public bool IsBackground
        {
            get { return SelectedLayer != Solid; }
        }

        static readonly Dictionary<int, LayerState> states = new Dictionary<int, LayerState>();

        // Used when no local player can be identified (menus, teardown), so callers
        // never have to null-check.
        static readonly LayerState fallback = new LayerState();

        public static LayerState For(global::Cursor cursor)
        {
            return cursor == null ? fallback : For(cursor.localNumber);
        }

        public static LayerState For(int localNumber)
        {
            LayerState state;
            if (!states.TryGetValue(localNumber, out state))
            {
                state = new LayerState();
                states[localNumber] = state;
            }

            return state;
        }

        // The state the screen is drawn from.
        //
        // Highlighting dims and brightens the level itself, so unlike placement it
        // cannot be per-player on a shared couch screen — there is only one view.
        // It follows the player who can actually drive it: the keyboard player,
        // since the shortcuts are keyboard-only, or the sole local player.
        public static LayerState View
        {
            get { return For(ControllingCursor()); }
        }

        // The cursor the keyboard shortcuts act on. Null when it is ambiguous —
        // several local players on gamepads, none of whom can press these keys.
        public static PiecePlacementCursor ControllingCursor()
        {
            PiecePlacementCursor onlyAuthority = null;
            int authorityCount = 0;

            foreach (PiecePlacementCursor cursor in Object.FindObjectsOfType<PiecePlacementCursor>())
            {
                if (cursor == null || !cursor.hasAuthority)
                {
                    continue;
                }

                authorityCount++;
                onlyAuthority = cursor;

                Player player = cursor.LocalPlayer;
                if (player != null && player.UseController != null && player.UseController.IsKeyboard)
                {
                    return cursor;
                }
            }

            return authorityCount == 1 ? onlyAuthority : null;
        }

        public string LayerName()
        {
            return LayerName(SelectedLayer);
        }

        public static string LayerName(int index)
        {
            if (index == Solid)
            {
                return SolidName;
            }

            if (index < 0 || index >= SortingLayer.layers.Length)
            {
                return "?";
            }

            return SortingLayer.layers[index].name;
        }

        // The cycle is the sorting layers with the solid pseudo-layer in front, so
        // the Layer keys walk in and out of solid like any other choice.
        public static int CycleCount
        {
            get { return SortingLayer.layers.Length + 1; }
        }

        public static int PositionOf(int layer)
        {
            return layer == Solid ? 0 : layer + 1;
        }

        public static int LayerAtPosition(int position)
        {
            return position == 0 ? Solid : position - 1;
        }

        // The Background key: switches the tool on or off. Turning it on resumes
        // the background layer that was being worked on, turning it off goes back
        // to solid. Returns the layer to select.
        public int ToggleMode()
        {
            if (IsBackground)
            {
                lastBackgroundLayer = SelectedLayer;
            }

            ModeEnabled = !ModeEnabled;
            return ModeEnabled ? lastBackgroundLayer : Solid;
        }

        public void Select(int layer)
        {
            if (layer != Solid)
            {
                lastBackgroundLayer = layer;
            }

            SelectedLayer = layer;
        }

        // -1 when the name is not a sorting layer, which can happen to a block
        // loaded from a level saved against a different set of layers.
        public static int IndexOf(string layerName)
        {
            for (int i = 0; i < SortingLayer.layers.Length; i++)
            {
                if (SortingLayer.layers[i].name == layerName)
                {
                    return i;
                }
            }

            return -1;
        }

        // Takes on what a block already is, so picking one up reads its layer
        // instead of imposing one. Returns true when the selection actually moved.
        //
        // A block with no BackgroundBlock is on the solid pseudo-layer, so picking
        // one up selects that — the same as cycling to it by hand.
        public bool AdoptFrom(BackgroundBlock block)
        {
            int wanted = Solid;
            if (block != null)
            {
                int index = IndexOf(block.layer);
                wanted = index >= 0 ? index : SelectedLayer;
            }

            if (wanted == SelectedLayer)
            {
                return false;
            }

            Select(wanted);
            return true;
        }

        // Leaving free play switches the tool off and puts everyone back on solid;
        // the background layer being worked on is remembered, so returning to
        // building resumes it.
        public static void ResetToSolid()
        {
            foreach (LayerState state in states.Values)
            {
                state.SelectedLayer = Solid;
                state.ModeEnabled = false;
            }
            fallback.SelectedLayer = Solid;
            fallback.ModeEnabled = false;
        }

        static int DefaultLayer()
        {
            int index = IndexOf(CustomBlocksMod.defaultBackgroundLayer);
            return index >= 0 ? index : 0;
        }
    }
}
