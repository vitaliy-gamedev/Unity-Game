using System.Collections.Generic;
using Deadband.Signals;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deadband.InventorySystem
{
    [DisallowMultipleComponent]
    public sealed class LootPickup : MonoBehaviour
    {
        private sealed class PendingStack
        {
            public ItemData Item;
            public int Quantity;
        }

        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private LootTable lootTable;
        [SerializeField] private SignalSystem signalSystem;
        [SerializeField, Min(0.5f)] private float interactionRange = 2.25f;
        [SerializeField, Range(1, 5)] private int rollCount = 2;

        private readonly List<PendingStack> contents = new();
        private float messageUntil;
        private string feedbackMessage;

        public LootTable Table => lootTable;
        public int RollCount => rollCount;

        public void Configure(
            PlayerInventory targetInventory,
            LootTable table,
            SignalSystem signal,
            int rolls)
        {
            inventory = targetInventory;
            lootTable = table;
            signalSystem = signal;
            rollCount = Mathf.Clamp(rolls, 1, 5);
        }

        private void Start()
        {
            RollContents();
        }

        private void Update()
        {
            if (inventory == null || contents.Count == 0 ||
                Vector3.Distance(transform.position, inventory.transform.position) > interactionRange)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                CollectAvailableLoot();
            }
        }

        private void RollContents()
        {
            contents.Clear();
            if (lootTable == null)
            {
                return;
            }

            float localSignal = signalSystem != null
                ? signalSystem.EvaluateStrengthAtPosition(transform.position)
                : 0f;
            var rolls = new List<LootTable.RollResult>();
            lootTable.Roll(localSignal, rollCount, rolls);
            foreach (LootTable.RollResult roll in rolls)
            {
                PendingStack existing = contents.Find(stack => stack.Item == roll.Item);
                if (existing != null)
                {
                    existing.Quantity += roll.Quantity;
                }
                else
                {
                    contents.Add(new PendingStack { Item = roll.Item, Quantity = roll.Quantity });
                }
            }
        }

        private void CollectAvailableLoot()
        {
            int collected = 0;
            for (int index = contents.Count - 1; index >= 0; index--)
            {
                PendingStack stack = contents[index];
                if (!inventory.TryAdd(stack.Item, stack.Quantity, out int added))
                {
                    continue;
                }

                collected += added;
                stack.Quantity -= added;
                if (stack.Quantity <= 0)
                {
                    contents.RemoveAt(index);
                }
            }

            feedbackMessage = collected > 0
                ? $"Collected {collected} item{(collected == 1 ? string.Empty : "s")}."
                : "Inventory capacity reached.";
            messageUntil = Time.time + 2.2f;
            if (contents.Count == 0)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnGUI()
        {
            if (inventory == null)
            {
                return;
            }

            bool nearby = contents.Count > 0 &&
                          Vector3.Distance(transform.position, inventory.transform.position) <= interactionRange;
            if (nearby)
            {
                GUIStyle promptStyle = new(GUI.skin.label)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.66f, 1f, 0.72f) }
                };
                GUI.Label(
                    new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.7f, 520f, 50f),
                    "[ E ] SEARCH FIELD CACHE",
                    promptStyle);
            }

            if (Time.time < messageUntil)
            {
                GUIStyle feedbackStyle = new(GUI.skin.label)
                {
                    fontSize = 15,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
                GUI.Label(
                    new Rect(Screen.width * 0.5f - 250f, Screen.height * 0.75f, 500f, 34f),
                    feedbackMessage,
                    feedbackStyle);
            }
        }
    }
}
