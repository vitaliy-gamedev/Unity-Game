using System;
using Deadband.CameraSystem;
using Deadband.Combat;
using Deadband.InventorySystem;
using Deadband.Signals;
using GameFoundation.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Deadband.Missions
{
    [DefaultExecutionOrder(-300)]
    [DisallowMultipleComponent]
    public sealed class MissionManager : MonoBehaviour
    {
        [SerializeField] private MissionData missionData;
        [SerializeField] private SignalSystem signalSystem;
        [SerializeField] private ExtractionPoint extractionPoint;
        [SerializeField] private Health[] squadHealth;
        [SerializeField] private Health leaderHealth;
        [SerializeField] private PlayerInventory missionInventory;
        [SerializeField] private RescueTarget rescueTarget;
        [SerializeField] private Health rescueGuardian;

        private float extractionTimeRemaining;
        private float bannerTimeRemaining;
        private string bannerText;
        private bool sceneTransitionRequested;
        private bool resultOverlayVisible;
        private bool isPaused;
        private bool extractionHoldWasValid;
        private string hudNoticeText;
        private float hudNoticeUntil;

        public event Action ObjectiveCompleted;
        public event Action<RunResult> MissionEnded;
        public event Action<MissionState> StateChanged;

        public MissionData Data => missionData;
        public MissionState State { get; private set; } = MissionState.TraceSignal;
        public RunResult Result { get; private set; } = RunResult.None;
        public float ExtractionTimeRemaining => extractionTimeRemaining;
        public float RequiredSignalStrength => missionData != null ? missionData.RequiredSignalStrength : 0f;
        public float CurrentSignalStrength => signalSystem != null ? signalSystem.Strength : 0f;
        public ExtractionPoint ExtractionPoint => extractionPoint;
        public Health RescueGuardian => rescueGuardian;
        public bool ResultOverlayVisible => resultOverlayVisible && !sceneTransitionRequested;
        public bool IsPaused => isPaused && !sceneTransitionRequested;

        public void Configure(
            MissionData data,
            SignalSystem signal,
            ExtractionPoint point,
            Health[] squadMembers,
            Health playerHealth,
            PlayerInventory inventory,
            RescueTarget targetToRescue)
        {
            missionData = data;
            signalSystem = signal;
            extractionPoint = point;
            squadHealth = squadMembers;
            leaderHealth = playerHealth;
            missionInventory = inventory;
            rescueTarget = targetToRescue;
        }

        public void SetRescueGuardian(Health guardian)
        {
            rescueGuardian = guardian;
        }

        private void Start()
        {
            if (signalSystem != null)
            {
                signalSystem.SignalStrengthChanged += HandleSignalStrengthChanged;
            }
            if (extractionPoint != null)
            {
                extractionPoint.ActivationRequested += BeginExtraction;
                extractionPoint.SetAvailable(false);
            }
            if (rescueTarget != null)
            {
                rescueTarget.Rescued += HandleRescued;
                rescueTarget.SetObjectiveAvailable(false);
            }
            if (rescueGuardian != null) rescueGuardian.Died += HandleRescueGuardianDied;
            HandleSignalStrengthChanged(signalSystem != null ? signalSystem.Strength : 0f);
            ShowBanner(
                "MISSION  //  TRACE SIGNAL  >  FIND SCOUT  >  ESCORT BACK  >  EXTRACT",
                7f);
        }

        private void OnDestroy()
        {
            if (signalSystem != null)
            {
                signalSystem.SignalStrengthChanged -= HandleSignalStrengthChanged;
            }
            if (extractionPoint != null)
            {
                extractionPoint.ActivationRequested -= BeginExtraction;
            }
            if (rescueTarget != null) rescueTarget.Rescued -= HandleRescued;
            if (rescueGuardian != null) rescueGuardian.Died -= HandleRescueGuardianDied;
            if (isPaused && !sceneTransitionRequested)
            {
                Time.timeScale = 1f;
                AudioListener.pause = false;
            }
        }

        private void Update()
        {
            bannerTimeRemaining = Mathf.Max(0f, bannerTimeRemaining - Time.unscaledDeltaTime);
            if (State is MissionState.Completed or MissionState.Failed)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
                {
                    RestartMission();
                }
                else if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                {
                    ReturnToMainMenu();
                }
                return;
            }
            if (missionData == null)
            {
                return;
            }

            Keyboard gameplayKeyboard = Keyboard.current;
            if (gameplayKeyboard != null && gameplayKeyboard.escapeKey.wasPressedThisFrame)
            {
                SetPaused(!isPaused);
                return;
            }
            if (isPaused)
            {
                return;
            }

            if (leaderHealth != null && !leaderHealth.IsAlive)
            {
                EndMission(RunResult.FailedExtraction);
                return;
            }
            if (rescueTarget != null && State != MissionState.TraceSignal && !rescueTarget.IsAlive)
            {
                EndMission(RunResult.FailedExtraction);
                return;
            }

            if (State != MissionState.Extracting)
            {
                return;
            }

            bool extractionHoldValid = IsExtractionHoldValid();
            if (extractionHoldValid)
            {
                extractionTimeRemaining -= Time.deltaTime;
            }

            if (extractionHoldValid != extractionHoldWasValid)
            {
                ShowHudNotice(
                    extractionHoldValid
                        ? "EXTRACTION RESUMED"
                        : "EXTRACTION CANCELLED  //  RETURN SQUAD AND SCOUT TO THE ZONE",
                    extractionHoldValid ? 1.8f : 3.4f);
                extractionHoldWasValid = extractionHoldValid;
            }

            if (extractionTimeRemaining <= 0f)
            {
                ResolveExtraction();
            }
        }

        private void HandleSignalStrengthChanged(float strength)
        {
            if (State != MissionState.TraceSignal || missionData == null ||
                strength < missionData.RequiredSignalStrength)
            {
                return;
            }

            State = rescueTarget != null ? MissionState.LocateRescue : MissionState.ExtractionAvailable;
            rescueTarget?.SetObjectiveAvailable(!IsRescueGuardianAlive());
            if (rescueTarget == null) extractionPoint?.SetAvailable(true);
            StateChanged?.Invoke(State);
            ShowBanner(
                rescueTarget != null
                    ? IsRescueGuardianAlive()
                        ? "SIGNAL TRACED  //  WARDEN BLOCKING ACCESS TO THE MISSING SCOUT"
                        : "SIGNAL TRACED  //  MISSING SCOUT LOCATED BENEATH THE RADIO TOWER"
                    : "SIGNAL TRACED  //  EXTRACTION UNLOCKED  //  FOLLOW THE GREEN BEACON",
                6f);
        }

        private void HandleRescueGuardianDied()
        {
            rescueTarget?.SetObjectiveAvailable(State == MissionState.LocateRescue);
            if (State == MissionState.LocateRescue)
            {
                ShowBanner("WARDEN ELIMINATED  //  REACH THE ORANGE-MARKED SCOUT", 5f);
            }
        }

        private void HandleRescued()
        {
            if (State != MissionState.LocateRescue) return;
            State = MissionState.ExtractionAvailable;
            extractionPoint?.SetAvailable(true);
            StateChanged?.Invoke(State);
            ShowBanner("SCOUT SECURED  //  ESCORT HIM TO THE GREEN EXTRACTION BEACON", 6f);
            ObjectiveCompleted?.Invoke();
        }

        private void BeginExtraction()
        {
            if (State != MissionState.ExtractionAvailable || missionData == null)
            {
                return;
            }
            if (!IsRescueInsideExtraction())
            {
                ShowBanner("EXTRACTION BLOCKED  //  BRING THE RESCUED SCOUT INTO THE ZONE", 4f);
                return;
            }

            State = MissionState.Extracting;
            extractionTimeRemaining = missionData.ExtractionDuration;
            extractionHoldWasValid = IsExtractionHoldValid();
            extractionPoint.SetExtractionActive(true);
            StateChanged?.Invoke(State);
            ShowBanner("FINAL STEP  //  KEEP THE SCOUT AND AT LEAST ONE SQUAD MEMBER INSIDE", 5f);
            CombatNoiseSystem.Emit(
                extractionPoint.Position,
                missionData.ExtractionNoiseRadius,
                extractionPoint.gameObject);
        }

        private void ResolveExtraction()
        {
            int extracted = extractionPoint.CountLivingSquadInside();
            EndMission(MissionOutcomeRules.ResolveExtraction(extracted, squadHealth?.Length ?? 0));
        }

        private void EndMission(RunResult result)
        {
            Result = result;
            State = result == RunResult.FailedExtraction ? MissionState.Failed : MissionState.Completed;
            resultOverlayVisible = true;
            Time.timeScale = 0f;
            extractionPoint?.SetExtractionActive(false);
            ThirdPersonCameraController cameraController = FindFirstObjectByType<ThirdPersonCameraController>();
            if (cameraController != null) cameraController.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            StateChanged?.Invoke(State);
            ShowBanner(
                MissionOutcomeRules.IsVictory(result)
                    ? "MISSION SUCCESS  //  YOU WON"
                    : "MISSION FAILED  //  YOU LOST",
                8f);
            MissionEnded?.Invoke(Result);
        }

        private void OnGUI()
        {
            if (missionData == null || sceneTransitionRequested)
            {
                return;
            }

            Rect panel = new(16f, 62f, 430f, 76f);
            Color previous = GUI.color;
            GUI.color = new Color(0.012f, 0.028f, 0.023f, 0.88f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previous;

            GUIStyle titleStyle = new(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.62f, 1f, 0.72f) }
            };
            GUIStyle bodyStyle = new(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(28f, 66f, 402f, 20f), missionData.MissionName, titleStyle);
            GUI.Label(new Rect(28f, 87f, 402f, 47f), GetObjectiveStatus(), bodyStyle);

            if (Time.unscaledTime < hudNoticeUntil && !string.IsNullOrWhiteSpace(hudNoticeText))
            {
                DrawHudNotice(bodyStyle);
            }

            if (resultOverlayVisible && !sceneTransitionRequested &&
                State is MissionState.Completed or MissionState.Failed)
            {
                DrawResultOverlay();
            }
            else if (isPaused)
            {
                DrawPauseOverlay();
            }
        }

        private string GetObjectiveStatus()
        {
            return State switch
            {
                MissionState.TraceSignal =>
                    $"STEP 1/4  FIND THE SIGNAL  //  {Mathf.RoundToInt(CurrentSignalStrength * 100f)} / " +
                    $"{Mathf.RoundToInt(missionData.RequiredSignalStrength * 100f)}%\n" +
                    "Follow stronger readings toward the radio tower.",
                MissionState.LocateRescue =>
                    IsRescueGuardianAlive()
                        ? "STEP 2/4  DEFEAT THE WARDEN\nThe missing scout is trapped beneath the radio tower."
                        : "STEP 2/4  REACH THE RADIO TOWER\nFind the orange locator light and help the missing scout.",
                MissionState.ExtractionAvailable =>
                    $"STEP 3/4  ESCORT SCOUT TO EXTRACTION  //  {GetExtractionDistance():0} m {GetExtractionBearing()}\n" +
                    "Follow the green beacon and enter the zone.",
                MissionState.Extracting =>
                    $"STEP 4/4  {(IsExtractionHoldValid() ? "HOLD EXTRACTION" : "EXTRACTION PAUSED")}  //  " +
                    $"{Mathf.Max(0f, extractionTimeRemaining):0.0}s\n" +
                    $"SCOUT {(IsRescueInsideExtraction() ? "IN ZONE" : "OUTSIDE")}  //  " +
                    $"SQUAD {extractionPoint.CountLivingSquadInside()}/{squadHealth.Length}",
                MissionState.Completed => "MISSION SUCCESS // YOU WON",
                _ => "MISSION FAILED // YOU LOST"
            };
        }

        private float GetExtractionDistance()
        {
            if (extractionPoint == null || leaderHealth == null)
            {
                return 0f;
            }

            Vector3 offset = extractionPoint.Position - leaderHealth.transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        private bool IsRescueInsideExtraction()
        {
            return rescueTarget == null ||
                   (rescueTarget.IsRescued && rescueTarget.IsAlive && extractionPoint != null &&
                    extractionPoint.ContainsPosition(rescueTarget.Position));
        }

        private bool IsExtractionHoldValid()
        {
            return extractionPoint != null && extractionPoint.CountLivingSquadInside() > 0 &&
                   IsRescueInsideExtraction();
        }

        private void ShowHudNotice(string message, float duration)
        {
            hudNoticeText = message;
            hudNoticeUntil = Time.unscaledTime + Mathf.Max(0.1f, duration);
        }

        private void DrawHudNotice(GUIStyle sourceStyle)
        {
            Rect notice = new(16f, 144f, 430f, 34f);
            Color previous = GUI.color;
            GUI.color = new Color(0.16f, 0.055f, 0.02f, 0.94f);
            GUI.DrawTexture(notice, Texture2D.whiteTexture);
            GUI.color = previous;
            GUIStyle style = new(sourceStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.72f, 0.24f) }
            };
            GUI.Label(notice, hudNoticeText, style);
        }

        private bool IsRescueGuardianAlive()
        {
            return rescueGuardian != null && rescueGuardian.IsAlive && rescueGuardian.gameObject.activeInHierarchy;
        }

        private string GetExtractionBearing()
        {
            if (extractionPoint == null || leaderHealth == null)
            {
                return "UNKNOWN";
            }

            Vector3 offset = extractionPoint.Position - leaderHealth.transform.position;
            if (offset.sqrMagnitude < 9f)
            {
                return "ZONE REACHED";
            }

            float angle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            string[] bearings = { "NORTH", "NORTH-EAST", "EAST", "SOUTH-EAST", "SOUTH", "SOUTH-WEST", "WEST", "NORTH-WEST" };
            int index = Mathf.RoundToInt(Mathf.Repeat(angle, 360f) / 45f) % bearings.Length;
            return bearings[index];
        }

        private void ShowBanner(string message, float duration)
        {
            // State changes are already reflected by the compact objective HUD.
            // Large transient banners obscured combat and are intentionally suppressed.
            bannerText = string.Empty;
            bannerTimeRemaining = 0f;
        }

        private void DrawStateBanner(GUIStyle sourceStyle)
        {
            float width = Mathf.Min(680f, Screen.width - 40f);
            Rect banner = new(Screen.width * 0.5f - width * 0.5f, 92f, width, 38f);
            Color previous = GUI.color;
            GUI.color = new Color(0.008f, 0.026f, 0.018f, 0.94f);
            GUI.DrawTexture(banner, Texture2D.whiteTexture);
            GUI.color = previous;
            GUIStyle style = new(sourceStyle)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            GUI.Label(banner, bannerText, style);
        }

        private void DrawResultOverlay()
        {
            float width = 620f;
            float height = 350f;
            Rect panel = new(
                Screen.width * 0.5f - width * 0.5f,
                Screen.height * 0.5f - height * 0.5f,
                width,
                height);
            Color previous = GUI.color;
            GUI.color = new Color(0.008f, 0.018f, 0.014f, 0.96f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previous;

            GUIStyle resultStyle = new(GUI.skin.label)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal =
                {
                    textColor = Result == RunResult.FullExtraction
                        ? new Color(0.4f, 1f, 0.58f)
                        : Result == RunResult.PartialExtraction
                            ? new Color(1f, 0.72f, 0.2f)
                            : new Color(1f, 0.22f, 0.14f)
                }
            };
            GUIStyle detailStyle = new(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(panel.x + 20f, panel.y + 20f, width - 40f, 76f), FormatResult(), resultStyle);
            GUI.Label(
                new Rect(panel.x + 30f, panel.y + 96f, width - 60f, 146f),
                BuildLootResult(),
                detailStyle);

            GUIStyle buttonStyle = new(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            Rect restartButton = new(panel.x + 64f, panel.y + height - 78f, 228f, 48f);
            Rect menuButton = new(panel.x + width - 292f, panel.y + height - 78f, 228f, 48f);
            GUI.enabled = !sceneTransitionRequested;
            if (GUI.Button(restartButton, "START AGAIN  [ENTER]", buttonStyle))
            {
                RestartMission();
            }
            if (GUI.Button(menuButton, "MAIN MENU  [ESC]", buttonStyle))
            {
                ReturnToMainMenu();
            }
            GUI.enabled = true;
        }

        private void DrawPauseOverlay()
        {
            float width = 520f;
            float height = 330f;
            Rect panel = new(
                Screen.width * 0.5f - width * 0.5f,
                Screen.height * 0.5f - height * 0.5f,
                width,
                height);
            Color previous = GUI.color;
            GUI.color = new Color(0.006f, 0.016f, 0.012f, 0.97f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previous;

            GUIStyle titleStyle = new(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.55f, 1f, 0.68f) }
            };
            GUIStyle subtitleStyle = new(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.72f, 0.78f, 0.74f) }
            };
            GUIStyle buttonStyle = new(GUI.skin.button)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold
            };

            GUI.Label(new Rect(panel.x + 24f, panel.y + 24f, width - 48f, 52f), "PAUSED", titleStyle);
            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 70f, width - 48f, 28f),
                "The operation is suspended.  Press ESC to continue.",
                subtitleStyle);

            float buttonX = panel.x + 90f;
            float buttonWidth = width - 180f;
            if (GUI.Button(new Rect(buttonX, panel.y + 112f, buttonWidth, 48f), "CONTINUE  [ESC]", buttonStyle))
            {
                SetPaused(false);
            }
            if (GUI.Button(new Rect(buttonX, panel.y + 174f, buttonWidth, 48f), "RESTART MISSION", buttonStyle))
            {
                LoadPauseDestination(SceneManager.GetActiveScene().name);
            }
            if (GUI.Button(new Rect(buttonX, panel.y + 236f, buttonWidth, 48f), "MAIN MENU", buttonStyle))
            {
                LoadPauseDestination("MainMenuScene");
            }
        }

        private string BuildLootResult()
        {
            if (Result == RunResult.FailedExtraction)
            {
                return "DEFEAT: the leader died or no living member completed extraction.\n\n" +
                       "MISSION LOOT LOST\nNo carried items were recovered.";
            }
            if (missionInventory == null || missionInventory.TotalItemCount == 0)
            {
                return "Victory confirmed: at least one living squad member extracted.\n\n" +
                       "MISSION LOOT\nNo items recovered.";
            }

            return "Victory confirmed: at least one living squad member extracted.\n\n" +
                   $"MISSION LOOT: {missionInventory.TotalItemCount} items  |  " +
                   $"{missionInventory.CurrentWeight:0.0} kg  |  Value {missionInventory.TotalValue}\n\n" +
                   missionInventory.BuildManifest(5);
        }

        public void RestartMission()
        {
            LoadResultDestination(SceneManager.GetActiveScene().name);
        }

        public void ReturnToMainMenu()
        {
            LoadResultDestination("MainMenuScene");
        }

        public void ForceResultForDiagnostics(RunResult result)
        {
            if (State is MissionState.Completed or MissionState.Failed) return;
            EndMission(result);
        }

        public void SetPausedForDiagnostics(bool paused)
        {
            SetPaused(paused);
        }

        private void SetPaused(bool paused)
        {
            if (sceneTransitionRequested || State is MissionState.Completed or MissionState.Failed) return;
            isPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;

            ThirdPersonCameraController cameraController = FindFirstObjectByType<ThirdPersonCameraController>();
            if (cameraController != null) cameraController.enabled = !paused;
            PlayerWeaponController playerWeapon = FindFirstObjectByType<PlayerWeaponController>();
            if (playerWeapon != null)
            {
                playerWeapon.enabled = !paused;
                if (!paused) playerWeapon.SuppressFireUntilRelease();
            }
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
        }

        private void LoadPauseDestination(string sceneName)
        {
            if (!isPaused) return;
            BeginSceneTransition(sceneName);
        }

        private void LoadResultDestination(string sceneName)
        {
            if (sceneTransitionRequested || State is not (MissionState.Completed or MissionState.Failed)) return;
            BeginSceneTransition(sceneName);
        }

        private void BeginSceneTransition(string sceneName)
        {
            if (sceneTransitionRequested) return;
            sceneTransitionRequested = true;
            resultOverlayVisible = false;
            isPaused = false;
            Time.timeScale = 0f;
            AudioListener.pause = true;

            if (ServiceLocator.TryGet<ISceneService>(out ISceneService sceneService))
            {
                sceneService.LoadSceneAsync(sceneName, onComplete: FinishSceneTransition);
                return;
            }

            SceneManager.LoadScene(sceneName);
            FinishSceneTransition();
        }

        private void FinishSceneTransition()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            sceneTransitionRequested = false;
        }

        private string FormatResult()
        {
            return Result switch
            {
                RunResult.FullExtraction => "MISSION SUCCESS — YOU WON\nFULL SQUAD EXTRACTION",
                RunResult.PartialExtraction => "MISSION SUCCESS — YOU WON\nPARTIAL SQUAD EXTRACTION",
                _ => "MISSION FAILED — YOU LOST\nFAILED EXTRACTION"
            };
        }
    }
}
