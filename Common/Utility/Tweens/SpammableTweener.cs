#if DOTWEEN
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Dessentials.Utility
{
    public class SpammableTweener
    {
        private Func<Tween> _tweenCreator;
        
        private Tween _runningTween;

        public SpammableTweener(Func<Tween> tweenCreator)
        {
            _tweenCreator = tweenCreator;
        }

        public void PlayTween()
        {
            if (_runningTween.IsActive())
                _runningTween.Kill(true);
            
            _runningTween = _tweenCreator().Play();
        }
    }
}

#endif