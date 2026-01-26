using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dessentials.Utility
{
    public class ExtendedParticleSystem : MonoBehaviour
    {
        [SerializeField]
        private ParticleSystem _mainParticleSystem;
        
        [SerializeField]
        private List<ParticleSystem> _subParticleSystems;

        public ParticleSystem Main
            => _mainParticleSystem;

        public ParticleSystemRenderer MainRenderer
        {
            get
            {
                if (_mainParticleSystem == null)
                    _mainParticleSystem = GetComponent<ParticleSystem>();

                return _mainRenderer;
            }
        }
        
        public List<ParticleSystem> Subs
            => _subParticleSystems;
        public List<ParticleSystemRenderer> SubRenderers
        {
            get
            {
                if (_subRenderers == null)
                    _subRenderers = GetComponentsInChildren<ParticleSystemRenderer>(true).ToList();

                return _subRenderers;
            }
        }

        
        private ParticleSystemRenderer _mainRenderer;
        private List<ParticleSystemRenderer> _subRenderers;

        private void Awake()
        {
            _mainRenderer = GetComponent<ParticleSystemRenderer>();
            _subRenderers = GetComponentsInChildren<ParticleSystemRenderer>(true).ToList();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _mainParticleSystem = GetComponent<ParticleSystem>();

            _subParticleSystems = GetComponentsInChildren<ParticleSystem>(true).Where(x => x != _mainParticleSystem).ToList();
        }
#endif

        public void SetSortingOrder(int sortingOrder)
        {
            MainRenderer.sortingOrder = sortingOrder;

            foreach (var sub in SubRenderers)
            {
                sub.sortingOrder = sortingOrder + 1;
            }
        }
    }
}

