using System.Collections;
using System.Collections.Generic;
using System.Globalization;
#if DOTWEEN
using DG.Tweening;
#endif
using UnityEngine;

namespace Dessentials.Extensions
{
    public static class DTransformExtensionsq
    {
#if DOTWEEN
        public static Sequence DOBouncyScale(this Transform transform, float duration, float strength = 0.1f)
        {
            var originalScale = transform.localScale;

            var stretchXScale = new Vector3
            {
                x = transform.localScale.x * (1 + strength),
                y = transform.localScale.y * (1 - strength),
                z = transform.localScale.z
            };

            var stretchYScale = new Vector3
            {
                x = transform.localScale.x * (1 - strength),
                y = transform.localScale.y * (1 + strength),
                z = transform.localScale.z
            };

            var seq = DOTween.Sequence();

            seq.Append(transform.DOScale(stretchXScale, duration * 2 / 5));
            seq.Append(transform.DOScale(stretchYScale, duration * 2 / 5));
            seq.Append(transform.DOScale(originalScale, duration * 1 / 5));

            return seq;
        }
#endif
    }
}
