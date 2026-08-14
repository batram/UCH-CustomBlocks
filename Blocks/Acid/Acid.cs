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

        public override Rect SpriteRect { get { return new Rect(0, 0, 81, 60); } }

        // note: indicator reuses the main sprite file
        protected Sprite ind_sp;
        public Sprite ind_sprite
        {
            get
            {
                if (ind_sp == null)
                {
                    ind_sp = LoadSprite(Name + ".png");
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

            // acid is a hazard wearing a Glue costume, not an attachment: the
            // base's required glue colliders would make the game's attachment
            // validation cull a restored acid that isn't stuck to an
            // attachableWithGlue placeable (the floor is not one)
            foreach (CheckColliding cc in placeable.GetComponentsInChildren<CheckColliding>(true))
            {
                cc.Required = false;
            }

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
            base.OnPlace(placeable, playerNumber, sendEvent, force);
            this.placed = true;

            // the glue colliders live on the GluePiece child, so trigger
            // callbacks never reach this component without its own trigger;
            // cover the block's own cell — the sprite hangs off toward the
            // glued surface and its bounds miss characters standing on it
            if (GetComponent<BoxCollider2D>() == null)
            {
                BoxCollider2D trigger = gameObject.AddComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                trigger.size = new Vector2(1f, 1f);
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}