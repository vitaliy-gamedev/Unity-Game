using UnityEngine;

namespace Deadband.Signals
{
    [DisallowMultipleComponent]
    public sealed class SignalReceiver : MonoBehaviour
    {
        public Vector3 Position => transform.position;
        public int UpgradeLevel { get; private set; } = 1;

        public void ConfigureUpgradeLevel(int level)
        {
            UpgradeLevel = Mathf.Clamp(level, 1, 4);
        }
    }
}
