using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Factura.Editor
{
    /// <summary>
    /// Регресійна перевірка стрільби впритул з різних боків машини.
    /// Відтворює колишню проблему: ціль біля борту або всередині довжини ствола не отримувала шкоди.
    /// Це допоміжний сценарій лише для Editor, а не бойова логіка чи автоприцілювання гравця.
    /// Запускати через Run в окремому процесі Unity: Finish закриває цей процес з кодом результату.
    /// </summary>
    [InitializeOnLoad]
    public static class CloseRangeCheck
    {
        private const string Key = "Factura.CloseRangeCheck.Running";
        private static readonly StringBuilder Report = new();
        // Матриця позицій покриває чотири боки, кути, ствол і рухомі сценарії.
        // Повторені позиції мають різне наведення/рух, а не дублюють один тест.
        private static readonly Vector3[] Positions =
        {
            new(1.65f, 0, 0), new(-1.65f, 0, 0), new(0, 0, 2.8f), new(0, 0, -2.8f),
            new(1.65f, 0, 2.8f), new(-1.65f, 0, 2.8f), new(1.65f, 0, -2.8f), new(-1.65f, 0, -2.8f),
            new(1.65f, 0, 0), new(-1.65f, 0, 0), new(.1f, 0, -.35f), new(1.65f, 0, 0),
            new(1.65f, 0, 0), new(1.65f, 0, 0)
        };
        private static readonly string[] Names =
        {
            "right contact", "left contact", "front contact", "rear contact", "front-right", "front-left", "rear-right", "rear-left",
            "right body-edge aim", "left body-edge aim", "inside barrel reach", "moving car / attacking enemy", "clear horizontal miss", "moving rear attacker"
        };
        private static GameManager game;
        private static CarController car;
        private static Health health;
        private static Spawner spawner;
        private static TurretController turret;
        private static ProjectilePool pool;
        private static EnemyAI victim;
        private static int index;
        private static int lastFrame = -1;
        private static int failed;
        private static float started;
        private static float commandedYaw;
        private static double deadline;

        // SessionState зберігає намір перевірки через перезавантаження домену при вході в Play Mode.
        // Сам атрибут InitializeOnLoad нічого не тестує без попереднього явного запуску Run.
        static CloseRangeCheck() { if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick; }

        public static void Run()
        {
            // Відкриваємо контрольовану сцену, а не використовуємо випадкову поточну сцену редактора.
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Gameplay.unity", OpenSceneMode.Single);
            SessionState.SetBool(Key, true);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!Application.isPlaying) return;
            Application.runInBackground = true;
            Time.captureDeltaTime = 1f / 60;
            EditorApplication.QueuePlayerLoopUpdate();
            // Editor.update може викликатись частіше ігрового кадру; не рахуємо один кадр двічі.
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            try
            {
                if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 90;
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Close range regression timed out.");
                if (!game)
                {
                    if (Time.time < .3f) return;
                    var candidate = UnityEngine.Object.FindFirstObjectByType<GameManager>();
                    if (candidate.IsPreparing) return;
                    game = candidate;
                    car = UnityEngine.Object.FindFirstObjectByType<CarController>();
                    health = car.GetComponent<Health>();
                    spawner = UnityEngine.Object.FindFirstObjectByType<Spawner>();
                    turret = car.GetComponentInChildren<TurretController>();
                    pool = UnityEngine.Object.FindFirstObjectByType<ProjectilePool>();
                    game.HandlePress();
                    StartCase();
                    return;
                }
                Aim();
                float elapsed = Time.time - started;
                // Негативний випадок перевіряє, що допоміжний нахил не перетворив
                // явний горизонтальний промах на приховане автонаведення.
                bool expectedMiss = index == 12;
                if ((!expectedMiss && !victim.IsAlive) || elapsed > 1.7f)
                {
                    bool pass = expectedMiss ? victim.GetComponent<Health>().Current == 30 : !victim.IsAlive;
                    if (!pass) failed++;
                    Report.AppendLine($"{(pass ? "PASS" : "FAIL")}: {Names[index]}, HP={victim.GetComponent<Health>().Current}, time={elapsed:F2}s, enemy={victim.transform.position:F2}, muzzle={turret.Muzzle.position:F2}, forward={turret.Muzzle.forward:F2}");
                    index++;
                    if (index == Positions.Length) { Finish(failed == 0 ? 0 : 1); return; }
                    StartCase();
                }
            }
            catch (Exception exception) { Report.AppendLine("ERROR: " + exception); Finish(2); }
        }

        private static void StartCase()
        {
            // Кожен випадок має чисті HP, пул і ворогів; решту цілей вимикаємо,
            // а бочку відсуваємо, щоб перевіряти саме одне близьке попадання.
            turret.SetActive(false);
            pool.ClearActive();
            car.ResetToStart();
            health.ResetHealth();
            spawner.Prepare(car.GetComponent<Collider>(), health);
            foreach (var enemy in spawner.Enemies) enemy.gameObject.SetActive(false);
            spawner.Barrel.ResetAt(new Vector3(20, 0, 60));
            victim = spawner.Enemies[0];
            victim.gameObject.SetActive(true);
            victim.GetComponent<Rigidbody>().position = victim.transform.position = Positions[index];
            // Після прямої зміни Transform фізичний запит має бачити нове положення вже зараз.
            Physics.SyncTransforms();
            turret.ResetAim();
            commandedYaw = 0;
            turret.SetActive(true);
            bool moving = index == 11 || index == 13;
            if (index == 13)
                victim.GetComponent<Rigidbody>().position = victim.transform.position = new Vector3(0, 0, -2.8f);
            car.SetRunning(moving);
            victim.SetCombatActive(moving);
            started = Time.time;
            Aim();
        }

        private static void Aim()
        {
            // Тест розраховує команду наведення, але шкода все одно проходить
            // через реальну турель і Projectile. Health напряму для успіху тесту не змінюємо.
            Vector3 point = car.transform.InverseTransformDirection(victim.AimPoint - turret.transform.position);
            float desired = Mathf.Atan2(point.x, point.z) * Mathf.Rad2Deg;
            if (index == 8 || index == 9) desired += 14;
            if (index == 12) desired += 24;
            float delta = Mathf.DeltaAngle(commandedYaw, desired);
            commandedYaw += delta;
            turret.Aim(delta / 180);
        }

        private static void Finish(int code)
        {
            // Звіт і тимчасові артефакти залишаються в Library, не серед ресурсів гри.
            // 0 означає успіх; інший код дозволяє зовнішньому запуску помітити збій.
            SessionState.SetBool(Key, false);
            EditorApplication.update -= Tick;
            Time.captureDeltaTime = 0;
            File.WriteAllText("Library/FacturaCloseRangeCheck.txt", Report.ToString());
            Debug.Log("Factura close range check\n" + Report);
            EditorApplication.Exit(code);
        }
    }
}
