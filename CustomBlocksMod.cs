using BepInEx;
using CustomBlocks.Backgrounds;
using CustomBlocks.Blocks;
using CustomBlocks.Core;
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
        public static bool enableBackgroundMode = false;
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

                // built-in blocks register through the same public API other mods use
                CustomBlockRegistry.Register<OneRoundWood>();
                CustomBlockRegistry.Register<ReCoin>();
                CustomBlockRegistry.Register<MultiStart>();
                CustomBlockRegistry.Register<RCReceiver>();
                CustomBlockRegistry.Register<RCTransmitter>();
                CustomBlockRegistry.Register<FloatyCloud>();
                CustomBlockRegistry.Register<PigFarmButton>();
                CustomBlockRegistry.Register<PigDirt>();
                CustomBlockRegistry.Register<ChickenRoll>();
                CustomBlockRegistry.Register<Acid>();
            }

            ToggleBackgroundKey = Config.Bind("INPUT", "ToggleBackgroundKey", KeyCode.G, "Keybinding: Toggle background mode for blocks");
            SwitchLayerKey = Config.Bind("INPUT", "SwitchLayerKey", KeyCode.L, "Keybinding: Switch to layer");
            HighlightBlockKey = Config.Bind("INPUT", "HighlightBlockKey", KeyCode.H, "Keybinding: Highlight blocks on current layer");
        }

        public static bool IsBackgroundBlock(GameObject go)
        {
            var meta = go.GetComponent<PlaceableMetadata>();
            return meta && meta.blockSerializeIndex >= magicBackgroundBlockNumber;
        }

        public static BackgroundBlock EnableBackgroundBlock(GameObject go)
        {
            BackgroundBlock mbi = go.GetComponent<BackgroundBlock>();
            if (mbi == null)
            {
                mbi = go.AddComponent<BackgroundBlock>();
            }
            return mbi;
        }

        public static void DisableBackgroundBlock(GameObject go)
        {
            Object.Destroy(go.GetComponent<BackgroundBlock>());
        }

        public static bool InFreePlace()
        {
            GameControl gameControl = LobbyManager.instance?.CurrentGameController;
            return GameSettings.GetInstance().GameMode == GameState.GameMode.FREEPLAY
                    && gameControl && gameControl.Phase == GameControl.GamePhase.PLACE;
        }
    }
}