using System.Collections.Generic;
using UnityEngine;

namespace Deadband.Squad
{
    public static class SquadFormation
    {
        public static List<Vector3> CreateTwoColumnSlots(
            Vector3 destination,
            Vector3 forward,
            int unitCount,
            float spacing)
        {
            var slots = new List<Vector3>(unitCount);
            if (unitCount <= 0)
            {
                return slots;
            }

            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            int columnCount = unitCount == 1 ? 1 : 2;
            int rowCount = Mathf.CeilToInt(unitCount / (float)columnCount);

            for (int index = 0; index < unitCount; index++)
            {
                int row = index / columnCount;
                int column = index % columnCount;
                float horizontal = columnCount == 1 ? 0f : (column - 0.5f) * spacing;
                float depth = (row - (rowCount - 1) * 0.5f) * spacing;
                slots.Add(destination + right * horizontal - forward * depth);
            }

            return slots;
        }

        public static Dictionary<SquadUnit, Vector3> AssignNearestSlots(
            IReadOnlyList<SquadUnit> units,
            IReadOnlyList<Vector3> slots)
        {
            var result = new Dictionary<SquadUnit, Vector3>(units.Count);
            var remainingUnits = new List<SquadUnit>(units);
            var remainingSlots = new List<Vector3>(slots);

            while (remainingUnits.Count > 0 && remainingSlots.Count > 0)
            {
                float bestDistance = float.PositiveInfinity;
                int bestUnit = 0;
                int bestSlot = 0;

                for (int unitIndex = 0; unitIndex < remainingUnits.Count; unitIndex++)
                {
                    for (int slotIndex = 0; slotIndex < remainingSlots.Count; slotIndex++)
                    {
                        float distance = (remainingUnits[unitIndex].Position - remainingSlots[slotIndex]).sqrMagnitude;
                        if (distance >= bestDistance)
                        {
                            continue;
                        }

                        bestDistance = distance;
                        bestUnit = unitIndex;
                        bestSlot = slotIndex;
                    }
                }

                result.Add(remainingUnits[bestUnit], remainingSlots[bestSlot]);
                remainingUnits.RemoveAt(bestUnit);
                remainingSlots.RemoveAt(bestSlot);
            }

            return result;
        }
    }
}
