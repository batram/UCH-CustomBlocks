using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.IO;
using BepInEx.Configuration;

[assembly: AssemblyVersion("0.2")]
[assembly: AssemblyInformationalVersion("0.2")]

namespace CustomBlocks
{
    [BepInPlugin("CustomBlocks", "CustomBlocks", "0.2")]
    public class CustomBlocksMod : BaseUnityPlugin
    {
        public static bool enableCustomBlockMode = false;
        public const int magicBackgroundBlockNumber = 9000;
        public const int magicCustomBlockNumber = 5000;

        public static int selectedLayer = 0;
        public static bool highlightSelectedLayer = false;
        public static string defaultBackgroundLayer = "Background 1";

        public static ConfigEntry<bool> CustomBlocksEnabled;

        public static ConfigEntry<KeyCode> ToggleBackgroundKey;
        public static ConfigEntry<KeyCode> SwitchLayerKey;
        public static ConfigEntry<KeyCode> HighlightBlockKey;


        public static string path;

        void Awake()
        {
            Debug.Log("Moin from CustomBlocks");
            path = Path.GetDirectoryName(this.Info.Location);

            for (int i = 0; i < SortingLayer.layers.Length; i++)
            {
                var s = SortingLayer.layers[i];
                if (s.name == defaultBackgroundLayer)
                {
                    selectedLayer = i;
                }
            }
            CustomBlocksEnabled = Config.Bind("General", "CustomBlocksEnabled", true);

            if (CustomBlocksEnabled.Value)
            {
                //TODO: Enable Background and individual CustomBlocks via config
                new Harmony("CustomBlocks").PatchAll();
            }

            ToggleBackgroundKey = Config.Bind("INPUT", "ToggleBackgroundKey", KeyCode.G, "Keybinding: Toggle background mode for blocks");
            SwitchLayerKey = Config.Bind("INPUT", "SwitchLayerKey", KeyCode.L, "Keybinding: Switch to layer");
            HighlightBlockKey = Config.Bind("INPUT", "HighlightBlockKey", KeyCode.H, "Keybinding: Highlight blocks on current layer");
        }

        public static bool IsCustomBlock(GameObject go)
        {
            var meta = go.GetComponent<PlaceableMetadata>();
            return meta && meta.blockSerializeIndex >= magicBackgroundBlockNumber;
        }

        public static CustomBlock EnableCustomBlock(GameObject go)
        {
            CustomBlock mbi = go.GetComponent<CustomBlock>();
            if (mbi == null)
            {
                mbi = go.AddComponent<CustomBlock>();
            }
            return mbi;
        }

        public static void DisableCustomBlock(GameObject go)
        {
            Object.Destroy(go.GetComponent<CustomBlock>());
        }

        public static bool InFreePlace()
        {
            GameControl gameControl = LobbyManager.instance?.CurrentGameController;
            return GameSettings.GetInstance().GameMode == GameState.GameMode.FREEPLAY
                    && gameControl && gameControl.Phase == GameControl.GamePhase.PLACE;
        }
    }
}