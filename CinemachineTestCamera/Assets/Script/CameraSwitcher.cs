using UnityEngine;
using Unity.Cinemachine;

namespace CameraDemo
{
    public enum CameraMode
    {
        ManualOrbit = 1,
        ManualFollow = 2,
        CinemachineFollow = 3,
        CinemachineComposer = 4,
        CinemachineOrbital = 5,
        CinemachineDolly = 6,
        CinemachineImpulse = 7,
    }

    public class CameraSwitcher : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera manualOrbitCamera;
        [SerializeField] private Camera manualFollowCamera;

        [SerializeField] private CinemachineCamera followCamera;
        [SerializeField] private CinemachineCamera composerCamera;
        [SerializeField] private CinemachineCamera orbitalCamera;
        [SerializeField] private CinemachineCamera dollyCamera;
        [SerializeField] private CinemachineCamera impulseCamera;

        [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
        [SerializeField] private CinemachineImpulseSource impulseSource;

        private const int ActivePriority = 10;
        private const int InactivePriority = 0;

        private CameraMode currentMode;
        private bool orbitalInitialized;

        private void Start()
        {
            SwitchTo(CameraMode.CinemachineFollow);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchTo(CameraMode.ManualOrbit);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchTo(CameraMode.ManualFollow);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchTo(CameraMode.CinemachineFollow);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchTo(CameraMode.CinemachineComposer);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchTo(CameraMode.CinemachineOrbital);
            if (Input.GetKeyDown(KeyCode.Alpha6)) SwitchTo(CameraMode.CinemachineDolly);
            if (Input.GetKeyDown(KeyCode.Alpha7)) SwitchTo(CameraMode.CinemachineImpulse);

            if (Input.GetKeyDown(KeyCode.Space) && impulseSource != null)
            {
                impulseSource.GenerateImpulse();
            }

            if (currentMode == CameraMode.CinemachineOrbital)
            {
                UpdateOrbitalInput();
            }
        }

        private void SwitchTo(CameraMode mode)
        {
            currentMode = mode;
            bool isManual = mode == CameraMode.ManualOrbit || mode == CameraMode.ManualFollow;

            mainCamera.gameObject.SetActive(!isManual);
            manualOrbitCamera.gameObject.SetActive(mode == CameraMode.ManualOrbit);
            manualFollowCamera.gameObject.SetActive(mode == CameraMode.ManualFollow);

            SetPriority(followCamera, mode == CameraMode.CinemachineFollow);
            SetPriority(composerCamera, mode == CameraMode.CinemachineComposer);
            SetPriority(orbitalCamera, mode == CameraMode.CinemachineOrbital);
            SetPriority(dollyCamera, mode == CameraMode.CinemachineDolly);
            SetPriority(impulseCamera, mode == CameraMode.CinemachineImpulse);

            if (mode == CameraMode.CinemachineOrbital && !orbitalInitialized)
            {
                orbitalInitialized = true;
                orbitalFollow.HorizontalAxis.Value = 0f;
                orbitalFollow.VerticalAxis.Value = 17.5f;
            }
        }

        private void UpdateOrbitalInput()
        {
            orbitalFollow.HorizontalAxis.Value += Input.GetAxisRaw("Mouse X") * 0.2f;
            orbitalFollow.VerticalAxis.Value -= Input.GetAxisRaw("Mouse Y") * 0.05f;
        }

        private static void SetPriority(CinemachineCamera camera, bool active)
        {
            if (camera != null)
            {
                camera.Priority.Value = active ? ActivePriority : InactivePriority;
            }
        }
    }
}
