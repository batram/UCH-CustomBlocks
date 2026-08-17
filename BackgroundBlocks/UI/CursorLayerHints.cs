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

        // The modifier tag: small enough to read as an annotation rather than a
        // second key, and pulled in over the button it annotates.
        const float badgeScale = 0.52f;
        const float badgePadding = 6f;
        static readonly Vector2 badgeOffset = new Vector2(-16f, -14f);

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

            // One per key box, left to right once ordered at update time.
            public readonly List<MultiControllerButton> drivers = new List<MultiControllerButton>();
            public readonly List<Badge> badges = new List<Badge>();

            // Which controller button each box stands for, and the keyboard key it
            // shows instead when the player is on a keyboard.
            public System.Func<PadButton>[] padButtons;
            public System.Func<KeyCode>[] keyboardKeys;
        }

        // The little "R2" tag in the corner of a key box, saying the binding is a
        // chord. Cloned from the box's own letter so it inherits font and styling,
        // and backed by the game's own key-cap sprite so it reads as a miniature
        // key rather than floating text — which was too faint over a bright level.
        static Badge MakeBadge(MultiControllerButton driver)
        {
            if (driver.buttonText == null)
            {
                return null;
            }

            GameObject label = Instantiate(driver.buttonText.gameObject, driver.transform, false);
            label.name = "CustomBlocksModifier";
            CustomBlocksMod.UnhideInstance(label);

            foreach (I2.Loc.Localize localize in label.GetComponentsInChildren<I2.Loc.Localize>(true))
            {
                Destroy(localize);
            }

            var labelRect = (RectTransform)label.transform;
            labelRect.localScale = Vector3.one * badgeScale;
            labelRect.anchoredPosition = new Vector2(
                labelRect.anchoredPosition.x + badgeOffset.x, labelRect.anchoredPosition.y + badgeOffset.y);

            var badge = new Badge();
            badge.label = label.GetComponent<Text>();
            badge.label.color = Color.white;

            // The plate is a sibling drawn before the text, not a child: uGUI draws
            // children after their parent, so a child would cover the letter.
            var plate = new GameObject("CustomBlocksModifierBg");
            plate.transform.SetParent(driver.transform, false);

            var plateRect = plate.AddComponent<RectTransform>();
            plateRect.anchorMin = labelRect.anchorMin;
            plateRect.anchorMax = labelRect.anchorMax;
            plateRect.pivot = labelRect.pivot;
            plateRect.localScale = labelRect.localScale;
            plateRect.anchoredPosition = labelRect.anchoredPosition;
            plateRect.sizeDelta = labelRect.sizeDelta + new Vector2(badgePadding, badgePadding);

            badge.plate = plate.AddComponent<Image>();
            plate.transform.SetSiblingIndex(label.transform.GetSiblingIndex());

            badge.Show(false);
            return badge;
        }

        class Badge
        {
            public Text label;
            public Image plate;

            public void Show(bool visible)
            {
                if (label != null && label.enabled != visible)
                {
                    label.enabled = visible;
                }

                // The plate has to follow the label, or a player with no chord gets
                // an empty tag sitting on their key box.
                if (plate != null && plate.enabled != visible)
                {
                    plate.enabled = visible;
                }
            }

            public void SetSprite(Sprite sprite)
            {
                if (plate != null && plate.sprite != sprite)
                {
                    plate.sprite = sprite;
                }
            }

            public void SetText(string text)
            {
                if (label != null && label.text != text)
                {
                    label.text = text;
                }
            }
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

            if (modeRow == null || layerRow == null || highlightRow == null)
            {
                return false;
            }

            modeRow.padButtons = new System.Func<PadButton>[]
                { () => CustomBlocksMod.PadBackground.Value };
            modeRow.keyboardKeys = new System.Func<KeyCode>[]
                { () => CustomBlocksMod.ToggleBackgroundKey.Value };

            layerRow.padButtons = new System.Func<PadButton>[]
                { () => CustomBlocksMod.PadPrevLayer.Value, () => CustomBlocksMod.PadNextLayer.Value };
            layerRow.keyboardKeys = new System.Func<KeyCode>[]
                { () => CustomBlocksMod.PrevLayerKey.Value, () => CustomBlocksMod.SwitchLayerKey.Value };

            highlightRow.padButtons = new System.Func<PadButton>[]
                { () => CustomBlocksMod.PadHighlight.Value };
            highlightRow.keyboardKeys = new System.Func<KeyCode>[]
                { () => CustomBlocksMod.HighlightBlockKey.Value };

            return true;
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

            // MultiControllerButton is kept alive and re-pointed at the button we
            // mean (see UpdateGlyphs). It is what draws the correctly coloured face
            // button per device — Xbox, PlayStation, Switch and Joycon all have
            // their own sprites in MultiControllerUIManager.UpdateButton — so
            // letting it run beats freezing one sprite and stamping a letter on it.
            //
            // Localize does have to go: it rewrites the row label from a
            // translation key and would stomp the text set in Refresh.
            foreach (MultiControllerButton glyphDriver in clone.GetComponentsInChildren<MultiControllerButton>(true))
            {
                row.drivers.Add(glyphDriver);
                if (glyphDriver.buttonImage != null)
                {
                    row.glyphImages.Add(glyphDriver.buttonImage);
                }
                if (glyphDriver.buttonBackgroundImage != null)
                {
                    row.glyphImages.Add(glyphDriver.buttonBackgroundImage);
                }
            }
            foreach (I2.Loc.Localize localize in clone.GetComponentsInChildren<I2.Loc.Localize>(true))
            {
                Destroy(localize);
            }

            foreach (MultiControllerButton driver in row.drivers)
            {
                row.badges.Add(MakeBadge(driver));
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

            UpdateGlyphs();

            bool visible = CustomBlocksMod.InFreePlace() && HintsLive();

            // The mode row always shows during building, so the feature is
            // discoverable. The other two follow the tool being switched on — not
            // the layer in use, so picking up an ordinary block moves the selection
            // to the solid layer without the controls vanishing from under the
            // player.
            bool toolOn = LayerState.For(cursor).ModeEnabled;

            Show(modeRow, visible);
            Show(layerRow, visible && toolOn);
            Show(highlightRow, visible && toolOn);

            if (visible)
            {
                Layout();
            }
        }

        // Whether this cursor is in a state where its hints belong on screen.
        //
        // frozen and placementPhysicsLock are the conditions
        // PiecePlacementCursor.UIUpdate guards its own hint updates with
        // (PiecePlacementCursor.cs:843) — opening the inventory book freezes the
        // cursor, which hides the game's rows, and ours are registered nowhere the
        // game hides, so without this they linger over the open book.
        //
        // disabled is the free-play one: when a player switches to play, their
        // cursor is disabled and hidden while the phase stays PLACE for whoever is
        // still building. Their rows used to be left behind, floating in the level
        // at the spot the cursor was last seen.
        bool HintsLive()
        {
            if (cursor == null || cursor.disabled || cursor.frozen || cursor.placementPhysicsLock)
            {
                return false;
            }

            return cursor.AssociatedGamePlayer != null && cursor.AssociatedGamePlayer.IsLocalPlayer;
        }

        // Points each key box at the button it actually stands for, so the game
        // draws it. On a controller that is the mod's own binding, which yields the
        // correctly coloured face button for whatever pad is plugged in, plus a
        // small modifier tag because the binding is a chord. On a keyboard the box
        // is left pointing at the row it was cloned from — which renders the plain
        // key sprite — and the letter is overwritten with our own key below.
        void UpdateGlyphs()
        {
            bool keyboard = OnKeyboard();

            UpdateGlyphs(modeRow, keyboard);
            UpdateGlyphs(layerRow, keyboard);
            UpdateGlyphs(highlightRow, keyboard);
        }

        void UpdateGlyphs(Row row, bool keyboard)
        {
            if (row == null || row.padButtons == null)
            {
                return;
            }

            row.drivers.Sort(CompareDriversByX);

            string modifier = PadBindings.ShortLabel(CustomBlocksMod.PadModifier.Value);
            bool chord = !keyboard && CustomBlocksMod.ControllerBindings.Value;

            for (int i = 0; i < row.drivers.Count; i++)
            {
                MultiControllerButton driver = row.drivers[i];
                if (driver == null || i >= row.padButtons.Length)
                {
                    continue;
                }

                // Only re-point it on a controller: on a keyboard the cloned row's
                // own key is what produces a plain key box to write our letter on.
                if (!keyboard)
                {
                    InputEvent.InputKey wanted = PadBindings.ToInputKey(row.padButtons[i]());
                    if (driver.inputKey != wanted)
                    {
                        driver.inputKey = wanted;
                        driver.MarkDirty();
                    }
                }
                else if (driver.buttonText != null && row.keyboardKeys != null && i < row.keyboardKeys.Length)
                {
                    // Set every frame, not once: the driver rewrites this letter
                    // from the game's own binding whenever it re-evaluates, which
                    // it does on any device switch.
                    string letter = row.keyboardKeys[i]().ToString();
                    if (driver.buttonText.text != letter)
                    {
                        driver.buttonText.text = letter;
                    }
                    if (!driver.buttonText.enabled)
                    {
                        driver.buttonText.enabled = true;
                    }
                }

                // Only shown when the binding actually is a chord. A keyboard
                // player has no modifier to press, so tagging their keys would be
                // a lie — and an empty plate on the key box if the text alone were
                // hidden.
                Badge badge = i < row.badges.Count ? row.badges[i] : null;
                if (badge != null)
                {
                    badge.Show(chord);
                    if (chord)
                    {
                        badge.SetText(modifier);
                        badge.SetSprite(KeyCapSprite());
                    }
                }
            }
        }

        // The game's own key-cap sprite, so the tag matches the keys drawn
        // elsewhere in the cluster instead of being a bare rectangle.
        static Sprite KeyCapSprite()
        {
            MultiControllerUIManager manager = MultiControllerUIManager.Instance;
            return manager != null ? manager.KeyboardKeySprite : null;
        }

        static int CompareDriversByX(MultiControllerButton a, MultiControllerButton b)
        {
            return ((RectTransform)a.transform).anchoredPosition.x
                .CompareTo(((RectTransform)b.transform).anchoredPosition.x);
        }

        bool OnKeyboard()
        {
            Player player = cursor != null ? cursor.LocalPlayer : null;
            return player == null || player.UseController == null || player.UseController.IsKeyboard;
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

            // Each cursor shows its own player's state, so two builders on one
            // couch do not read each other's layer off the wrong cluster.
            LayerState state = LayerState.For(cursor);

            SetLabel(modeRow, "Background");
            SetLabel(layerRow, "Layer: " + state.LayerName());
            SetLabel(highlightRow, "Highlight");

            // On/off lives on the key box, not in the text: the state is already
            // obvious from the blocks themselves, so the cue can stay quiet.
            Tint(modeRow, state.ModeEnabled);
            Tint(highlightRow, state.HighlightLayer);
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

        static void SetLabel(Row row, string text)
        {
            if (row != null && row.label != null)
            {
                row.label.text = text;
            }
        }
    }
}
