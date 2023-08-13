using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CustomBlocks.CustomBlocks
{
    class CustomBlock : Placeable
    {
        public static Dictionary<int, Placeable> Blocks = new Dictionary<int, Placeable>();

        public static string ImageDir = Path.Combine(CustomBlocksMod.path, "assets");

        public static int OriginalBlockCount;

        public virtual int BasedId { get; }
        public virtual string BasePlaceableName { get; }
        public virtual string BasePickableBlockName { get; }
        public new virtual string Name { get; }

        public static int StaticId { get; set;  }

        virtual public int CustomId
        {
            get { return StaticId; }
            set { StaticId = value; }
        }


        public Sprite sprite;

        private PickableBlock pblock;

        new public PickableBlock PickableBlock
        {
            get
            {
                if (pblock == null)
                {
                    pblock = CreatePickableBlock();
                }
                return pblock;
            }
        }

        private Placeable pla;

        public Placeable PlaceablePrefab
        {
            get
            {
                if (pla == null)
                {
                    pla = CreatePlaceablePrefab();
                    pla.PickableBlock = PickableBlock;
                }
                return pla;
            }
        }

        public override void Awake()
        {

        }

        virtual public PickableBlock CreatePickableBlock()
        {
            var BasePickableBlock = FindObject<PickableBlock>(this.BasePickableBlockName);
            PickableBlock PickB = Object.Instantiate<PickableBlock>(BasePickableBlock);
            PickB.name = this.Name + "_Pick";
            Object.DontDestroyOnLoad(PickB.gameObject);
            PickB.gameObject.hideFlags = HideFlags.HideAndDontSave;
            PickB.blockSerializeIndex = CustomBlock.OriginalBlockCount + CustomId;
            PickB.placeablePrefab = PlaceablePrefab;

            var default_sprite = PickB.transform.Find("ArtHolder/Sprite");

            if (default_sprite)
            {
                this.FixSprite(default_sprite);
            }

            return PickB;
        }

        virtual public void FixSprite(Transform sprite_holder)
        {
            Debug.Log("Plain FixSprite");
        }

        virtual public Placeable CreatePlaceablePrefab()
        {
            var BasePlaceable = FindObject<Placeable>(this.BasePlaceableName);

            Placeable placeable = Object.Instantiate<Placeable>(BasePlaceable);
            Object.DontDestroyOnLoad(placeable);
            Object.DontDestroyOnLoad(placeable.gameObject);
            placeable.gameObject.hideFlags = HideFlags.HideAndDontSave;
            placeable.gameObject.transform.position = new Vector3(2000, 2000, 100);

            placeable.name = this.Name;
            placeable.Name = this.Name;
            placeable.gameObject.name = this.Name;
            placeable.TwitchShortName = "";

            placeable.ID = 0;
            if (placeable.gameObject.GetComponent<PlaceableMetadata>())
            {
                placeable.gameObject.GetComponent<PlaceableMetadata>().blockSerializeIndex = CustomBlock.OriginalBlockCount + CustomId;
            }

            Placeable.AllPlaceables.Remove(placeable);

            this.FixSprite(placeable.transform.Find("Sprite"));
            return placeable;
        }

        static public void AddBlock<T>() where T : CustomBlock, new()
        {
            var block = new T();
            block.CustomId = Blocks.Count;
            Blocks.Add(CustomBlock.OriginalBlockCount + Blocks.Count, block.PlaceablePrefab);
        }


        public static void InitBlocks()
        {
            if (Blocks.Count == 0)
            {
                var c = GameSettings.GetInstance().DefaultRuleset.Blocks.Length;
                CustomBlock.OriginalBlockCount = c;

                AddBlock<OneRoundWood>();
                AddBlock<ReCoin>();
                AddBlock<MultiStart>();
                AddBlock<RCReceiver>();
                AddBlock<RCTransmitter>();
                AddBlock<FloatyCloud>();
                AddBlock<PigFarmButton>();

                Placeable.AllPlaceables = new List<Placeable> { };

                System.Array.Resize(ref GameSettings.GetInstance().DefaultRuleset.Blocks, GameSettings.GetInstance().DefaultRuleset.Blocks.Length + CustomBlock.Blocks.Count);

                foreach (Placeable block in Blocks.Values)
                {
                    GameSettings.GetInstance().DefaultRuleset.Blocks[c] = new GameRulePreset.BlockData(block);
                    c += 1;
                }
            }
        }

        public virtual void OnPlace(Placeable placeable, int playerNumber, bool sendEvent, bool force = false)
        {
        }

        public static Texture2D LoadTexture(string path)
        {
            byte[] data = File.ReadAllBytes(path);

            Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            texture.LoadImage(data);
            texture.name = Path.GetFileNameWithoutExtension(path);

            return texture;
        }

        static public T FindObject<T>(string name) where T : Object
        {
            T pla = null;
            foreach (var o in Resources.FindObjectsOfTypeAll<T>())
            {
                if (o.name == name)
                {
                    pla = o;
                    break;
                }
            }
            return pla;
        }

        public void AddToInventoryPage(InventoryPage inventoryPage)
        {
            inventoryPage.AddPickable(this.PickableBlock);
            this.PickableBlock.InventoryBook = inventoryPage.inventoryBook;
            this.PickableBlock.transform.parent = inventoryPage.transform.Find("Items");
        }
    }
}