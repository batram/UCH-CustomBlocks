using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomBlocks.Core
{
    // Public entry point for other mods: call Register<T>() from your plugin's
    // Awake (i.e. before the first level loads). Ids are derived from the block's
    // Name, so they are stable across sessions and independent of registration order.
    public static class CustomBlockRegistry
    {
        public const int MinHashedId = 100;   // below this: reserved for legacy ids
        public const int MaxCustomId = 3999;  // magic 5000 + id must stay below magicBackgroundBlockNumber

        static readonly List<CustomBlock> definitions = new List<CustomBlock>();
        static readonly Dictionary<Type, CustomBlock> byType = new Dictionary<Type, CustomBlock>();
        static readonly Dictionary<int, CustomBlock> byCustomId = new Dictionary<int, CustomBlock>();
        static readonly Dictionary<Type, int> customIds = new Dictionary<Type, int>();
        static readonly Dictionary<Type, int> serializeIndices = new Dictionary<Type, int>();

        public static int OriginalBlockCount { get; private set; }
        public static bool Initialized { get; private set; }
        public static int Count { get { return definitions.Count; } }

        // Blocks that existed before stable ids; they keep their historic
        // registration-order ids so old saves resolve to the same block.
        static readonly Dictionary<string, int> legacyIds = new Dictionary<string, int>
        {
            { "OneRoundWood", 0 },
            { "ReCoin", 1 },
            { "MultiStart", 2 },
            { "RCReceiver", 3 },
            { "RCTransmitter", 4 },
            { "FloatyCloud", 5 },
            { "PigFarmButton", 6 },
            { "PigDirt", 7 },
            { "ChickenRoll", 8 },
            { "Acid", 9 },
        };

        public static void Register<T>() where T : CustomBlock, new()
        {
            Register(new T());
        }

        public static void Register(CustomBlock definition)
        {
            if (Initialized)
            {
                Debug.LogError("CustomBlockRegistry: too late to register " + definition.Name
                    + " - register during your plugin's Awake");
                return;
            }
            Type type = definition.GetType();
            if (byType.ContainsKey(type))
            {
                Debug.LogError("CustomBlockRegistry: " + type + " is already registered");
                return;
            }
            if (string.IsNullOrEmpty(definition.Name))
            {
                Debug.LogError("CustomBlockRegistry: " + type + " has no Name - block not registered");
                return;
            }
            foreach (CustomBlock existing in definitions)
            {
                if (existing.Name == definition.Name)
                {
                    Debug.LogError("CustomBlockRegistry: block name '" + definition.Name
                        + "' already registered by " + existing.GetType() + " - block not registered");
                    return;
                }
            }

            definitions.Add(definition);
            byType[type] = definition;
        }

        // Ids are assigned at init, over the definitions sorted by name, with
        // deterministic linear probing on hash collisions — peers running the
        // same mod set agree on every id regardless of registration order.
        static void AssignCustomIds()
        {
            List<CustomBlock> byName = new List<CustomBlock>(definitions);
            byName.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

            int range = MaxCustomId - MinHashedId + 1;
            foreach (CustomBlock definition in byName)
            {
                int id;
                if (!legacyIds.TryGetValue(definition.Name, out id))
                {
                    id = StableId(definition.Name);
                    int probes = 0;
                    while (byCustomId.ContainsKey(id) && probes < range)
                    {
                        id = MinHashedId + ((id - MinHashedId + 1) % range);
                        probes += 1;
                    }
                    if (probes >= range)
                    {
                        Debug.LogError("CustomBlockRegistry: id space exhausted - "
                            + definition.Name + " gets no id");
                        continue;
                    }
                }
                byCustomId[id] = definition;
                customIds[definition.GetType()] = id;
            }
        }

        // FNV-1a over the block name, folded into [MinHashedId, MaxCustomId]
        static int StableId(string name)
        {
            uint hash = 2166136261;
            foreach (char c in name)
            {
                hash = (hash ^ c) * 16777619;
            }
            return MinHashedId + (int)(hash % (uint)(MaxCustomId - MinHashedId + 1));
        }

        public static int GetCustomId(Type type)
        {
            int id;
            if (customIds.TryGetValue(type, out id))
            {
                return id;
            }
            Debug.LogError("CustomBlockRegistry: " + type + " is not registered");
            return -1;
        }

        public static int GetSerializeIndex(Type type)
        {
            int idx;
            if (serializeIndices.TryGetValue(type, out idx))
            {
                return idx;
            }
            Debug.LogError("CustomBlockRegistry: no serialize index for " + type
                + " (registered after init?)");
            return -1;
        }

        // Maps the id stored in a save (magic offset already removed) back to the
        // block's slot in the prefab/ruleset arrays for this session.
        public static bool TryGetSerializeIndexForSaveId(int customId, out int serializeIndex)
        {
            CustomBlock definition;
            if (byCustomId.TryGetValue(customId, out definition))
            {
                serializeIndex = serializeIndices[definition.GetType()];
                return true;
            }
            serializeIndex = -1;
            return false;
        }

        public static IEnumerable<Placeable> Prefabs
        {
            get
            {
                foreach (CustomBlock definition in definitions)
                {
                    yield return definition.PlaceablePrefab;
                }
            }
        }

        public static void InitBlocks()
        {
            if (Initialized)
            {
                return;
            }
            Initialized = true;

            AssignCustomIds();

            GameRulePreset ruleset = GameSettings.GetInstance().DefaultRuleset;
            OriginalBlockCount = ruleset.Blocks.Length;

            // slots follow CustomId order, not registration order, so peers
            // running the same mod set agree on every serialize index no
            // matter which plugin loaded first (review finding #13). Sorting
            // the definitions list itself keeps every downstream iteration
            // (Prefabs, tablet, book) in the same order as the slot map.
            definitions.Sort((a, b) => GetCustomId(a.GetType()).CompareTo(GetCustomId(b.GetType())));

            int slot = OriginalBlockCount;
            foreach (CustomBlock definition in definitions)
            {
                serializeIndices[definition.GetType()] = slot;
                slot += 1;
            }

            // A block that cannot build its prefab is fatal, and deliberately so:
            // this runs from PlaceableMetadataList.Awake, and continuing without
            // it means the game comes up with no metadata list and dies later at
            // the main menu with a KeyNotFoundException naming a vanilla block.
            // Fail here, naming the block that is actually broken.
            List<Placeable> prefabs = new List<Placeable>();
            foreach (CustomBlock definition in definitions)
            {
                try
                {
                    prefabs.Add(definition.PlaceablePrefab);
                }
                catch (Exception e)
                {
                    throw new Exception("CustomBlocks: block '" + definition.Name + "' ("
                        + definition.GetType() + ") failed to build its prefab - see inner exception", e);
                }
            }

            slot = OriginalBlockCount;
            Array.Resize(ref ruleset.Blocks, OriginalBlockCount + prefabs.Count);
            foreach (Placeable prefab in prefabs)
            {
                ruleset.Blocks[slot] = new GameRulePreset.BlockData(prefab);
                slot += 1;
            }

            // register in the item filter explicitly instead of relying on
            // buildItemFilter not having run yet (review finding #2)
            GameSettings settings = GameSettings.GetInstance();
            foreach (Placeable prefab in prefabs)
            {
                if (!settings.itemFilter.ContainsKey(prefab))
                {
                    settings.itemFilter.Add(prefab, new GameRulePreset.BlockData(prefab));
                }
            }
        }
    }
}
