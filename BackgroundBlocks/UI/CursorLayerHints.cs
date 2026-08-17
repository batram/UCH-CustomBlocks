using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CustomBlocks.Backgrounds.UI
{
    // Adds the mod's shortcuts to the cursor's own control-hint cluster, cloned
    // from the game's hint rows so they carry the real key box, font and colours.
    //
    // They stack up and to the left of the Inventory row's key box, on a gentle
    // arc, with their labels running leftwards — which keeps them off the
    // character art and out of the way of the game's own rows below.
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

        // Key boxes are 50 tall, so this is just enough to keep them apart.
        const float rowStep = 49f;

        // One key box plus a small gap, left of the Inventory row's box.
        const float inventoryGap = 56f;

        const float labelGap = 8f;

        static readonly Color activeTint = new Color(1f, 0.84f, 0.42f);

        static readonly Dictionary<PiecePlacementCursor, CursorLayerHints> instances =
            new Dictionary<PiecePlacementCursor, CursorLayerHints>();

        PiecePlacementCursor cursor;
        RectTransform rows;
        CursorControlHintButton inventoryRow;

        // The key-box sprite is not authored on the prefab: MultiControllerUIManager
        // assigns it every frame from the button's inputKey and the active
        // controller type. Our rows have that driver removed (see CloneRow), so the
        // sprite is mirrored from the row we cloned instead — otherwise it freezes
        // at whatever was set the instant we cloned, which early in a cursor's life
        // is nothing at all, and the box renders blank and washed out.
        Image templateGlyph;

        Row modeRow;
        Row layerRow;
        Row highlightRow;

        class Row
        {
            public CursorControlHintButton button;
            public RectTransform rect;
            public Text label;
            public readonly List<Image> glyphImages = new List<Image>();
        }

        // Resolved at layout time, never cached. The key boxes are not at their
        // final positions when the row is cloned: MultiControllerButton is only
        // destroyed at the end of that frame, and it moves the second box before
        // then — so a lookup done in CloneRow finds both edges on the same child.
        static RectTransform EdgeGlyph(RectTransform row, bool rightmost)
        {
            RectTransform best = null;
            foreach (Transform child in row)
            {
                if (!child.name.Contains("MultiControllerButton"))
                {
                    continue;
                }

                var glyph = (RectTransform)child;
                if (best == null
                    || (rightmost
                        ? glyph.anchoredPosition.x > best.anchoredPosition.x
                        : glyph.anchoredPosition.x < best.anchoredPosition.x))
                {
                    best = glyph;
                }
            }

            return best;
        }

        // Each row up also steps left, easing off after the first, so the stack
        // curves away from the character rather than running off on a diagonal.
        static float ArcX(int step)
        {
            if (step <= 0)
            {
                return 0f;
            }
            return step == 1 ? -26f : -40f;
        }

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
            CursorControlHintButton single = null;
            CursorControlHintButton pair = null;
            foreach (CursorControlHintButton button in cursor.cursorControlHints.hintButtons)
            {
                if (button == null)
                {
                    continue;
                }
                if (button.button == CursorControlHints.Button.Inventory)
                {
                    single = button;
                }
                else if (button.button == CursorControlHints.Button.Rotate)
                {
                    pair = button;
                }
            }

            if (single == null || single.transform.parent == null)
            {
                Debug.LogWarning("CustomBlocks: no Inventory hint row to clone; layer hints disabled");
                return false;
            }

            inventoryRow = single;
            rows = single.transform.parent as RectTransform;
            if (rows == null)
            {
                return false;
            }

            foreach (MultiControllerButton glyphDriver in single.GetComponentsInChildren<MultiControllerButton>(true))
            {
                templateGlyph = glyphDriver.buttonImage;
                break;
            }

            // The layer row shows two keys, so it clones the Rotate row — the
            // game's own two-key template (Q/E) — rather than the one-key one.
            modeRow = CloneRow(single, "Mode");
            layerRow = CloneRow(pair != null ? pair : single, "Layer");
            highlightRow = CloneRow(single, "Highlight");

            return modeRow != null && layerRow != null && highlightRow != null;
        }

        Row CloneRow(CursorControlHintButton template, string suffix)
        {
            GameObject clone = Instantiate(template.gameObject, rows, false);
            clone.name = clonePrefix + suffix;
            CustomBlocksMod.UnhideInstance(clone);

            var source = (RectTransform)template.transform;
            var rect = (RectTransform)clone.transform;
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.sizeDelta = source.sizeDelta;

            var row = new Row();
            row.rect = rect;
            row.button = clone.GetComponent<CursorControlHintButton>();
            if (row.button != null)
            {
                row.label = row.button.buttonText;
            }

            // MultiControllerButton rewrites the glyph from its own inputKey every
            // frame, and Localize rewrites the label from a translation key. Both
            // would stomp the text set below, and neither has anything to say about
            // a key the game does not know about. Keep the Images they drove so the
            // sprite can be mirrored and the box tinted.
            foreach (MultiControllerButton glyphDriver in clone.GetComponentsInChildren<MultiControllerButton>(true))
            {
                if (glyphDriver.buttonImage != null)
                {
                    row.glyphImages.Add(glyphDriver.buttonImage);
                }
                if (glyphDriver.buttonBackgroundImage != null)
                {
                    row.glyphImages.Add(glyphDriver.buttonBackgroundImage);
                }
                Destroy(glyphDriver);
            }
            foreach (I2.Loc.Localize localize in clone.GetComponentsInChildren<I2.Loc.Localize>(true))
            {
                Destroy(localize);
            }

            return row.button != null && EdgeGlyph(rect, true) != null ? row : null;
        }

        void OnDestroy()
        {
            DestroyRow(modeRow);
            DestroyRow(layerRow);
            DestroyRow(highlightRow);
        }

        static void DestroyRow(Row row)
        {
            if (row != null && row.rect != null)
            {
                Destroy(row.rect.gameObject);
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

            if (visible)
            {
                Layout();
            }
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

            SyncGlyphSprite(modeRow);
            SyncGlyphSprite(layerRow);
            SyncGlyphSprite(highlightRow);
        }

        void SyncGlyphSprite(Row row)
        {
            if (row == null)
            {
                return;
            }

            for (int i = 0; i < row.glyphImages.Count; i++)
            {
                Image glyph = row.glyphImages[i];
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
        // against the level, so on/off state is a tint on the key box instead.
        static void Show(Row row, bool visible)
        {
            if (row != null && row.button != null)
            {
                // textKey null leaves the label alone: it is set directly in
                // Refresh rather than through I2 localization.
                row.button.SetVisible(visible, null, false);
            }
        }

        // Anchors the stack to the Inventory row's key box, which does not move as
        // the game shows and hides its other rows — so the toggles stay put instead
        // of shuffling under the cursor.
        void Layout()
        {
            if (rows == null || inventoryRow == null || !inventoryRow.gameObject.activeSelf)
            {
                return;
            }

            RectTransform anchorGlyph = null;
            foreach (Transform child in inventoryRow.transform)
            {
                if (!child.name.Contains("MultiControllerButton"))
                {
                    continue;
                }

                var glyph = (RectTransform)child;
                if (anchorGlyph == null || glyph.anchoredPosition.x > anchorGlyph.anchoredPosition.x)
                {
                    anchorGlyph = glyph;
                }
            }

            if (anchorGlyph == null)
            {
                return;
            }

            Vector3 anchor = rows.InverseTransformPoint(anchorGlyph.position);

            PlaceRow(modeRow, anchor, 0);
            PlaceRow(layerRow, anchor, 1);
            PlaceRow(highlightRow, anchor, 2);
        }

        void PlaceRow(Row row, Vector3 anchor, int step)
        {
            if (row == null)
            {
                return;
            }

            RectTransform rightGlyph = EdgeGlyph(row.rect, true);
            RectTransform leftGlyph = EdgeGlyph(row.rect, false);
            if (rightGlyph == null || leftGlyph == null)
            {
                return;
            }

            // Label right-aligned against the leftmost key box, sized to its own
            // text so it grows leftwards as the layer name changes.
            if (row.label != null)
            {
                var text = (RectTransform)row.label.transform;
                text.pivot = new Vector2(1f, 0.5f);
                text.sizeDelta = new Vector2(row.label.preferredWidth, text.sizeDelta.y);
                text.anchoredPosition = new Vector2(
                    leftGlyph.anchoredPosition.x - leftGlyph.sizeDelta.x * 0.5f - labelGap,
                    leftGlyph.anchoredPosition.y);
            }

            // Shift by the delta between where our rightmost box is and where it
            // should be: the rows carry different anchors and pivots, so moving
            // them by measurement avoids converting each one's frame by hand.
            Vector3 current = rows.InverseTransformPoint(rightGlyph.position);
            row.rect.anchoredPosition += new Vector2(
                (anchor.x - inventoryGap + ArcX(step)) - current.x,
                (anchor.y + step * rowStep) - current.y);
        }

        public void Refresh()
        {
            if (modeRow == null)
            {
                return;
            }

            SetRow(modeRow, new KeyCode[] { CustomBlocksMod.ToggleBackgroundKey.Value }, "Background");

            // Previous key first, so the pair reads in the direction it cycles,
            // matching Rotate's Q/E.
            SetRow(layerRow,
                new KeyCode[] { CustomBlocksMod.PrevLayerKey.Value, CustomBlocksMod.SwitchLayerKey.Value },
                "Layer: " + CurrentLayerName());

            SetRow(highlightRow, new KeyCode[] { CustomBlocksMod.HighlightBlockKey.Value }, "Highlight");

            // On/off lives on the key box, not in the text: the state is already
            // obvious from the blocks themselves, so the cue can stay quiet.
            Tint(modeRow, CustomBlocksMod.enableBackgroundMode);
            Tint(highlightRow, CustomBlocksMod.highlightSelectedLayer);
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

        static void Tint(Row row, bool on)
        {
            if (row == null)
            {
                return;
            }

            Color wanted = on ? activeTint : Color.white;
            for (int i = 0; i < row.glyphImages.Count; i++)
            {
                Image glyph = row.glyphImages[i];
                if (glyph != null && glyph.color != wanted)
                {
                    glyph.color = wanted;
                }
            }
        }

        static void SetRow(Row row, KeyCode[] keys, string text)
        {
            if (row == null)
            {
                return;
            }

            if (row.label != null)
            {
                row.label.text = text;
            }

            // Assign left to right, so a two-key row reads in layout order rather
            // than in whatever order GetComponentsInChildren happens to walk.
            var overlays = new List<Text>();
            foreach (Text child in row.rect.GetComponentsInChildren<Text>(true))
            {
                if (child != row.label && child.name == "ButtonTextOverlay")
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
    }
}
