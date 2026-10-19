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
    public static class CompactMenuCheck
    {
        private const string Key = "Factura.CompactMenuCheck";
        private static readonly StringBuilder Report = new();
        private static GameManager game;
        private static GameAudio audio;
        private static GameUI ui;
        private static Health health;
        private static Mouse mouse;
        private static Transform ready;
        private static int phase;
        private static int lastFrame = -1;
        private static double phaseTime;
        private static double deadline;

        static CompactMenuCheck()
        {
            if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick;
        }

        public static void Run()
        {
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
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            try
            {
                if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 30;
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Menu input check timed out.");
                double elapsed = EditorApplication.timeSinceStartup - phaseTime;
                if (phase == 0)
                {
                    if (Time.time < .3f) return;
                    game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
                    if (game.IsPreparing) return;
                    audio = UnityEngine.Object.FindFirstObjectByType<GameAudio>();
                    ui = UnityEngine.Object.FindFirstObjectByType<GameUI>();
                    health = UnityEngine.Object.FindFirstObjectByType<CarController>().GetComponent<Health>();
                    ready = ui.transform.Find("Ready");
                    Check(game.State == GameState.Ready && ready.gameObject.activeSelf, "Compact menu appears in Ready.");
                    Check(ready.GetComponentsInChildren<Button>().Length == 3 && ready.Find("Quit/Label").GetComponent<Text>().text == "EXIT",
                        "Play, sound and exit are serialized scene buttons.");
                    var settings = UnityEngine.Object.Instantiate(InputSystem.settings);
                    settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                    settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    InputSystem.settings = settings;
                    mouse = InputSystem.AddDevice<Mouse>("FacturaMenuMouse");
                    Canvas.ForceUpdateCanvases();
                    Move("Sound", false);
                    Next();
                }
                else if (phase == 1 && elapsed > .12) { Move("Sound", true); Next(); }
                else if (phase == 2 && elapsed > .12)
                {
                    Check(game.State == GameState.Ready, "Pressing Sound does not leak into tap-to-start.");
                    Move("Sound", false);
                    Next();
                }
                else if (phase == 3 && elapsed > .12)
                {
                    Check(audio.IsMuted && ready.Find("Sound/Label").GetComponent<Text>().text == "SOUND: OFF"
                        && game.State == GameState.Ready, "A real UI click mutes audio and updates its label without starting the car.");
                    Move("Sound", true);
                    Next();
                }
                else if (phase == 4 && elapsed > .12) { Move("Sound", false); Next(); }
                else if (phase == 5 && elapsed > .12)
                {
                    Check(!audio.IsMuted && ready.Find("Sound/Label").GetComponent<Text>().text == "SOUND: ON",
                        "The next real UI click restores audio.");
                    Move("Play", true);
                    Next();
                }
                else if (phase == 6 && elapsed > .12)
                {
                    Check(game.State == GameState.Ready, "Play waits for the UI click release, not a stray gameplay press.");
                    Move("Play", false);
                    Next();
                }
                else if (phase == 7 && elapsed > .12)
                {
                    Check(game.State == GameState.Playing && !ready.gameObject.activeSelf, "Play starts the run and hides the menu.");
                    health.ApplyDamage(health.Current);
                    Next();
                }
                else if (phase == 8 && elapsed > .75)
                {
                    Check(game.State == GameState.Lose, "Loss still shows the result state.");
                    Queue(new Vector2(Screen.width * .5f, Screen.height * .6f), true);
                    Queue(new Vector2(Screen.width * .5f, Screen.height * .6f), false);
                    Check(game.State == GameState.Ready && ready.gameObject.activeSelf && !audio.IsMuted,
                        "A separate result tap restores Ready, the menu and the selected sound setting.");
                    Next();
                }
                else if (phase == 9 && elapsed > .12 && !game.IsPreparing)
                {
                    Queue(new Vector2(Screen.width * .1f, Screen.height * .6f), true);
                    Queue(new Vector2(Screen.width * .1f, Screen.height * .6f), false);
                    Check(game.State == GameState.Playing, "Tapping the road also starts the run, as required by the brief.");
                    Finish(0);
                }
            }
            catch (Exception error) { Report.AppendLine("FAIL: " + error); Finish(1); }
        }

        private static void Move(string button, bool held)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, ready.Find(button).position);
            Queue(point, held);
        }

        private static void Queue(Vector2 position, bool held)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left, held));
            InputSystem.Update();
        }

        private static void Next() { phase++; phaseTime = EditorApplication.timeSinceStartup; }
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            Report.AppendLine("PASS: " + message);
        }
        private static void Finish(int code)
        {
            SessionState.SetBool(Key, false);
            EditorApplication.update -= Tick;
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            File.WriteAllText("Library/FacturaCompactMenuCheck.txt", Report.ToString());
            Debug.Log("FACTURA_MENU_CHECK\n" + Report);
            EditorApplication.Exit(code);
        }
    }
}
