using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Dessentials.Utility
{
    /// <summary>
    /// Holds an Addressable particle effect and keeps it as a child of this object: <see cref="Play"/> loads and
    /// instantiates it the first time and only switches it back on after that, and, unless Auto Disable is off,
    /// it switches itself off again after the delay. The effect is a prefab nobody references directly, so a scene
    /// that never plays it never loads it.
    /// </summary>
    /// <remarks>
    /// <see cref="Preload"/> does the load and the instantiation ahead of time, leaving the effect off, so
    /// the first <see cref="Play"/> has nothing left to wait for. <see cref="Unload"/> destroys the instance and
    /// releases the Addressable. A <see cref="Play"/> that comes while the effect is still loading plays it as
    /// soon as it arrives.
    /// </remarks>
    public class ParticleSpawner : MonoBehaviour, IAddressablePreloadable
    {
        [SerializeField]
        [Tooltip("The particle effect prefab. It is instantiated as a child of this object.")]
        private AssetReferenceGameObject _particle;

        [SerializeField]
        [Tooltip("Local position of the effect under this object when it is spawned. Replaces whatever position " +
                 "the prefab has.")]
        private Vector3 _spawnLocalPosition;

        [SerializeField]
        [Tooltip("Load and instantiate the effect (switched off) when this object wakes, so the first Play is instant.")]
        private bool _preloadOnAwake;

        [SerializeField]
        [Tooltip("Switch the effect's object off Auto Disable Delay seconds after Play. Off, the effect stays on " +
                 "until the next Play restarts it or Unload destroys it.")]
        private bool _autoDisable = true;

#if ODIN_INSPECTOR
        [ShowIf(nameof(_autoDisable))]
#endif
        [SerializeField, Min(0f)]
        [Tooltip("Seconds after Play before the effect is switched off. Only used while Auto Disable is on.")]
        private float _autoDisableDelay = 2f;

        private AsyncOperationHandle<GameObject> _handle;
        private GameObject _instance;

        // Bumped by Unload, so a load still in flight when it is called knows it was cancelled.
        private int _loadVersion;
        private bool _playWhenLoaded;
        private float _disableAt = -1f;

        /// <summary>The effect is instantiated as a child, on or off.</summary>
        public bool IsLoaded => _instance != null;

        /// <summary>The effect is instantiated and switched on.</summary>
        public bool IsPlaying => _instance != null && _instance.activeSelf;

        private void Awake()
        {
            if (_preloadOnAwake) Preload();
        }

        private void Update() => Tick(Time.time);

        private void OnDestroy() => Unload();

        /// <summary>
        /// Loads and instantiates the effect as a child, switched off. Does nothing when it is already there or on
        /// its way.
        /// </summary>
        public void Preload()
        {
            if (_instance != null || _handle.IsValid()) return;

            if (_particle == null || !_particle.RuntimeKeyIsValid())
            {
                Debug.LogWarning($"[{nameof(ParticleSpawner)}] No particle assigned.", this);
                return;
            }

            int version = _loadVersion;
            _handle = Addressables.InstantiateAsync(_particle.RuntimeKey, transform, false);
            _handle.Completed += operation => OnLoaded(operation, version);
        }

        /// <summary>
        /// Destroys the instance and releases the Addressable. A load still running is dropped the moment it
        /// finishes. Safe to call when nothing is loaded.
        /// </summary>
        public void Unload()
        {
            _loadVersion++;
            _playWhenLoaded = false;
            _disableAt = -1f;

            if (_instance != null) Addressables.ReleaseInstance(_instance);
            _instance = null;

            // A load still in flight releases its own result when it lands (OnLoaded sees the new version),
            // so a Preload straight after this is free to start another.
            _handle = default;
        }

        /// <summary>
        /// Plays the effect: loads and instantiates it first if it is not there yet, then switches it on and
        /// restarts it. Switches itself off again after the auto-disable delay, when Auto Disable is on.
        /// </summary>
        public void Play()
        {
            _playWhenLoaded = true;
            if (_instance != null) Show();
            else Preload();
        }

        /// <summary>
        /// Switches the effect off now and drops a <see cref="Play"/> still waiting for its load. The effect
        /// stays instantiated, so the next <see cref="Play"/> is instant. Safe when nothing is loaded.
        /// </summary>
        public void Stop()
        {
            _playWhenLoaded = false;
            _disableAt = -1f;
            if (_instance != null) _instance.SetActive(false);
        }

        private void OnLoaded(AsyncOperationHandle<GameObject> operation, int version)
        {
            // Unload (or destroying this object) came while it was loading: nobody wants it any more.
            if (this == null || version != _loadVersion)
            {
                if (operation.IsValid() && operation.Status == AsyncOperationStatus.Succeeded)
                    Addressables.ReleaseInstance(operation);
                return;
            }

            if (operation.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[{nameof(ParticleSpawner)}] Could not load '{_particle.RuntimeKey}'.", this);
                _handle = default;
                _playWhenLoaded = false;
                return;
            }

            _instance = operation.Result;
            _instance.transform.localPosition = _spawnLocalPosition;
            if (_playWhenLoaded) Show();
            else _instance.SetActive(false);
        }

        private void Show()
        {
            _playWhenLoaded = false;
            _instance.SetActive(true);

            // A prefab whose particles do not play on awake would come on silent, and a Play while it is
            // already running should start it over rather than do nothing.
            var system = _instance.GetComponentInChildren<ParticleSystem>(true);
            if (system != null)
            {
                system.Clear(true);
                system.Play(true);
            }

            _disableAt = _autoDisable ? Time.time + _autoDisableDelay : -1f;
        }

        /// <param name="now">Passed in rather than read, so an Edit Mode check can drive the delay.</param>
        private void Tick(float now)
        {
            if (_disableAt < 0f || now < _disableAt) return;

            _disableAt = -1f;
            if (_instance != null) _instance.SetActive(false);
        }
    }
}
