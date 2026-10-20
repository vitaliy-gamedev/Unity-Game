using System;
using System.Threading;
using Unity.Cinemachine;
using UnityEngine;
using VContainer.Unity;

namespace Factura
{
    /// <summary>
    /// Координатор запуску, рестарту та зв'язків між системами.
    /// Це звичайний C#-клас, а не компонент сцени: VContainer передає залежності
    /// через конструктор і керує його життєвим циклом.
    /// GameManager визначає стан гри, а цей клас застосовує його до компонентів.
    /// Так менеджеру станів не потрібно знати деталі турелі, камери чи спавну.
    /// </summary>
    public sealed class GameBootstrap : IAsyncStartable, IDisposable
    {
        // readonly фіксує набір залежностей після створення координатора.
        // Локальні посилання моделі/префаба залишаються в Inspector своїх компонентів.
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
        // resetting не дозволяє двом натисканням одночасно підготувати два заїзди.
        private bool resetting;
        private bool disposed;

        public GameBootstrap(GameManager game, InputReader input, CarController car, TurretController turret,
            ProjectilePool projectiles, Health carHealth, CinemachineCamera followCamera, GameUI ui,
            Spawner spawner, CombatEffects combatEffects)
        {
            // Конструктор лише приймає залежності. Роботу зі сценою та async-підготовку
            // виконуємо в StartAsync, коли контейнер уже завершив складання об'єкта.
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
            // Пов'язаний токен завершується і при скасуванні запуску контейнером,
            // і при нашому Dispose. Підготовка не повинна переживати свою сцену.
            lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            turret.Initialize(projectiles, spawner.Enemies, spawner.Barrel);
            // Події з'єднують системи без покадрового опитування UI чи стану HP.
            // Ті самі обробники обов'язково відписуємо в Dispose нижче.
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
            // Повторне звільнення безпечне; після нього координатор більше не запускає роботу.
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
            // Спочатку прибираємо зв'язки, потім скасовуємо незавершений спавн.
            lifetime?.Cancel();
            lifetime?.Dispose();
        }

        // async void залишено тільки на межі події Action, яка не повертає Awaitable.
        // Скасування та помилки обробляються всередині PrepareRunAsync, а не губляться тут.
        private async void OnResetRequested() => await PrepareRunAsync(lifetime.Token);

        private void ApplyState(GameState state)
        {
            // Один стан одночасно дозволяє/забороняє рух, стрільбу та атаки ворогів.
            // Це не Time.timeScale = 0: меню й візуальні реакції можуть продовжити роботу.
            bool playing = state == GameState.Playing;
            car.SetRunning(playing);
            turret.SetActive(playing);
            spawner.SetCombatActive(playing);
            // Кулі старого заїзду не повинні влучати після перемоги, поразки чи рестарту.
            if (!playing) projectiles.ClearActive();
            ui.ShowState(state);
        }

        private async Awaitable PrepareRunAsync(CancellationToken cancellation)
        {
            if (disposed || resetting) return;
            resetting = true;
            try
            {
                // Спочатку блокуємо запуск і скидаємо тимчасові ефекти та стан авто.
                // Лише після завершення спавну дозволяємо наступне натискання Play.
                cancellation.ThrowIfCancellationRequested();
                game.BeginPreparation();
                projectiles.ClearActive();
                combatEffects.Clear();
                car.ResetToStart();
                turret.ResetAim();
                carHealth.ResetHealth();
                await spawner.PrepareAsync(car.GetComponent<Collider>(), carHealth, cancellation);
                cancellation.ThrowIfCancellationRequested();
                // Після телепортації авто камера не повинна плавно їхати назад з фінішу.
                followCamera.PreviousStateIsValid = false;
                game.EnterReady();
            }
            // Закриття сцени — очікуване скасування, а не помилка підготовки рівня.
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception error)
            {
                // Не видаємо незавершену підготовку за Ready: показуємо помилку в UI.
                Debug.LogException(error);
                if (ui) ui.ShowPreparationError();
            }
            // Прапорець знімається і після успіху, і після помилки/скасування.
            finally { resetting = false; }
        }
    }
}
