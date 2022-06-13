using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ModBlocks.CustomBlocks
{
    class MultiStart : CustomBlock
    {
        public const int XCustomId = 104;
        public override int CustomId { get { return XCustomId; } }
        public override int BasedId { get { return 38; } }

        public override string BasePlaceableName { get { return "StartPlank"; } }
        public override string BasePickableBlockName { get { return "StartPlank_Pick"; } }

        new public string Name = typeof(MultiStart).Name;

        public MultiStart()
        {
            base.Name = this.Name;
        }

        override public PickableBlock CreatePickableBlock()
        {
            PickableBlock pb = base.CreatePickableBlock();
            pb.transform.localPosition -= new Vector3(19f, 23.5f, 1);
            pb.transform.localScale = new Vector3(0.75f, 0.75f, 1);

            //pb.transform.Find("StartZone").gameObject.SetActive(false);
            pb.transform.Find("StartZone").GetComponentInChildren<Text>().text = "MultiStart";
            pb.transform.Find("StartZone").transform.parent = pb.transform.Find("ArtHolder");

            /*
            foreach(SpriteRenderer sp in pb.transform.GetComponentsInChildren<SpriteRenderer>()){
                sp.enabled = false;
            }*/
            return pb;
        }

        override public Placeable CreatePlaceablePrefab()
        {
            Placeable placeable = base.CreatePlaceablePrefab();
            placeable.gameObject.AddComponent<MultiStart>();
            placeable.transform.Find("StartZone").GetComponentInChildren<Text>().text = "MultiStart";

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