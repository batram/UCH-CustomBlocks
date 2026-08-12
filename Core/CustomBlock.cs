using System.IO;
using UnityEngine;

namespace CustomBlocks.Core
{
    // Base class for custom blocks. Derive from it, override the Base* names of
    // the vanilla block to clone, then CustomBlockRegistry.Register<T>() it from
    // your plugin's Awake.
    public class CustomBlock : Placeable
    {
        public virtual int BasedId { get; }
        public virtual string BasePlaceableName { get; }
        public virtual string BasePickableBlockName { get; }
        public new virtual string Name { get { return GetType().Name; } }

        // Stable identity, resolved via the registry so it also works on the
        // component instances AddComponent creates at runtime.
        public int CustomId { get { return CustomBlockRegistry.GetCustomId(GetType()); } }
        public int SerializeIndex { get { return CustomBlockRegistry.GetSerializeIndex(GetType()); } }

        // Assets live next to the assembly that defines the block, so blocks
        // from other mods load their own sprites and sounds.
        public virtual string AssetDir
        {
            get { return Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "assets"); }
        }

        public virtual Rect SpriteRect { get { return new Rect(0, 0, 54, 54); } }
        public virtual Vector2 SpritePivot { get { return Vector2.zero; } }

        private Sprite sp;
        public Sprite sprite
        {
            get
            {
                if (sp == null)
                {
                    sp = LoadSprite(Name + ".png");
                }
                return sp;
            }
        }

        protected Sprite LoadSprite(string file)
        {
            Texture2D texture = LoadTexture(Path.Combine(AssetDir, file));
            Sprite s = Sprite.Create(texture, SpriteRect, SpritePivot, 100f);
            Object.DontDestroyOnLoad(s);
            return s;
        }

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
            PickB.blockSerializeIndex = SerializeIndex;
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
                placeable.gameObject.GetComponent<PlaceableMetadata>().blockSerializeIndex = SerializeIndex;
            }

            Placeable.AllPlaceables.Remove(placeable);

            this.FixSprite(placeable.transform.Find("Sprite"));
            return placeable;
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
