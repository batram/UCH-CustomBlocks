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
        public const int magicBackgroundBlockNumber = 9000;
        public const int magicCustomBlockNumber = 5000;

        // CustomBlockNet global action codes (negative = channel-wide)
        public const short netActionBackground = -1;

        // Background mode, selected layer and highlight are per local player and
        // live in Backgrounds.LayerState.
        public static string defaultBackgroundLayer = "Background 1";

        public static ConfigEntry<bool> CustomBlocksEnabled;

        public static ConfigEntry<KeyCode> ToggleBackgroundKey;
        public static ConfigEntry<KeyCode> PrevLayerKey;
        public static ConfigEntry<KeyCode> SwitchLayerKey;
        public static ConfigEntry<KeyCode> HighlightBlockKey;


        public static string path;

        void Awake()
        {
            Debug.Log("Moin from CustomBlocks");
            path = Path.GetDirectoryName(this.Info.Location);

            CustomBlocksEnabled = Config.Bind("General", "CustomBlocksEnabled", true);

            if (CustomBlocksEnabled.Value)
            {
                //TODO: Enable Background and individual CustomBlocks via config
                new Harmony("CustomBlocks").PatchAll();

                Backgrounds.Patches.BackgroundNetSync.Register();

                // built-in blocks register through the same public API other mods use
                CustomBlockRegistry.Register<OneRoundWood>();
                CustomBlockRegistry.Register<ReCoin>();
                CustomBlockRegistry.Register<MultiStart>();
                CustomBlockRegistry.Register<RCReceiver>();
                CustomBlockRegistry.Register<RCTransmitter>();
                CustomBlockRegistry.Register<FloatyCloud>();
                CustomBlockRegistry.Register<PigDirt>();
                CustomBlockRegistry.Register<ChickenRoll>();
                CustomBlockRegistry.Register<Acid>();
            }

            ToggleBackgroundKey = Config.Bind("INPUT", "ToggleBackgroundKey", KeyCode.G, "Keybinding: Toggle background mode for blocks");
            PrevLayerKey = Config.Bind("INPUT", "PrevLayerKey", KeyCode.K, "Keybinding: Switch to the previous layer");
            SwitchLayerKey = Config.Bind("INPUT", "SwitchLayerKey", KeyCode.L, "Keybinding: Switch to the next layer");
            HighlightBlockKey = Config.Bind("INPUT", "HighlightBlockKey", KeyCode.H, "Keybinding: Highlight blocks on current layer");
        }

        // Unity copies hideFlags to Instantiate clones, so every instance made
        // from the mod's hidden prefabs is born invisible to FindObjectsOfType
        // (save sweeps, ClearLevel, the metadata census). Rendering and physics
        // don't care, which is why it looks fine on screen. Clear the flags on
        // anything that becomes a real scene instance.
        public static void UnhideInstance(GameObject go)
        {
            if (go == null || go.hideFlags == HideFlags.None)
            {
                return;
            }
            go.hideFlags = HideFlags.None;
            foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.hideFlags = HideFlags.None;
            }
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