using Unity.Cinemachine;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Factura
{
    // One scene = one scope. Scene references belong here, not in a service locator or singleton.
    public sealed class GameLifetimeScope : LifetimeScope
    {
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
            builder.RegisterComponent(game);
            builder.RegisterComponent(input);
            builder.RegisterComponent(car);
            builder.RegisterComponent(car.GetComponent<Health>());
            builder.RegisterComponent(turret);
            builder.RegisterComponent(projectiles);
            builder.RegisterComponent(followCamera);
            builder.RegisterComponent(ui);
            builder.RegisterComponent(spawner);
            builder.RegisterComponent(combatEffects);
            builder.RegisterEntryPoint<GameBootstrap>().AsSelf();
        }
    }
}
