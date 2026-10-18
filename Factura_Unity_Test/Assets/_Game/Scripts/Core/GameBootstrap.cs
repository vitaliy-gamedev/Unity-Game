using Unity.Cinemachine;
using UnityEngine;

namespace Factura
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameManager game;
        [SerializeField] private InputReader input;
        [SerializeField] private CarController car;
        [SerializeField] private TurretController turret;
        [SerializeField] private ProjectilePool projectiles;
        [SerializeField] private Health carHealth;
        [SerializeField] private CinemachineCamera followCamera;
        [SerializeField] private GameUI ui;
        [SerializeField] private Spawner spawner;
        [SerializeField] private CombatEffects combatEffects;

        private void Awake() => turret.Initialize(projectiles, spawner.Enemies, spawner.Barrel);

        private void OnEnable()
        {
            input.Pressed += game.HandlePress;
            input.AimDelta += turret.Aim;
            input.PointerAim += turret.AimAtScreenPosition;
            car.DestinationReached += game.RequestWin;
            car.ProgressChanged += ui.ShowProgress;
            carHealth.Died += game.RequestLoss;
            carHealth.Changed += ui.ShowHealth;
            game.StateChanged += ApplyState;
            game.ResetRequested += ResetRun;
            spawner.KillsChanged += ui.ShowKills;
        }

        private void OnDisable()
        {
            input.Pressed -= game.HandlePress;
            input.AimDelta -= turret.Aim;
            input.PointerAim -= turret.AimAtScreenPosition;
            car.DestinationReached -= game.RequestWin;
            car.ProgressChanged -= ui.ShowProgress;
            carHealth.Died -= game.RequestLoss;
            carHealth.Changed -= ui.ShowHealth;
            game.StateChanged -= ApplyState;
            game.ResetRequested -= ResetRun;
            spawner.KillsChanged -= ui.ShowKills;
        }

        private void Start() => ResetRun();

        private void ApplyState(GameState state)
        {
            bool playing = state == GameState.Playing;
            car.SetRunning(playing);
            turret.SetActive(playing);
            spawner.SetCombatActive(playing);
            if (!playing) projectiles.ClearActive();
            ui.ShowState(state);
        }

        private void ResetRun()
        {
            projectiles.ClearActive();
            combatEffects.Clear();
            car.ResetToStart();
            turret.ResetAim();
            carHealth.ResetHealth();
            spawner.Prepare(car.GetComponent<Collider>(), carHealth);
            followCamera.PreviousStateIsValid = false;
            game.EnterReady();
        }
    }
}
