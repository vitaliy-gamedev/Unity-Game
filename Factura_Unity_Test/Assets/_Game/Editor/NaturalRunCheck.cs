using System;
using System.IO;
using System.Text;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Factura.Editor
{
    /// <summary>
    /// Автоматизований повний проїзд траси з перевіркою результатів і повторних заїздів.
    /// Має два явні режими: Run для перевірок та RecordPreview для локального відеоогляду.
    /// Наведення в цьому сценарії програмне: це НЕ автопілот і не механіка, доступна гравцю.
    /// Допоміжний код лежить у Editor і не входить у гру; FFmpeg потрібен лише для запису.
    /// Finish завершує процес Unity, тому запускати слід в окремому процесі редактора.
    /// </summary>
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
        private static FileStream audioCapture;
        private static byte[] audioBuffer;
        private static bool audioRecording;
        private static int videoFrames;
        private static long audioFrames;
        private static int audioSampleRate;

        static NaturalRunCheck()
        {
            // SessionState переносить прапорці через вхід у Play Mode/перезавантаження домену.
            if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick;
        }

        // Жоден режим не запускається сам при відкритті сцени або імпорті цього скрипта.
        public static void Run() => Begin(false);
        public static void RecordPreview() => Begin(true);

        private static void Begin(bool video)
        {
            // Сцена й режим задаються до входу в Play Mode; зіграний стан не зберігаємо як сцену.
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
            // Фіксуємо крок часу для відтворюваного запису/сценарію.
            // Це не вимірювання реального FPS чи продуктивності пристрою.
            Time.captureDeltaTime = 1f / (recording ? 30 : 60);
            EditorApplication.QueuePlayerLoopUpdate();
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            try
            {
                if (deadline == 0) deadline = EditorApplication.timeSinceStartup + (recording ? 240 : 150);
                Require(EditorApplication.timeSinceStartup < deadline, "Full route check timeout.");
                if (encoder != null) Capture(null);
                if (!game)
                {
                    if (Time.time < .3f) return;
                    var candidate = UnityEngine.Object.FindFirstObjectByType<GameManager>();
                    if (candidate.IsPreparing) return;
                    game = candidate;
                    car = UnityEngine.Object.FindFirstObjectByType<CarController>();
                    health = car.GetComponent<Health>();
                    turret = car.GetComponentInChildren<TurretController>();
                    spawner = UnityEngine.Object.FindFirstObjectByType<Spawner>();
                    pool = UnityEngine.Object.FindFirstObjectByType<ProjectilePool>();
                    pool.Impact += CountImpact;
                    if (recording) StartRecording();
                    Require(game.State == GameState.Ready && pool.ActiveCount == 0, "Ready has no active bullets.");
                    pool.Fire(new Vector3(10, 2, 0), Vector3.down);
                    runStart = Time.time;
                    checkingGround = true;
                    return;
                }
                if (checkingGround)
                {
                    // Окремі негативні випадки: промах у землю та старт кулі всередині
                    // землі теж повинні коректно повернути снаряд у пул.
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
                    if (Time.time < pendingStart || game.IsPreparing) return;
                    pendingStart = -1;
                    if (run == (recording ? 2 : 4)) { Finish(0); return; }
                    StartRun();
                    return;
                }
                maxBullets = Mathf.Max(maxBullets, pool.ActiveCount);
                // Для цієї конфігурації вистачає прогрітих 24 куль. Тест виявляє
                // зростання, але це не означає, що ObjectPool має жорсткий active-ліміт 24.
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
                    // Перший заїзд ігнорує ворогів і має програти; наступні наводяться
                    // та мають дістатись фінішу. Після кожного перевіряємо очищення куль/лінії.
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
                if (recording ? Time.time - resultFrameTime < 2 || Time.unscaledTime - resultTime < .35f
                    : Time.unscaledTime - resultTime < .4f) return;
                if (recording && run == 1) { Finish(0); return; }
                game.HandlePress();
                Require(game.State == GameState.Ready && car.Progress == 0 && health.Current == health.Maximum && spawner.KilledCount == 0,
                    "Result did not reset to a clean Ready state.");
                run++;
                pendingStart = Time.time + (recording ? .75f : .1f);
            }
            catch (Exception exception)
            {
                Report.AppendLine("FAIL: " + exception);
                Finish(1);
            }
        }

        private static void StartRun()
        {
            // Фіксовані різні seed дають відтворювані варіанти розстановки.
            // Зміни SerializedObject стосуються цього Play Mode, не записуються у сцену на диску.
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
            // Тільки тестовий агент: обирає близьку загрозу або бочку біля групи
            // й надсилає команди Aim. Реальна гра залишає горизонтальний кут гравцю.
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
            // path == null означає відеокадр у вже виділені буфери;
            // інший шлях — окремий PNG. Для відео не створюємо нову Texture2D щокадру.
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
                    CaptureAudio();
                    videoFrames++;
                }
                else File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                // Відновлюємо тимчасову конфігурацію рендеру навіть при збої запису.
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
            // Шлях до локального енкодера не зашито в проєкт і не потрібно встановлювати
            // FFmpeg заради самої гри. Запис — окрема опція через FACTURA_FFMPEG.
            string ffmpeg = Environment.GetEnvironmentVariable("FACTURA_FFMPEG");
            Require(File.Exists(ffmpeg), "Set FACTURA_FFMPEG to a local ffmpeg executable for opt-in video recording.");
            videoTarget = RenderTexture.GetTemporary(640, 1136, 24);
            videoPixels = new Texture2D(640, 1136, TextureFormat.RGB24, false);
            videoBuffer = new byte[640 * 1136 * 3];
            string destination = Path.GetFullPath("Library/Factura_Autoplay_Review.mp4");
            // Передаємо RGB-кадри в stdin без shell. MP4 є локальним вихідним
            // артефактом; vflip виправляє орієнтацію ReadPixels для енкодера.
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
            // Звук беремо з реального Unity-міксу через offline AudioRenderer,
            // а не накладаємо довільну музику замість ігрових ефектів.
            Require(AudioSettings.speakerMode == AudioSpeakerMode.Stereo, "Offline review recording expects stereo output.");
            audioSampleRate = AudioSettings.outputSampleRate;
            audioCapture = new FileStream("Library/Factura_ReviewAudio.f32", FileMode.Create, FileAccess.Write);
            audioRecording = AudioRenderer.Start();
            Require(audioRecording, "Could not start offline Unity audio capture.");
            UnityEngine.Object.FindFirstObjectByType<GameAudio>().SendMessage("OnApplicationFocus", true);
            var label = new GameObject("Autoplay review label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(GameObject.Find("Game UI").transform, false);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(20, 15);
            rect.sizeDelta = new Vector2(680, 28);
            var text = label.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            // Позначка чесно відрізняє програмне керування у відео від ручного плейтесту.
            text.text = "UNITY EDITOR / SCRIPTED INPUT";
            text.color = new Color(1, 1, 1, .8f);
            text.raycastTarget = false;
        }

        private static void Finish(int code)
        {
            // Зупиняємо захоплення та закриваємо stdin енкодера, щоб він завершив MP4.
            // Перевіряємо тривалість реального звуку й відео та повертаємо код результату.
            SessionState.SetBool(Key, false);
            SessionState.SetBool(VideoKey, false);
            EditorApplication.update -= Tick;
            if (audioRecording) { AudioRenderer.Stop(); audioRecording = false; }
            audioCapture?.Dispose();
            audioCapture = null;
            Time.captureDeltaTime = 0;
            if (encoder != null)
            {
                try
                {
                    encoder.StandardInput.Close();
                    Require(encoder.WaitForExit(20000), "Video encoder did not finish.");
                    Require(encoder.ExitCode == 0, "Video encoding failed: " + encoder.StandardError.ReadToEnd());
                    double videoDuration = videoFrames / 30.0;
                    double audioDuration = (double)audioFrames / audioSampleRate;
                    Require(Math.Abs(videoDuration - audioDuration) < .04, "Offline audio and video duration diverged.");
                    Report.AppendLine($"PASS: Video {videoDuration:F2}s / {videoFrames} frames; actual Unity stereo audio {audioDuration:F2}s at {audioSampleRate} Hz.");
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

        private static void CaptureAudio()
        {
            // Unity повідомляє кількість аудіосемплів для поточного capture-кадру.
            // Два канали stereo записуємо як float32; тимчасовий NativeArray
            // звільняється через using, керований byte-буфер повторно використовується.
            int frames = AudioRenderer.GetSampleCountForCaptureFrame();
            using var samples = new NativeArray<float>(frames * 2, Allocator.Temp);
            Require(AudioRenderer.Render(samples), "Unity offline audio rendering failed.");
            int bytes = samples.Length * sizeof(float);
            if (audioBuffer == null || audioBuffer.Length != bytes) audioBuffer = new byte[bytes];
            samples.Reinterpret<byte>(sizeof(float)).CopyTo(audioBuffer);
            audioCapture.Write(audioBuffer, 0, bytes);
            audioFrames += frames;
        }
    }
}
