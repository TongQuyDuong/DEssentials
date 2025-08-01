#if DOTWEEN
using DG.Tweening;
#endif
using System.Collections;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dessentials.Common.UI
{
    public class SectionedProgressBar : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField]
        private bool _autoInitOnAwake = false;
        [SerializeField]
#if ODIN_INSPECTOR
        [ShowIf(nameof(_autoInitOnAwake))]
#endif
        private int _initialBarCount = 2;
        
        [Header("Components")]
        [SerializeField]
        private GameObject _barDividerPrefab;
        [SerializeField]
        private GameObject _barDividerParent;
        [SerializeField]
        private Slider _barSlider;

        private List<GameObject> _activeBarDividers = new();
        
        private void Awake()
        {
            if (_autoInitOnAwake)
            {
                InitBarCount(_initialBarCount);
            }
        }

        public virtual void InitBarCount(int count, bool setFillMax = false)
        {
            _barSlider.maxValue = count;
            _barSlider.value = setFillMax ? count : 0;

            if (_activeBarDividers.Count < count)
            {
                while (_activeBarDividers.Count + 1 < count)
                {
                    var newDivider = Instantiate(_barDividerPrefab, _barDividerParent.transform);

                    _activeBarDividers.Add(newDivider);
                }
            }
            else if (_activeBarDividers.Count >= count)
            {
                for (int i = _activeBarDividers.Count - 1; i >= count - 1; --i)
                {
                    var divider = _activeBarDividers[i];

                    _activeBarDividers.RemoveAt(i);

                    Destroy(divider);
                }
            }
        }

#if DOTWEEN
        public virtual Tween TweenCurrentFill(int barCount, float duration = 0.1f)
        {
            return _barSlider.DOValue(barCount, duration);
        }
#endif
        
        public virtual void SetCurrentFill(int barCount)
        {
            _barSlider.value = barCount;
        }
    }
}
