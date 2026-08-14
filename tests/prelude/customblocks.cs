// Game-side façade for CustomBlocks tests. Injected by Harmonic Sheep Fleet
// after its own prelude (Fleet.* is available). Same dialect rules as the
// prelude: Mono.CSharp, conservative C# 5 — no interpolation, no ?., no
// expression-bodied members.
//
// The mod's public API (CustomBlocks.Core.CustomBlockRegistry) is resolved via
// Type.GetType against the plugin assembly, so this file compiles in the REPL
// whether or not the mod is loaded — scenarios ask ModLoaded() first.

using System;
using System.Collections.Generic;
using UnityEngine;

public static class FleetCB
{
    public const string Version = "1.28";

    static Type Registry()
    {
        return Type.GetType("CustomBlocks.Core.CustomBlockRegistry, CustomBlocksMod");
    }

    public static bool ModLoaded()
    {
        return Registry() != null;
    }

    // ------------------------------------------------------------- json bits

    static string Q(string s)
    {
        if (s == null) return "null";
        return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    static string Arr(List<string> items)
    {
        return "[" + string.Join(",", items.ToArray()) + "]";
    }

    // ------------------------------------------------------------- registry

    // Stable id -> block type, plus the per-session serialize index. This is
    // the contract saves depend on; golden it.
    public static string RegistryJson()
    {
        Type reg = Registry();
        if (reg == null) return "{\"loaded\":false}";

        var byId = (System.Collections.IDictionary)reg
            .GetField("byCustomId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .GetValue(null);

        List<int> ids = new List<int>();
        foreach (System.Collections.DictionaryEntry e in byId) ids.Add((int)e.Key);
        ids.Sort();

        List<string> rows = new List<string>();
        foreach (int id in ids)
        {
            object def = byId[id];
            rows.Add("{\"id\":" + id
                + ",\"type\":" + Q(def.GetType().FullName)
                + ",\"serializeIndex\":" + reg.GetMethod("GetSerializeIndex").Invoke(null, new object[] { def.GetType() })
                + "}");
        }
        return "{\"loaded\":true,\"count\":" + ids.Count + ",\"blocks\":" + Arr(rows) + "}";
    }

    // --------------------------------------------------------- inventory book

    static InventoryBook Book()
    {
        InventoryBook book = UnityEngine.Object.FindObjectOfType<InventoryBook>();
        if (book == null) throw new Exception("FleetCB: no InventoryBook in scene (not in free play place phase?)");
        return book;
    }

    // Structural record of every page: name plus the pickables on it, in child
    // order. This is the golden; screenshots are per-run evidence only.
    public static string BookJson()
    {
        InventoryBook book = Book();
        List<string> pages = new List<string>();
        for (int i = 0; i < book.InventoryPages.Length; i++)
        {
            InventoryPage page = book.InventoryPages[i];
            if (page == null) { pages.Add("{\"index\":" + i + ",\"name\":null}"); continue; }

            List<string> picks = new List<string>();
            foreach (Transform child in page.transform.Find("Items"))
            {
                PickableBlock pb = child.GetComponent<PickableBlock>();
                if (pb != null && child.gameObject.activeSelf)
                    picks.Add(Q(pb.name) );
            }
            pages.Add("{\"index\":" + i + ",\"name\":" + Q(page.name) + ",\"pickables\":" + Arr(picks) + "}");
        }
        return Arr(pages);
    }

    // Freeplay parks the local character at (-1000,-1000) while in cursor
    // mode. Touch tests need the character in the world: same event the R key
    // sends from the cursor side.
    public static void SwitchToPlay()
    {
        PiecePlacementCursor cursor = LocalCursor();
        GameEvent.GameEventManager.SendEvent(
            new GameEvent.FreePlayPlayerSwitchEvent(cursor.networkNumber, GameControl.GamePhase.PLAY));
        cursor.CallCmdSwitchFreeMode();
    }

    // Free play spawns players as characters; the book only shows in cursor
    // mode. Same path the R key takes.
    public static void SwitchToPlace()
    {
        Character ch = null;
        foreach (Character c in UnityEngine.Object.FindObjectsOfType<Character>())
        {
            if (c.gameObject.activeInHierarchy) { ch = c; break; }
        }
        if (ch == null) throw new Exception("FleetCB: no active character to switch");
        GameEvent.GameEventManager.SendEvent(
            new GameEvent.FreePlayPlayerSwitchEvent(ch.networkNumber, GameControl.GamePhase.PLACE));
        ch.CallCmdSwitchFreeMode();
    }

    // Open the book on its first page, or flip forward to a given page. The
    // book's own page numbering is not the array index, so navigation is
    // "reset, then flip N times" — the same thing a player's input does.
    public static void OpenBook(int flips)
    {
        InventoryBook book = Book();
        book.Show(false);
        book.ShowCursor(1);
        book.GotoPage(0, false, false);
        for (int i = 0; i < flips; i++) book.NextPage(1, false, false);
    }

    public static void HideBook()
    {
        Book().Hide();
    }

    public static int BookPageCount()
    {
        return Book().InventoryPages.Length;
    }

    // -------------------------------------------------------- rules tablet

    // The rules screen (block percentages / disable toggles) is a screen of
    // the inventory book's tablet page. Open it the way the game does: book in
    // screen mode on the tablet page, tablet switched to the rules screen.
    // Requires the book, i.e. free play place phase.
    public static string ShowRulesScreen()
    {
        InventoryBook book = Book();
        book.Show(false);
        book.ShowCursor(1);
        book.TurnScreenOn(book.TabletPage);

        Tablet tab = book.TabletPage.GetComponent<Tablet>();
        if (tab == null) throw new Exception("FleetCB: tablet page has no Tablet component");
        tab.OnShowTablet();
        tab.GotoScreen(tab.rulesScreen);
        return tab.rulesScreen.gameObject.name;
    }

    // Proof the screen is actually being presented, so the screenshot check
    // cannot pass while photographing something else.
    public static bool RulesScreenVisible()
    {
        InventoryBook book = Book();
        Tablet tab = book.TabletPage.GetComponent<Tablet>();
        return tab != null && tab.rulesScreen.gameObject.activeInHierarchy;
    }

    // One level deeper: the Block Probability page (per-block percentage and
    // disable controls). Sub-pages of the rules screen are subdialogs; this is
    // the same transition the "Block Probability" row triggers.
    public static string ShowBlockProbability()
    {
        InventoryBook book = Book();
        Tablet tab = book.TabletPage.GetComponent<Tablet>();
        TabletRulesScreen rules = tab.rulesScreen;
        rules.subdialogController.TransitionLeftTo(rules.blockSettingsSubdialog);
        return rules.blockSettingsSubdialog.gameObject.name;
    }

    // The custom blocks are appended, so they live on the LAST page of the
    // block grid. OnClickNextPage ignores its cursor argument.
    public static string GotoLastBlockPage()
    {
        InventoryBook book = Book();
        TabletRulesScreen rules = book.TabletPage.GetComponent<Tablet>().rulesScreen;
        TabletBlockList list = rules.tabletBlockList;
        int guard = 0;
        while (list.CurrentPage < list.NumPages - 1 && guard < 32)
        {
            list.OnClickNextPage(null);
            guard++;
        }
        return (list.CurrentPage + 1) + "/" + list.NumPages;
    }

    public static bool BlockProbabilityVisible()
    {
        InventoryBook book = Book();
        Tablet tab = book.TabletPage.GetComponent<Tablet>();
        TabletRulesScreen rules = tab.rulesScreen;
        return rules.subdialogController.currentSubdialog == rules.blockSettingsSubdialog
            && rules.tabletBlockList.gameObject.activeInHierarchy;
    }

    // Structural record of the tablet block list: every entry's pickable name
    // and serialize index, in list order. Custom blocks must appear here.
    public static string TabletJson()
    {
        TabletBlockList list = null;
        foreach (TabletBlockList l in Resources.FindObjectsOfTypeAll<TabletBlockList>())
        {
            if (l.gameObject.scene.IsValid()) { list = l; break; }
        }
        if (list == null) throw new Exception("FleetCB: no TabletBlockList found");

        List<string> rows = new List<string>();
        for (int i = 0; i < list.tabletBlocks.Length; i++)
        {
            TabletBlock tb = list.tabletBlocks[i];
            if (tb == null || tb.pickableBlockPrefab == null) continue;
            rows.Add("{\"name\":" + Q(tb.pickableBlockPrefab.name)
                + ",\"serializeIndex\":" + tb.pickableBlockPrefab.blockSerializeIndex + "}");
        }
        return Arr(rows);
    }

    // ------------------------------------------------------ place/save/load

    // Place a custom block through Placeable.Place — the path the mod patches.
    public static string PlaceCustom(string typeName, float x, float y)
    {
        Type reg = Registry();
        if (reg == null) throw new Exception("FleetCB: mod not loaded");

        var prefabs = (System.Collections.IEnumerable)reg.GetProperty("Prefabs").GetValue(null, null);
        Placeable prefab = null;
        foreach (Placeable p in prefabs)
        {
            if (p != null && p.name == typeName) { prefab = p; break; }
        }
        if (prefab == null) throw new Exception("FleetCB: no custom block named " + typeName);

        Placeable placed = UnityEngine.Object.Instantiate<Placeable>(prefab);
        placed.gameObject.hideFlags = HideFlags.None;
        placed.gameObject.SetActive(true);
        placed.transform.position = new Vector3(x, y, 0f);
        placed.Place(1, false, true);
        return placed.name;
    }

    // Every custom block present in the scene, sorted, with its placed flag —
    // "restored but never marked placed" must be distinguishable from "gone".
    public static string PlacedCustomJson()
    {
        List<string> names = new List<string>();
        foreach (PlaceableMetadata meta in UnityEngine.Object.FindObjectsOfType<PlaceableMetadata>())
        {
            Placeable p = meta.GetComponent<Placeable>();
            if (p != null && meta.blockSerializeIndex >= 102)
                names.Add(p.name + ":" + meta.blockSerializeIndex + ":" + (p.placed ? "placed" : "unplaced"));
        }
        names.Sort(StringComparer.Ordinal);
        List<string> quoted = new List<string>();
        foreach (string n in names) quoted.Add(Q(n));
        return Arr(quoted);
    }

    // Index-agnostic placed probe: serialize slots are deterministic but not
    // stable across mod-set changes, so scenarios must not hardcode them.
    public static bool IsPlacedCustom(string namePrefix)
    {
        foreach (PlaceableMetadata meta in UnityEngine.Object.FindObjectsOfType<PlaceableMetadata>())
        {
            Placeable p = meta.GetComponent<Placeable>();
            if (p != null && p.placed && p.name.StartsWith(namePrefix)) return true;
        }
        return false;
    }

    // Diagnostic census: every custom-block placeable including hidden/marked
    // ones, with enough state to tell "never restored" from "destroyed after".
    public static string DiagAllCustom()
    {
        List<string> rows = new List<string>();
        foreach (Placeable p in Resources.FindObjectsOfTypeAll<Placeable>())
        {
            if (!p.gameObject.scene.IsValid()) continue;
            PlaceableMetadata meta = p.GetComponent<PlaceableMetadata>();
            if (meta == null || meta.blockSerializeIndex < 102) continue;
            rows.Add(Q(p.name + ":" + meta.blockSerializeIndex + ":" + (p.placed ? "placed" : "unplaced")
                + ":hf=" + p.gameObject.hideFlags + ":marked=" + p.MarkedForDestruction
                + ":pos=" + p.transform.position.x.ToString("F0") + "," + p.transform.position.y.ToString("F0")));
        }
        rows.Sort(StringComparer.Ordinal);
        return Arr(rows);
    }

    // ------------------------------------------------- book pick (real path)

    static PiecePlacementCursor LocalCursor()
    {
        foreach (PiecePlacementCursor c in UnityEngine.Object.FindObjectsOfType<PiecePlacementCursor>())
        {
            if (c.hasAuthority) return c;
        }
        throw new Exception("FleetCB: no local PiecePlacementCursor");
    }

    // The same event a player's click on a book pickable raises. The cursor's
    // handler instantiates the placeable and sends MsgBookPiecePicked, so the
    // pick crosses the network exactly as it does for a real player.
    public static string PickFromBook(string blockName)
    {
        PiecePlacementCursor cursor = LocalCursor();
        PickableBlock pick = null;
        foreach (PickableBlock pb in Resources.FindObjectsOfTypeAll<PickableBlock>())
        {
            if (pb.name == blockName + "_Pick" && pb.gameObject.scene.IsValid()) { pick = pb; break; }
        }
        if (pick == null) throw new Exception("FleetCB: no pickable " + blockName + "_Pick in scene");
        GameEvent.GameEventManager.SendEvent(new GameEvent.PickBlockEvent(cursor.networkNumber, pick, null));
        return pick.name;
    }

    // Place the picked (still unplaced) instance, with sendEvent so the
    // placement itself is networked too.
    public static string PlacePicked(string blockName, float x, float y)
    {
        foreach (PlaceableMetadata meta in UnityEngine.Object.FindObjectsOfType<PlaceableMetadata>())
        {
            Placeable p = meta.GetComponent<Placeable>();
            if (p != null && !p.placed && meta.blockSerializeIndex >= 102 && p.name.StartsWith(blockName))
            {
                p.transform.position = new Vector3(x, y, 0f);
                PiecePlacementCursor cursor = LocalCursor();
                p.Place(cursor.networkNumber, true, true);
                // placing out-of-band leaves cursor.Piece stale; the next
                // pick's SetPiece(destroyPrevious:true) would DESTROY the
                // block we just placed
                if (cursor.Piece == p) cursor.SetPiece(null, false, true);
                return p.name;
            }
        }
        throw new Exception("FleetCB: no unplaced " + blockName + " instance to place");
    }

    // The REAL drop: position the held piece, validate CanPlace, and fire the
    // cursor's own accept handler — MsgPiecePlaced goes out, the echo places
    // the piece on every peer (which Placeable.Place alone does not).
    public static string CursorDropAt(float x, float y)
    {
        PiecePlacementCursor cursor = LocalCursor();
        if (cursor.Piece == null) throw new Exception("FleetCB: cursor holds no piece");
        cursor.transform.position = new Vector3(x, y, cursor.transform.position.z);
        cursor.Piece.transform.position = new Vector3(x, y, cursor.Piece.transform.position.z);
        Physics2D.SyncTransforms();
        if (!cursor.Piece.CanPlace()) return "cannot-place:" + cursor.Piece.name;
        typeof(PiecePlacementCursor)
            .GetMethod("OnAcceptDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(cursor, null);
        return cursor.Piece.name;
    }

    public static string CursorHolds()
    {
        PiecePlacementCursor cursor = LocalCursor();
        return cursor.Piece == null ? "nothing" : cursor.Piece.name;
    }

    // --------------------------------------------- level geometry (QuickSaver)

    // Move a piece the LEVEL shipped with (not one we placed): its new position
    // should persist through save/load via the <moved> records — the behavior
    // the mod's MemorizeInitialLevelPlaceables patch currently destroys.
    public static string MoveLevelPiece(float dx)
    {
        // run before placing anything: every PLACED Placeable in the scene is
        // level geometry (every Placeable carries a Rigidbody2D, so that is no
        // discriminator). Leave the start plank and goal alone.
        foreach (Placeable p in UnityEngine.Object.FindObjectsOfType<Placeable>())
        {
            if (!p.placed) continue;
            if (p.name.Contains("Start") || p.name.Contains("Goal")) continue;
            p.transform.position += new Vector3(dx, 0f, 0f);
            // a real player move (pick + place) refreshes OriginalPosition,
            // which is what the <moved>-record comparison actually reads
            p.OriginalPosition = p.transform.position;
            Physics2D.SyncTransforms();
            return p.name;
        }
        throw new Exception("FleetCB: no level piece found to move");
    }

    // ALL x positions of pieces with that name, sorted — the fixture's boxes
    // share a name, so a single-match probe would be ambiguous.
    public static string LevelPieceX(string pieceName)
    {
        List<string> xs = new List<string>();
        foreach (Placeable p in UnityEngine.Object.FindObjectsOfType<Placeable>())
        {
            if (p.name == pieceName && p.placed)
                xs.Add(p.transform.position.x.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
        }
        if (xs.Count == 0) return "gone";
        xs.Sort(StringComparer.Ordinal);
        return string.Join(";", xs.ToArray());
    }

    // ------------------------------------------------------------ party box

    // Weight of every custom block in the party box's base rotation. 0 means
    // "never offered". Reflection: the list is private, but its query is not.
    public static string PartyWeightsJson()
    {
        PartyBox box = UnityEngine.Object.FindObjectOfType<PartyBox>();
        if (box == null) throw new Exception("FleetCB: no PartyBox in scene (not in party mode?)");
        object weights = box.GetType()
            .GetField("baseBlockWeights", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(box);
        System.Reflection.MethodInfo query = weights.GetType().GetMethod("GetWeightForPlaceable");

        Type reg = Registry();
        var prefabs = (System.Collections.IEnumerable)reg.GetProperty("Prefabs").GetValue(null, null);
        List<string> rows = new List<string>();
        foreach (Placeable p in prefabs)
        {
            rows.Add("{\"name\":" + Q(p.name) + ",\"weight\":" + query.Invoke(weights, new object[] { p }) + "}");
        }
        return Arr(rows);
    }

    public static int PartyWeightOf(string blockName)
    {
        PartyBox box = UnityEngine.Object.FindObjectOfType<PartyBox>();
        if (box == null) throw new Exception("FleetCB: no PartyBox in scene (not in party mode?)");
        object weights = box.GetType()
            .GetField("baseBlockWeights", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(box);
        System.Reflection.MethodInfo query = weights.GetType().GetMethod("GetWeightForPlaceable");

        Type reg = Registry();
        var prefabs = (System.Collections.IEnumerable)reg.GetProperty("Prefabs").GetValue(null, null);
        foreach (Placeable p in prefabs)
        {
            if (p.name == blockName) return (int)query.Invoke(weights, new object[] { p });
        }
        throw new Exception("FleetCB: no custom block named " + blockName);
    }

    public static int SetCustomFrequency(string blockName, int freq)
    {
        Type reg = Registry();
        var prefabs = (System.Collections.IEnumerable)reg.GetProperty("Prefabs").GetValue(null, null);
        foreach (Placeable p in prefabs)
        {
            if (p.name == blockName)
            {
                int idx = p.GetComponent<PlaceableMetadata>().blockSerializeIndex;
                GameSettings gs = GameSettings.GetInstance();
                // both layers, like the tablet UI: the live filter AND the
                // ruleset preset that level entry re-reads the filter from
                gs.SetBlockFrequency(idx, freq);
                GameRulePreset.BlockData data = gs.DefaultRuleset.Blocks[idx];
                data.Frequency = freq;
                gs.DefaultRuleset.Blocks[idx] = data;
                return gs.GetBlockFrequency(idx);
            }
        }
        throw new Exception("FleetCB: no custom block named " + blockName);
    }

    // ------------------------------------------------------------ background

    // Reflection like the registry: this file must keep compiling in the REPL
    // when the mod is NOT loaded (vanilla-profile runs).
    static Type ModType(string fullName)
    {
        Type t = Type.GetType(fullName + ", CustomBlocksMod");
        if (t == null) throw new Exception("FleetCB: " + fullName + " unavailable (mod not loaded?)");
        return t;
    }

    // Make an already-placed block a background block, the way the pick patch
    // does (component + layer). For blocks that cannot go through the real
    // pick path — free play allows each player only ONE cursor-placed block
    // per phase; a second real pick networks a PieceDestroyed for the first.
    public static bool MakeBackground(string namePrefix, string layer)
    {
        Placeable p = PlacedInstance(namePrefix);
        object mbi = ModType("CustomBlocks.CustomBlocksMod")
            .GetMethod("EnableBackgroundBlock")
            .Invoke(null, new object[] { p.gameObject });
        mbi.GetType().GetField("layer").SetValue(mbi, layer);
        return true;
    }

    public static bool SetBackgroundMode(bool on)
    {
        System.Reflection.FieldInfo f = ModType("CustomBlocks.CustomBlocksMod").GetField("enableBackgroundMode");
        f.SetValue(null, on);
        return (bool)f.GetValue(null);
    }

    // Every background block in the scene: cleaned name, layer, alpha, and the
    // (magic-offset) serialize index its metadata carries.
    public static string BackgroundJson()
    {
        Type bbType = ModType("CustomBlocks.Backgrounds.BackgroundBlock");
        string nameTag = (string)bbType.GetField("nameTag").GetValue(null);

        List<string> rows = new List<string>();
        foreach (UnityEngine.Object o in UnityEngine.Object.FindObjectsOfType(bbType))
        {
            MonoBehaviour bb = (MonoBehaviour)o;
            string clean = bb.gameObject.name.Replace(nameTag, "");
            PlaceableMetadata meta = bb.GetComponent<PlaceableMetadata>();
            rows.Add("{\"name\":" + Q(clean)
                + ",\"layer\":" + Q((string)bbType.GetField("layer").GetValue(bb))
                + ",\"alpha\":" + bbType.GetField("alpha").GetValue(bb)
                + ",\"serializeIndex\":" + (meta == null ? -1 : meta.blockSerializeIndex) + "}");
        }
        rows.Sort(StringComparer.Ordinal);
        return Arr(rows);
    }

    // ------------------------------------------------------- behavior probes

    static Placeable PlacedInstance(string namePrefix)
    {
        foreach (PlaceableMetadata meta in UnityEngine.Object.FindObjectsOfType<PlaceableMetadata>())
        {
            Placeable p = meta.GetComponent<Placeable>();
            if (p != null && p.placed && p.name.StartsWith(namePrefix)) return p;
        }
        throw new Exception("FleetCB: no placed instance named " + namePrefix);
    }

    // Teleport the character INTO a placed block, wherever it currently is —
    // rolling blocks leave the cell they were placed in, and a solid-on-solid
    // approach just pushes the character away without any trigger overlap.
    public static bool TeleportIntoPlaced(string namePrefix, float dy)
    {
        Placeable p = PlacedInstance(namePrefix);
        Vector3 pos = p.transform.position;
        return Fleet.PlaceCharacter(pos.x, pos.y + dy);
    }

    // Remove a placed block so it cannot contaminate later steps (a ChickenRoll
    // barrel keeps rolling forever and camps the respawn point).
    public static bool DestroyPlaced(string namePrefix)
    {
        Placeable p = PlacedInstance(namePrefix);
        UnityEngine.Object.Destroy(p.gameObject);
        return true;
    }

    public static string BlockPos(string namePrefix)
    {
        Vector3 pos = PlacedInstance(namePrefix).transform.position;
        return pos.x + ";" + pos.y;
    }

    public static float BlockY(string namePrefix)
    {
        return PlacedInstance(namePrefix).transform.position.y;
    }

    public static bool HasCircleCollider(string namePrefix)
    {
        return PlacedInstance(namePrefix).GetComponentInChildren<CircleCollider2D>() != null;
    }

    public static string LocalAnimal()
    {
        Character c = Fleet.LocalCharacter();
        return c == null ? "none" : c.CharacterSprite.ToString();
    }

    public static int LocalFliesCount()
    {
        Character c = Fleet.LocalCharacter();
        if (c == null) return -1;
        var flies = (System.Collections.ICollection)typeof(Character)
            .GetField("spawnedFlies", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            .GetValue(c);
        return flies == null ? -1 : flies.Count;
    }

    public static bool ReceiverLinked()
    {
        foreach (Placeable p in UnityEngine.Object.FindObjectsOfType<Placeable>())
        {
            object rc = p.GetComponent("RCReceiver");
            if (rc != null && p.placed)
            {
                object linked = rc.GetType().GetField("ConnectedTransmitter").GetValue(rc);
                return linked != null;
            }
        }
        return false;
    }

    // Every placed RC pairing as "rx:<placeableID>=tx:<placeableID|none>",
    // sorted — cross-peer agreement on this string proves the link (and its
    // direction) synchronized, not just that some link exists locally.
    public static string RCLinkJson()
    {
        List<string> rows = new List<string>();
        foreach (Placeable p in UnityEngine.Object.FindObjectsOfType<Placeable>())
        {
            object rc = p.GetComponent("RCReceiver");
            if (rc == null || !p.placed) continue;
            object tx = rc.GetType().GetField("ConnectedTransmitter").GetValue(rc);
            string txId = "none";
            if (tx != null) txId = ((Placeable)tx).ID.ToString();
            rows.Add(Q("rx:" + p.ID + "=tx:" + txId));
        }
        rows.Sort(StringComparer.Ordinal);
        return Arr(rows);
    }

    // How many of the sampled spawn positions land near (x,y) — MultiStart's
    // whole purpose is to add its own transform to the spawn candidates.
    public static int SpawnsNear(float x, float y, int samples, float radius)
    {
        Level level = UnityEngine.Object.FindObjectOfType<Level>();
        if (level == null) throw new Exception("FleetCB: no Level in scene");
        int near = 0;
        for (int i = 0; i < samples; i++)
        {
            Vector3 pos = level.GetSpawnPosition((float)i / samples);
            if (Vector2.Distance(new Vector2(pos.x, pos.y), new Vector2(x, y)) <= radius) near++;
        }
        return near;
    }

    // -------------------------------------------------------------- scoring

    // Feed a genuine suicide point through the real message loop and report
    // what the ScoreKeeper recorded. Vanilla intent: one suicide PointBlock.
    public static string AwardSuicideAndReport(int playerNumber)
    {
        ScoreKeeper keeper = ScoreKeeper.Instance;
        if (keeper == null) throw new Exception("FleetCB: no ScoreKeeper (not in a scored mode?)");
        // raw message, not AwardPoint: default party rules carry no 'suicide'
        // entry in the points dictionary, so AwardPoint silently drops the
        // block before it ever reaches the network. The receive path (the one
        // the PigDirt patch used to hijack) has no such filter.
        MsgPointAwarded msg = new MsgPointAwarded();
        msg.PlayerNumber = playerNumber;
        msg.PointType = PointBlock.pointBlockType.suicide;
        msg.AlwaysAward = false;
        UnityEngine.Networking.NetworkManager.singleton.client.Send(NetMsgTypes.PointAwarded, msg);
        return "sent";
    }

    public static string ScoreBlocksJson()
    {
        ScoreKeeper keeper = ScoreKeeper.Instance;
        if (keeper == null) throw new Exception("FleetCB: no ScoreKeeper");
        List<string> rows = new List<string>();
        foreach (System.Reflection.FieldInfo f in typeof(ScoreKeeper).GetFields(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            var list = f.GetValue(keeper) as System.Collections.IEnumerable;
            if (list == null || !f.FieldType.IsGenericType) continue;
            foreach (object o in list)
            {
                PointBlock pb = o as PointBlock;
                if (pb == null) break;
                rows.Add("{\"field\":" + Q(f.Name) + ",\"player\":" + pb.playerNumber
                    + ",\"type\":" + Q(pb.type.ToString())
                    + ",\"suicideValue\":" + pb.suicideValue
                    + ",\"pointValue\":" + pb.pointValue + "}");
            }
        }
        rows.Sort(StringComparer.Ordinal);
        return Arr(rows);
    }

    // ---------------------------------------------------------------- party

    // Sum of party weights over the VANILLA blocks — 0 means the box can only
    // draw custom blocks.
    public static int VanillaPartyWeightSum()
    {
        PartyBox box = UnityEngine.Object.FindObjectOfType<PartyBox>();
        if (box == null) throw new Exception("FleetCB: no PartyBox in scene");
        object weights = box.GetType()
            .GetField("baseBlockWeights", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(box);
        System.Reflection.MethodInfo query = weights.GetType().GetMethod("GetWeightForPlaceable");

        Type reg = Registry();
        int vanillaCount = (int)reg.GetProperty("OriginalBlockCount").GetValue(null, null);
        GameObject[] prefabs = PlaceableMetadataList.Instance.allBlockPrefabs;
        int sum = 0;
        for (int i = 0; i < vanillaCount && i < prefabs.Length; i++)
        {
            Placeable p = prefabs[i].GetComponent<Placeable>();
            if (p != null && !p.isSetPiece) sum += (int)query.Invoke(weights, new object[] { p });
        }
        return sum;
    }

    public static int ZeroAllVanillaFrequencies()
    {
        Type reg = Registry();
        int vanillaCount = (int)reg.GetProperty("OriginalBlockCount").GetValue(null, null);
        GameSettings gs = GameSettings.GetInstance();
        int changed = 0;
        for (int i = 0; i < vanillaCount; i++)
        {
            gs.SetBlockFrequency(i, 0);
            GameRulePreset.BlockData data = gs.DefaultRuleset.Blocks[i];
            data.Frequency = 0;
            gs.DefaultRuleset.Blocks[i] = data;
            changed++;
        }
        return changed;
    }

    // Undo ZeroAllVanillaFrequencies: default frequencies derive from each
    // block's BaseRarity, which is what the BlockData constructor computes.
    public static int RestoreVanillaFrequencies()
    {
        Type reg = Registry();
        int vanillaCount = (int)reg.GetProperty("OriginalBlockCount").GetValue(null, null);
        GameSettings gs = GameSettings.GetInstance();
        GameObject[] prefabs = PlaceableMetadataList.Instance.allBlockPrefabs;
        int changed = 0;
        for (int i = 0; i < vanillaCount && i < prefabs.Length; i++)
        {
            Placeable p = prefabs[i].GetComponent<Placeable>();
            if (p == null) continue;
            GameRulePreset.BlockData data = new GameRulePreset.BlockData(p);
            gs.DefaultRuleset.Blocks[i] = data;
            gs.SetBlockFrequency(i, data.Frequency);
            changed++;
        }
        return changed;
    }

    static QuickSaver Saver()
    {
        QuickSaver qs = UnityEngine.Object.FindObjectOfType<QuickSaver>();
        if (qs == null) throw new Exception("FleetCB: no QuickSaver in scene");
        return qs;
    }

    // Round-trip through the real save path without touching disk.
    public static string SnapshotB64()
    {
        System.Xml.XmlDocument doc = Saver().GetCurrentXmlSnapshot(false);
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(doc.OuterXml));
    }

    // Every <block> record's blockID + name from a snapshot, sorted. The ids
    // custom blocks write must be magic (5000+) STABLE ids, never raw slots.
    public static string SnapshotBlockIdsB64(string b64)
    {
        string xml = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(b64));
        System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
        doc.LoadXml(xml);

        List<string> rows = new List<string>();
        foreach (System.Xml.XmlNode node in doc.SelectNodes("//block"))
        {
            System.Xml.XmlAttribute id = node.Attributes["blockID"];
            System.Xml.XmlAttribute name = node.Attributes["overrideName"];
            if (id != null && int.Parse(id.Value) >= 5000)
                rows.Add((name != null ? name.Value : "") + ":" + id.Value);
        }
        rows.Sort(StringComparer.Ordinal);
        List<string> quoted = new List<string>();
        foreach (string r in rows) quoted.Add(Q(r));
        return Arr(quoted);
    }

    public static void ClearLevel()
    {
        Saver().QuickClear(false);
    }

    public static bool LoadSnapshotB64(string b64)
    {
        string xml = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(b64));
        return Saver().LoadXmlSnapshotFromString(xml);
    }

    // ------------------------------------------------------------ death watch
    //
    // Freeplay respawns are instant and LastDeath resets with them, so polling
    // AnyCharacterDead races the respawn and randomly misses kills. Count
    // PlayerKilledEvents instead — the authority client sends one for its own
    // character on every KillCharacter, teleport-kill included.

    public static int DeathCount = 0;
    static object deathWatch;

    public static void StartDeathWatch()
    {
        if (deathWatch == null)
        {
            FleetCBDeathListener w = new FleetCBDeathListener();
            GameEvent.GameEventManager.ChangeListener<GameEvent.PlayerKilledEvent>(w, true);
            deathWatch = w;
        }
        DeathCount = 0;
    }

    public static int DeathsSeen()
    {
        return DeathCount;
    }
}

public class FleetCBDeathListener : GameEvent.IGameEventListener
{
    public void handleEvent(GameEvent.GameEvent e)
    {
        FleetCB.DeathCount = FleetCB.DeathCount + 1;
    }
}
