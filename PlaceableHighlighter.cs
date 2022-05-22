using UnityEngine;

namespace ModBlocks
{
    public class PlaceableHighlighter
    {
        public static void HighlightAlpha(Placeable placeable)
        {
            SetAlpha(placeable, 0.7f);
        }

        public static void LowlightAlpha(Placeable placeable)
        {
            SetAlpha(placeable, 0.3f);
        }

        public static void ResetAlpha(Placeable placeable)
        {
            SetAlpha(placeable, 1f);
        }

        public static void SetAlpha(Placeable placeable, float value)
        {
            placeable.CustomColor.a = value;
            placeable.SetColor(placeable.CustomColor);
        }

    }
}