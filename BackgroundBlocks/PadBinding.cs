using System.ComponentModel;

namespace CustomBlocks.Backgrounds
{
    // The controller buttons the mod is willing to bind, as a curated enum rather
    // than the game's InputEvent.InputKey — that has thirty-odd members including
    // stick axes, and would make a useless dropdown.
    //
    // Enums are one of the types BepInEx's ConfigurationManager draws natively, so
    // these are rebindable from the standard config UI with no custom drawer and no
    // reference to ConfigurationManager.dll. DescriptionAttribute supplies the
    // labels it shows.
    public enum PadButton
    {
        [Description("A / Cross (bottom)")]
        A,
        [Description("B / Circle (right)")]
        B,
        [Description("X / Square (left)")]
        X,
        [Description("Y / Triangle (top)")]
        Y,
        [Description("L1 / LB (left bumper)")]
        L1,
        [Description("R1 / RB (right bumper)")]
        R1,
        [Description("L2 / LT (left trigger)")]
        L2,
        [Description("R2 / RT (right trigger)")]
        R2,
    }

    public static class PadBindings
    {
        // The game reads these as input keys; MultiControllerUIManager.UpdateButton
        // also keys its glyph sprites off them, so the same value drives both what
        // a press means and what the hint row draws — on Xbox, PlayStation, Switch
        // or Joycon, without the mod knowing which.
        public static InputEvent.InputKey ToInputKey(PadButton button)
        {
            switch (button)
            {
                case PadButton.A: return InputEvent.InputKey.Accept;
                case PadButton.B: return InputEvent.InputKey.Back;
                case PadButton.X: return InputEvent.InputKey.Sprint;
                case PadButton.Y: return InputEvent.InputKey.Inventory;
                case PadButton.L1: return InputEvent.InputKey.RotateLeft;
                case PadButton.R1: return InputEvent.InputKey.RotateRight;
                case PadButton.L2: return InputEvent.InputKey.LeftTrigger;
                default: return InputEvent.InputKey.RightTrigger;
            }
        }

        // Short form for the modifier badge on the hint rows.
        public static string ShortLabel(PadButton button)
        {
            return button.ToString();
        }
    }
}
