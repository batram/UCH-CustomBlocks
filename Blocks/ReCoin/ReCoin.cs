using CustomBlocks.Core;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace CustomBlocks.Blocks
{
    class ReCoin : CustomBlock
    {
        public override int BasedId { get { return 30; } }
        public override string BasePlaceableName { get { return "Coin"; } }
        public override string BasePickableBlockName { get { return "Coin_Pick"; } }

        override public PickableBlock CreatePickableBlock()
        {
            PickableBlock pb = base.CreatePickableBlock();
            pb.transform.localPosition -= new Vector3(19f, 20.5f, 1);
            pb.transform.localScale = new Vector3(0.75f, 0.75f, 1);

            return pb;
        }

        override public Placeable CreatePlaceablePrefab()
        {
            Placeable placeable = base.CreatePlaceablePrefab();
            placeable.gameObject.AddComponent(GetType());
            placeable.gameObject.GetComponent<Coin>().ForceAlwaysRespawn = true;

            return placeable;
        }

        override public void FixSprite(Transform sprite_holder)
        {
        }
    }
}