using System;
using System.Collections.Generic;
using GameFoundation.Core;
using UnityEngine;

namespace GameFoundation.UI
{
    /// <summary>
    /// Central window manager. Every menu screen registers here, while navigation
    /// always goes through Open / OpenOverlay / Back.
    /// </summary>
    public class UIService : MonoBehaviour
    {
        private readonly Dictionary<Type, IWindow> _windows = new();
        private readonly Stack<IWindow> _stack = new();

        public bool HasWindowsOpen => _stack.Count > 0;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        public void Register<T>(T window) where T : IWindow
        {
            _windows[window.GetType()] = window;
        }

        public T Open<T>() where T : class, IWindow
        {
            var window = ResolveWindow<T>();
            if (window == null) return null;

            if (_stack.Count > 0 && _stack.Peek() == window)
                return window as T;

            if (TryRevealExisting(window))
                return window as T;

            if (_stack.Count > 0)
                _stack.Peek().Close();

            if (window is MonoBehaviour mono && !mono.gameObject.activeSelf)
                mono.gameObject.SetActive(true);

            window.Open();
            _stack.Push(window);
            return window as T;
        }

        public T OpenOverlay<T>() where T : class, IWindow
        {
            var window = ResolveWindow<T>();
            if (window == null) return null;

            if (window is MonoBehaviour mono && !mono.gameObject.activeSelf)
                mono.gameObject.SetActive(true);

            RemoveFromStack(window);
            window.Open();
            _stack.Push(window);
            return window as T;
        }

        public void Back()
        {
            if (_stack.Count <= 1) return;

            var current = _stack.Pop();
            current.Close();

            var previousWindow = _stack.Peek();
            if (previousWindow is MonoBehaviour prevMono && !prevMono.gameObject.activeSelf)
                prevMono.gameObject.SetActive(true);

            previousWindow.Open();
        }

        private void Update()
        {
            if (Input.GetButtonDown("Cancel") && _stack.Count > 1)
                Back();
        }

        private IWindow ResolveWindow<T>() where T : class, IWindow
        {
            if (_windows.TryGetValue(typeof(T), out var window))
                return window;

            var candidates = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (var candidate in candidates)
            {
                if (candidate is T match)
                {
                    Register(match);
                    return match;
                }
            }

            Debug.LogError($"[UIService] Window of type {typeof(T)} is not registered and was not found on the scene!");
            return null;
        }

        private bool TryRevealExisting(IWindow window)
        {
            if (!ContainsInStack(window))
                return false;

            while (_stack.Count > 0 && _stack.Peek() != window)
                _stack.Pop().Close();

            if (window is MonoBehaviour mono && !mono.gameObject.activeSelf)
                mono.gameObject.SetActive(true);

            window.Open();
            return true;
        }

        private bool ContainsInStack(IWindow window)
        {
            foreach (var item in _stack)
            {
                if (item == window)
                    return true;
            }

            return false;
        }

        private void RemoveFromStack(IWindow window)
        {
            if (_stack.Count == 0) return;

            var rebuilt = new Stack<IWindow>();
            while (_stack.Count > 0)
            {
                var item = _stack.Pop();
                if (item != window)
                    rebuilt.Push(item);
            }

            while (rebuilt.Count > 0)
                _stack.Push(rebuilt.Pop());
        }
    }
}
