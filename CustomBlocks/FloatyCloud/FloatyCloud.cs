using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ModBlocks.CustomBlocks
{
    class FloatyCloud : CustomBlock
    {
        public override int BasedId { get { return 4; } }
        public override string BasePlaceableName { get { return "05_Plank5"; } }
        public override string BasePickableBlockName { get { return "05_Plank5_Pick"; } }
        public override string Name { get { return typeof(FloatyCloud).Name; } }
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
                    //TODO: Get texture from prefab
                    Texture2D texture = LoadTexture(Path.Combine(CustomBlock.ImageDir, this.Name + ".png"));
                    sp = Sprite.Create(texture, new Rect(0, 0, 768, 232), new Vector2(0.5f, 0.5f), 100f);
                }
                return sp;
            }
        }

        public HashSet<Character> CloudPlayers = new HashSet<Character>();
        public Vector3 CloudBasePosition;
        public float speed = 3;

        override public PickableBlock CreatePickableBlock()
        {
            PickableBlock pb = base.CreatePickableBlock();
            pb.transform.localPosition -= new Vector3(19.08f, 18.5f, 1);
            pb.transform.localScale = new Vector3(0.55f, 0.55f, 1);

            return pb;
        }

        override public Placeable CreatePlaceablePrefab()
        {
            Placeable placeable = base.CreatePlaceablePrefab();
            placeable.gameObject.AddComponent(GetType());
            return placeable;
        }

        override public void FixSprite(Transform sprite_holder)
        {
            if (sprite_holder && sprite)
            {
                sprite_holder.GetComponent<SpriteRenderer>().sprite = sprite;
                sprite_holder.transform.localScale = new Vector3(1, 1, 1);
                sprite_holder.transform.localPosition = new Vector3(0f, 0.1f, 0);
            }
        }

        void Update()
        {
            if (Placed)
            {
                if (CloudPlayers.Count > 0)
                {
                    this.transform.position -= new Vector3(0, speed * Time.deltaTime * CloudPlayers.Count, 0);
                }
                else
                {
                    float step = speed * Time.deltaTime;
                    transform.position = Vector3.MoveTowards(transform.position, CloudBasePosition, step);
                }
            }
        }

        void OnTriggerEnter2D(Collider2D col)
        {
            Character c = col.GetComponentInParent<Character>();
            if (c == null)
            {
                return;
            }

            CloudPlayers.Add(c);
        }

        void OnTriggerExit2D(Collider2D col)
        {
            Character c = col.GetComponentInParent<Character>();
            if (c == null)
            {
                return;
            }

            CloudPlayers.Remove(c);
        }

        public override void OnPlace(Placeable placeable, int playerNumber, bool sendEvent, bool force = false)
        {
            base.OnPlace(placeable, playerNumber, sendEvent, force);
            CloudBasePosition = placeable.transform.position;
            this.placed = true;
        }

    }
}