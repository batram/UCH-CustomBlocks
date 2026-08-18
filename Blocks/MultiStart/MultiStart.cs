using CustomBlocks.Core;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace CustomBlocks.Blocks
{
    class MultiStart : CustomBlock
    {
        public override int BasedId { get { return 38; } }
        public override string BasePlaceableName { get { return "StartPlank"; } }
        public override string BasePickableBlockName { get { return "StartPlank_Pick"; } }

        override public PickableBlock CreatePickableBlock()
        {
            PickableBlock pb = base.CreatePickableBlock();
            pb.transform.localPosition -= new Vector3(19f, 23.5f, 1);
            // 0.45 rather than the 0.75 the other blocks use: this icon is a
            // replica of the whole start platform — a 4-wide bar under a
            // near-square spawn area — and at 0.75 it measures 3.1 x 3.9 while
            // the next tallest block on the page is 1.9. Uniform, so the
            // proportions still match the placed block.
            pb.transform.localScale = new Vector3(0.45f, 0.45f, 1);

            Transform startZone = pb.transform.Find("StartZone");

            // Copy the render setup off the art we are about to replace, before
            // reparenting: StartZone is world furniture, sitting on the Default
            // GameObject layer and the GraphPaper sorting layer. The book draws
            // through InventoryBook.UiCamera, whose culling mask excludes layer
            // 0, so moved as-is the platform is present, enabled, correctly
            // positioned — and never rasterised.
            Transform plank = pb.transform.Find("ArtHolder/Sprite");
            int artLayer = plank != null ? plank.gameObject.layer : pb.gameObject.layer;
            SpriteRenderer plankRenderer = plank != null ? plank.GetComponent<SpriteRenderer>() : null;
            string artSortingLayer = plankRenderer != null ? plankRenderer.sortingLayerName : "Default";

            startZone.parent = pb.transform.Find("ArtHolder");

            foreach (Transform t in startZone.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = artLayer;
            }
            foreach (SpriteRenderer sp in startZone.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sp.sortingLayerName = artSortingLayer;
            }
            foreach (Canvas cv in startZone.GetComponentsInChildren<Canvas>(true))
            {
                cv.sortingLayerName = artSortingLayer;
            }

            // Draw the bar the block actually places.
            //
            // The vanilla StartPlank pair disagrees with itself: the placeable
            // lays down four IndestructibleBlock.000 (grey metal) while its
            // pickable icon was authored with _static-StaticBlocks.003 (wood).
            // Cloning it inherited both halves, so the book advertised a wooden
            // plank for a metal platform. Borrow the placed sprite rather than
            // shipping a PNG of our own — it cannot drift out of step.
            // The placeable lays four of them side by side at x -1.5..1.5, so
            // one lone block is a quarter of the real bar. Rebuild the row.
            List<SpriteRenderer> barPieces = new List<SpriteRenderer>();
            SpriteRenderer placedArt = null;
            foreach (Transform t in PlaceablePrefab.transform)
            {
                if (!t.name.StartsWith("Sprite")) continue;
                SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null) { placedArt = sr; break; }
            }
            if (plankRenderer != null && placedArt != null)
            {
                plankRenderer.sprite = placedArt.sprite;
                plankRenderer.transform.localPosition = new Vector3(-1.5f, 0f, 0f);
                barPieces.Add(plankRenderer);
                for (int i = 1; i < 4; i++)
                {
                    GameObject copy = Object.Instantiate(plankRenderer.gameObject,
                                                         plankRenderer.transform.parent);
                    copy.name = "BarPiece" + i;
                    copy.transform.localPosition = new Vector3(-1.5f + i, 0f, 0f);
                    barPieces.Add(copy.GetComponent<SpriteRenderer>());
                }
            }

            // Sit the spawn area on the bar at its native proportions. It is
            // near-square in the level (4.09 x 4.14) and the bar spans the same
            // width, so nothing needs rescaling — an earlier attempt squashed
            // it to 45% height and the icon stopped resembling the block.
            SpriteRenderer hatch = startZone.GetComponentInChildren<SpriteRenderer>(true);
            if (barPieces.Count > 0 && hatch != null && hatch.sprite != null)
            {
                Vector3 barCentre = WorldCentre(barPieces[0]);
                float barTop = barCentre.y + WorldSize(barPieces[0]).y * 0.5f;
                float barLeft = WorldCentre(barPieces[0]).x - WorldSize(barPieces[0]).x * 0.5f;
                float barRight = WorldCentre(barPieces[barPieces.Count - 1]).x
                                 + WorldSize(barPieces[barPieces.Count - 1]).x * 0.5f;

                Vector3 want = new Vector3((barLeft + barRight) * 0.5f,
                                           barTop + WorldSize(hatch).y * 0.5f,
                                           WorldCentre(hatch).z);
                startZone.position += want - WorldCentre(hatch);
                LabelStartZone(startZone, "MultiStart", barRight - barLeft);
            }

            // Enable(false) only switches off renderers listed in ArtSprites, so
            // the copies have to join it or they keep drawing over a hidden page.
            if (barPieces.Count > 1)
            {
                List<SpriteRenderer> art = new List<SpriteRenderer>(pb.ArtSprites);
                for (int i = 1; i < barPieces.Count; i++) art.Add(barPieces[i]);
                pb.ArtSprites = art.ToArray();
            }

            // ArtSprites is only half the job, though: it is not what re-orders
            // a pickable when its page changes layer. That is SortOrder, and
            // SortOrder is a snapshot taken when the pickable awoke — so the bar
            // copies and everything under the spawn zone, all built after that,
            // kept the sorting order of the page state they were born into and
            // drew in front of the next page for a second.
            //
            // The spawn zone cannot join ArtSprites in any case: Tint recolours
            // everything in there, and this block does not set
            // noneDefaultColors, so the hatch would come out the flat neutral
            // pick colour instead of its own — and a Canvas cannot go in a
            // SpriteRenderer array at all.
            for (int i = 1; i < barPieces.Count; i++) AdoptExtraArt(pb, barPieces[i].transform);
            AdoptExtraArt(pb, startZone);
            return pb;
        }

        // Name the spawn zone, and make the name stay named.
        //
        // The label carries an I2.Loc.Localize component, which re-applies the
        // localised term every time it runs. Setting Text.text alone looks
        // right until the component next fires and puts "Start" back — which is
        // why a placed MultiStart reverted while the book icon kept our value.
        // Drop the component so the block owns its own name.
        //
        // maxWidth keeps the caption inside the platform it labels; "MultiStart"
        // is half again as wide as "Start" and overhung the hatched area.
        static void LabelStartZone(Transform startZone, string label, float maxWidth)
        {
            if (startZone == null) return;
            Text text = startZone.GetComponentInChildren<Text>(true);
            if (text == null) return;

            // Disabled, not destroyed. Localize applies its term from OnEnable,
            // so switching it off is enough to stop it overwriting us, and it
            // leaves I2's registry of live Localize components intact —
            // DestroyImmediate here coincided with an intermittent
            // NullReferenceException inside Component.GetComponent<T> on a
            // client during treehouse return. That was never reproduced and so
            // never proven to be the cause, but removing a component from
            // under a manager that tracks them buys nothing over disabling it.
            Behaviour localize = text.GetComponent("Localize") as Behaviour;
            if (localize != null) localize.enabled = false;
            text.text = label;

            RectTransform rect = text.transform as RectTransform;
            if (rect == null || maxWidth <= 0f) return;

            Canvas.ForceUpdateCanvases();
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float width = corners[2].x - corners[0].x;
            if (width > maxWidth) rect.localScale = rect.localScale * (maxWidth / width);
        }

        // Renderer geometry straight from the sprite through the transform.
        // Renderer.bounds is not reliable here — the prefab is being assembled
        // and the transforms above have only just moved.
        static Vector3 WorldSize(SpriteRenderer sr)
        {
            Vector3 s = sr.sprite.bounds.size;
            Vector3 l = sr.transform.lossyScale;
            return new Vector3(s.x * Mathf.Abs(l.x), s.y * Mathf.Abs(l.y), 0f);
        }

        static Vector3 WorldCentre(SpriteRenderer sr)
        {
            return sr.transform.TransformPoint(sr.sprite.bounds.center);
        }

        override public Placeable CreatePlaceablePrefab()
        {
            Placeable placeable = base.CreatePlaceablePrefab();
            placeable.gameObject.AddComponent<MultiStart>();
            // Same treatment as the icon: without dropping Localize the placed
            // block reads "Start" again the moment the component next runs.
            // Width comes from the platform's own bar — the four Sprite pieces
            // at the placeable root. An earlier version looked for a renderer
            // under StartZone, found none (the art is not parented there), and
            // silently skipped the fit, leaving "MultiStart" 5.97 wide over a
            // 4.16 platform.
            float left = float.MaxValue, right = float.MinValue;
            foreach (Transform t in placeable.transform)
            {
                if (!t.name.StartsWith("Sprite")) continue;
                SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
                if (sr == null || sr.sprite == null) continue;
                float half = WorldSize(sr).x * 0.5f;
                left = Mathf.Min(left, WorldCentre(sr).x - half);
                right = Mathf.Max(right, WorldCentre(sr).x + half);
            }
            LabelStartZone(placeable.transform.Find("StartZone"), "MultiStart",
                           right > left ? right - left : 0f);
            // the StartPlank base is level furniture and ships IsSaveable=false;
            // a player-placed MultiStart must survive save/load
            placeable.IsSaveable = true;

            return placeable;
        }

        override public void FixSprite(Transform sprite_holder)
        {
        }
    }

    [HarmonyPatch(typeof(Level), nameof(Level.GetSpawnPosition))]
    static class LevelGetSpawnPositionPatch
    {
        static void Prefix(Level __instance, out List<Transform> __state)
        {
            __state = new List<Transform>();
            __state.Add(__instance.StartPoint);
            foreach (MultiStart ms in GameObject.FindObjectsOfType<MultiStart>())
            {
                __state.Add(ms.transform);
            }
            int rando = Random.Range(0, __state.Count);
            Debug.Log("rando start: " + rando);
            __instance.StartPoint = __state[rando];
        }

        static void Postfix(Level __instance, List<Transform> __state)
        {
            __instance.StartPoint = __state[0];
        }
    }

}