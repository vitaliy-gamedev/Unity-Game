using UnityEngine;

namespace Deadband.Signals
{
    [DisallowMultipleComponent]
    public sealed class SignalSource : MonoBehaviour
    {
        public Vector3 Position => transform.position;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.35f, 1f, 0.55f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, 1.2f);
        }
    }
}
