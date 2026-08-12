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

            int id;
            if (!legacyIds.TryGetValue(definition.Name, out id))
            {
                id = StableId(definition.Name);
            }
            if (byCustomId.ContainsKey(id))
            {
                Debug.LogError("CustomBlockRegistry: id collision between " + definition.Name
                    + " and " + byCustomId[id].Name + " (id " + id + ") - block not registered");
                return;
            }

            definitions.Add(definition);
            byType[type] = definition;
            byCustomId[id] = definition;
            customIds[type] = id;
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

            GameRulePreset ruleset = GameSettings.GetInstance().DefaultRuleset;
            OriginalBlockCount = ruleset.Blocks.Length;

            // slots are assigned in registration order, but identity in saves
            // comes from the stable CustomId, so order shifts are harmless
            int slot = OriginalBlockCount;
            foreach (CustomBlock definition in definitions)
            {
                serializeIndices[definition.GetType()] = slot;
                slot += 1;
            }

            List<Placeable> prefabs = new List<Placeable>();
            foreach (CustomBlock definition in definitions)
            {
                prefabs.Add(definition.PlaceablePrefab);
            }

            Placeable.AllPlaceables = new List<Placeable> { };

            slot = OriginalBlockCount;
            Array.Resize(ref ruleset.Blocks, OriginalBlockCount + prefabs.Count);
            foreach (Placeable prefab in prefabs)
            {
                ruleset.Blocks[slot] = new GameRulePreset.BlockData(prefab);
                slot += 1;
            }
        }
    }
}
