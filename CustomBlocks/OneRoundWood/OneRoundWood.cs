using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ModBlocks.CustomBlocks
{
    class OneRoundWood : CustomBlock
    {
        public override int BasedId { get { return 0; } }
        public override string BasePlaceableName { get { return "01_1x1 Box"; } }
        public override string BasePickableBlockName { get { return "01_1x1 Box_Pick"; } }
        public override string Name { get { return typeof(OneRoundWood).Name; } }
        public new static int StaticId { get; set; }
        public override int CustomId
        {
            get { return StaticId; }
            set { StaticId = value; }
        }

        protected Sprite sp;
        new public Sprite sprite
        {
            get
            {
                if (sp == null)
                {
                    Texture2D texture = LoadTexture(Path.Combine(CustomBlock.ImageDir, this.Name + ".png"));
                    sp = Sprite.Create(texture, new Rect(0, 0, 54, 54), new Vector2(0, 0), 100f);
                }
                return sp;
            }
        }

        override public PickableBlock CreatePickableBlock()
        {
            PickableBlock pb = base.CreatePickableBlock();
            pb.transform.localPosition -= new Vector3(20.08f, 20.5f, 1);
            pb.transform.localScale = new Vector3(0.75f, 0.75f, 1);

            return pb;
        }

        override public Placeable CreatePlaceablePrefab()
        {
            Placeable placeable = base.CreatePlaceablePrefab();
            placeable.gameObject.AddComponent<OneRoundWood>();

            Object.Destroy(placeable.transform.Find("SolidCollider").GetComponent<BoxCollider2D>());

            CircleCollider2D cc = placeable.transform.Find("SolidCollider").gameObject.AddComponent<CircleCollider2D>();
            cc.radius = 0.45f;

            return placeable;
        }


        override public void FixSprite(Transform sprite_holder)
        {
            if (sprite_holder && sprite)
            {
                sprite_holder.GetComponent<SpriteRenderer>().sprite = sprite;
                sprite_holder.transform.localScale = new Vector3(2, 2, 1);
                sprite_holder.transform.localPosition = new Vector3(-0.53f, -0.53f, 0);
            }
        }
    }
}