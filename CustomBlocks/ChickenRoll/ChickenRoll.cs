using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Media;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace CustomBlocks.CustomBlocks
{
    class ChickenRoll : CustomBlock
    {
        public override int BasedId { get { return 0; } }
        public override string BasePlaceableName { get { return "07_Barrel"; } }
        public override string BasePickableBlockName { get { return "07_Barrel_Pick"; } }
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
                    sp = Sprite.Create(texture, new Rect(0, 0, 160, 160), new Vector2(0, 0), 100f);
                }
                return sp;
            }
        }


        public Vector3 startPosition;
        public Quaternion startRotation;
        public bool chill = false;
        public bool sound_playing = false;

        protected SoundPlayer _sound;
        private Placeable pp;

        public SoundPlayer ChickenBawk
        {
            get
            {
                if (_sound == null)
                {
                    _sound = new SoundPlayer(Path.Combine(CustomBlock.ImageDir, this.Name + ".wav"));
                    _sound.Load();
                }
                return _sound;
            }
        }


        override public PickableBlock CreatePickableBlock()
        {
            PickableBlock pb = base.CreatePickableBlock();
            pb.transform.localPosition -= new Vector3(22.08f, 22.5f, 1);
            pb.transform.localScale = new Vector3(0.50f, 0.50f, 1);

            return pb;
        }

        void OnTriggerEnter2D(Collider2D col)
        {
            if (placed)
            {
                Debug.Log(col.gameObject.name + " : " + gameObject.name + " : " + Time.time);

                Character c = col.GetComponentInParent<Character>();
                OnTrigger(c);
            }
        }

        override public Placeable CreatePlaceablePrefab()
        {
            Placeable placeable = base.CreatePlaceablePrefab();
            placeable.gameObject.AddComponent(GetType());

            UnityEngine.Object.Destroy(placeable.transform.Find("SolidCollider").GetComponent<BoxCollider2D>());

            CircleCollider2D cc = placeable.transform.Find("SolidCollider").gameObject.AddComponent<CircleCollider2D>();
            cc.radius = 0.45f;
            return placeable;
        }


        override public void FixSprite(Transform sprite_holder)
        {
            if (sprite_holder && sprite)
            {
                sprite_holder.GetComponent<SpriteRenderer>().sprite = sprite;
                sprite_holder.transform.localScale = new Vector3(1.5f, 1.5f, 1);
                sprite_holder.transform.localPosition = new Vector3(-1.2f, -1.2f, 0);
            }
        }

        public override void OnPlace(Placeable placeable, int playerNumber, bool sendEvent, bool force = false)
        {
            base.OnPlace(placeable, playerNumber, sendEvent, force);
            pp = placeable;

            placed = true;
            startPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            startRotation = transform.rotation;
        }

        void OnTrigger(Character c = null)
        {
            if (!sound_playing)
            {
                StartCoroutine(ChickenKill(c));
            }
        }

        IEnumerator ChickenKill(Character c)
        {
            if (c != null)
            {            
                sound_playing = true;
                ChickenBawk.Play();
                c.KillCharacter("moep", false, 0, true);
                Debug.Log("dead? " + c.name);
                yield return new WaitForSeconds(2);
                sound_playing = false;
            }
        }

        public override void FixedUpdate()
        {
            if (pp != null && pp.placed && !pp.disabled && !pp.PickedUp && !pp.Placing)
            {

                GameControl gameControl = LobbyManager.instance?.CurrentGameController;
                if (gameControl != null)
                {
                    if (gameControl.Phase == GameControl.GamePhase.PLAY || gameControl.Phase == GameControl.GamePhase.SUDDENDEATH)
                    {
                        transform.Rotate(Vector3.forward * (120f * Time.deltaTime));
                        transform.localPosition -= new Vector3(Time.deltaTime * 2f, 0, 0);
                        return;
                    }
                }
                if (startPosition != null)
                {
                    transform.localPosition = startPosition;
                    transform.rotation = startRotation;
                }
            }
        }
    }
}