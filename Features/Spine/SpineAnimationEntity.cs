#if DESSENTIALS_SPINE_ANIMATION
using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Events;

namespace Dessentials.Features.Spine
{
    public class SpineAnimationEntity : MonoBehaviour
    {
        [Serializable]
        private enum CallbackType
        {
            EndOfAnimation,
            CustomDelay
        }
        
        [SerializeField]
        private CallbackType _callbackType = CallbackType.EndOfAnimation;

        [ShowIf(nameof(_callbackType), CallbackType.CustomDelay)]
        [SerializeField]
        private float _callbackDelay;
        
        [SerializeField]
        private SkeletonAnimation _skeletonAnimation;
        
        [SerializeField]
        private AnimationReferenceAsset _playedAnim;

        [SerializeField]
        private Vector2 _localPositionSpawnOffset;
        
        [SerializeField]
        private UnityEvent _serializedCallback;

        private void OnDisable()
        {
            _skeletonAnimation.AnimationState.ClearTracks();
            _skeletonAnimation.AnimationState.SetEmptyAnimation(0, 0);
        }

        public void Play()
        {
            _skeletonAnimation.AnimationState.ClearTracks();
            _skeletonAnimation.AnimationState.SetAnimation(0, _playedAnim, false);
        }
        
        public async UniTask Play(Action callback)
        {
            Play();

            await UniTask.Yield();
            
            if (_callbackType == CallbackType.EndOfAnimation)
            {
                var currentAnim = _skeletonAnimation.AnimationState.GetCurrent(0);
                
                if (currentAnim != null)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(currentAnim.Animation.Duration));
                }
            }
            else
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_callbackDelay));
            }
            
            callback?.Invoke();
            _serializedCallback?.Invoke();
        }

        public async UniTask Play(Transform parent, Action callback)
        {
            transform.SetParent(parent);
            transform.localPosition = new Vector3(_localPositionSpawnOffset.x, _localPositionSpawnOffset.y, 0f);
            
            await Play(callback);
        }
    }
}
#endif