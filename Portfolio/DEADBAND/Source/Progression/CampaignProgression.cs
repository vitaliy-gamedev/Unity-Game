using System;
using System.IO;
using Deadband.InventorySystem;
using Deadband.Missions;
using Deadband.Signals;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deadband.Progression
{
    [DefaultExecutionOrder(-240)]
    [DisallowMultipleComponent]
    public sealed class CampaignProgression : MonoBehaviour
    {
        [SerializeField] private MissionManager missionManager;
        [SerializeField] private PlayerInventory missionInventory;
        [SerializeField] private SignalReceiver signalReceiver;
        [SerializeField] private string saveFileName = "deadband_campaign_v1.json";

        private CampaignProfile profile;
        private string savePath;
        private int lastRecoveredCredits;
        private string upgradeFeedback = string.Empty;

        public CampaignProfile Profile => profile;
        public bool IsOverlayVisible => profile != null && missionManager != null &&
                                        missionManager.ResultOverlayVisible;

        public void Configure(
            MissionManager manager,
            PlayerInventory inventory,
            SignalReceiver receiver)
        {
            missionManager = manager;
            missionInventory = inventory;
            signalReceiver = receiver;
        }

        private void Awake()
        {
            savePath = Path.Combine(Application.persistentDataPath, saveFileName);
            profile = LoadProfile(savePath);
            ApplyProfile();
        }

        private void OnEnable()
        {
            if (missionManager != null)
            {
                missionManager.MissionEnded += HandleMissionEnded;
            }
        }

        private void OnDisable()
        {
            if (missionManager != null)
            {
                missionManager.MissionEnded -= HandleMissionEnded;
            }
        }

        private void Update()
        {
            if (missionManager == null ||
                missionManager.State is not (MissionState.Completed or MissionState.Failed) ||
                !missionManager.ResultOverlayVisible ||
                Keyboard.current == null || !Keyboard.current.uKey.wasPressedThisFrame)
            {
                return;
            }

            if (profile.SignalReceiverLevel >= 4)
            {
                upgradeFeedback = "SIGNAL RECEIVER IS FULLY UPGRADED";
                return;
            }

            if (profile.TryPurchaseSignalReceiverUpgrade(out int price))
            {
                ApplyProfile();
                SaveProfile();
                upgradeFeedback = $"RECEIVER UPGRADED TO LEVEL {profile.SignalReceiverLevel}  (-{price} CR)";
            }
            else
            {
                upgradeFeedback = $"UPGRADE REQUIRES {price} CR";
            }
        }

        private void HandleMissionEnded(RunResult result)
        {
            lastRecoveredCredits = profile.RecordRun(result, missionInventory);
            SaveProfile();
        }

        private void ApplyProfile()
        {
            signalReceiver?.ConfigureUpgradeLevel(profile != null ? profile.SignalReceiverLevel : 1);
        }

        private static CampaignProfile LoadProfile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    CampaignProfile loaded = JsonUtility.FromJson<CampaignProfile>(File.ReadAllText(path));
                    if (loaded != null)
                    {
                        loaded.Normalize();
                        return loaded;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("DEADBAND campaign save could not be loaded: " + exception.Message);
            }

            var profile = new CampaignProfile();
            profile.Normalize();
            return profile;
        }

        private void SaveProfile()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(savePath) ?? Application.persistentDataPath);
                File.WriteAllText(savePath, JsonUtility.ToJson(profile, true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("DEADBAND campaign save could not be written: " + exception.Message);
            }
        }

        private void OnGUI()
        {
            if (!IsOverlayVisible)
            {
                return;
            }

            Rect panel = new(22f, Screen.height - 244f, 410f, 220f);
            Color previous = GUI.color;
            GUI.color = new Color(0.008f, 0.018f, 0.014f, 0.94f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previous;

            GUIStyle title = new(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.42f, 1f, 0.58f) }
            };
            GUIStyle body = new(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(panel.x + 16f, panel.y + 12f, 380f, 26f), "CAMPAIGN PROGRESSION", title);
            GUI.Label(
                new Rect(panel.x + 16f, panel.y + 42f, 380f, 58f),
                $"Credits: {profile.Credits} CR   Recovered this run: {lastRecoveredCredits} CR\n" +
                $"Receiver: LEVEL {profile.SignalReceiverLevel}/4   Runs: {profile.CompletedRuns}",
                body);
            GUI.Label(new Rect(panel.x + 16f, panel.y + 94f, 190f, 110f), profile.BuildStorageManifest(), body);

            int price = CampaignProfile.GetSignalReceiverUpgradePrice(profile.SignalReceiverLevel);
            string upgrade = price > 0
                ? $"[ U ] RECEIVER UPGRADE\n{price} CR"
                : "RECEIVER MAX LEVEL";
            GUI.Label(new Rect(panel.x + 215f, panel.y + 100f, 178f, 52f), upgrade, body);
            if (!string.IsNullOrEmpty(upgradeFeedback))
            {
                GUI.Label(new Rect(panel.x + 215f, panel.y + 154f, 180f, 48f), upgradeFeedback, body);
            }
        }
    }
}
