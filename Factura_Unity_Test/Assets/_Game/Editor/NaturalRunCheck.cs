using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Factura.Editor
{
    // Opt-in autoplay using the same public start/aim commands as input. Never changes saved balance/route.
    [InitializeOnLoad]
    public static class NaturalRunCheck
    {
        private const string Key = "Factura.NaturalRunCheck.Running";
        private const string VideoKey = "Factura.NaturalRunCheck.Video";
        private static readonly StringBuilder Report = new();
        private static GameManager game;
        private static CarController car;
        private static Health health;
        private static Spawner spawner;
        private static TurretController turret;
        private static ProjectilePool pool;
        private static EnemyAI target;
        private static bool barrelTarget;
        private static int run;
        private static int lastFrame = -1;
        private static int maxBullets;
        private static float commandedYaw;
        private static float runStart;
        private static float resultTime;
        private static float resultFrameTime;
        private static float nextCapture;
        private static float lastProgress;
        private static double deadline;
        private static int impacts;
        private static bool checkingGround;
        private static bool recording;
        private static float pendingStart = -1;
        private static System.Diagnostics.Process encoder;
        private static Texture2D videoPixels;
        private static RenderTexture videoTarget;
        private static byte[] videoBuffer;

        static NaturalRunCheck()
        {
            if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick;
        }

        public static void Run() => Begin(false);
        public static void RecordPreview() => Begin(true);

        private static void Begin(bool video)
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Gameplay.unity", OpenSceneMode.Single);
            SessionState.SetBool(Key, true);
            SessionState.SetBool(VideoKey, video);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!Application.isPlaying) return;
            Application.runInBackground = true;
            recording = SessionState.GetBool(VideoKey, false);
            Time.captureDeltaTime = 1f / (recording ? 30 : 60);
            EditorApplication.QueuePlayerLoopUpdate();
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            try
            {
                if (deadline == 0) deadline = EditorApplication.timeSinceStartup + (recording ? 240 : 150);
                Require(EditorApplication.timeSinceStartup < deadline, "Full route check timeout.");
                // Debounce uses real time, but the review only needs two seconds of result frames.
                if (encoder != null && (resultTime == 0 || Time.time - resultFrameTime < 2)) Capture(null);
                if (!game)
                {
                    if (Time.time < .3f) return;
                    game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
                    car = UnityEngine.Object.FindFirstObjectByType<CarController>();
                    health = car.GetComponent<Health>();
                    turret = car.GetComponentInChildren<TurretController>();
                    spawner = UnityEngine.Object.FindFirstObjectByType<Spawner>();
                    pool = UnityEngine.Object.FindFirstObjectByType<ProjectilePool>();
                    pool.Impact += CountImpact;
                    if (recording) StartRecording();
                    Require(game.State == GameState.Ready && pool.ActiveCount == 0, "Ready has no active bullets.");
                    // In Ready, shoot only these test projectiles away from the car/enemies into actual ground colliders.
                    pool.Fire(new Vector3(10, 2, 0), Vector3.down);
                    runStart = Time.time;
                    checkingGround = true;
                    return;
                }
                if (checkingGround)
                {
                    if (Time.time - runStart < .15f) return;
                    Require(pool.ActiveCount == 0 && impacts == 1, "A missed projectile hits ground and returns to the pool.");
                    pool.Fire(new Vector3(10, -.1f, 0), Vector3.down);
                    Require(pool.ActiveCount == 0 && impacts == 2, "A projectile starting inside ground is released immediately.");
                    Report.AppendLine("PASS: Ground impact, initial overlap, pooled release.");
                    checkingGround = false;
                    if (recording) pendingStart = Time.time + 1;
                    else StartRun();
                    return;
                }
                if (pendingStart >= 0)
                {
                    if (Time.time < pendingStart) return;
                    pendingStart = -1;
                    StartRun();
                    return;
                }
                maxBullets = Mathf.Max(maxBullets, pool.ActiveCount);
                Require(pool.CreatedCount == 24, "Projectile pool grew beyond the prewarmed 24 objects.");
                if (game.State == GameState.Playing)
                {
                    Require(car.Progress + .001f >= lastProgress, "Route progress moved backwards.");
                    lastProgress = car.Progress;
                    Require(Time.time - runStart < 27, "100m route exceeded the expected travel time.");
                    if (run > 0) AimAtThreat();
                    if (Time.time - runStart >= nextCapture && run == 1)
                    {
                        Capture("Library/FacturaFullRun_" + Mathf.RoundToInt(car.Progress * 100) + "pct.png");
                        nextCapture += 6;
                    }
                    return;
                }
                if (game.State != GameState.Win && game.State != GameState.Lose) return;
                if (resultTime == 0)
                {
                    Require(!turret.IsActive && pool.ActiveCount == 0, "Results left shooting/projectiles active.");
                    Require(!turret.GetComponentInChildren<LineRenderer>().enabled,
                        "Aim guide remained visible after the run ended.");
                    if (run > 0) Require(game.State == GameState.Win, "Aiming run lost before finishing.");
                    else Require(game.State == GameState.Lose, "Ignoring enemies never caused a loss.");
                    Report.AppendLine($"PASS: run={run}, seed={41 + run}, aim={run > 0}, result={game.State}, time={Time.time - runStart:F2}s, progress={car.Progress:P0}, HP={health.Current}, kills={spawner.KilledCount}/{spawner.Enemies.Count}, peak bullets={maxBullets}, created={pool.CreatedCount}");
                    Capture("Library/FacturaFullRun_Result" + run + ".png");
                    resultTime = Time.unscaledTime;
                    resultFrameTime = Time.time;
                    Debug.Log(Report.ToString());
                }
                if (Time.unscaledTime - resultTime < (recording ? 2 : .4f)) return;
                game.HandlePress();
                Require(game.State == GameState.Ready && car.Progress == 0 && health.Current == health.Maximum && spawner.KilledCount == 0,
                    "Result did not reset to a clean Ready state.");
                run++;
                if (run == (recording ? 2 : 4)) { Finish(0); return; }
                if (recording) pendingStart = Time.time + .75f;
                else StartRun();
            }
            catch (Exception exception)
            {
                Report.AppendLine("FAIL: " + exception);
                Finish(1);
            }
        }

        private static void StartRun()
        {
            var settings = new SerializedObject(spawner);
            settings.FindProperty("useFixedSeed").boolValue = true;
            settings.FindProperty("seed").intValue = 41 + run;
            settings.ApplyModifiedPropertiesWithoutUndo();
            spawner.Prepare(car.GetComponent<Collider>(), health);
            Require(spawner.Enemies.Count == 18, "Random placement failed to produce all enemies.");
            commandedYaw = 0;
            maxBullets = 0;
            target = null;
            barrelTarget = false;
            resultTime = 0;
            nextCapture = 3;
            lastProgress = 0;
            runStart = Time.time;
            game.HandlePress();
            Require(game.State == GameState.Playing, "Start command failed.");
        }

        private static void AimAtThreat()
        {
            if (barrelTarget && !spawner.Barrel.IsAlive) barrelTarget = false;
            if (!target || !target.IsAlive)
            {
                target = null;
                float nearest = 20 * 20;
                foreach (var enemy in spawner.Enemies)
                {
                    if (!enemy || !enemy.IsAlive) continue;
                    float distance = (enemy.transform.position - car.transform.position).sqrMagnitude;
                    if (distance >= nearest) continue;
                    nearest = distance;
                    target = enemy;
                }
                if (spawner.Barrel.IsAlive && (spawner.Barrel.transform.position - car.transform.position).sqrMagnitude < 18 * 18)
                {
                    int neighbours = 0;
                    foreach (var enemy in spawner.Enemies)
                        if (enemy.IsAlive && (enemy.AimPoint - spawner.Barrel.AimPoint).sqrMagnitude < 4 * 4) neighbours++;
                    barrelTarget = neighbours >= 2;
                }
            }
            if (!target && !barrelTarget) return;
            Vector3 aimPoint = barrelTarget ? spawner.Barrel.AimPoint : target.AimPoint;
            Vector3 local = car.transform.InverseTransformDirection(aimPoint - turret.transform.position);
            float desired = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            float delta = Mathf.Clamp(Mathf.DeltaAngle(commandedYaw, desired), -360 * Time.deltaTime, 360 * Time.deltaTime);
            commandedYaw += delta;
            turret.Aim(delta / 180);
        }

        private static void CountImpact(Vector3 point, Vector3 normal) => impacts++;
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        private static void Capture(string path)
        {
            var camera = Camera.main;
            var canvas = GameObject.Find("Game UI").GetComponent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>();
            bool video = path == null;
            var targetTexture = video ? videoTarget : RenderTexture.GetTemporary(720, 1280, 24);
            var previous = RenderTexture.active;
            var mode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            bool scaling = scaler.enabled;
            var pixels = video ? videoPixels : new Texture2D(720, 1280, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = targetTexture;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = .5f;
                scaler.enabled = false;
                canvas.scaleFactor = targetTexture.width / 720f;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = targetTexture;
                pixels.ReadPixels(new Rect(0, 0, targetTexture.width, targetTexture.height), 0, 0);
                pixels.Apply();
                if (video)
                {
                    pixels.GetRawTextureData<byte>().CopyTo(videoBuffer);
                    encoder.StandardInput.BaseStream.Write(videoBuffer, 0, videoBuffer.Length);
                }
                else File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                canvas.renderMode = mode;
                canvas.worldCamera = previousCamera;
                scaler.enabled = scaling;
                RenderTexture.active = previous;
                if (!video)
                {
                    RenderTexture.ReleaseTemporary(targetTexture);
                    UnityEngine.Object.DestroyImmediate(pixels);
                }
            }
        }

        private static void StartRecording()
        {
            string ffmpeg = Environment.GetEnvironmentVariable("FACTURA_FFMPEG");
            Require(File.Exists(ffmpeg), "Set FACTURA_FFMPEG to a local ffmpeg executable for opt-in video recording.");
            videoTarget = RenderTexture.GetTemporary(640, 1136, 24);
            videoPixels = new Texture2D(640, 1136, TextureFormat.RGB24, false);
            videoBuffer = new byte[640 * 1136 * 3];
            string destination = Path.GetFullPath("Library/Factura_Autoplay_Review.mp4");
            encoder = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = "-hide_banner -loglevel error -y -f rawvideo -pixel_format rgb24 -video_size 640x1136 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset veryfast -crf 22 -pix_fmt yuv420p \"" + destination + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true
                }
            };
            Require(encoder.Start(), "Could not start video encoder.");
            var label = new GameObject("Autoplay review label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(GameObject.Find("Game UI").transform, false);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(20, 15);
            rect.sizeDelta = new Vector2(680, 28);
            var text = label.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.text = "AUTOMATED PLAYTEST / UNITY EDITOR";
            text.color = new Color(1, 1, 1, .8f);
            text.raycastTarget = false;
        }

        private static void Finish(int code)
        {
            SessionState.SetBool(Key, false);
            SessionState.SetBool(VideoKey, false);
            EditorApplication.update -= Tick;
            Time.captureDeltaTime = 0;
            if (encoder != null)
            {
                try
                {
                    encoder.StandardInput.Close();
                    Require(encoder.WaitForExit(20000), "Video encoder did not finish.");
                    Require(encoder.ExitCode == 0, "Video encoding failed: " + encoder.StandardError.ReadToEnd());
                    Report.AppendLine("PASS: Local autoplay review video encoded (not a manual playtest or device performance test).");
                }
                catch (Exception exception) { code = 1; Report.AppendLine("FAIL: " + exception.Message); }
                finally { encoder.Dispose(); encoder = null; }
            }
            if (videoTarget) RenderTexture.ReleaseTemporary(videoTarget);
            if (videoPixels) UnityEngine.Object.DestroyImmediate(videoPixels);
            File.WriteAllText("Library/FacturaNaturalRunCheck.txt", Report.ToString());
            Debug.Log("Factura full route check\n" + Report);
            EditorApplication.Exit(code);
        }
    }
}
