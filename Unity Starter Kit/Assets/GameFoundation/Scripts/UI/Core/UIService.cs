using System;
using System.Collections.Generic;
using GameFoundation.Core;
using UnityEngine;

namespace GameFoundation.UI
{
    /// <summary>
    /// Central window manager. Every screen (MainMenu, Settings, LevelSelect, popups)
    /// registers itself here, and navigation always goes through Open&lt;T&gt;() / Back().
    /// This is what makes Android back-button / Esc-on-PC work identically everywhere.
    /// </summary>
    public class UIService : MonoBehaviour
    {
        private readonly Dictionary<Type, IWindow> _windows = new();
        private readonly Stack<IWindow> _stack = new();

        private void Awake()
        {
            // Self-registers so Bootstrap doesn't need a special case for this one
            // service — see README_UA.md section 3 for why this matters.
            ServiceLocator.Register(this);
        }

        public void Register<T>(T window) where T : IWindow
        {
            if (window is MonoBehaviour mono && !mono.gameObject.activeSelf)
            {
                mono.gameObject.SetActive(true);
            }

            _windows[window.GetType()] = window;
        }

        public T Open<T>() where T : class, IWindow
        {
            if (!_windows.TryGetValue(typeof(T), out var window))
            {
                T foundWindow = null;
                foreach (var candidate in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true))
                {
                    if (candidate is T match)
                    {
                        foundWindow = match;
                        break;
                    }
                }

                if (foundWindow != null)
                {
                    Register(foundWindow);
                    window = foundWindow;
                }
                else
                {
                    Debug.LogError($"[UIService] Window of type {typeof(T)} is not registered and was not found on the scene!");
                    return null;
                }
            }

            // Закриваємо і фізично ховаємо попереднє вікно
            if (_stack.Count > 0 && _stack.Peek() != window)
            {
                var topWindow = _stack.Peek();
                topWindow.Close();
                if (topWindow is MonoBehaviour topMono)
                {
                    topMono.gameObject.SetActive(false);
                }
            }

            // Активуємо і відкриваємо нове вікно
            if (window is MonoBehaviour mono && !mono.gameObject.activeSelf)
            {
                mono.gameObject.SetActive(true);
            }

            window.Open();
            _stack.Push(window);
            return window as T;
        }

        /// <summary>Opens a window without touching the navigation stack — for popups on top of a screen.</summary>
        public T OpenOverlay<T>() where T : class, IWindow
        {
            if (!_windows.TryGetValue(typeof(T), out var window))
            {
                T foundWindow = null;
                foreach (var candidate in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true))
                {
                    if (candidate is T match)
                    {
                        foundWindow = match;
                        break;
                    }
                }

                if (foundWindow != null)
                {
                    Register(foundWindow);
                    window = foundWindow;
                }
                else
                {
                    Debug.LogError($"[UIService] Window of type {typeof(T)} is not registered.");
                    return null;
                }
            }

            if (window is MonoBehaviour mono && !mono.gameObject.activeSelf)
            {
                mono.gameObject.SetActive(true);
            }

            window.Open();
            _stack.Push(window);
            return window as T;
        }

        public void Back()
        {
            if (_stack.Count <= 1) return; // Не даємо закрити єдине головне вікно

            var current = _stack.Pop();
            current.Close();
            if (current is MonoBehaviour currentMono)
            {
                currentMono.gameObject.SetActive(false); // Фізично ховаємо поточне вікно
            }

            // Повертаємо попереднє вікно на екрані
            if (_stack.Count > 0)
            {
                var previousWindow = _stack.Peek();
                if (previousWindow is MonoBehaviour prevMono && !prevMono.gameObject.activeSelf)
                {
                    prevMono.gameObject.SetActive(true);
                }
                previousWindow.Open();
            }
        }

        public bool HasWindowsOpen => _stack.Count > 0;

        private void Update()
        {
            // ESC on desktop / back-gesture on Android both fire Cancel.
            if (Input.GetButtonDown("Cancel") && _stack.Count > 1)
                Back();
        }
    }
}