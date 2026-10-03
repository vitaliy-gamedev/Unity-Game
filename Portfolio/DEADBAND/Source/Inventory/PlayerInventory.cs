using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deadband.InventorySystem
{
    [DefaultExecutionOrder(-350)]
    [DisallowMultipleComponent]
    public sealed class PlayerInventory : MonoBehaviour
    {
        [Serializable]
        public sealed class InventoryStack
        {
            [SerializeField] private ItemData item;
            [SerializeField] private int quantity;

            public ItemData Item => item;
            public int Quantity => quantity;

            public InventoryStack(ItemData itemData, int amount)
            {
                item = itemData;
                quantity = amount;
            }

            public void Add(int amount)
            {
                quantity += amount;
            }
        }

        [SerializeField, Min(1)] private int maximumSlots = 8;
        [SerializeField, Min(1f)] private float maximumWeight = 20f;
        [SerializeField] private List<InventoryStack> stacks = new();

        private bool inventoryVisible;

        public event Action Changed;

        public IReadOnlyList<InventoryStack> Stacks => stacks;
        public int MaximumSlots => maximumSlots;
        public float MaximumWeight => maximumWeight;
        public int UsedSlots => stacks.Count;
        public int TotalItemCount { get; private set; }
        public float CurrentWeight { get; private set; }
        public int TotalValue { get; private set; }

        public void Configure(int slotLimit, float weightLimit)
        {
            maximumSlots = Mathf.Max(1, slotLimit);
            maximumWeight = Mathf.Max(1f, weightLimit);
            RecalculateTotals();
        }

        public bool TryAdd(ItemData item, int requestedAmount, out int addedAmount)
        {
            addedAmount = 0;
            if (item == null || requestedAmount <= 0)
            {
                return false;
            }

            int weightCapacity = Mathf.FloorToInt((maximumWeight - CurrentWeight + 0.0001f) / item.Weight);
            int remaining = Mathf.Min(requestedAmount, Mathf.Max(0, weightCapacity));
            if (remaining <= 0)
            {
                return false;
            }

            foreach (InventoryStack stack in stacks)
            {
                if (stack.Item != item || stack.Quantity >= item.MaximumStack)
                {
                    continue;
                }

                int amount = Mathf.Min(remaining, item.MaximumStack - stack.Quantity);
                stack.Add(amount);
                remaining -= amount;
                addedAmount += amount;
                if (remaining <= 0)
                {
                    break;
                }
            }

            while (remaining > 0 && stacks.Count < maximumSlots)
            {
                int amount = Mathf.Min(remaining, item.MaximumStack);
                stacks.Add(new InventoryStack(item, amount));
                remaining -= amount;
                addedAmount += amount;
            }

            if (addedAmount <= 0)
            {
                return false;
            }

            RecalculateTotals();
            Changed?.Invoke();
            return true;
        }

        public string BuildManifest(int maximumLines = 8)
        {
            if (stacks.Count == 0)
            {
                return "No recovered loot.";
            }

            var builder = new StringBuilder();
            int lines = Mathf.Min(maximumLines, stacks.Count);
            for (int index = 0; index < lines; index++)
            {
                InventoryStack stack = stacks[index];
                builder.Append(stack.Item.DisplayName)
                    .Append("  x")
                    .Append(stack.Quantity)
                    .AppendLine();
            }

            if (stacks.Count > lines)
            {
                builder.Append("+").Append(stacks.Count - lines).Append(" more stacks");
            }
            return builder.ToString().TrimEnd();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.iKey.wasPressedThisFrame)
            {
                inventoryVisible = !inventoryVisible;
            }
        }

        private void RecalculateTotals()
        {
            TotalItemCount = 0;
            CurrentWeight = 0f;
            TotalValue = 0;
            stacks.RemoveAll(stack => stack == null || stack.Item == null || stack.Quantity <= 0);
            foreach (InventoryStack stack in stacks)
            {
                TotalItemCount += stack.Quantity;
                CurrentWeight += stack.Item.Weight * stack.Quantity;
                TotalValue += stack.Item.BaseValue * stack.Quantity;
            }
        }

        private void OnGUI()
        {
            if (!inventoryVisible)
            {
                return;
            }

            float width = 420f;
            float height = 330f;
            Rect panel = new(Screen.width - width - 28f, Screen.height - height - 28f, width, height);
            Color previous = GUI.color;
            GUI.color = new Color(0.008f, 0.02f, 0.015f, 0.96f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previous;

            GUIStyle titleStyle = new(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.44f, 1f, 0.59f) }
            };
            GUIStyle itemStyle = new(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(panel.x + 20f, panel.y + 16f, width - 40f, 28f), "FIELD INVENTORY", titleStyle);
            GUI.Label(
                new Rect(panel.x + 20f, panel.y + 48f, width - 40f, 24f),
                $"{UsedSlots}/{MaximumSlots} slots   {CurrentWeight:0.0}/{MaximumWeight:0.0} kg   Value {TotalValue}",
                itemStyle);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 82f, width - 40f, height - 104f), BuildManifest(), itemStyle);
        }
    }
}
