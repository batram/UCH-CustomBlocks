using System.Collections.Generic;
using UnityEngine;

namespace CustomBlocks.Core
{
    // Arranges the mod's inventory book page.
    //
    // The vanilla pages this one sits between are hand-arranged: items at their
    // true relative size, loosely grouped under a label, packed from the top
    // left, on no grid at all. Matching that is an editorial job, so the order
    // below is a hand-curated list rather than anything derived. Blocks missing
    // from it are still placed — appended as a trailing group — so a block from
    // another mod is never silently dropped off the page.
    //
    // What this replaces: AddToInventoryPage assigns transform.parent, which
    // preserves WORLD position, so each block landed wherever its prefab
    // happened to sit and the per-block constants in CreatePickableBlock
    // (-= new Vector3(20.08f, 20.5f, 1) and friends) were fitted to that
    // accident. ChickenRoll hung off the left edge of the page, FloatyCloud
    // overlapped the title, and RCReceiver and Acid sat at exactly the same
    // spot with one drawn on top of the other.
    public class BookPageLayout : MonoBehaviour
    {
        public Transform Items;
        public SpriteRenderer Paper;

        // Loose thematic grouping. One label for the whole page, so these only
        // control adjacency and the slightly wider gap between groups.
        static readonly string[][] Groups =
        {
            new[] { "OneRoundWood_Pick", "MultiStart_Pick", "FloatyCloud_Pick" }, // stand on
            new[] { "ReCoin_Pick", "PigDirt_Pick" },                              // worth points
            new[] { "RCTransmitter_Pick", "RCReceiver_Pick" },                    // the remote pair
            new[] { "ChickenRoll_Pick", "Acid_Pick" },                            // hurt
        };

        // Clear of the ring binding on the left and of the page title on top.
        // The title sits at y 1004.0-1004.5 against a paper top of 1006.9.
        const float LeftInset = 0.95f;
        const float RightInset = 0.25f;
        const float TopClear = 3.2f;

        readonly Dictionary<Transform, Vector2> smallest = new Dictionary<Transform, Vector2>();

        void LateUpdate()
        {
            if (Items == null || Paper == null)
            {
                enabled = false;
                return;
            }

            // Same reasoning as PickColliderAligner: glue-based blocks carry a
            // rig that is transparent at rest and animates in on mouse-over, so
            // the resting artwork is the SMALLEST the bounds ever get. Re-lay
            // out only when something shrinks; once every block has settled
            // this stops doing anything.
            bool changed = false;
            foreach (Transform child in Items)
            {
                Bounds b;
                if (!Measure(child, out b)) continue;
                Vector2 size = new Vector2(b.size.x, b.size.y);
                Vector2 known;
                if (smallest.TryGetValue(child, out known)
                    && size.x * size.y >= known.x * known.y - 1e-4f) continue;
                smallest[child] = size;
                changed = true;
            }

            if (changed) Arrange();
        }

        void Arrange()
        {
            List<Transform> ordered = new List<Transform>();
            List<int> group = new List<int>();

            Dictionary<string, Transform> byName = new Dictionary<string, Transform>();
            foreach (Transform child in Items) byName[child.name] = child;

            HashSet<Transform> placed = new HashSet<Transform>();
            for (int g = 0; g < Groups.Length; g++)
            {
                foreach (string name in Groups[g])
                {
                    Transform child;
                    if (!byName.TryGetValue(name, out child)) continue;
                    ordered.Add(child);
                    group.Add(g);
                    placed.Add(child);
                }
            }
            // anything the curated list does not know about
            foreach (Transform child in Items)
            {
                if (placed.Contains(child)) continue;
                ordered.Add(child);
                group.Add(Groups.Length);
            }

            Bounds paper = Paper.bounds;
            float left = paper.min.x + LeftInset;
            float right = paper.max.x - RightInset;
            float rowTop = paper.max.y - TopClear;

            float x = left, rowHeight = 0f;
            int previousGroup = -1;

            for (int i = 0; i < ordered.Count; i++)
            {
                Transform child = ordered[i];
                Vector2 size;
                if (!smallest.TryGetValue(child, out size)) continue;

                Bounds current;
                if (!Measure(child, out current)) continue;

                if (x > left) x += (group[i] != previousGroup) ? 0.95f : 0.4f + 0.25f * Wobble(child.name, 3);
                if (x + size.x > right)
                {
                    rowTop -= rowHeight + 0.8f + 0.4f * Wobble(child.name, 5);
                    x = left;
                    rowHeight = 0f;
                }

                float jitter = 0.35f * Wobble(child.name, 1);
                Vector3 want = new Vector3(x + size.x * 0.5f, rowTop - size.y * 0.5f + jitter, current.center.z);
                child.position += want - current.center;

                x += size.x;
                rowHeight = Mathf.Max(rowHeight, size.y + Mathf.Abs(jitter));
                previousGroup = group[i];
            }
        }

        // Artwork plus any caption, which is what the page has to make room for.
        //
        // Deliberately wider than CustomBlock.VisibleBounds, which counts
        // SpriteRenderers only. MultiStart carries a "MultiStart" text label
        // above its icon; invisible to the sprite-only measure, it sailed
        // straight over the page title. The hitbox path keeps using the
        // sprite-only bounds — a caption is not something you click.
        static bool Measure(Transform child, out Bounds bounds)
        {
            bool any = CustomBlock.VisibleBounds(child, out bounds);
            foreach (UnityEngine.UI.Text text in child.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                if (!text.enabled || !text.gameObject.activeInHierarchy) continue;
                if (string.IsNullOrEmpty(text.text)) continue;
                RectTransform rt = text.transform as RectTransform;
                if (rt == null) continue;
                Vector3[] corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                for (int i = 0; i < 4; i++)
                {
                    if (!any) { bounds = new Bounds(corners[i], Vector3.zero); any = true; }
                    else bounds.Encapsulate(corners[i]);
                }
            }
            return any;
        }

        // Deterministic stand-in for a hand's imprecision. Seeded from the block
        // name rather than Random so the page looks identical every launch —
        // a layout that shuffled itself would make every screenshot and every
        // comparison between runs meaningless.
        static float Wobble(string name, int salt)
        {
            int h = salt * 7919;
            foreach (char c in name) h = h * 31 + c;
            h &= 0x7fffffff;
            return ((h % 1000) / 1000f) - 0.5f;
        }
    }
}
