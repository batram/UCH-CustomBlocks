using System.Collections.Generic;
using HarmonyLib;

namespace CustomBlocks.Backgrounds.Patches
{
    // Controller bindings, as a right-trigger chord over the face buttons:
    //
    //     R2 + A   background mode on/off
    //     R2 + X   previous layer          R2 + B   next layer
    //     R2 + Y   highlight on/off
    //
    // The face buttons are laid out to match the hint stack on the cursor: A sits
    // at the bottom of the diamond like the Background row, Y at the top like
    // Highlight, and the left/right pair steps through layers the way K and L do.
    //
    // A chord rather than a spare button because there is no spare: every face
    // button, both bumpers and the left trigger are already bound in build phase,
    // and the D-pad is not free either — it feeds InControl's Direction alongside
    // the stick (X360Controller.cs:71), so pressing it moves the cursor.
    //
    // The right trigger is taken outright while building. The game uses it as a
    // second Sprint binding (cursor speed, and Copy with a hovered piece), which is
    // given up here; ControllerBindings turns the whole thing off and hands it
    // back. Outside free play building nothing is intercepted at all.
    //
    // Input arrives per receiving cursor, so each player's presses land on their
    // own LayerState with no guessing about who pressed what — unlike the keyboard
    // shortcuts, which have to resolve a controlling player.
    [HarmonyPatch(typeof(global::Cursor), nameof(global::Cursor.ReceiveEvent))]
    static class CursorComboInputPatch
    {
        // Keyed by local player, so it cannot leak as cursors respawn.
        static readonly Dictionary<int, bool> triggerHeld = new Dictionary<int, bool>();

        // Presses consumed as part of a chord. Their release has to be consumed
        // too: the cursor turns a release into acceptUp/backUp and would act on a
        // button whose press it never saw — placing a piece on the way out of a
        // chord, for instance.
        static readonly Dictionary<int, HashSet<InputEvent.InputKey>> consumed =
            new Dictionary<int, HashSet<InputEvent.InputKey>>();

        static bool Prefix(global::Cursor __instance, InputEvent e)
        {
            var cursor = __instance as PiecePlacementCursor;
            if (cursor == null || !cursor.hasAuthority || e == null)
            {
                return true;
            }

            if (!CustomBlocksMod.ControllerBindings.Value || !CustomBlocksMod.InFreePlace())
            {
                Forget(cursor.localNumber);
                return true;
            }

            int player = cursor.localNumber;

            if (e.Key == PadBindings.ToInputKey(CustomBlocksMod.PadModifier.Value))
            {
                triggerHeld[player] = e.Valueb;
                return false;
            }

            if (!IsChordKey(e.Key))
            {
                return true;
            }

            // Release of something already taken: swallow it and forget it.
            HashSet<InputEvent.InputKey> pending = Pending(player);
            if (!e.Valueb)
            {
                return !pending.Remove(e.Key);
            }

            bool held;
            if (!triggerHeld.TryGetValue(player, out held) || !held)
            {
                return true;
            }

            pending.Add(e.Key);

            if (e.Changed)
            {
                Act(cursor, e.Key);
            }

            return false;
        }

        static void Act(PiecePlacementCursor cursor, InputEvent.InputKey key)
        {
            if (key == Bound(CustomBlocksMod.PadBackground))
            {
                LayerSelectionGUI.ToggleBackgroundMode(cursor);
                return;
            }

            // The rest only act while the tool is on, matching the keyboard side:
            // they cannot change state the player has no controls shown for.
            if (!LayerState.For(cursor).ModeEnabled)
            {
                return;
            }

            if (key == Bound(CustomBlocksMod.PadPrevLayer))
            {
                LayerSelectionGUI.CycleLayer(cursor, true);
            }
            else if (key == Bound(CustomBlocksMod.PadNextLayer))
            {
                LayerSelectionGUI.CycleLayer(cursor, false);
            }
            else if (key == Bound(CustomBlocksMod.PadHighlight))
            {
                LayerSelectionGUI.ToggleHighlight(cursor);
            }
        }

        static InputEvent.InputKey Bound(BepInEx.Configuration.ConfigEntry<PadButton> entry)
        {
            return PadBindings.ToInputKey(entry.Value);
        }

        static bool IsChordKey(InputEvent.InputKey key)
        {
            return key == Bound(CustomBlocksMod.PadBackground)
                || key == Bound(CustomBlocksMod.PadPrevLayer)
                || key == Bound(CustomBlocksMod.PadNextLayer)
                || key == Bound(CustomBlocksMod.PadHighlight);
        }

        static HashSet<InputEvent.InputKey> Pending(int player)
        {
            HashSet<InputEvent.InputKey> set;
            if (!consumed.TryGetValue(player, out set))
            {
                set = new HashSet<InputEvent.InputKey>();
                consumed[player] = set;
            }

            return set;
        }

        static void Forget(int player)
        {
            triggerHeld.Remove(player);
            consumed.Remove(player);
        }
    }
}
