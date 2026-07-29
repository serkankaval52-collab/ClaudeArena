using System.Collections;
using UnityEngine;

namespace CoreFactory.UI
{
    public static class SimpleTween
    {
        public static IEnumerator ScaleTween(Transform target, Vector3 fromScale, Vector3 toScale, float duration, AnimationCurve curve)
        {
            if (target == null) yield break;
            target.localScale = fromScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                float t = curve != null ? curve.Evaluate(progress) : progress;
                target.localScale = Vector3.LerpUnclamped(fromScale, toScale, t);
                yield return null;
            }
            if (target != null) target.localScale = toScale;
        }

        public static IEnumerator FadeTween(CanvasGroup target, float fromAlpha, float toAlpha, float duration)
        {
            if (target == null) yield break;
            target.alpha = fromAlpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                target.alpha = Mathf.Lerp(fromAlpha, toAlpha, progress);
                yield return null;
            }
            if (target != null) target.alpha = toAlpha;
        }
    }
}