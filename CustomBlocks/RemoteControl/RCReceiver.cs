using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ModBlocks.CustomBlocks
{
    class RCReceiver : CustomBlock
    {
        public override int BasedId { get { return 32; } }
        public override string BasePlaceableName { get { return "Glue"; } }
        public override string BasePickableBlockName { get { return "Glue_Pick"; } }
        public override string Name { get { return typeof(RCReceiver).Name; } }
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
                    sp = Sprite.Create(texture, new Rect(0, 0, 72, 73), new Vector2(0, 0), 100f);
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
                    Texture2D texture = LoadTexture(Path.Combine(CustomBlock.ImageDir, this.Name + "_selection_indicator.png"));
                    ind_sp = Sprite.Create(texture, new Rect(0, 0, 72, 73), new Vector2(0, 0), 100f);
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
            BaseSprite.transform.localPosition = new Vector3(-0.68f, -1.13f, 0);
            BaseSprite.transform.localScale = new Vector3(1.4f, 1.4f, 1);

            return pb;
        }

        override public Placeable CreatePlaceablePrefab()
        {
            Placeable placeable = base.CreatePlaceablePrefab();
            placeable.gameObject.AddComponent<RCReceiver>().alwaysMovingSpriteLayer = true;


            var BaseSprite = placeable.transform.Find("GluePiece/Sprite");

            BaseSprite.GetComponent<SpriteRenderer>().sprite = sprite;
            BaseSprite.GetComponent<SpriteRenderer>().sortingLayerName = "MovingBlocks";
            BaseSprite.transform.localScale = new Vector3(1f, 1f, 1);
            BaseSprite.transform.localPosition = new Vector3(-0.68f, - 1.13f, 0);


            var Indicator = Object.Instantiate(BaseSprite, BaseSprite.transform.parent);
            Indicator.name = "Indicator";
            Indicator.transform.parent = BaseSprite.transform.parent;
            this.Indicator_spr = Indicator.GetComponent<SpriteRenderer>();
            this.ArtSprites = new SpriteRenderer[] { this.Indicator_spr };
            this.Indicator_spr.sprite = this.ind_sprite;
            Debug.Log("RCR Place this.Indicator_spr init " + this.Indicator_spr);
            this.Honey = placeable.transform.Find("GluePiece").GetComponent<HoneyPiece>();
            placeable.alwaysMovingSpriteLayer = true;
            Placeable.AllPlaceables = new List<Placeable> { };

            return placeable;
        }

        override public void FixSprite(Transform sprite_holder)
        {
        }

        override public void FixedUpdate()
        {
            if(ConnectedTransmitter == null)
            {
                ResetConnectionColor();
            }
        }


        public void SetConnectionColor(Color c)
        {
            if (this.Indicator_spr)
            {
                this.Indicator_spr.material.color = c;
            }
        }

        public void ResetConnectionColor()
        {
            if (this.Indicator_spr)
            {
                this.Indicator_spr.material.color = new Color(0, 0, 0, 1);
            }
        }

        public void Trigger()
        {
            if (this.AttachedTo)
            {
                this.AttachedTo.transform.GetComponent<ProjectileLauncher>()?.ShootProjectile();
                foreach (ProjectileLauncher pl in this.AttachedTo.transform.GetComponentsInChildren<ProjectileLauncher>())
                {
                    pl.ShootProjectile();
                }
                if (AttachedTo.GetType() == typeof(UpBlower))
                {
                    ToggleUpBlower((UpBlower)AttachedTo);
                }
            }
        }

        public void ToggleUpBlower(UpBlower blower)
        {
            var anim = blower.GetComponent<Animator>();
            string[] comps = new string[] { "SolidCollider (1)", "BlowTriggerPlayer", "BlowTriggerProjectiles", "Particle System", "Particle System (1)", "Particle System (2)" };

            if (anim.speed == 1)
            {
                anim.speed = 0;
                foreach(string comp in comps)
                {
                    blower.transform.Find(comp).gameObject.SetActive(false);
                }
            } else
            {
                anim.speed = 1;
                foreach (string comp in comps)
                {
                    blower.transform.Find(comp).gameObject.SetActive(true);
                }
            }
        }

        public override void OnPlace(Placeable placeable, int playerNumber, bool sendEvent, bool force = false)
        {
            Debug.Log("RCR Place " + placeable);
            this.Indicator_spr = placeable.transform.Find("Indicator")?.GetComponent<SpriteRenderer>();
            if (Indicator_spr)
            {
                Indicator_spr.sortingLayerName = "MovingBlocks";
                Indicator_spr.sortingOrder = 767;
                var glue_spr = placeable.transform.Find("GluePiece/Sprite")?.GetComponent<SpriteRenderer>();
                if (glue_spr)
                {
                    glue_spr.sortingLayerName = "MovingBlocks";
                    glue_spr.sortingOrder = 766;
                }

            }
            this.AttachedTo = placeable.transform.parent?.GetComponent<Placeable>();
            Debug.Log("RCR Place AttachedTo " + this.AttachedTo);
            if (this.AttachedTo)
            {
                if (AttachedTo.GetType() == typeof(ProjectileLauncher))
                {
                    og_interval = (AttachedTo as ProjectileLauncher).interval;
                    (AttachedTo as ProjectileLauncher).interval = 1000000000;
                } else if(AttachedTo.GetType() == typeof(UpBlower))
                {

                }
            }
        }
        public override void OnDestroy()
        {
            if (this.AttachedTo)
            {
                if (AttachedTo.GetType() == typeof(ProjectileLauncher))
                {
                    (AttachedTo as ProjectileLauncher).interval = og_interval;
                }
                else if (AttachedTo.GetType() == typeof(UpBlower))
                {
                    UpBlower blower = (UpBlower) AttachedTo;
                    blower.GetComponent<Animator>().speed = 1;
                }
            }
            base.OnDestroy();

        }
    }
}