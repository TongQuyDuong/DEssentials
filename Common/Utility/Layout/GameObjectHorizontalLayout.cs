using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System;
using System.Collections;
using UnityEngine;

namespace Dessentials.Utility
{
    public partial class GameObjectHorizontalLayout : MonoBehaviour
    {
        [SerializeField]
        private float space = 1.5f;

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            
            RepositionNow();
        }

        public Sequence SetSpaceAndRepositionSmooth(float newSpace)
        {
            space = newSpace; 

            return RepositionSmooth();
        }

#if UNITASK_DOTWEEN_SUPPORT
        public async UniTask SetSpaceAndPlayStartLevelAnimation(float newSpace)
        {
            space = newSpace;

            RepositionNow();

            await UniTask.DelayFrame(2);

            var taskList = new List<UniTask>();

            var activeChilds = GetActiveChilds();
            var activeChildCount = activeChilds.Count; 

            for (int i = 0; i < activeChildCount; i++)
            {
                var child = activeChilds[i];
                var leftPos = transform.position.x - (activeChildCount - 1) * space / 2;
                var targetLocalPos = new Vector3(leftPos + i * space, 0, transform.position.z);

                child.gameObject.SetActive(false);

                child.DOKill();
                taskList.Add(child.DOLocalMoveX(targetLocalPos.x, 0.3f).From(targetLocalPos.x + 15)
                    .SetDelay(0.5f + i * 0.05f)
                    .SetEase(Ease.OutCubic)
                    .OnStart(() =>
                    {
                        child.gameObject.SetActive(true);
                    })
                    .ToUniTask()
                );
            }
            await UniTask.WhenAll(taskList);
        }
#endif

#if ODIN_INSPECTOR
        [Button]
#endif
        public Sequence RepositionSmooth()
        {
            var activeChilds = GetActiveChilds();
            var activeChildCount = activeChilds.Count;
            var leftPos = transform.position.x - (activeChildCount - 1) * space / 2;

            var seq = DOTween.Sequence();

            for (int i = 0; i < activeChildCount; i++)
            {
                var child = activeChilds[i];
                child.DOKill();
                seq.Join(child.DOLocalMoveX(leftPos + i * space, 0.2f));
            }

            return seq;
        }

#if ODIN_INSPECTOR
        [Button]
#endif
        public void RepositionNow()
        {
            var activeChilds = GetActiveChilds();
            var activeChildCount = activeChilds.Count;
            var leftPos = transform.position.x - (activeChildCount - 1) * space / 2;
            for (int i = 0; i < activeChildCount; i++)
            {
                var child = activeChilds[i];
                child.localPosition = new Vector3(leftPos + i * space, 0, transform.position.z);
            }
        }
        
        public void SetSpaceAndRepositionNow(float newSpace)
        {
            space = newSpace; 

            RepositionNow();
        }

        List<Transform> GetActiveChilds()
        {
            var activeChilds = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.gameObject.activeSelf)
                {
                    activeChilds.Add(child);
                }
            }

            return activeChilds;
        }
        
        public Vector2 GetNextPosition()
        {
            var activeChilds = GetActiveChilds();
            var rightPosition = transform.position.x + (activeChilds.Count - 1) * space / 2;
            return new Vector2(rightPosition + space, 0);
        }
        
        public Vector2 GetNextWorldPosition()
        {
            return transform.TransformPoint(GetNextPosition());
        }
    }
}