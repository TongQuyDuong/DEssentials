#if DOTWEEN
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Dessentials.Utility
{
    public class RestartableTweener
    {
        private Func<Tween> _tweenCreator;
        
        private Tween _tween;

        public RestartableTweener(Func<Tween> tweenCreator)
        {
            _tweenCreator = tweenCreator;
        }

        public void PlayTween()
        {
            if (_tween != null)
            {
                if (_tween.IsActive())
                    _tween.Kill(true);
                
                _tween.Restart();
            }
            else 
            {
                _tween = _tweenCreator();
            }
        }
    }
}
#endif
