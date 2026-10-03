using System;
using System.Collections.Generic;
using System.Text;
using Deadband.InventorySystem;
using Deadband.Missions;
using UnityEngine;

namespace Deadband.Progression
{
    [Serializable]
    public sealed class CampaignProfile
    {
        [Serializable]
        public sealed class StoredItem
        {
            [SerializeField] private string itemId;
            [SerializeField] private string displayName;
            [SerializeField] private int quantity;

            public string ItemId => itemId;
            public string DisplayName => displayName;
            public int Quantity => quantity;

            public StoredItem(string id, string itemName, int amount)
            {
                itemId = id;
                displayName = itemName;
                quantity = amount;
            }

            public void Add(int amount)
            {
                quantity += Mathf.Max(0, amount);
            }
        }

        [SerializeField] private int saveVersion = 1;
        [SerializeField] private int credits;
        [SerializeField] private int signalReceiverLevel = 1;
        [SerializeField] private int completedRuns;
        [SerializeField] private int successfulExtractions;
        [SerializeField] private int lifetimeRecoveredValue;
        [SerializeField] private List<StoredItem> storage = new();

        public int SaveVersion => saveVersion;
        public int Credits => credits;
        public int SignalReceiverLevel => signalReceiverLevel;
        public int CompletedRuns => completedRuns;
        public int SuccessfulExtractions => successfulExtractions;
        public int LifetimeRecoveredValue => lifetimeRecoveredValue;
        public IReadOnlyList<StoredItem> Storage => storage;

        public void Normalize()
        {
            saveVersion = Mathf.Max(1, saveVersion);
            credits = Mathf.Max(0, credits);
            signalReceiverLevel = Mathf.Clamp(signalReceiverLevel, 1, 4);
            completedRuns = Mathf.Max(0, completedRuns);
            successfulExtractions = Mathf.Clamp(successfulExtractions, 0, completedRuns);
            lifetimeRecoveredValue = Mathf.Max(0, lifetimeRecoveredValue);
            storage ??= new List<StoredItem>();
            storage.RemoveAll(item => item == null || string.IsNullOrWhiteSpace(item.ItemId) || item.Quantity <= 0);
        }

        public int RecordRun(RunResult result, PlayerInventory inventory)
        {
            completedRuns++;
            if (result == RunResult.FailedExtraction || result == RunResult.None || inventory == null)
            {
                return 0;
            }

            successfulExtractions++;
            float resultMultiplier = result == RunResult.FullExtraction ? 1f : 0.7f;
            int recoveredValue = Mathf.RoundToInt(inventory.TotalValue * resultMultiplier);
            credits += recoveredValue;
            lifetimeRecoveredValue += recoveredValue;
            foreach (PlayerInventory.InventoryStack stack in inventory.Stacks)
            {
                AddToStorage(stack.Item, stack.Quantity);
            }
            return recoveredValue;
        }

        public bool TryPurchaseSignalReceiverUpgrade(out int price)
        {
            price = GetSignalReceiverUpgradePrice(signalReceiverLevel);
            if (price <= 0 || credits < price)
            {
                return false;
            }

            credits -= price;
            signalReceiverLevel++;
            return true;
        }

        public static int GetSignalReceiverUpgradePrice(int currentLevel)
        {
            return currentLevel switch
            {
                1 => 300,
                2 => 700,
                3 => 1400,
                _ => 0
            };
        }

        public string BuildStorageManifest(int maximumLines = 5)
        {
            if (storage.Count == 0)
            {
                return "Storage empty.";
            }

            var builder = new StringBuilder();
            int lines = Mathf.Min(Mathf.Max(1, maximumLines), storage.Count);
            for (int index = 0; index < lines; index++)
            {
                StoredItem item = storage[index];
                builder.Append(item.DisplayName).Append("  x").Append(item.Quantity).AppendLine();
            }
            if (storage.Count > lines)
            {
                builder.Append('+').Append(storage.Count - lines).Append(" more item types");
            }
            return builder.ToString().TrimEnd();
        }

        private void AddToStorage(ItemData item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                return;
            }
            foreach (StoredItem storedItem in storage)
            {
                if (storedItem.ItemId == item.ItemId)
                {
                    storedItem.Add(quantity);
                    return;
                }
            }
            storage.Add(new StoredItem(item.ItemId, item.DisplayName, quantity));
        }
    }
}
