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
    public const string Version = "1.5";

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
}
