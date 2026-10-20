using UnityEngine;
using UnityEngine.UI;

namespace Factura
{
    /// <summary>
    /// Представлення меню, результату й HUD: показує дані, не обчислює правила бою.
    /// Одна проста панель Ready з Play/Sound/Exit достатня для тестового;
    /// окрема меню-сцена та сервіс вікон лише ускладнили б цей невеликий цикл гри.
    /// </summary>
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
        // Зберігаємо останній відсоток, щоб не форматувати однаковий текст на кожному фізичному кроці.
        private int lastPercent = -1;
        private float damagePulse;

        private void OnEnable()
        {
            // Програмні підписки знімаємо симетрично в OnDisable, інакше повторне
            // ввімкнення UI могло б виконувати один і той самий клік кілька разів.
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
            // Кнопка подає ту саму команду, що й натискання на дорогу.
            // Остаточний дозвіл старту під час підготовки перевіряє GameManager.
            if (game.State == GameState.Ready) game.HandlePress();
        }

        private void ToggleSound()
        {
            // Один перемикач керує музикою та ефектами без додаткового меню налаштувань.
            // Вибір зберігається між заїздами, але тут навмисно немає запису в PlayerPrefs.
            gameAudio.SetMuted(!gameAudio.IsMuted);
            RefreshSoundLabel();
        }

        private void RefreshSoundLabel() => soundLabel.text = gameAudio.IsMuted ? "SOUND: OFF" : "SOUND: ON";

        private void Quit()
        {
            // Application.Quit не закриває Play Mode редактора, тому ці середовища розділено.
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Update()
        {
            // Візуальний імпульс HUD затухає у реальному часі незалежно від timeScale.
            damagePulse = Mathf.Max(0, damagePulse - Time.unscaledDeltaTime);
            damageOverlay.color = new Color(0.85f, 0.04f, 0.03f, damagePulse / 0.22f * 0.2f);
        }

        public void ShowHealth(int current, int maximum)
        {
            // Підсвічуємо лише зменшення HP; первинне заповнення та рестарт не є ударом.
            if (lastHealth >= 0 && current < lastHealth) damagePulse = 0.22f;
            lastHealth = current;
            healthText.text = $"HP  {current} / {maximum}";
            healthFill.fillAmount = (float)current / maximum;
            healthFill.color = (float)current / maximum <= 0.3f ? new Color(1, 0.25f, 0.16f) : new Color(0.25f, 0.88f, 0.6f);
        }

        public void ShowKills(int count) => killsText.text = $"KILLS  {count:00}";

        public void ShowProgress(float value)
        {
            // Заповнення смуги може змінюватися плавніше, ніж цілочисельний підпис відсотків.
            progressFill.fillAmount = value;
            int percent = Mathf.RoundToInt(value * 100);
            if (percent == lastPercent) return;
            lastPercent = percent;
            progressText.text = $"{percent}%";
        }

        public void ShowState(GameState state)
        {
            // Один метод синхронно перемикає панелі з тим самим станом, який бачить бойова логіка.
            readyPanel.SetActive(state == GameState.Ready);
            bool result = state == GameState.Win || state == GameState.Lose;
            resultPanel.SetActive(result);
            if (result) resultText.text = state == GameState.Win ? "You win" : "You lose";
            if (state == GameState.Ready)
            {
                // Ready ще може готуватися: панель видима, але Play до завершення заблокована.
                damagePulse = 0;
                playButton.interactable = !game.IsPreparing;
                playButton.GetComponentInChildren<Text>().text = game.IsPreparing ? "PREPARING..." : "PLAY";
                RefreshSoundLabel();
            }
        }

        public void ShowPreparationError()
        {
            // Помилку підготовки показуємо явно; не дозволяємо грати в частково створений рівень.
            playButton.interactable = false;
            playButton.GetComponentInChildren<Text>().text = "SETUP ERROR";
            readyPanel.transform.Find("Hint").GetComponent<Text>().text = "Could not prepare the level. Please restart.";
        }
    }
}
