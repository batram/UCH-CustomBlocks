using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CustomBlocks.Backgrounds.UI
{
    // Adds the mod's shortcuts to the cursor's own control-hint cluster, cloned
    // from the game's hint rows so they carry the real key box, font and colours.
    //
    // Two earlier attempts are worth not repeating. A screen-anchored panel cannot
    // work here at all: the gameplay camera follows the cursor, so a widget pinned
    // to a screen corner slides away as the cursor approaches it. A custom canvas
    // riding the cursor solved the chase but looked nothing like the game and
    // duplicated a cluster the game already draws in exactly the right place.
    //
    // These rows are deliberately NOT registered in CursorControlHints.buttonMap or
    // its hintButtons array. Registered buttons are hidden every frame by
    // PiecePlacementCursor.UIUpdate's HideAll(ButtonsNotToHide); staying out of
    // those arrays means the game never touches ours and there is no per-frame
    // fight over visibility.
    public class CursorLayerHints : MonoBehaviour
    {
        const string clonePrefix = "CustomBlocksHint_";

        const float rowHeight = 50f;
        const float rowSpacing = 46f;
        const float stackGap = 10f;

        static readonly Dictionary<PiecePlacementCursor, CursorLayerHints> instances =
            new Dictionary<PiecePlacementCursor, CursorLayerHints>();

        static readonly Vector3[] cornerCache = new Vector3[4];

        PiecePlacementCursor cursor;
        RectTransform rows;

        CursorControlHintButton modeRow;
        CursorControlHintButton layerRow;
        CursorControlHintButton highlightRow;

        Text modeLabel;
        Text layerLabel;
        Text highlightLabel;

        // The key-box sprite is not authored on the prefab: MultiControllerUIManager
        // assigns it every frame from the button's inputKey and the active
        // controller type. Our rows have that driver removed (see CloneRow), so the
        // sprite is mirrored from the row we cloned instead — otherwise it freezes
        // at whatever was set the instant we cloned, which early in a cursor's life
        // is nothing at all, and the box renders blank and washed out.
        Image templateGlyph;
        readonly List<Image> glyphImages = new List<Image>();

        // Called for every cursor from the FixedUpdate patch, so the rows appear
        // however the cursor came to exist (spawn, respawn, free-play player
        // switch) without betting on a single lifecycle hook.
        public static void Ensure(PiecePlacementCursor cursor)
        {
            if (cursor == null || !cursor.hasAuthority || cursor.cursorControlHints == null)
            {
                return;
            }

            CursorLayerHints existing;
            if (instances.TryGetValue(cursor, out existing) && existing != null)
            {
                return;
            }

            CursorLayerHints hints = cursor.gameObject.AddComponent<CursorLayerHints>();
            hints.cursor = cursor;
            instances[cursor] = hints;

            if (!hints.Build())
            {
                instances.Remove(cursor);
                Destroy(hints);
                return;
            }

            hints.Refresh();
        }

        public static void RefreshAll()
        {
            Prune();

            foreach (CursorLayerHints hints in instances.Values)
            {
                if (hints != null)
                {
                    hints.Refresh();
                }
            }
        }

        static void Prune()
        {
            List<PiecePlacementCursor> dead = null;
            foreach (KeyValuePair<PiecePlacementCursor, CursorLayerHints> pair in instances)
            {
                if (pair.Key == null || pair.Value == null)
                {
                    if (dead == null)
                    {
                        dead = new List<PiecePlacementCursor>();
                    }
                    dead.Add(pair.Key);
                }
            }

            if (dead == null)
            {
                return;
            }

            foreach (PiecePlacementCursor key in dead)
            {
                instances.Remove(key);
            }
        }

        bool Build()
        {
            // The Inventory row is the keyboard-key template: a lettered key box
            // plus a label. The PickUp row looks wrong cloned, because accept is
            // bound to the mouse and its glyph is a mouse, not a key.
            CursorControlHintButton template = null;
            foreach (CursorControlHintButton button in cursor.cursorControlHints.hintButtons)
            {
                if (button != null && button.button == CursorControlHints.Button.Inventory)
                {
                    template = button;
                    break;
                }
            }

            if (template == null || template.transform.parent == null)
            {
                Debug.LogWarning("CustomBlocks: no Inventory hint row to clone; layer hints disabled");
                return false;
            }

            rows = template.transform.parent as RectTransform;
            if (rows == null)
            {
                return false;
            }

            foreach (MultiControllerButton glyphDriver in template.GetComponentsInChildren<MultiControllerButton>(true))
            {
                templateGlyph = glyphDriver.buttonImage;
                break;
            }

            modeRow = CloneRow(template, "Mode", out modeLabel);
            layerRow = CloneRow(template, "Layer", out layerLabel);
            highlightRow = CloneRow(template, "Highlight", out highlightLabel);

            return modeRow != null && layerRow != null && highlightRow != null;
        }

        CursorControlHintButton CloneRow(CursorControlHintButton template, string suffix, out Text label)
        {
            label = null;

            GameObject clone = Instantiate(template.gameObject, rows, false);
            clone.name = clonePrefix + suffix;
            CustomBlocksMod.UnhideInstance(clone);

            var source = (RectTransform)template.transform;
            var rect = (RectTransform)clone.transform;
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.sizeDelta = source.sizeDelta;

            // MultiControllerButton rewrites the glyph from its own inputKey every
            // frame, and Localize rewrites the label from a translation key. Both
            // would stomp the text set below, and neither has anything to say about
            // a key the game does not know about.
            foreach (MultiControllerButton glyphDriver in clone.GetComponentsInChildren<MultiControllerButton>(true))
            {
                // Keep the Image it was driving so the sprite can be mirrored from
                // the template; the driver itself has to go before it overwrites the
                // glyph letter from an inputKey that means nothing here.
                if (glyphDriver.buttonImage != null)
                {
                    glyphImages.Add(glyphDriver.buttonImage);
                }
                if (glyphDriver.buttonBackgroundImage != null)
                {
                    glyphImages.Add(glyphDriver.buttonBackgroundImage);
                }
                Destroy(glyphDriver);
            }
            foreach (I2.Loc.Localize localize in clone.GetComponentsInChildren<I2.Loc.Localize>(true))
            {
                Destroy(localize);
            }

            CursorControlHintButton row = clone.GetComponent<CursorControlHintButton>();
            if (row != null)
            {
                label = row.buttonText;
            }

            return row;
        }

        void OnDestroy()
        {
            DestroyRow(modeRow);
            DestroyRow(layerRow);
            DestroyRow(highlightRow);
        }

        static void DestroyRow(CursorControlHintButton row)
        {
            if (row != null)
            {
                Destroy(row.gameObject);
            }
        }

        void LateUpdate()
        {
            if (modeRow == null)
            {
                return;
            }

            SyncGlyphSprite();

            bool inPlace = CustomBlocksMod.InFreePlace();
            bool background = CustomBlocksMod.enableBackgroundMode;

            // The mode row always shows during building, so the feature is
            // discoverable; the other two only mean anything once it is on.
            Show(modeRow, inPlace);
            Show(layerRow, inPlace && background);
            Show(highlightRow, inPlace && background);
        }

        void SyncGlyphSprite()
        {
            if (templateGlyph == null || templateGlyph.sprite == null)
            {
                return;
            }

            for (int i = 0; i < glyphImages.Count; i++)
            {
                Image glyph = glyphImages[i];
                if (glyph == null)
                {
                    continue;
                }

                if (glyph.sprite != templateGlyph.sprite)
                {
                    glyph.sprite = templateGlyph.sprite;
                }
                if (!glyph.enabled)
                {
                    glyph.enabled = true;
                }
            }
        }

        // Never passes highlighted: the authored highlight colours are near-invisible
        // against the level, so on/off state is carried in the label text instead.
        static void Show(CursorControlHintButton row, bool visible)
        {
            if (row != null)
            {
                // textKey null leaves the label alone: it is set directly in
                // Refresh rather than through I2 localization.
                row.SetVisible(visible, null, false);
            }
        }

        public void Refresh()
        {
            if (modeRow == null)
            {
                return;
            }

            SetRow(modeRow, modeLabel, CustomBlocksMod.ToggleBackgroundKey.Value,
                CustomBlocksMod.enableBackgroundMode ? "Background: On" : "Background: Off");
            SetRow(layerRow, layerLabel, CustomBlocksMod.SwitchLayerKey.Value,
                "Layer: " + CurrentLayerName());
            SetRow(highlightRow, highlightLabel, CustomBlocksMod.HighlightBlockKey.Value,
                CustomBlocksMod.highlightSelectedLayer ? "Highlight: On" : "Highlight: Off");

            Layout();
        }

        static string CurrentLayerName()
        {
            int index = CustomBlocksMod.selectedLayer;
            if (index < 0 || index >= SortingLayer.layers.Length)
            {
                return "?";
            }

            return SortingLayer.layers[index].name;
        }

        static void SetRow(CursorControlHintButton row, Text label, KeyCode key, string text)
        {
            if (label != null)
            {
                label.text = text;
            }

            foreach (Text child in row.GetComponentsInChildren<Text>(true))
            {
                if (child != label && child.name == "ButtonTextOverlay")
                {
                    child.text = key.ToString();
                    child.enabled = true;
                }
            }
        }

        // Stacks the mod's rows under the game's keyboard hint rows. The game's row
        // positions are authored, not laid out by a layout group, so the bottom of
        // the stack has to be measured. The alt (controller) rows sit lower still
        // and are ignored on purpose: these shortcuts are keyboard-only.
        void Layout()
        {
            float bottom = float.MaxValue;
            foreach (CursorControlHintButton button in cursor.cursorControlHints.hintButtons)
            {
                if (button == null || button.name.StartsWith(clonePrefix))
                {
                    continue;
                }

                ((RectTransform)button.transform).GetWorldCorners(cornerCache);
                for (int i = 0; i < cornerCache.Length; i++)
                {
                    float y = rows.InverseTransformPoint(cornerCache[i]).y;
                    if (y < bottom)
                    {
                        bottom = y;
                    }
                }
            }

            if (bottom == float.MaxValue)
            {
                return;
            }

            float firstCenter = bottom - stackGap - rowHeight * 0.5f;
            PlaceRow(modeRow, firstCenter);
            PlaceRow(layerRow, firstCenter - rowSpacing);
            PlaceRow(highlightRow, firstCenter - rowSpacing * 2f);
        }

        void PlaceRow(CursorControlHintButton row, float centerY)
        {
            if (row == null)
            {
                return;
            }

            // The rows container is anchored to its parent's corners, so the rows'
            // own anchor sits at its top edge: convert a local centre into the
            // anchored offset the authored rows use.
            var rect = (RectTransform)row.transform;
            rect.anchoredPosition = new Vector2(
                rect.anchoredPosition.x,
                centerY - rows.rect.height * 0.5f);
        }
    }
}
