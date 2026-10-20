// Інтеграційний сценарій бою та повторних заїздів; не замінює ручний плейтест.
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Factura.Editor
{
    [InitializeOnLoad]
    public static class CombatPlayCheck
    {
        private const string Key = "Factura.CombatPlayCheck.Running";
        private static readonly StringBuilder Report = new();
        private static GameManager game;
        private static CarController car;
        private static Health health;
        private static Spawner spawner;
        private static TurretController turret;
        private static Mouse mouse;
        private static Touchscreen touch;
        private static EnemyAI victim;
        private static int phase;
        private static float phaseTime;
        private static double deadline;
        private static int shots;
        private static int lastFrame = -1;
        private static Quaternion victimRestRotation;
        private static EnemyAI attacker;
        private static bool runPoseChecked;
        private static bool windupChecked;
        private static GameAudio audio;
        private static bool hitReactionChecked;
        private static float maxLegOpposition;
        private static float resultEnteredAt;
        private static int afterReset;

        static CombatPlayCheck()
        {
            if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick;
        }

        public static void Run()
        {
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
            EditorApplication.QueuePlayerLoopUpdate();
            if (Time.frameCount == lastFrame) return;
            lastFrame = Time.frameCount;
            try
            {
                if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 45;
                Check(EditorApplication.timeSinceStartup < deadline, "Integration check timed out.");
                float elapsed = Time.time - phaseTime;
                if (afterReset != 0)
                {
                    if (game.IsPreparing) return;
                    var data = new SerializedObject(car);
                    if (afterReset == 1)
                    {
                        Check(game.State == GameState.Ready && spawner.Barrel.IsAlive && spawner.KilledCount == 0,
                            "Run reset restores the barrel and kill counter.");
                        data.FindProperty("routeLength").floatValue = 1;
                        data.ApplyModifiedPropertiesWithoutUndo();
                        Click();
                    }
                    else
                    {
                        Check(game.State == GameState.Ready, "Win also resets to Ready with a separate press.");
                        data.FindProperty("routeLength").floatValue = 100;
                        data.ApplyModifiedPropertiesWithoutUndo();
                        touch = InputSystem.AddDevice<Touchscreen>("FacturaCheckTouch");
                        QueueTouch(UnityEngine.InputSystem.TouchPhase.Began, 0.5f);
                        Check(game.State == GameState.Playing, "Primary touch starts the run.");
                        QueueTouch(UnityEngine.InputSystem.TouchPhase.Moved, 0.6f);
                        QueueMouse(new Vector2(Screen.width * 0.2f, Screen.height * 0.2f), false);
                    }
                    afterReset = 0;
                    Next();
                    return;
                }
                if (phase == 4 && attacker.State == EnemyState.Chase)
                {
                    var runningSkin = attacker.GetComponentInChildren<SkinnedMeshRenderer>();
                    maxLegOpposition = Mathf.Max(maxLegOpposition,
                        Quaternion.Angle(runningSkin.bones[8].localRotation, runningSkin.bones[11].localRotation));
                }
                if (phase == 0)
                {
                    if (Time.time < 0.3f) return;
                    game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
                    if (game.IsPreparing) return;
                    game.StateChanged += state =>
                    {
                        if (state == GameState.Win || state == GameState.Lose) resultEnteredAt = Time.unscaledTime;
                    };
                    car = UnityEngine.Object.FindFirstObjectByType<CarController>();
                    health = car.GetComponent<Health>();
                    spawner = UnityEngine.Object.FindFirstObjectByType<Spawner>();
                    turret = car.GetComponentInChildren<TurretController>();
                    audio = UnityEngine.Object.FindFirstObjectByType<GameAudio>();
                    audio.SendMessage("OnApplicationFocus", true);
                    Check(audio.VoiceCount == 6 && audio.HasMusicLoop, "Six prewarmed SFX voices and licensed music loop are wired.");
                    Check(game.State == GameState.Ready && health.Current == 100 && spawner.Enemies.Count == 18, "Ready state and spawn.");
                    var skin = spawner.Enemies[0].GetComponentInChildren<SkinnedMeshRenderer>();
                    Check(skin && skin.bones.Length == 14 && skin.sharedMesh.vertexCount == 1652,
                        "Provided enemy geometry is skinned to 14 bones.");
                    Check(skin.sharedMaterial.IsKeywordEnabled("_EMISSION"), "URP hit-flash shader variant is enabled, not only its property-block value.");
                    Check(Quaternion.Angle(skin.bones[4].localRotation, Quaternion.identity) > 60,
                        "Idle pose has lowered arms, not a T-pose.");
                    spawner.Barrel.ResetAt(new Vector3(4, 0, 65));
                    var inputSettings = UnityEngine.Object.Instantiate(InputSystem.settings);
                    inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                    inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    InputSystem.settings = inputSettings;
                    mouse = InputSystem.AddDevice<Mouse>("FacturaCheckMouse");
                    turret.Fired += CountShot;
                    Capture("Ready");
                    Click();
                    Check(game.State == GameState.Playing, "Pointer press starts the run.");
                    car.SetRunning(false);
                    foreach (var enemy in spawner.Enemies) enemy.SetCombatActive(false);
                    victim = spawner.Enemies[0];
                    victimRestRotation = victim.transform.Find("Visual").localRotation;
                    victim.GetComponent<Rigidbody>().position = victim.transform.position = new Vector3(0, 0, 12);
                    Physics.SyncTransforms();
                    Vector3 aimPoint = turret.BarrelOrigin + Quaternion.Euler(0, 36, 0) * Vector3.forward * 10;
                    aimPoint.y = 0.9f;
                    QueueMouseAtWorld(aimPoint);
                    Next();
                }
                else if (phase == 1 && elapsed > 0.15f)
                {
                    float yaw = Mathf.DeltaAngle(0, turret.transform.localEulerAngles.y);
                    Check(yaw > 25 && yaw < 40 && !mouse.leftButton.isPressed, $"Mouse points toward the cursor without holding: {yaw:F1} degrees.");
                    Check(audio.PlayedShots > 0 && audio.MusicLevel > 0 && audio.MusicLevel <= .181f,
                        "Automatic firing plays SFX while background music stays quiet.");
                    QueueMouseAtWorld(victim.AimPoint);
                    Next();
                }
                else if (phase == 2 && !hitReactionChecked && victim.IsAlive && victim.GetComponent<Health>().Current < 30)
                {
                    var renderer = victim.GetComponentInChildren<SkinnedMeshRenderer>();
                    var properties = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(properties);
                    if (properties.GetColor("_EmissionColor").maxColorComponent > .1f)
                    {
                        var visual = victim.transform.Find("Visual");
                        Check(visual.localScale.y < .999f && visual.localPosition.sqrMagnitude > .00001f,
                            "A nonfatal projectile visibly flashes, recoils and compresses the enemy without moving its hitbox.");
                        Capture("HitReaction");
                        hitReactionChecked = true;
                    }
                }
                else if (phase == 2 && elapsed > 1)
                {
                    Check(hitReactionChecked && audio.PlayedImpacts > 0, "Nonfatal hit feedback and impact sounds precede death.");
                    Check(!victim.IsAlive && spawner.KilledCount >= 1 && shots >= 3, "Real projectiles apply damage, death and kill counter.");
                    var visual = victim.transform.Find("Visual");
                    Check(visual.gameObject.activeSelf && Quaternion.Angle(visual.localRotation, victimRestRotation) > 20, "Death fall remains visible before cleanup.");
                    Check(turret.GetComponent<TurretFeedback>().enabled, "Turret feedback is connected.");
                    Capture("ImpactAndDeath");
                    Next();
                }
                else if (phase == 3 && (!victim.transform.Find("Visual").gameObject.activeSelf || elapsed > 5))
                {
                    var feedback = victim.GetComponent<EnemyFeedback>();
                    var deathTime = typeof(EnemyFeedback).GetField("deathTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(feedback);
                    Check(!victim.transform.Find("Visual").gameObject.activeSelf, $"Dead visual finishes and hides (timer={deathTime}, enabled={feedback.enabled}).");
                    attacker = spawner.Enemies[1];
                    attacker.GetComponent<Rigidbody>().position = attacker.transform.position = new Vector3(3.4f, 0, 0);
                    attacker.SetCombatActive(true);
                    Physics.SyncTransforms();
                    Next();
                }
                else if (phase == 4 && elapsed > 0.13f && !runPoseChecked)
                {
                    Check(attacker.State == EnemyState.Chase && attacker.GetComponent<EnemyAnimation>().MovementBlend > 0.3f,
                        "Chasing enemy drives its run pose from actual movement.");
                    Check(maxLegOpposition > 5,
                        "Running legs move in opposing phases.");
                    Capture("EnemyRun");
                    runPoseChecked = true;
                }
                else if (phase == 4 && attacker.State == EnemyState.Attack && attacker.AttackWindupProgress > 0.6f && !windupChecked)
                {
                    Check(health.Current == 100, "Melee windup is visible before damage.");
                    Capture("EnemyWindup");
                    windupChecked = true;
                }
                else if (phase == 4 && health.Current < 100)
                {
                    Check(runPoseChecked && windupChecked, "Run and windup both precede the melee hit.");
                    Check(health.Current > 0, "Enemy melee damages the car.");
                    Check(audio.PlayedCarHits > 0, "Car damage plays a bounded hit sound.");
                    health.ApplyDamage(health.Current - 25, car.transform.position + Vector3.up, Vector3.left);
                    Next();
                }
                else if (phase == 5 && elapsed > 0.1f)
                {
                    var skin = attacker.GetComponentInChildren<SkinnedMeshRenderer>();
                    Check(Mathf.Abs(Mathf.DeltaAngle(0, skin.bones[4].localEulerAngles.x)) > 45,
                        "Melee strike reaches forward at the damage moment.");
                    Check(car.transform.Find("Low HP smoke").GetComponent<ParticleSystem>().isPlaying, "Low HP smoke starts.");
                    Capture("CarDamage");
                    health.ApplyDamage(health.Current);
                    Next();
                }
                else if (phase == 6 && game.State == GameState.Lose && Time.unscaledTime - resultEnteredAt > 0.5f)
                {
                    Check(game.State == GameState.Lose && !turret.IsActive, "Fatal damage enters Lose and stops firing.");
                    Check(!turret.GetComponentInChildren<LineRenderer>().enabled, "Loss immediately clears the aim guide.");
                    Check(GameObject.Find("Game UI").transform.Find("Result/Result title").GetComponent<Text>().text == "You lose", "Exact loss label.");
                    Click();
                    Next();
                }
                else if (phase == 7 && elapsed > 0.1f && !game.IsPreparing)
                {
                    Check(game.State == GameState.Ready && health.Current == 100 && car.Progress == 0 && spawner.KilledCount == 0,
                        $"One press resets to Ready, HP, progress and kills (state={game.State}, hp={health.Current}, progress={car.Progress:F4}, kills={spawner.KilledCount}).");
                    Check(spawner.Enemies.Count == 18 && !car.transform.Find("Low HP smoke").GetComponent<ParticleSystem>().isPlaying, "Enemies and transient damage effects reset.");
                    Check(audio.PlayedShots == 0 && audio.PlayedImpacts == 0 && audio.PlayedCarHits == 0,
                        "Reset clears transient sound state without creating more voices.");
                    spawner.Barrel.ResetAt(new Vector3(0, 0, 12));
                    victim = spawner.Enemies[0];
                    victim.GetComponent<Rigidbody>().position = victim.transform.position = new Vector3(1.3f, 0, 12.5f);
                    var outside = spawner.Enemies[1];
                    outside.GetComponent<Rigidbody>().position = outside.transform.position = new Vector3(7, 0, 12);
                    Physics.SyncTransforms();
                    Click();
                    car.SetRunning(false);
                    foreach (var enemy in spawner.Enemies) enemy.SetCombatActive(false);
                    Next();
                }
                else if (phase == 8 && elapsed > 0.85f)
                {
                    Check(!spawner.Barrel.IsAlive && !victim.IsAlive, "Real projectiles detonate the barrel and blast a nearby enemy.");
                    Check(audio.PlayedExplosions == 1, "Barrel explosion plays exactly one sound.");
                    Check(spawner.Enemies[1].IsAlive && spawner.KilledCount == 1, "Blast radius and kill counter exclude outside enemy and barrel itself.");
                    spawner.Barrel.GetComponent<Health>().ApplyDamage(20);
                    Check(spawner.KilledCount == 1, "Barrel explosion cannot repeat after death.");
                    Check(audio.PlayedExplosions == 1, "Dead barrel cannot replay its explosion sound.");
                    Capture("BarrelExplosion");
                    health.ApplyDamage(health.Current);
                    Next();
                }
                else if (phase == 9 && game.State == GameState.Lose && Time.unscaledTime - resultEnteredAt > 0.5f)
                {
                    Check(game.State == GameState.Lose, "Second fatal hit ends the barrel check run.");
                    Click();
                    afterReset = 1;
                }
                else if (phase == 10 && game.State == GameState.Win && Time.unscaledTime - resultEnteredAt > 0.5f)
                {
                    Check(game.State == GameState.Win && car.Progress >= 1 && !turret.IsActive, "Finish enters Win and stops movement/firing.");
                    Check(!turret.GetComponentInChildren<LineRenderer>().enabled, "Win immediately clears the aim guide.");
                    Check(GameObject.Find("Game UI").transform.Find("Result/Result title").GetComponent<Text>().text == "You win", "Exact win label.");
                    Capture("Win");
                    Click();
                    afterReset = 2;
                }
                else if (phase == 11 && elapsed > 0.15f)
                {
                    float yaw = Mathf.DeltaAngle(0, turret.transform.localEulerAngles.y);
                    Check(yaw > 15 && yaw < 21, $"Touch swipe aims the turret while a mouse device is present: {yaw:F1} degrees.");
                    QueueTouch(UnityEngine.InputSystem.TouchPhase.Ended, 0.6f);
                    car.SetRunning(false);
                    foreach (var enemy in spawner.Enemies) enemy.SetCombatActive(false);
                    victim = spawner.Enemies[0];
                    victim.GetComponent<Rigidbody>().position = victim.transform.position = car.transform.position + new Vector3(0, 0, -2.9f);
                    Physics.SyncTransforms();
                    QueueMouseAtWorld(victim.AimPoint);
                    Next();
                }
                else if (phase == 12 && elapsed > 0.45f)
                {
                    float yaw = Mathf.Abs(Mathf.DeltaAngle(0, turret.transform.localEulerAngles.y));
                    Check(yaw > 175 && !mouse.leftButton.isPressed, $"Cursor below the car turns the turret rearward without dragging: {yaw:F1} degrees.");
                    Capture("MouseRearAim");
                    Next();
                }
                else if (phase == 13 && (!victim.IsAlive || elapsed > 1.5f))
                {
                    Check(!victim.IsAlive, "Cursor-directed rear fire kills a close enemy.");
                    audio.SetMuted(true);
                    Check(audio.IsMuted && audio.MusicLevel == 0, "Mute stops the voices and silences the music immediately.");
                    audio.SetMuted(false);
                    Check(!audio.IsMuted && audio.HasMusicLoop && audio.VoiceCount == 6,
                        "Unmute preserves the looping track and fixed voice pool.");
                    Finish(0);
                }
            }
            catch (Exception exception)
            {
                Report.AppendLine("FAIL: " + exception);
                Finish(1);
            }
        }

        private static void CountShot() => shots++;
        private static void Next() { phase++; phaseTime = Time.time; }
        private static void Check(bool condition, string description)
        {
            if (!condition) throw new InvalidOperationException(description);
            if (!description.EndsWith("timed out.")) Report.AppendLine("PASS: " + description);
        }

        private static void QueueMouse(Vector2 position, bool held)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left, held));
            InputSystem.Update();
        }

        private static void QueueMouseAtWorld(Vector3 worldPoint)
        {
            Vector3 point = Camera.main.WorldToScreenPoint(worldPoint);
            QueueMouse(new Vector2(point.x, point.y), false);
        }

        private static void Click()
        {
            var position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            QueueMouse(position, true);
            QueueMouse(position, false);
        }

        private static void QueueTouch(UnityEngine.InputSystem.TouchPhase phase, float x)
        {
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = phase, position = new Vector2(Screen.width * x, Screen.height * 0.5f) });
            InputSystem.Update();
        }

        private static void Capture(string name)
        {
            var camera = Camera.main;
            var canvas = GameObject.Find("Game UI").GetComponent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>();
            var target = RenderTexture.GetTemporary(720, 1280, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            var mode = canvas.renderMode;
            var previousTarget = camera.targetTexture;
            var previousCamera = canvas.worldCamera;
            bool scaling = scaler.enabled;
            try
            {
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 0.5f;
                scaler.enabled = false;
                canvas.scaleFactor = 1;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(720, 1280, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 720, 1280), 0, 0);
                image.Apply();
                File.WriteAllBytes("Library/FacturaCheck_" + name + ".png", image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                canvas.renderMode = mode;
                canvas.worldCamera = previousCamera;
                scaler.enabled = scaling;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private static void Finish(int code)
        {
            SessionState.SetBool(Key, false);
            EditorApplication.update -= Tick;
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (touch != null) InputSystem.RemoveDevice(touch);
            File.WriteAllText("Library/FacturaCombatPlayCheck.txt", Report.ToString());
            Debug.Log("Factura combat play check\n" + Report);
            EditorApplication.Exit(code);
        }
    }
}
