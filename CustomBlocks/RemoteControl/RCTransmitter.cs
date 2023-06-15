using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace CustomBlocks.CustomBlocks
{
    class RCTransmitter : CustomBlock
    {
        public override int BasedId { get { return 72; } }
        public override string BasePlaceableName { get { return "BoxingGlove"; } }
        public override string BasePickableBlockName { get { return "BoxingGlove_Pick"; } }
        public override string Name { get { return typeof(RCTransmitter).Name; } }
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
                    sp = Sprite.Create(texture, new Rect(0, 0, 248, 127), new Vector2(0, 0), 100f);
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
                    ind_sp = Sprite.Create(texture, new Rect(0, 0, 248, 127), new Vector2(0, 0), 100f);
                    Object.DontDestroyOnLoad(ind_sp);
                }
                return ind_sp;
            }
        }

        public RCReceiver ConnectedReceiver;

        override public PickableBlock CreatePickableBlock()
        {
            PickableBlock pb = base.CreatePickableBlock();
            pb.transform.localPosition -= new Vector3(1f, 20.5f, 1);
            pb.transform.localScale = new Vector3(0.75f, 0.75f, 1);

            var sprite_holder = pb.transform.Find("SpriteHolder");
            sprite_holder.GetComponent<Animator>().enabled = false ;
            var glove = pb.transform.Find("SpriteHolder/Offset/Glove");

            glove.GetComponent<SpriteRenderer>().sprite = sprite;
            glove.transform.localScale = new Vector3(1.4f, 1.4f, 1);
            sprite_holder.transform.localPosition = new Vector3(-0.53f, -0.53f, 0);
            glove.transform.localPosition = new Vector3(-11.9327f, -1.25f, 0);

            pb.gameObject.transform.Rotate(0, 0, 90f, Space.Self);

            return pb;
        }

        override public Placeable CreatePlaceablePrefab()
        {
            Placeable placeable = base.CreatePlaceablePrefab();
            placeable.gameObject.AddComponent<RCTransmitter>().alwaysMovingSpriteLayer = true;


            var sprite_holder = placeable.transform.Find("SpriteHolder/Offset/Glove");
            //TODO: Custom animation
            sprite_holder.GetComponent<Animator>().enabled = false;
            var glove = placeable.transform.Find("SpriteHolder/Offset/Glove/Glove");

            glove.GetComponent<SpriteRenderer>().sprite = sprite;
            glove.transform.localScale = new Vector3(1.4f, 1.4f, 1);
            sprite_holder.transform.localPosition = new Vector3(-1.68f, -0.63f, 0);

            placeable.transform.Find("SpriteHolder/Indicators-Crossbow_indicator.000 (1)").gameObject.SetActive(false);

            var Indicator = Object.Instantiate(glove, sprite_holder);
            GameObject.DontDestroyOnLoad(Indicator);
            Indicator.name = "Indicator";
            Indicator.transform.parent = glove.transform.parent;
            SpriteRenderer Indicator_spr = Indicator.GetComponent<SpriteRenderer>();
            Indicator_spr.sprite = ind_sprite;
            placeable.alwaysMovingSpriteLayer = true;

            placeable.gameObject.transform.Rotate(0, 0, 90f, Space.Self);


            return placeable;
        }

        override public void FixSprite(Transform sprite_holder)
        {
        }

        public override void FixedUpdate()
        {
            if (this.gameObject.GetComponent<Placeable>().Placed)
            {
                ConnectToReceiver();
            }
            if (ConnectedReceiver == null)
            {
                ResetConnectionColor();
            }
        }

        public void SetConnectionColor(Color c)
        {
            this.transform.Find("SpriteHolder/Offset/Glove/Indicator").GetComponent<SpriteRenderer>().material.color = c;
        }

        public void ResetConnectionColor()
        {
            this.transform.Find("SpriteHolder/Offset/Glove/Indicator").GetComponent<SpriteRenderer>().material.color = new Color(0, 0, 0, 1);
        }


        public override void OnPlace(Placeable __instance, int playerNumber, bool sendEvent, bool force = false)
        {
            var indi = this.transform.Find("SpriteHolder/Offset/Glove/Indicator")?.GetComponent<SpriteRenderer>();
            if (indi)
            {
                indi.sortingLayerName = "MovingBlocks";
                indi.sortingOrder = 767;

                var glove_spr = this.transform.Find("SpriteHolder/Offset/Glove/Glove")?.GetComponent<SpriteRenderer>();
                if (glove_spr)
                {
                    glove_spr.sortingLayerName = "MovingBlocks";
                    glove_spr.sortingOrder = 766;
                }
            }
        }

        public void ConnectToReceiver()
        {
            //if (this.NetSurrogate == null || this.NetSurrogate.hasAuthority)
            {
                if (this.ConnectedReceiver != null)
                {
                    return;
                }

                foreach (RCReceiver receiver in GameObject.FindObjectsOfType<RCReceiver>())
                {
                    if (receiver != null && receiver.ConnectedTransmitter == null && receiver.gameObject.GetComponent<Placeable>().Placed)
                    {
                        this.ConnectedReceiver = receiver;
                        receiver.ConnectedTransmitter = this;

                        //TODO: Use preset of colors and sync connection color via network
                        Color color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
                        Debug.Log("Place color " + color);
                        this.SetConnectionColor(color);
                        receiver.SetConnectionColor(color);
                        break;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(PunchingBlock), nameof(PunchingBlock.Punch))]
    static class PunchingBlockPatch
    {
        static void Postfix(PunchingBlock __instance, UnityEngine.Collider2D __0)
        {
            try
            {
                RCTransmitter rct = __instance.gameObject.GetComponent<RCTransmitter>();
                if (rct && rct.ConnectedReceiver && rct.ConnectedReceiver.AttachedTo)
                {
                    rct.ConnectedReceiver.Trigger();
                    __instance.StartCoroutine(Reset(__instance));
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Exception in patch :\n{ex}");
            }
        }

        static public IEnumerator<WaitForSeconds> Reset(PunchingBlock __instance)
        {
            yield return new WaitForSeconds(0.3f);
            __instance.punching = false;
            yield return null;
        }
    }

}