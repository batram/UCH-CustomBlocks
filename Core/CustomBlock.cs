using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

        // The bytes of one of this block's assets. Blocks are read through here
        // rather than straight off disk so a block can choose where its art
        // lives: an asset embedded in the defining assembly wins over a file in
        // AssetDir, so a block can bake its art into its own DLL and have
        // nothing left to deploy.
        //
        // Worth preferring, because this runs during
        // PlaceableMetadataList.Awake: a file that failed to deploy throws from
        // inside that Awake, the game then never assigns
        // PlaceableMetadataList.instance, GameSettings can never build its item
        // filter, and the main menu dies on the first ReadBlockSettings.
        public virtual byte[] ReadAsset(string file)
        {
            Assembly assembly = GetType().Assembly;
            string resource = FindEmbeddedAsset(assembly, file);
            if (resource != null)
            {
                using (Stream stream = assembly.GetManifestResourceStream(resource))
                {
                    byte[] embedded = new byte[stream.Length];
                    int read = 0;
                    while (read < embedded.Length)
                    {
                        int n = stream.Read(embedded, read, embedded.Length - read);
                        if (n <= 0) break;
                        read += n;
                    }
                    return embedded;
                }
            }
            return File.ReadAllBytes(Path.Combine(AssetDir, file));
        }

        // Same asset, as a stream — for APIs that take one, such as
        // System.Media.SoundPlayer.
        public Stream OpenAsset(string file)
        {
            return new MemoryStream(ReadAsset(file));
        }

        // Embedded resource names are prefixed with the defining project's root
        // namespace and folders, so match on the trailing file name.
        static string FindEmbeddedAsset(Assembly assembly, string file)
        {
            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (string.Equals(name, file, System.StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith("." + file, System.StringComparison.OrdinalIgnoreCase))
                {
                    return name;
                }
            }
            return null;
        }

        public virtual Rect SpriteRect { get { return new Rect(0, 0, 54, 54); } }
        public virtual Vector2 SpritePivot { get { return Vector2.zero; } }

        // How the block presents on the Block Probability tablet page.
        //
        // Both default to automatic, which is right for most blocks: the tile
        // is scaled so the art reads at the same size as the vanilla block it
        // is based on, then centred in its 300x300 tile. Auto-fit means a block
        // whose artwork changes never needs these retuned, and blocks from
        // other mods get sensible sizing without knowing any of this exists.
        //
        // Override when a block should deliberately differ. Note that some
        // vanilla blocks are not themselves centred — Glue sits high in its
        // tile — so a glue-based block that should sit exactly where vanilla
        // glue sits wants TabletOffset = Vector2.zero rather than auto.
        public virtual float? TabletScale { get { return null; } }
        public virtual Vector2? TabletOffset { get { return null; } }

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
            Texture2D texture = CreateTexture(ReadAsset(file), Path.GetFileNameWithoutExtension(file));
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
                    // Aligning here would measure too early — see
                    // PickableBlockEnablePatch. Just record the owner so the
                    // patch can find its way back to this block.
                    OwnerOf[pblock] = this;
                }
                return pblock;
            }
        }

        // Which block owns a given pickable, so the Enable patch can call back
        // into the right instance for MinPickSize.
        internal static readonly Dictionary<PickableBlock, CustomBlock> OwnerOf =
            new Dictionary<PickableBlock, CustomBlock>();

        // Smallest clickable box, in block units. Artwork alone would make
        // PigDirt a 0.41-unit target; vanilla picks are 1x1 or larger.
        public virtual float MinPickSize { get { return 0.75f; } }

        // Make the clickable area agree with the drawn artwork.
        //
        // The pick collider arrives from the cloned base block: it is that
        // block's footprint, sitting at the pickable's origin. The artwork is
        // then moved and resized independently — Acid shifts its BaseSprite to
        // (-0.88,-1.33) and scales it, the sprite swap changes its extents —
        // and nothing keeps the two in step. Measured on the book page, Acid's
        // and RCReceiver's hitboxes had drifted further than their own height
        // from their art, so the thing you see and the thing you can pick up
        // did not overlap at all.
        public void AlignPickCollider(PickableBlock pick)
        {
            Bounds art;
            if (pick != null && VisibleBounds(pick.transform, out art))
            {
                AlignPickCollider(pick, art);
            }
        }

        public void AlignPickCollider(PickableBlock pick, Bounds art)
        {
            if (pick == null || pick.PickColliders == null) return;

            foreach (Collider2D collider in pick.PickColliders)
            {
                BoxCollider2D box = collider as BoxCollider2D;
                if (box == null) continue;

                Transform t = box.transform;
                Vector3 centre = t.InverseTransformPoint(art.center);
                Vector3 size = t.InverseTransformVector(art.size);

                box.offset = new Vector2(centre.x, centre.y);
                box.size = new Vector2(
                    Mathf.Max(Mathf.Abs(size.x), MinPickSize),
                    Mathf.Max(Mathf.Abs(size.y), MinPickSize));
            }
        }

        // Bounds of what a thing actually DRAWS.
        //
        // Three ways a SpriteRenderer contributes nothing on screen while still
        // reporting a size: no sprite, disabled, or fully transparent. The last
        // one is not hypothetical — the glue rig's StickingBlock and
        // RotatingBlock sit at colour alpha 0, and counting them inflated
        // Acid's height from 0.45 to 2.91.
        //
        // Computed from sprite.bounds through the renderer's matrix rather than
        // Renderer.bounds, which can still describe a transform as it was
        // before the line that just moved it.
        public static bool VisibleBounds(Transform root, out Bounds bounds)
        {
            bounds = new Bounds();
            bool any = false;
            foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                // activeInHierarchy matters as well as enabled: deactivating a
                // GameObject is the only way to hide art that Enable(true)
                // would otherwise switch back on, and such a renderer still
                // reports enabled == true.
                if (sr.sprite == null || !sr.enabled || !sr.gameObject.activeInHierarchy
                    || sr.color.a <= 0.01f) continue;
                Bounds local = sr.sprite.bounds;
                Matrix4x4 m = sr.transform.localToWorldMatrix;
                for (int i = 0; i < 4; i++)
                {
                    Vector3 corner = new Vector3((i & 1) == 0 ? local.min.x : local.max.x,
                                                 (i & 2) == 0 ? local.min.y : local.max.y, 0f);
                    Vector3 world = m.MultiplyPoint3x4(corner);
                    if (!any) { bounds = new Bounds(world, Vector3.zero); any = true; }
                    else bounds.Encapsulate(world);
                }
            }
            return any;
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
            // hideFlags do not propagate: sub-element placeables (e.g. GluePiece)
            // would still be swept up by FindObjectsOfType during saves
            foreach (Transform child in placeable.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
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

            // the hidden prefab and its sub-element placeables (GluePiece) must
            // not linger in the global census
            Placeable.AllPlaceables.Remove(placeable);
            foreach (Placeable child in placeable.GetComponentsInChildren<Placeable>(true))
            {
                Placeable.AllPlaceables.Remove(child);
            }

            this.FixSprite(placeable.transform.Find("Sprite"));
            return placeable;
        }

        public virtual void OnPlace(Placeable placeable, int playerNumber, bool sendEvent, bool force = false)
        {
        }

        // A CustomBlockNet message addressed to this block (by Placeable.ID)
        // arrived — on every peer, including the one that sent it.
        public virtual void OnNetworkEvent(MsgCustomBlockEvent e)
        {
        }

        // The base Placeable component carrying the real networked ID; the
        // CustomBlock component is itself a Placeable, so a plain
        // GetComponent<Placeable>() is ambiguous.
        public Placeable RealPlaceable
        {
            get
            {
                foreach (Placeable p in GetComponents<Placeable>())
                {
                    if (!(p is CustomBlock))
                    {
                        return p;
                    }
                }
                return this;
            }
        }

        public static Texture2D LoadTexture(string path)
        {
            return CreateTexture(File.ReadAllBytes(path), Path.GetFileNameWithoutExtension(path));
        }

        public static Texture2D CreateTexture(byte[] data, string name)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            texture.LoadImage(data);
            texture.name = name;

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
