using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace CustomBlocks.CustomBlocks
{
    class FloatyCloud : CustomBlock
    {
        public override int BasedId { get { return 4; } }
        public override string BasePlaceableName { get { return "05_Plank5"; } }
        public override string BasePickableBlockName { get { return "05_Plank5_Pick"; } }
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
        public float FloatDownMax = 10;
        public float debounceTimer = 0f;

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

            GameObject sc = placeable.transform.Find("SolidCollider").gameObject;
            sc.GetComponent<BoxCollider2D>().usedByEffector = true;
            PlatformEffector2D peff = sc.AddComponent<PlatformEffector2D>();

            placeable.transform.Find("InnerHazard").gameObject.SetActive(false);

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
                Vector3 downtown = CloudBasePosition - new Vector3(0, FloatDownMax, 0);

                if (CloudPlayers.Count > 0)
                {
                    float step = speed * CloudPlayers.Count * Time.deltaTime;
                    transform.position = Vector3.MoveTowards(transform.position, downtown, step);
                }
                else
                {
                    float step = speed * Time.deltaTime;
                    transform.position = Vector3.MoveTowards(transform.position, CloudBasePosition, step);
                }

                float a = (Mathf.Abs(transform.position.y - downtown.y) / FloatDownMax) + 0.2f;
                transform.Find("Sprite").GetComponent<SpriteRenderer>().color = new Color(0.5f, 0.5f, 0.5f, a);
                if (debounceTimer < 0)
                {
                    GameObject sc = transform.Find("SolidCollider").gameObject;
                    if (a < 0.5f)
                    {
                        if (sc.activeSelf) {
                            debounceTimer = 2f;
                            sc.SetActive(false);
                        }
                    }
                    else
                    {
                        sc.SetActive(true);
                    }
                }
                else
                {
                    debounceTimer -= Time.deltaTime;
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