using UnityEngine;
using UnityEngine.UI;

namespace Factura
{
    public sealed class GameUI : MonoBehaviour
    {
        [SerializeField] private Text healthText;
        [SerializeField] private Image healthFill;
        [SerializeField] private Image progressFill;
        [SerializeField] private Text progressText;
        [SerializeField] private GameObject readyPanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultText;
        [SerializeField] private Text killsText;
        [SerializeField] private Image damageOverlay;
        [SerializeField] private GameManager game;
        [SerializeField] private GameAudio gameAudio;
        [SerializeField] private Button playButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private Button quitButton;
        private Text soundLabel;
        private int lastHealth = -1;
        private int lastPercent = -1;
        private float damagePulse;

        private void OnEnable()
        {
            playButton.onClick.AddListener(Play);
            soundButton.onClick.AddListener(ToggleSound);
            quitButton.onClick.AddListener(Quit);
            soundLabel = soundButton.GetComponentInChildren<Text>();
            RefreshSoundLabel();
        }

        private void OnDisable()
        {
            playButton.onClick.RemoveListener(Play);
            soundButton.onClick.RemoveListener(ToggleSound);
            quitButton.onClick.RemoveListener(Quit);
        }

        private void Play()
        {
            if (game.State == GameState.Ready) game.HandlePress();
        }

        private void ToggleSound()
        {
            gameAudio.SetMuted(!gameAudio.IsMuted);
            RefreshSoundLabel();
        }

        private void RefreshSoundLabel() => soundLabel.text = gameAudio.IsMuted ? "SOUND: OFF" : "SOUND: ON";

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Update()
        {
            damagePulse = Mathf.Max(0, damagePulse - Time.unscaledDeltaTime);
            damageOverlay.color = new Color(0.85f, 0.04f, 0.03f, damagePulse / 0.22f * 0.2f);
        }

        public void ShowHealth(int current, int maximum)
        {
            if (lastHealth >= 0 && current < lastHealth) damagePulse = 0.22f;
            lastHealth = current;
            healthText.text = $"HP  {current} / {maximum}";
            healthFill.fillAmount = (float)current / maximum;
            healthFill.color = (float)current / maximum <= 0.3f ? new Color(1, 0.25f, 0.16f) : new Color(0.25f, 0.88f, 0.6f);
        }

        public void ShowKills(int count) => killsText.text = $"KILLS  {count:00}";

        public void ShowProgress(float value)
        {
            progressFill.fillAmount = value;
            int percent = Mathf.RoundToInt(value * 100);
            if (percent == lastPercent) return;
            lastPercent = percent;
            progressText.text = $"{percent}%";
        }

        public void ShowState(GameState state)
        {
            readyPanel.SetActive(state == GameState.Ready);
            bool result = state == GameState.Win || state == GameState.Lose;
            resultPanel.SetActive(result);
            if (result) resultText.text = state == GameState.Win ? "You win" : "You lose";
            if (state == GameState.Ready)
            {
                damagePulse = 0;
                playButton.interactable = !game.IsPreparing;
                playButton.GetComponentInChildren<Text>().text = game.IsPreparing ? "PREPARING..." : "PLAY";
                RefreshSoundLabel();
            }
        }

        public void ShowPreparationError()
        {
            playButton.interactable = false;
            playButton.GetComponentInChildren<Text>().text = "SETUP ERROR";
            readyPanel.transform.Find("Hint").GetComponent<Text>().text = "Could not prepare the level. Please restart.";
        }
    }
}
