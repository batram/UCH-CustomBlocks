using UnityEngine;

namespace CustomBlocks.Backgrounds
{
    // Dims and brightens placed blocks so the layer being worked on stands out.
    //
    // Unlike what a player places, this is a property of the screen rather than of
    // a player: there is only one level being drawn, so on a shared couch screen it
    // cannot be per-player. It renders from LayerState.View — the player who can
    // actually drive it.
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
            LayerState view = LayerState.View;
            foreach (Placeable pla in Object.FindObjectsOfType<Placeable>())
            {
                HighlightUpdateBlock(pla, view);
            }
        }

        public static void HighlightUpdateBlock(Placeable pla)
        {
            HighlightUpdateBlock(pla, LayerState.View);
        }

        public static void HighlightUpdateBlock(Placeable pla, LayerState view)
        {
            if (pla.markedForDestruction)
            {
                return;
            }

            // highlight only in Place Phase
            if (!CustomBlocksMod.InFreePlace())
            {
                PlaceableHighlighter.ResetAlpha(pla);
            }
            else
            {
                // revert to normal scheme if layer highlight is not active
                if (!view.HighlightLayer)
                {
                    if (CustomBlocksMod.IsBackgroundBlock(pla.gameObject))
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
                    if (!view.IsBackground)
                    {
                        if (CustomBlocksMod.IsBackgroundBlock(pla.gameObject))
                        {
                            PlaceableHighlighter.LowlightAlpha(pla);
                        }
                        else
                        {
                            PlaceableHighlighter.ResetAlpha(pla);
                        }

                    }
                    // highlight mod blocks that are on the current layer
                    else if (pla.gameObject.GetComponent<BackgroundBlock>()
                    && pla.gameObject.GetComponent<BackgroundBlock>().layer == view.LayerName())
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
