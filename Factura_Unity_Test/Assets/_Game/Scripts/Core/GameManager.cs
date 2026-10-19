using System;
using UnityEngine;

namespace Factura
{
    public enum GameState { Ready, Playing, Win, Lose }

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
            if (IsPreparing) return;
            if (State == GameState.Ready) SetState(GameState.Playing);
            else if (State != GameState.Playing && Time.unscaledTime - resultTime >= 0.3f)
                ResetRequested?.Invoke();
        }

        public void EnterReady()
        {
            IsPreparing = false;
            finishPending = lossPending = false;
            SetState(GameState.Ready);
        }

        public void BeginPreparation()
        {
            IsPreparing = true;
            finishPending = lossPending = false;
            SetState(GameState.Ready);
        }

        public void RequestWin() { if (State == GameState.Playing) finishPending = true; }
        public void RequestLoss() { if (State == GameState.Playing) lossPending = true; }

        private void LateUpdate()
        {
            if (State != GameState.Playing || (!finishPending && !lossPending)) return;
            // Resolve after simulation callbacks; fatal damage wins a simultaneous finish.
            SetState(lossPending ? GameState.Lose : GameState.Win);
            resultTime = Time.unscaledTime;
            finishPending = lossPending = false;
        }

        private void SetState(GameState value)
        {
            State = value;
            StateChanged?.Invoke(State);
        }
    }
}
