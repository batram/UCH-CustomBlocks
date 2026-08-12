using CustomBlocks.Core;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace CustomBlocks.Blocks
{
    class Acid : CustomBlock
    {
        public override int BasedId { get { return 32; } }
        public override string BasePlaceableName { get { return "Glue"; } }
        public override string BasePickableBlockName { get { return "Glue_Pick"; } }
        public override string Name { get { return GetType().Name; } }
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
                    sp = Sprite.Create(texture, new Rect(0, 0, 81, 60), new Vector2(0, 0), 100f);
                    Object.DontDestroyOnLoad(sp);
                }
                return sp;
            }
        }

        protected Sprite ind_sp;
        public Sprite ind_sprite
        {
            get
            {
                if (ind_sp == null)
                {
                    Texture2D texture = LoadTexture(Path.Combine(CustomBlock.ImageDir, this.Name + ".png"));
                    ind_sp = Sprite.Create(texture, new Rect(0, 0, 81, 60), new Vector2(0, 0), 100f);
                    Object.DontDestroyOnLoad(ind_sp);
                }
                return ind_sp;
            }
        }

        public Placeable AttachedTo;
        public float og_interval;
        public Placeable ConnectedTransmitter;
        public SpriteRenderer Indicator_spr;
        public Placeable Honey;

        override public PickableBlock CreatePickableBlock()
        {
            PickableBlock pb = base.CreatePickableBlock();
            pb.transform.localPosition -= new Vector3(19f, 26.5f, 1);
            pb.transform.localScale = new Vector3(0.75f, 0.75f, 1);

            var BaseSprite = pb.transform.Find("ArtHolder/rotatingCenter/BaseSprite");

            BaseSprite.GetComponent<SpriteRenderer>().sprite = sprite;
            BaseSprite.transform.localPosition = new Vector3(-0.88f, -1.33f, 0);
            BaseSprite.transform.localScale = new Vector3(1.4f, 1.4f, 1);

            return pb;
        }

        override public Placeable CreatePlaceablePrefab()
        {
            Placeable placeable = base.CreatePlaceablePrefab();
            placeable.gameObject.AddComponent<Acid>().alwaysMovingSpriteLayer = true;


            var BaseSprite = placeable.transform.Find("GluePiece/Sprite");

            BaseSprite.GetComponent<SpriteRenderer>().sprite = sprite;

            return placeable;
        }

        override public void FixSprite(Transform sprite_holder)
        {
        }

        public void Trigger()
        {

        }
        
        void OnTriggerEnter2D(Collider2D col)
        {
            if (placed)
            {
                Debug.Log("acid: " + col.gameObject.name + " : " + gameObject.name + " : " + Time.time);

                Character c = col.GetComponentInParent<Character>();
                if (c != null)
                {
                    c.KillCharacter("moep", false, 0, true);
                }
            }
        }

        public override void OnPlace(Placeable placeable, int playerNumber, bool sendEvent, bool force = false)
        {
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}