using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CubeBlast.Game
{
    public enum Ease
    {
        Linear,
        InQuad,
        OutQuad,
        InOutQuad,
        OutCubic,
        InBack,
        OutBack,
        OutBounce
    }

    /// <summary>
    /// Minimal coroutine-based tween engine used for all animations in the game
    /// (the case study allows third-party tween libraries, but a small in-house
    /// one keeps the project dependency-free and fully under our control).
    /// </summary>
    public static class Tween
    {
        private class TweenRunner : MonoBehaviour
        {
        }

        private static TweenRunner _runner;

        private static TweenRunner Runner
        {
            get
            {
                if (_runner == null)
                {
                    var go = new GameObject("~TweenRunner");
                    go.hideFlags = HideFlags.HideInHierarchy;
                    _runner = go.AddComponent<TweenRunner>();
                }
                return _runner;
            }
        }

        public static float Evaluate(Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.InQuad: return t * t;
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t);
                case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.InBack:
                {
                    const float s = 1.70158f;
                    return t * t * ((s + 1f) * t - s);
                }
                case Ease.OutBack:
                {
                    const float s = 1.70158f;
                    float u = t - 1f;
                    return u * u * ((s + 1f) * u + s) + 1f;
                }
                case Ease.OutBounce:
                {
                    const float n = 7.5625f;
                    const float d = 2.75f;
                    if (t < 1f / d) { return n * t * t; }
                    if (t < 2f / d) { t -= 1.5f / d; return n * t * t + 0.75f; }
                    if (t < 2.5f / d) { t -= 2.25f / d; return n * t * t + 0.9375f; }
                    t -= 2.625f / d;
                    return n * t * t + 0.984375f;
                }
                default: return t;
            }
        }

        /// <summary>Animates an arbitrary 0..1 value. The workhorse behind every helper below.</summary>
        public static Coroutine To(Action<float> onUpdate, float duration, Ease ease = Ease.Linear,
            float delay = 0f, Action onComplete = null)
        {
            return Runner.StartCoroutine(Run(onUpdate, duration, ease, delay, onComplete));
        }

        private static IEnumerator Run(Action<float> onUpdate, float duration, Ease ease, float delay, Action onComplete)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                try
                {
                    onUpdate(Evaluate(ease, t));
                }
                catch (MissingReferenceException)
                {
                    yield break; // target destroyed mid-tween
                }
                yield return null;
            }
            if (onComplete != null)
            {
                onComplete();
            }
        }

        public static Coroutine Move(Transform target, Vector3 to, float duration, Ease ease = Ease.OutQuad,
            float delay = 0f, Action onComplete = null)
        {
            Vector3 from = Vector3.zero;
            bool captured = false;
            return To(t =>
            {
                if (target == null) { return; }
                if (!captured) { from = target.localPosition; captured = true; }
                target.localPosition = Vector3.LerpUnclamped(from, to, t);
            }, duration, ease, delay, onComplete);
        }

        public static Coroutine Scale(Transform target, Vector3 to, float duration, Ease ease = Ease.OutQuad,
            float delay = 0f, Action onComplete = null)
        {
            Vector3 from = Vector3.zero;
            bool captured = false;
            return To(t =>
            {
                if (target == null) { return; }
                if (!captured) { from = target.localScale; captured = true; }
                target.localScale = Vector3.LerpUnclamped(from, to, t);
            }, duration, ease, delay, onComplete);
        }

        /// <summary>Quick grow-and-settle used for pops and pulses.</summary>
        public static void Punch(Transform target, float amount, float duration)
        {
            if (target == null) { return; }
            Vector3 baseScale = target.localScale;
            To(t =>
            {
                if (target == null) { return; }
                float wave = Mathf.Sin(t * Mathf.PI);
                target.localScale = baseScale * (1f + amount * wave);
            }, duration, Ease.Linear, 0f, () =>
            {
                if (target != null) { target.localScale = baseScale; }
            });
        }

        public static Coroutine Fade(SpriteRenderer target, float toAlpha, float duration, Ease ease = Ease.Linear,
            float delay = 0f, Action onComplete = null)
        {
            float from = -1f;
            return To(t =>
            {
                if (target == null) { return; }
                if (from < 0f) { from = target.color.a; }
                var c = target.color;
                c.a = Mathf.LerpUnclamped(from, toAlpha, t);
                target.color = c;
            }, duration, ease, delay, onComplete);
        }

        public static Coroutine Rotate(Transform target, float toZ, float duration, Ease ease = Ease.Linear,
            float delay = 0f, Action onComplete = null)
        {
            float from = 0f;
            bool captured = false;
            return To(t =>
            {
                if (target == null) { return; }
                if (!captured) { from = target.localEulerAngles.z; captured = true; }
                target.localEulerAngles = new Vector3(0f, 0f, Mathf.LerpUnclamped(from, toZ, t));
            }, duration, ease, delay, onComplete);
        }

        public static Coroutine Delay(float seconds, Action action)
        {
            return To(_ => { }, seconds, Ease.Linear, 0f, action);
        }
    }
}
