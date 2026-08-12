using CustomBlocks.Core;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Media;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace CustomBlocks.Blocks
{
    class PigFarmButton : CustomBlock
    {
        public override int BasedId { get { return 0; } }
        public override string BasePlaceableName { get { return "01_1x1 Box"; } }
        public override string BasePickableBlockName { get { return "01_1x1 Box_Pick"; } }


        public bool sound_playing = false;



        protected SoundPlayer _sound;
        public SoundPlayer PigSound
        {
            get
            {
                if (_sound == null)
                {
                    _sound = new SoundPlayer(Path.Combine(AssetDir, this.Name + ".wav"));
                    _sound.Load();
                }
                return _sound;
            }
        }


        override public PickableBlock CreatePickableBlock()
        {
            PickableBlock pb = base.CreatePickableBlock();
            pb.transform.localPosition -= new Vector3(21.08f, 21.5f, 1);
            pb.transform.localScale = new Vector3(0.75f, 0.75f, 1);

            return pb;
        }

        void OnTriggerEnter2D(Collider2D col)
        {
            if (placed)
            {
                Debug.Log(col.gameObject.name + " : " + gameObject.name + " : " + Time.time);

                Character c = col.GetComponentInParent<Character>();
                OnTrigger(true, c);
            }
        }

        override public Placeable CreatePlaceablePrefab()
        {
            Placeable placeable = base.CreatePlaceablePrefab();
            placeable.gameObject.AddComponent(GetType());

            UnityEngine.Object.Destroy(placeable.transform.Find("SolidCollider").GetComponent<BoxCollider2D>());

            CircleCollider2D cc = placeable.transform.Find("SolidCollider").gameObject.AddComponent<CircleCollider2D>();
            cc.radius = 0.45f;
            //cc.isTrigger = true; //will kill you
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

        public override void OnPlace(Placeable placeable, int playerNumber, bool sendEvent, bool force = false)
        {
            base.OnPlace(placeable, playerNumber, sendEvent, force);
            this.placed = true;
            if (playerNumber > 0)
            {
                OnTrigger(false);
            }
        }

        void OnTrigger(bool load = true, Character c = null)
        {
            if (!sound_playing)
            {
                StartCoroutine(LoadPigFarm(load, c));
            }
        }

        IEnumerator LoadPigFarm(bool load, Character c)
        {
            sound_playing = true;
            PigSound.Play();
            if (load && c != null)
            {
                int[] piggy = { -1, -1, -1, 18, -1, -1, -1, -11241731, 738911751, -1226552711, -886338714, 349086914, -1462554672, -669091654, -289795455 };
                int[] og = c.GetOutfitsAsArray();
                if(og.Length > 6)
                {
                    piggy[6] = og[6];
                }

                //Check for og pigfarm give coin??
                //Else give some pigdirt (negative points)

                SmokePool.Instance.SpawnSmoke(SmokePool.SmokeType.POOF, c.transform.position, 0.5f);

                c.CharacterSprite = Character.Animals.ELEPHANT;
                c.OutfitArt.SwitchToCharacter();
                c.SetOutfitsFromArray(piggy);
            }
            yield return new WaitForSeconds(2);
            sound_playing = false;
        }
    }
}