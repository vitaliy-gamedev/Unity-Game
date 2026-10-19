# Factura Unity Test — Turret Run

A compact car-mounted turret prototype implementing the supplied Unity developer test.

## Run

1. Open **this folder** (`Factura_Unity_Test`) in Unity **6000.3.6f1**, not the repository root.
2. Allow Package Manager to restore dependencies. VContainer 1.19.0 is pinned through the scoped OpenUPM registry.
3. Open `Assets/_Game/Scenes/Gameplay.unity` and enter Play Mode.

Choose **Play** or tap the road to start. The car drives automatically and the turret fires automatically.
Point the mouse to aim through 360 degrees; on touch devices, hold and swipe horizontally to rotate the turret.
Enemies begin idle, chase the approaching car and deal melee damage. Reach the finish with HP remaining to win.
After a result, one tap resets to Ready; a separate tap starts the next run.
The menu includes a sound toggle and Exit. Exit stops Play Mode in the editor and closes the application in a player.

## Implementation

- `GameLifetimeScope` is the scene composition root. VContainer registers the existing scene components and constructs one `GameBootstrap` entry point.
- `GameBootstrap` is plain C# with constructor injection. It implements VContainer's `IAsyncStartable` and `IDisposable`, coordinates a run and pairs event subscriptions with unsubscription.
- `GameManager` owns Ready / Playing / Win / Lose. Preparation gates input without adding a separate gameplay state; fatal damage takes priority over a simultaneous finish.
- `Spawner.PrepareAsync` uses Unity's built-in `Awaitable.NextFrameAsync` to instantiate at most four enemies per frame. It runs on the main thread, not a background worker. A linked cancellation token follows the DI scope lifetime; cancellation removes partial enemies. The synchronous `Prepare` path shares the same population logic for deterministic editor fixtures.
- `CarController` moves its kinematic Rigidbody in `FixedUpdate`. `TurretController` handles smooth aim and firing; `InputReader` separates pointer/touch input and prevents menu clicks from starting a run.
- `Health` publishes damage/death events. `EnemyAI` handles detection, chase and melee; separate animation/feedback components present those events.
- `ProjectilePool` reuses bullets, and swept collision checks cover normal flight and point-blank barrel clearance. Impact effects and sound voices are also bounded and reused.
- `GameUI` and `GameAudio` present state and combat feedback. Scene/prefab-local references remain serialized in the inspector.

Optional additions include a destructible explosive barrel, enemy hit/death animation, muzzle flash and recoil, car damage feedback, a compact HUD, music and synthesized sound effects.

## Verification

Opt-in Play Mode checks live in `Assets/_Game/Editor`. Save and close the editor before invoking them from a separate Unity process; each check exits that process itself:

```text
Unity.exe -batchmode -projectPath "<Factura_Unity_Test>" -executeMethod Factura.Editor.DependencyAsyncCheck.Run -logFile "<check.log>"
```

Other entry points are `CombatPlayCheck.Run`, `CompactMenuCheck.Run`, `CloseRangeCheck.Run` and `NaturalRunCheck.Run` in the same `Factura.Editor` namespace.
Reports are written under `Library` and are not committed. They cover DI identity/disposal, async preparation/cancellation, real pointer/touch input, firing, near hits, damage, results, restarts and pooling.
These are automated editor checks, not a substitute for a manual device playtest or a performance profile.

`NaturalRunCheck.RecordPreview` optionally records an editor gameplay demo with scripted input and the actual Unity audio mix. It requires the `FACTURA_FFMPEG` environment variable pointing to a local FFmpeg executable; FFmpeg is not a game dependency.

## Dependencies and credits

- Unity URP, Input System, uGUI and Cinemachine: versions are pinned in `Packages`.
- [VContainer 1.19.0](https://github.com/hadashiA/VContainer): MIT license, restored by Package Manager.
- Original test models/textures are retained in `Assets/_Game/Art/Source`.
- Music attribution and sound provenance are in `Assets/_Game/Audio/ThirdPartyNotices.txt`.

`Docs/Word` contains the implementation plan, GDD, system map and TDD, updated to match the implemented prototype and DI/async lifecycle. Personal interview notes and development logs remain outside the repository.
