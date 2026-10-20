// Координатор подій і підготовки заїзду. VContainer створює його та завершує lifetime через Dispose.
using System;
using System.Threading;
using Unity.Cinemachine;
using UnityEngine;
using VContainer.Unity;

namespace Factura
{
    public sealed class GameBootstrap : IAsyncStartable, IDisposable
    {
        private readonly GameManager game;
        private readonly InputReader input;
        private readonly CarController car;
        private readonly TurretController turret;
        private readonly ProjectilePool projectiles;
        private readonly Health carHealth;
        private readonly CinemachineCamera followCamera;
        private readonly GameUI ui;
        private readonly Spawner spawner;
        private readonly CombatEffects combatEffects;
        private CancellationTokenSource lifetime;
        private bool resetting;
        private bool disposed;

        public GameBootstrap(GameManager game, InputReader input, CarController car, TurretController turret,
            ProjectilePool projectiles, Health carHealth, CinemachineCamera followCamera, GameUI ui,
            Spawner spawner, CombatEffects combatEffects)
        {
            this.game = game;
            this.input = input;
            this.car = car;
            this.turret = turret;
            this.projectiles = projectiles;
            this.carHealth = carHealth;
            this.followCamera = followCamera;
            this.ui = ui;
            this.spawner = spawner;
            this.combatEffects = combatEffects;
        }

        public async Awaitable StartAsync(CancellationToken cancellation = default)
        {
            lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            turret.Initialize(projectiles, spawner.Enemies, spawner.Barrel);
            input.Pressed += game.HandlePress;
            input.AimDelta += turret.Aim;
            input.PointerAim += turret.AimAtScreenPosition;
            car.DestinationReached += game.RequestWin;
            car.ProgressChanged += ui.ShowProgress;
            carHealth.Died += game.RequestLoss;
            carHealth.Changed += ui.ShowHealth;
            game.StateChanged += ApplyState;
            game.ResetRequested += OnResetRequested;
            spawner.KillsChanged += ui.ShowKills;
            await PrepareRunAsync(lifetime.Token);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            input.Pressed -= game.HandlePress;
            input.AimDelta -= turret.Aim;
            input.PointerAim -= turret.AimAtScreenPosition;
            car.DestinationReached -= game.RequestWin;
            car.ProgressChanged -= ui.ShowProgress;
            carHealth.Died -= game.RequestLoss;
            carHealth.Changed -= ui.ShowHealth;
            game.StateChanged -= ApplyState;
            game.ResetRequested -= OnResetRequested;
            spawner.KillsChanged -= ui.ShowKills;
            // Спочатку відписуємо події, потім скасовуємо незавершену підготовку.
            lifetime?.Cancel();
            lifetime?.Dispose();
        }

        // Межа події Action; cancellation та помилки обробляються всередині PrepareRunAsync.
        private async void OnResetRequested() => await PrepareRunAsync(lifetime.Token);

        private void ApplyState(GameState state)
        {
            bool playing = state == GameState.Playing;
            car.SetRunning(playing);
            turret.SetActive(playing);
            // Явний дозвіл бою зупиняє шкоду й AI, не вимикаючи UI та анімації через timeScale.
            spawner.SetCombatActive(playing);
            if (!playing) projectiles.ClearActive();
            ui.ShowState(state);
        }

        private async Awaitable PrepareRunAsync(CancellationToken cancellation)
        {
            if (disposed || resetting) return;
            resetting = true;
            try
            {
                cancellation.ThrowIfCancellationRequested();
                game.BeginPreparation();
                projectiles.ClearActive();
                combatEffects.Clear();
                car.ResetToStart();
                turret.ResetAim();
                carHealth.ResetHealth();
                await spawner.PrepareAsync(car.GetComponent<Collider>(), carHealth, cancellation);
                cancellation.ThrowIfCancellationRequested();
                // Після телепортації на старт камера не повинна плавно повертатися з фінішу.
                followCamera.PreviousStateIsValid = false;
                game.EnterReady();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (ui) ui.ShowPreparationError();
            }
            finally { resetting = false; }
        }
    }
}
