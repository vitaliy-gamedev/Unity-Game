using UnityEngine;

namespace GameFoundation.Pro.Animation
{
    public enum EaseType
    {
        Linear,
        InSine, OutSine, InOutSine,
        InQuad, OutQuad, InOutQuad,
        InCubic, OutCubic, InOutCubic,
        InBack, OutBack, InOutBack,
        InElastic, OutElastic,
        InBounce, OutBounce
    }

    /// <summary>
    /// Self-contained Robert Penner-style easing functions. No third-party
    /// dependency (DOTween etc.) so this compiles and ships cleanly inside
    /// a paid package with no licensing questions attached.
    /// </summary>
    public static class Easing
    {
        public static float Evaluate(EaseType type, float t)
        {
            t = Mathf.Clamp01(t);
            switch (type)
            {
                case EaseType.Linear: return t;

                case EaseType.InSine: return 1f - Mathf.Cos(t * Mathf.PI / 2f);
                case EaseType.OutSine: return Mathf.Sin(t * Mathf.PI / 2f);
                case EaseType.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;

                case EaseType.InQuad: return t * t;
                case EaseType.OutQuad: return 1f - (1f - t) * (1f - t);
                case EaseType.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

                case EaseType.InCubic: return t * t * t;
                case EaseType.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case EaseType.InOutCubic: return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

                case EaseType.InBack: return BackIn(t);
                case EaseType.OutBack: return BackOut(t);
                case EaseType.InOutBack: return BackInOut(t);

                case EaseType.InElastic: return ElasticIn(t);
                case EaseType.OutElastic: return ElasticOut(t);

                case EaseType.InBounce: return 1f - BounceOut(1f - t);
                case EaseType.OutBounce: return BounceOut(t);

                default: return t;
            }
        }

        private const float Back = 1.70158f;

        private static float BackIn(float t) => t * t * ((Back + 1f) * t - Back);

        private static float BackOut(float t)
        {
            t -= 1f;
            return t * t * ((Back + 1f) * t + Back) + 1f;
        }

        private static float BackInOut(float t)
        {
            float s = Back * 1.525f;
            t *= 2f;
            if (t < 1f) return 0.5f * (t * t * ((s + 1f) * t - s));
            t -= 2f;
            return 0.5f * (t * t * ((s + 1f) * t + s) + 2f);
        }

        private static float ElasticIn(float t)
        {
            if (t == 0f || t == 1f) return t;
            float p = 0.3f;
            float s = p / 4f;
            t -= 1f;
            return -(Mathf.Pow(2f, 10f * t) * Mathf.Sin((t - s) * (2f * Mathf.PI) / p));
        }

        private static float ElasticOut(float t)
        {
            if (t == 0f || t == 1f) return t;
            float p = 0.3f;
            float s = p / 4f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - s) * (2f * Mathf.PI) / p) + 1f;
        }

        private static float BounceOut(float t)
        {
            if (t < 1f / 2.75f) return 7.5625f * t * t;
            if (t < 2f / 2.75f) { t -= 1.5f / 2.75f; return 7.5625f * t * t + 0.75f; }
            if (t < 2.5f / 2.75f) { t -= 2.25f / 2.75f; return 7.5625f * t * t + 0.9375f; }
            t -= 2.625f / 2.75f;
            return 7.5625f * t * t + 0.984375f;
        }
    }
}
