using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Reflection;

[assembly: AssemblyVersion("0.0.0.1")]
[assembly: AssemblyInformationalVersion("0.0.0.1")]

namespace BackgroundBlocks
{
    [BepInPlugin("BackgroundBlocks", "BackgroundBlocks", "0.0.0.1")]
    public class BackgroundBlocksMod : BaseUnityPlugin
    {
        public static bool enableBackgroundMode = false;
        public static int magicBackgroundNumber = 9000;

        void Awake()
        {
            Debug.Log("Moin from BackgroundBlock");
            new Harmony("BackgroundBlocks").PatchAll();
        }

        public static bool IsBackground(GameObject go)
        {
            var meta = go.GetComponent<PlaceableMetadata>();
            if (meta && meta.blockSerializeIndex > magicBackgroundNumber)
            {
                return true;
            }
            var parent = go.transform.parent;
            if (parent)
            {
                return IsBackground(parent.gameObject);
            }
            return false;
        }

        public static void EnableBackground(GameObject go, bool layersAndColliders = true, bool alpha = false)
        {
            Placeable placeable = go.GetComponent<Placeable>();

            if(!go.name.Contains(" [background]"))
            {
                go.name += " [background]";
            }

            PlaceableMetadata meta = go.GetComponent<PlaceableMetadata>();
            if(meta.blockSerializeIndex < BackgroundBlocksMod.magicBackgroundNumber)
            {
                meta.blockSerializeIndex += BackgroundBlocksMod.magicBackgroundNumber;
            }

            if (layersAndColliders)
            {
                SetLayer(placeable, "Background 1");
                SetCollide(go, false);
            }

            if (alpha)
            {
                HighlightAlpha(placeable);
            }
        }

        public static void DisableBackground(GameObject go, bool alpha = true)
        {
            Placeable placeable = go.GetComponent<Placeable>();

            if (go.name.Contains(" [background]"))
            {
                go.name = go.name.Replace(" [background]", "");
            }

            PlaceableMetadata meta = go.GetComponent<PlaceableMetadata>();
            if (meta.blockSerializeIndex > BackgroundBlocksMod.magicBackgroundNumber)
            {
                meta.blockSerializeIndex -= BackgroundBlocksMod.magicBackgroundNumber;
            }

            SetLayer(placeable, "Default", false);
            SetCollide(go, true);

            if (alpha)
            {
                ResetAlpha(placeable);
            }
        }

        public static void SetLayer(Placeable pla, string layer, bool nochange = true)
        {
            pla.dontChangeArtLayers = nochange;

            foreach (SpriteRenderer spren in pla.gameObject.GetComponents<SpriteRenderer>())
            {
                spren.sortingLayerName = layer;
            }
            foreach (SpriteRenderer spren in pla.gameObject.GetComponentsInChildren<SpriteRenderer>())
            {
                spren.sortingLayerName = layer;
            }
        }

        public static void SetCollide(GameObject go, bool active)
        {
            go.transform.Find("SolidCollider").transform.gameObject.SetActive(active);
            go.transform.Find("InnerHazard").transform.gameObject.SetActive(active);
        }

        public static void HighlightAlpha(Placeable placeable)
        {
            placeable.CustomColor.a = 0.7f;
            placeable.SetColor(placeable.CustomColor);
        }
        
        public static void ResetAlpha(Placeable placeable)
        {
            placeable.CustomColor.a = 1f;
            placeable.SetColor(placeable.CustomColor);
        }
    }
}