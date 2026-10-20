using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Factura.Editor
{
    /// <summary>
    /// Перевіряє інтеграцію VContainer і покадрової асинхронної підготовки.
    /// Важливі не лише правильні типи, а й ті самі екземпляри сцени, блокування вводу,
    /// розмір пачки спавну та скасування незавершеного рестарту при звільненні scope.
    /// Запускається явно в окремому Editor-процесі та завершує його після звіту.
    /// </summary>
    [InitializeOnLoad]
    public static class DependencyAsyncCheck
    {
        private const string Key = "Factura.DependencyAsyncCheck";
        private static readonly StringBuilder Report = new();
        private static GameLifetimeScope scope;
        private static GameManager game;
        private static CarController car;
        private static Health health;
        private static Spawner spawner;
        private static GameUI ui;
        private static int phase;
        private static int lastFrame = -1;
        private static int preparationStart;
        private static int previousFrame;
        private static int previousCount;
        private static double phaseTime;
        private static double deadline;

        static DependencyAsyncCheck()
        {
            // Вхід у Play Mode може перезавантажити статичні поля; прапорець SessionState
            // відновлює тільки явно замовлений сценарій, не запускає тести при звичайній грі.
            if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick;
        }

        public static void Run()
        {
            // Сценарій працює зі справжнім composition root Gameplay, не з підробленим контейнером.
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Gameplay.unity", OpenSceneMode.Single);
            SessionState.SetBool(Key, true);
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!Application.isPlaying) return;
            Application.runInBackground = true;
            EditorApplication.QueuePlayerLoopUpdate();
            // Один крок сценарію на ігровий кадр, навіть якщо Editor.update викликається частіше.
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            try
            {
                if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 30;
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("DI/async check timed out.");
                double elapsed = EditorApplication.timeSinceStartup - phaseTime;
                if (phase == 0)
                {
                    // Спершу чекаємо завершення реального async-старту і звіряємо
                    // DI-ідентичність, тип координатора та наповнення сцени.
                    game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
                    if (!game || game.IsPreparing) return;
                    scope = UnityEngine.Object.FindFirstObjectByType<GameLifetimeScope>();
                    car = UnityEngine.Object.FindFirstObjectByType<CarController>();
                    health = car.GetComponent<Health>();
                    spawner = UnityEngine.Object.FindFirstObjectByType<Spawner>();
                    ui = UnityEngine.Object.FindFirstObjectByType<GameUI>();
                    var bootstrap = scope.Container.Resolve<GameBootstrap>();
                    Check(!File.ReadAllText("Assets/_Game/Scenes/Gameplay.unity").Contains("Assembly-CSharp::Factura.GameBootstrap"),
                        "The plain C# coordinator is not retained as a legacy scene component.");
                    Check(ReferenceEquals(bootstrap, scope.Container.Resolve<GameBootstrap>()), "One scope resolves one coordinator instance.");
                    Check(!typeof(MonoBehaviour).IsAssignableFrom(typeof(GameBootstrap)), "Coordinator is plain C#, constructed by VContainer.");
                    Check(scope.Container.Resolve<GameManager>() == game && scope.Container.Resolve<Health>() == health,
                        "DI resolves the actual scene manager and car health, not an enemy health component.");
                    Check(spawner.Enemies.Count == 18 && !spawner.IsPreparing, "Async startup completes with 18 enemies.");
                    game.HandlePress();
                    health.ApplyDamage(health.Current);
                    Next();
                }
                else if (phase == 1 && game.State == GameState.Lose && elapsed > .75)
                {
                    // Після натискання рестарту має існувати лише перша пачка з чотирьох
                    // ворогів; багато натискань поспіль не повинні обійти блокування.
                    game.HandlePress();
                    Check(game.IsPreparing && spawner.IsPreparing && spawner.Enemies.Count == 4,
                        "Restart yields after the first four enemies instead of building the whole level in one frame.");
                    var play = ui.transform.Find("Ready/Play").GetComponent<Button>();
                    Check(!play.interactable && play.GetComponentInChildren<Text>().text == "PREPARING...",
                        "Preparation is visible and the Play button is disabled.");
                    for (int i = 0; i < 20; i++) game.HandlePress();
                    Check(game.State == GameState.Ready && game.IsPreparing && spawner.Enemies.Count == 4,
                        "Repeated presses cannot start an incomplete run or duplicate preparation.");
                    previousCount = 4;
                    preparationStart = previousFrame = Time.frameCount;
                    Next();
                }
                else if (phase == 2)
                {
                    // Вимірюємо приріст кількості ворогів між кадрами, а не віримо
                    // лише слову async у назві методу. Потім перевіряємо повний reset.
                    int newEnemies = spawner.Enemies.Count - previousCount;
                    if (newEnemies > 4 * (Time.frameCount - previousFrame))
                        throw new InvalidOperationException("Spawn batch exceeded four enemies per frame.");
                    previousCount = spawner.Enemies.Count;
                    previousFrame = Time.frameCount;
                    if (game.IsPreparing) return;
                    Check(Time.frameCount - preparationStart >= 4 && spawner.Enemies.Count == 18,
                        "Population progresses across frames and completes without missing or duplicate enemies.");
                    Check(health.Current == 100 && car.Progress == 0 && spawner.KilledCount == 0 && spawner.Barrel.IsAlive,
                        "Async restart restores health, progress, kills and barrel before unlocking input.");
                    Check(ui.transform.Find("Ready/Play").GetComponent<Button>().interactable,
                        "Play is enabled after successful preparation.");
                    game.HandlePress();
                    Check(game.State == GameState.Playing, "A prepared run starts normally.");
                    health.ApplyDamage(health.Current);
                    Next();
                }
                else if (phase == 3 && game.State == GameState.Lose && elapsed > .75)
                {
                    // Навмисно звільняємо scope посеред підготовки: найважливіший
                    // крайовий випадок для токена життєвого циклу сцени.
                    game.HandlePress();
                    Check(game.IsPreparing && spawner.Enemies.Count == 4, "A second real restart is pending when the scope is disposed.");
                    scope.DisposeCore();
                    Next();
                }
                else if (phase == 4 && elapsed > .15)
                {
                    // Скасування має прибрати часткових ворогів і не опублікувати
                    // запізнілий EnterReady після завершення життєвого циклу координатора.
                    Check(scope.Container == null && !spawner.IsPreparing && spawner.Enemies.Count == 0,
                        "Disposing the DI scope cancels its pending Awaitable and removes partial enemies.");
                    Check(game.IsPreparing && car.Progress == 0,
                        "A cancelled operation cannot publish Ready completion or start movement afterward.");
                    Finish(0);
                }
            }
            catch (Exception error) { Report.AppendLine("FAIL: " + error); Finish(1); }
        }

        // Відлік Editor-time дає сценарію власний timeout незалежно від ігрового timeScale.
        private static void Next() { phase++; phaseTime = EditorApplication.timeSinceStartup; }
        private static void Check(bool condition, string message)
        {
            // При першому порушенні зупиняємо ланцюжок: наступні фази вже не мали б надійної основи.
            if (!condition) throw new InvalidOperationException(message);
            Report.AppendLine("PASS: " + message);
        }
        private static void Finish(int code)
        {
            // Прибираємо callback і прапорець до виходу, щоб перевірка не повторилась сама.
            SessionState.SetBool(Key, false);
            EditorApplication.update -= Tick;
            File.WriteAllText("Library/FacturaDependencyAsyncCheck.txt", Report.ToString());
            Debug.Log("FACTURA_DI_ASYNC_CHECK\n" + Report);
            EditorApplication.Exit(code);
        }
    }
}
