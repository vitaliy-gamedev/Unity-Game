using Unity.Cinemachine;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Factura
{
    /// <summary>
    /// Точка складання залежностей сцени (composition root) у VContainer.
    /// Тут описано, які готові компоненти отримає координатор GameBootstrap.
    /// Реєструємо об'єкти зі сцени, а не створюємо їх повторно: вони вже мають
    /// налаштовані в Inspector посилання, моделі та параметри.
    /// </summary>
    public sealed class GameLifetimeScope : LifetimeScope
    {
        // SerializeField зберігає конкретні посилання сцени. Це явна конфігурація,
        // на відміну від пошуку першого відповідного об'єкта під час гри.
        [SerializeField] private GameManager game;
        [SerializeField] private InputReader input;
        [SerializeField] private CarController car;
        [SerializeField] private TurretController turret;
        [SerializeField] private ProjectilePool projectiles;
        [SerializeField] private CinemachineCamera followCamera;
        [SerializeField] private GameUI ui;
        [SerializeField] private Spawner spawner;
        [SerializeField] private CombatEffects combatEffects;

        protected override void Configure(IContainerBuilder builder)
        {
            // Контейнер надає ці самі екземпляри всім залежним від них об'єктам.
            // Не реєструємо кожного ворога: їх створює та ініціалізує Spawner.
            builder.RegisterComponent(game);
            builder.RegisterComponent(input);
            builder.RegisterComponent(car);
            // У сцені багато Health, але координатору потрібне саме здоров'я авто.
            builder.RegisterComponent(car.GetComponent<Health>());
            builder.RegisterComponent(turret);
            builder.RegisterComponent(projectiles);
            builder.RegisterComponent(followCamera);
            builder.RegisterComponent(ui);
            builder.RegisterComponent(spawner);
            builder.RegisterComponent(combatEffects);
            // Entry point запускає IAsyncStartable та прив'язує IDisposable до scope.
            // AsSelf також дозволяє перевіркам отримати координатор із контейнера.
            // Окремий MonoBehaviour-bootstrap або статичний Singleton тут не потрібен.
            builder.RegisterEntryPoint<GameBootstrap>().AsSelf();
        }
    }
}
