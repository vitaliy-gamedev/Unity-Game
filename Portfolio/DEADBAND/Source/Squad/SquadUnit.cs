using UnityEngine;
using UnityEngine.AI;

namespace Deadband.Squad
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class SquadUnit : MonoBehaviour
    {
        [SerializeField] private GameObject selectionIndicator;
        [SerializeField, Min(0f)] private float stoppingDistance = 0.15f;

        private NavMeshAgent agent;

        public bool IsSelected { get; private set; }
        public bool IsAvailable => isActiveAndEnabled && agent != null && agent.isOnNavMesh;
        public Vector3 Position => transform.position;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.stoppingDistance = stoppingDistance;
            SetSelected(false);
        }

        public void ConfigureSelectionIndicator(GameObject indicator)
        {
            selectionIndicator = indicator;
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(selected);
            }
        }

        public bool MoveTo(Vector3 destination)
        {
            if (!IsAvailable)
            {
                return false;
            }

            agent.isStopped = false;
            return agent.SetDestination(destination);
        }

        public void Stop()
        {
            if (!IsAvailable)
            {
                return;
            }

            agent.isStopped = true;
            agent.ResetPath();
        }
    }
}
