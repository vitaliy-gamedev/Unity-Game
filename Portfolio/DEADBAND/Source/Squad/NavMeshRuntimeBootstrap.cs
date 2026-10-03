using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Deadband.Squad
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class NavMeshRuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] private NavMeshSurface surface;
        [SerializeField] private NavMeshAgent[] agents;

        public int AgentCount => agents?.Length ?? 0;

        public void Configure(NavMeshSurface navMeshSurface, NavMeshAgent[] sceneAgents)
        {
            surface = navMeshSurface;
            agents = sceneAgents;
        }

        private void Awake()
        {
            if (surface == null || surface.navMeshData == null)
            {
                Debug.LogError("DEADBAND runtime NavMesh asset is missing.", this);
                return;
            }

            surface.RemoveData();
            surface.AddData();
            foreach (NavMeshAgent agent in agents)
            {
                if (agent != null) SnapAgentToNavMesh(agent, 18f, true);
            }
        }

        public static bool SnapAgentToNavMesh(NavMeshAgent agent, float searchRadius = 18f, bool enableAfterSnap = true)
        {
            if (agent == null) return false;
            agent.enabled = false;
            float clearance = Mathf.Max(0.75f, agent.radius + 0.35f);
            if (!TryFindSafePosition(
                    agent.transform.position,
                    searchRadius,
                    clearance,
                    agent.areaMask,
                    out Vector3 safePosition))
            {
                Debug.LogError($"No safe NavMesh spawn was found for '{agent.name}'.", agent);
                return false;
            }

            agent.transform.position = safePosition;
            agent.enabled = enableAfterSnap;
            return true;
        }

        public static bool TryFindSafePosition(
            Vector3 desiredPosition,
            float searchRadius,
            float clearance,
            int areaMask,
            out Vector3 safePosition)
        {
            safePosition = desiredPosition;
            float bestScore = float.PositiveInfinity;
            bool found = false;
            const float ringStep = 1.5f;
            const int directions = 20;
            int ringCount = Mathf.Max(1, Mathf.CeilToInt(searchRadius / ringStep));

            for (int ring = 0; ring <= ringCount; ring++)
            {
                int samples = ring == 0 ? 1 : directions;
                float radius = Mathf.Min(searchRadius, ring * ringStep);
                for (int index = 0; index < samples; index++)
                {
                    float angle = index * Mathf.PI * 2f / samples;
                    Vector3 probe = desiredPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    if (!NavMesh.SamplePosition(probe, out NavMeshHit hit, 1.1f, areaMask) ||
                        !NavMesh.FindClosestEdge(hit.position, out NavMeshHit edge, areaMask) ||
                        edge.distance < clearance)
                    {
                        continue;
                    }

                    float score = (hit.position - desiredPosition).sqrMagnitude - Mathf.Min(edge.distance, 4f) * 0.08f;
                    if (score >= bestScore) continue;
                    bestScore = score;
                    safePosition = hit.position;
                    found = true;
                }

                if (found && radius * radius > bestScore + ringStep * ringStep) break;
            }

            return found;
        }
    }
}
