using Deadband.Squad;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deadband.CameraSystem
{
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public sealed class MinimapController : MonoBehaviour
    {
        [SerializeField] private Camera minimapCamera;
        [SerializeField] private GameObject minimapPanel;
        [SerializeField, Min(5f)] private float cameraHeight = 30f;
        [SerializeField, Min(1f)] private float orthographicSize = 10.5f;
        [SerializeField, Min(0.01f)] private float followSmoothTime = 0.18f;
        [SerializeField] private Vector2 worldXLimits = new(-4f, 4f);
        [SerializeField] private Vector2 worldZLimits = new(-1f, 1f);

        private SquadUnit[] squadUnits;
        private Vector3 followVelocity;

        public void Configure(
            Camera cameraReference,
            GameObject panel,
            float height,
            float viewSize,
            Vector2 xLimits,
            Vector2 zLimits)
        {
            minimapCamera = cameraReference;
            minimapPanel = panel;
            cameraHeight = Mathf.Max(5f, height);
            orthographicSize = Mathf.Max(1f, viewSize);
            worldXLimits = xLimits;
            worldZLimits = zLimits;
        }

        private void Awake()
        {
            squadUnits = FindObjectsByType<SquadUnit>(FindObjectsSortMode.None);
            if (minimapCamera == null)
            {
                Debug.LogError("Minimap camera reference is missing.", this);
                enabled = false;
                return;
            }

            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = orthographicSize;
            SnapToSquad();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.mKey.wasPressedThisFrame && minimapPanel != null)
            {
                minimapPanel.SetActive(!minimapPanel.activeSelf);
            }
        }

        private void LateUpdate()
        {
            if (!TryGetSquadCenter(out Vector3 center))
            {
                return;
            }

            center.x = Mathf.Clamp(center.x, worldXLimits.x, worldXLimits.y);
            center.z = Mathf.Clamp(center.z, worldZLimits.x, worldZLimits.y);
            Vector3 desiredPosition = new(center.x, cameraHeight, center.z);
            minimapCamera.transform.position = Vector3.SmoothDamp(
                minimapCamera.transform.position,
                desiredPosition,
                ref followVelocity,
                followSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void SnapToSquad()
        {
            if (!TryGetSquadCenter(out Vector3 center))
            {
                center = Vector3.zero;
            }

            center.x = Mathf.Clamp(center.x, worldXLimits.x, worldXLimits.y);
            center.z = Mathf.Clamp(center.z, worldZLimits.x, worldZLimits.y);
            minimapCamera.transform.SetPositionAndRotation(
                new Vector3(center.x, cameraHeight, center.z),
                Quaternion.Euler(90f, 0f, 0f));
        }

        private bool TryGetSquadCenter(out Vector3 center)
        {
            center = Vector3.zero;
            int availableCount = 0;

            foreach (SquadUnit unit in squadUnits)
            {
                if (unit == null || !unit.gameObject.activeInHierarchy)
                {
                    continue;
                }

                center += unit.Position;
                availableCount++;
            }

            if (availableCount == 0)
            {
                return false;
            }

            center /= availableCount;
            return true;
        }
    }
}
