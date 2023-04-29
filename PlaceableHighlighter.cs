using UnityEngine;

namespace ModBlocks
{
    public class PlaceableHighlighter
    {
        public static void HighlightAlpha(Placeable placeable)
        {
            SetAlpha(placeable, 0.85f);
        }

        public static void LowlightAlpha(Placeable placeable)
        {
            SetAlpha(placeable, 0.2f);
        }

        public static void ResetAlpha(Placeable placeable)
        {
            SetAlpha(placeable, 1f);
        }

        public static void SetAlpha(Placeable placeable, float value)
        {
            try
            {
                placeable.CustomColor.a = value;
                placeable.SetColor(placeable.CustomColor);
            }
            catch (System.Exception e)
            {
                Debug.LogError("failed to set color on " + placeable + ": " + e);
            }
        }

        public static void HighlightUpdateAll()
        {
            foreach (Placeable pla in Object.FindObjectsOfType<Placeable>())
            {
                HighlightUpdateBlock(pla);
            }
        }

        public static void HighlightUpdateBlock(Placeable pla)
        {
            if (pla.markedForDestruction)
            {
                return;
            }

            // highlight only in Place Phase
            if (!ModBlocksMod.InFreePlace())
            {
                PlaceableHighlighter.ResetAlpha(pla);
            }
            else
            {
                // revert to normal scheme if layer highlight is not active
                if (!ModBlocksMod.highlightSelectedLayer)
                {
                    if (ModBlocksMod.IsModBlock(pla.gameObject))
                    {
                        PlaceableHighlighter.HighlightAlpha(pla);
                    }
                    else
                    {
                        PlaceableHighlighter.ResetAlpha(pla);
                    }
                }
                else
                {
                    // highlight (as solid) normal blocks if we are not in background mode
                    if (!ModBlocksMod.enableModBlockMode)
                    {
                        if (ModBlocksMod.IsModBlock(pla.gameObject))
                        {
                            PlaceableHighlighter.LowlightAlpha(pla);
                        }
                        else
                        {
                            PlaceableHighlighter.ResetAlpha(pla);
                        }

                    }
                    // highlight mod blocks that are on the current layer
                    else if (pla.gameObject.GetComponent<ModBlock>()
                    && pla.gameObject.GetComponent<ModBlock>().layer == SortingLayer.layers[ModBlocksMod.selectedLayer].name)
                    {
                        PlaceableHighlighter.HighlightAlpha(pla);
                    }
                    else
                    {
                        PlaceableHighlighter.LowlightAlpha(pla);
                    }
                }
            }
        }
    }
}