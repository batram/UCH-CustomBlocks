using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.IO;

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

            new Harmony("CustomBlocks").PatchAll();
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