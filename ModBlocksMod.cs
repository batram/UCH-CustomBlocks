using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Reflection;

[assembly: AssemblyVersion("0.0.0.1")]
[assembly: AssemblyInformationalVersion("0.0.0.1")]

namespace ModBlocks
{
    [BepInPlugin("ModBlocks", "ModBlocks", "0.0.0.1")]
    public class ModBlocksMod : BaseUnityPlugin
    {
        public static bool enableModBlockMode = false;
        public static int magicModBlockNumber = 9000;

        public static int selectedLayer = 0;
        public static string defaultBackgroundLayer = "Background 1";

        void Awake()
        {
            Debug.Log("Moin from ModBlocks");

            for (int i = 0; i < SortingLayer.layers.Length; i++)
            {
                var s = SortingLayer.layers[i];
                if (s.name == defaultBackgroundLayer)
                {
                    selectedLayer = i;
                }
            }

            new Harmony("ModBlocks").PatchAll();
        }

        public static bool IsModBlock(GameObject go)
        {
            var meta = go.GetComponent<PlaceableMetadata>();
            return meta && meta.blockSerializeIndex >= magicModBlockNumber;
        }

        public static ModBlock EnableModBlock(GameObject go)
        {
            ModBlock mbi = go.GetComponent<ModBlock>();
            if (mbi == null)
            {
                mbi = go.AddComponent<ModBlock>();
            }
            return mbi;
        }

        public static void DisableModBlock(GameObject go)
        {
            Object.Destroy(go.GetComponent<ModBlock>());
        }

        public static bool InFreePlace()
        {
            GameControl gameControl = Object.FindObjectOfType<GameControl>();
            return GameSettings.GetInstance().GameMode == GameState.GameMode.FREEPLAY
                    && gameControl && gameControl.Phase == GameControl.GamePhase.PLACE;
        }
    }
}