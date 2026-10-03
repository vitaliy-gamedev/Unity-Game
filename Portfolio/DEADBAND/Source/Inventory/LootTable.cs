using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deadband.InventorySystem
{
    [CreateAssetMenu(menuName = "DEADBAND/Inventory/Loot Table", fileName = "LT_ZoneCache")]
    public sealed class LootTable : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private ItemData item;
            [SerializeField, Min(0.01f)] private float baseWeight = 1f;
            [SerializeField, Min(1)] private int minimumQuantity = 1;
            [SerializeField, Min(1)] private int maximumQuantity = 1;

            public ItemData Item => item;
            public float BaseWeight => baseWeight;
            public int MinimumQuantity => minimumQuantity;
            public int MaximumQuantity => maximumQuantity;

            public Entry(ItemData itemData, float weight, int minimum, int maximum)
            {
                item = itemData;
                baseWeight = Mathf.Max(0.01f, weight);
                minimumQuantity = Mathf.Max(1, minimum);
                maximumQuantity = Mathf.Max(minimumQuantity, maximum);
            }
        }

        public readonly struct RollResult
        {
            public ItemData Item { get; }
            public int Quantity { get; }

            public RollResult(ItemData item, int quantity)
            {
                Item = item;
                Quantity = quantity;
            }
        }

        [SerializeField] private List<Entry> entries = new();

        public int EntryCount => entries.Count;
        public IReadOnlyList<Entry> Entries => entries;

        public void Configure(IEnumerable<Entry> configuredEntries)
        {
            entries = configuredEntries != null ? new List<Entry>(configuredEntries) : new List<Entry>();
        }

        public void Roll(float signalStrength, int rollCount, List<RollResult> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }
            results.Clear();
            if (entries.Count == 0 || rollCount <= 0)
            {
                return;
            }

            float strength = Mathf.Clamp01(signalStrength);
            for (int roll = 0; roll < rollCount; roll++)
            {
                Entry selected = SelectEntry(strength);
                if (selected == null || selected.Item == null)
                {
                    continue;
                }
                int quantity = UnityEngine.Random.Range(selected.MinimumQuantity, selected.MaximumQuantity + 1);
                results.Add(new RollResult(selected.Item, quantity));
            }
        }

        private Entry SelectEntry(float signalStrength)
        {
            float totalWeight = 0f;
            foreach (Entry entry in entries)
            {
                if (entry?.Item != null)
                {
                    totalWeight += entry.BaseWeight * GetSignalModifier(entry.Item.Rarity, signalStrength);
                }
            }

            float choice = UnityEngine.Random.value * totalWeight;
            foreach (Entry entry in entries)
            {
                if (entry?.Item == null)
                {
                    continue;
                }
                choice -= entry.BaseWeight * GetSignalModifier(entry.Item.Rarity, signalStrength);
                if (choice <= 0f)
                {
                    return entry;
                }
            }
            return entries.Count > 0 ? entries[^1] : null;
        }

        private static float GetSignalModifier(ItemRarity rarity, float signalStrength)
        {
            return rarity switch
            {
                ItemRarity.Common => Mathf.Lerp(1.15f, 0.55f, signalStrength),
                ItemRarity.Uncommon => Mathf.Lerp(0.85f, 1.35f, signalStrength),
                ItemRarity.Rare => Mathf.Lerp(0.4f, 2.2f, signalStrength),
                ItemRarity.VeryRare => Mathf.Lerp(0.12f, 3.8f, signalStrength),
                _ => 1f
            };
        }
    }
}
