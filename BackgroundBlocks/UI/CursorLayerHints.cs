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

            // The layer row shows two keys, so it clones the Rotate row — the
            // game's own two-key template (Q/E) — rather than the one-key one.
            CursorControlHintButton pairTemplate = template;
            foreach (CursorControlHintButton button in cursor.cursorControlHints.hintButtons)
            {
                if (button != null && button.button == CursorControlHints.Button.Rotate)
                {
                    pairTemplate = button;
                    break;
                }
            }

            modeRow = CloneRow(template, "Mode", out modeLabel);
            layerRow = CloneRow(pairTemplate, "Layer", out layerLabel);
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

            bool visible = CustomBlocksMod.InFreePlace() && HintsLive();
            bool background = CustomBlocksMod.enableBackgroundMode;

            // The mode row always shows during building, so the feature is
            // discoverable; the other two only mean anything once it is on.
            Show(modeRow, visible);
            Show(layerRow, visible && background);
            Show(highlightRow, visible && background);
        }

        // The same condition PiecePlacementCursor.UIUpdate guards its own hint
        // updates with (PiecePlacementCursor.cs:843). Opening the inventory book
        // freezes the cursor, which hides the game's rows; without this our rows
        // are not registered anywhere the game hides, so they would linger on
        // screen over the open book.
        bool HintsLive()
        {
            if (cursor == null || cursor.frozen || cursor.placementPhysicsLock)
            {
                return false;
            }

            return cursor.AssociatedGamePlayer != null && cursor.AssociatedGamePlayer.IsLocalPlayer;
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

            SetRow(modeRow, modeLabel,
                new KeyCode[] { CustomBlocksMod.ToggleBackgroundKey.Value },
                CustomBlocksMod.enableBackgroundMode ? "Background: On" : "Background: Off");

            // Previous key first, so the pair reads in the direction it cycles,
            // matching Rotate's Q/E.
            SetRow(layerRow, layerLabel,
                new KeyCode[] { CustomBlocksMod.PrevLayerKey.Value, CustomBlocksMod.SwitchLayerKey.Value },
                "Layer: " + CurrentLayerName());

            SetRow(highlightRow, highlightLabel,
                new KeyCode[] { CustomBlocksMod.HighlightBlockKey.Value },
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

        static void SetRow(CursorControlHintButton row, Text label, KeyCode[] keys, string text)
        {
            if (label != null)
            {
                label.text = text;
            }

            // Assign left to right, so a two-key row reads in layout order rather
            // than in whatever order GetComponentsInChildren happens to walk.
            List<Text> overlays = new List<Text>();
            foreach (Text child in row.GetComponentsInChildren<Text>(true))
            {
                if (child != label && child.name == "ButtonTextOverlay")
                {
                    overlays.Add(child);
                }
            }
            overlays.Sort(CompareByX);

            for (int i = 0; i < overlays.Count; i++)
            {
                Text overlay = overlays[i];
                if (i < keys.Length)
                {
                    overlay.text = keys[i].ToString();
                    overlay.enabled = true;
                }
                else
                {
                    // Template had more key boxes than this row needs.
                    overlay.enabled = false;
                }
            }
        }

        static int CompareByX(Text a, Text b)
        {
            return a.transform.position.x.CompareTo(b.transform.position.x);
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

            // The authored rows do not share anchors — Inventory is anchored to the
            // top of the container, Rotate to the middle — so a target centre has to
            // be converted through whichever anchor this row actually uses.
            var rect = (RectTransform)row.transform;
            float anchorFraction = (rect.anchorMin.y + rect.anchorMax.y) * 0.5f;
            float anchorLocalY = (anchorFraction - rows.pivot.y) * rows.rect.height;

            rect.anchoredPosition = new Vector2(
                rect.anchoredPosition.x,
                centerY - anchorLocalY);
        }
    }
}
