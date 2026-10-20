using System;
using UnityEngine;

namespace Factura
{
    // Чотирьох станів достатньо для ТЗ. Підготовка має окремий прапорець,
    // бо це тимчасове блокування Ready, а не додаткова фаза самого заїзду.
    public enum GameState { Ready, Playing, Win, Lose }

    /// <summary>
    /// Єдине джерело поточного стану гри: Ready, Playing, Win або Lose.
    /// Не керує рухом та стрільбою напряму, а сповіщає про перехід через подію.
    /// Запити на фініш і смерть узгоджуються наприкінці кадру.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        private bool finishPending;
        private bool lossPending;
        private float resultTime;
        public GameState State { get; private set; } = GameState.Ready;
        public bool IsPreparing { get; private set; } = true;
        public event Action<GameState> StateChanged;
        public event Action ResetRequested;

        public void HandlePress()
        {
            // Поки вороги готуються між кадрами, натискання не може почати заїзд.
            if (IsPreparing) return;
            if (State == GameState.Ready) SetState(GameState.Playing);
            // Коротке блокування не дає випадковому кліку миттєво прибрати результат.
            // Натискання на результаті лише скидає гру; наступне натискання запускає її.
            else if (State != GameState.Playing && Time.unscaledTime - resultTime >= 0.3f)
                ResetRequested?.Invoke();
        }

        public void EnterReady()
        {
            // Викликається після повного відновлення авто, ворогів і лічильників.
            IsPreparing = false;
            finishPending = lossPending = false;
            SetState(GameState.Ready);
        }

        public void BeginPreparation()
        {
            // Повторно публікуємо Ready, навіть якщо він уже активний:
            // UI має показати PREPARING, а координатор — вимкнути бойові системи.
            IsPreparing = true;
            finishPending = lossPending = false;
            SetState(GameState.Ready);
        }

        // Запити накопичуються, а не змінюють стан одразу з фізичного callback.
        public void RequestWin() { if (State == GameState.Playing) finishPending = true; }
        public void RequestLoss() { if (State == GameState.Playing) lossPending = true; }

        private void LateUpdate()
        {
            if (State != GameState.Playing || (!finishPending && !lossPending)) return;
            // Якщо в одному кадрі досягнуто фінішу і втрачено останнє HP,
            // поразка має пріоритет. Результат не залежить від порядку цих подій.
            SetState(lossPending ? GameState.Lose : GameState.Win);
            resultTime = Time.unscaledTime;
            finishPending = lossPending = false;
        }

        private void SetState(GameState value)
        {
            // Єдина точка публікації стану; підписники самі оновлюють свій компонент.
            State = value;
            StateChanged?.Invoke(State);
        }
    }
}
