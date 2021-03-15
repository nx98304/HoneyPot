using System;
using System.Collections;
using System.Collections.Generic;
#if IPA
using IllusionInjector;
using IllusionPlugin;
#endif
using UnityEngine;

namespace ToolBox.Extensions {
    internal static class MonoBehaviourExtensions
    {
#if IPA
        private static PluginComponent _pluginComponent;
        private static void CheckPluginComponent()
        {
            if (_pluginComponent == null)
                _pluginComponent = UnityEngine.Object.FindObjectOfType<PluginComponent>();
        }
        public static Coroutine ExecuteDelayed(this IPlugin self, Action action, int framecount = 1)
        {
            CheckPluginComponent();
            return _pluginComponent.ExecuteDelayed(action, framecount);
        }
        public static Coroutine ExecuteDelayed(this IPlugin self, Action action, float delay, bool timeScaled = true)
        {
            CheckPluginComponent();
            return _pluginComponent.ExecuteDelayed(action, delay, timeScaled);
        }
        public static Coroutine ExecuteDelayedFixed(this IPlugin self, Action action, int waitCount = 1)
        {
            CheckPluginComponent();
            return _pluginComponent.ExecuteDelayedFixed(action, waitCount);
        }
        public static Coroutine ExecuteDelayed(this IPlugin self, Func<bool> waitUntil, Action action)
        {
            CheckPluginComponent();
            return _pluginComponent.ExecuteDelayed(waitUntil, action);
        }

        public static Coroutine StartCoroutine(this IPlugin self, IEnumerator routine)
        {
            CheckPluginComponent();
            return _pluginComponent.StartCoroutine(routine);
        }
        public static Coroutine StartCoroutine(this IPlugin self, string methodName)
        {
            CheckPluginComponent();
            return _pluginComponent.StartCoroutine(methodName);
        }
        public static Coroutine StartCoroutine(this IPlugin self, string methodName, object value)
        {
            CheckPluginComponent();
            return _pluginComponent.StartCoroutine(methodName, value);
        }

        public static void StopCoroutine(this IPlugin self, Coroutine routine)
        {
            CheckPluginComponent();
            _pluginComponent.StopCoroutine(routine);
        }
        public static void StopCoroutine(this IPlugin self, IEnumerator routine)
        {
            CheckPluginComponent();
            _pluginComponent.StopCoroutine(routine);
        }
        public static void StopCoroutine(this IPlugin self, string methodName)
        {
            CheckPluginComponent();
            _pluginComponent.StopCoroutine(methodName);
        }
        public static void StopAllCouroutines(this IPlugin self)
        {
            CheckPluginComponent();
            _pluginComponent.StopAllCoroutines();
        }
#endif

        public static Coroutine ExecuteDelayed(this MonoBehaviour self, Action action, int frameCount = 1)
        {
            return self.StartCoroutine(ExecuteDelayed_Routine(action, frameCount));
        }

        private static IEnumerator ExecuteDelayed_Routine(Action action, int frameCount = 1)
        {
            for (int i = 0; i < frameCount; i++)
                yield return null;
            action();
        }

        public static Coroutine ExecuteDelayed(this MonoBehaviour self, Action action, float delay, bool timeScaled = true)
        {
            return self.StartCoroutine(ExecuteDelayed_Routine(action, delay, timeScaled));
        }

        private static IEnumerator ExecuteDelayed_Routine(Action action, float delay, bool timeScaled)
        {
            if (timeScaled)
                yield return new WaitForSeconds(delay);
            else
                yield return new WaitForSecondsRealtime(delay);
            action();
        }

        public static Coroutine ExecuteDelayed(this MonoBehaviour self, Func<bool> waitUntil, Action action)
        {
            return self.StartCoroutine(ExecuteDelayed_Routine(waitUntil, action));
        }

        private static IEnumerator ExecuteDelayed_Routine(Func<bool> waitUntil, Action action)
        {
            yield return new WaitUntil(waitUntil);
            action();
        }
    }
}