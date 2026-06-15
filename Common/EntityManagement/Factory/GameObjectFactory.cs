using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Dessentials.Common.EntityManagement
{
    public class GameObjectFactory<TObject> : Singleton<GameObjectFactory<TObject>>
    where TObject : MonoBehaviour
    {
        private GameObject _prefab;
        private AsyncOperationHandle<GameObject> _prefabHandle;
        private readonly Stack<TObject> _pool = new();

        protected virtual string AddressableID => typeof(TObject).Name;

        public GameObjectFactory() { }

        private async UniTask<GameObject> LoadPrefabAsync()
        {
            if (_prefab != null)
                return _prefab;

            _prefabHandle = Addressables.LoadAssetAsync<GameObject>(AddressableID);
            _prefab = await _prefabHandle.ToUniTask();
            return _prefab;
        }

        public async UniTask<TObject> GetAsync(Transform parent = null)
        {
            TObject instance;

            if (_pool.Count > 0)
            {
                instance = _pool.Pop();
                instance.transform.SetParent(parent);
                instance.gameObject.SetActive(true);
            }
            else
            {
                var prefab = await LoadPrefabAsync();
                instance = Object.Instantiate(prefab, parent).GetComponent<TObject>();
            }

            EntityRegistry<TObject>.Register(instance);
            return instance;
        }

        public void Return(TObject obj)
        {
            EntityRegistry<TObject>.Unregister(obj);
            obj.gameObject.SetActive(false);
            _pool.Push(obj);
        }

        public void Preload(int count, Transform parent = null)
        {
            PreloadAsync(count, parent).Forget();
        }

        public async UniTask PreloadAsync(int count, Transform parent = null)
        {
            var prefab = await LoadPrefabAsync();

            for (int i = 0; i < count; i++)
            {
                var instance = Object.Instantiate(prefab, parent).GetComponent<TObject>();
                instance.gameObject.SetActive(false);
                _pool.Push(instance);
            }
        }

        protected override void Dispose()
        {
            while (_pool.Count > 0)
            {
                var obj = _pool.Pop();
                if (obj != null)
                    Object.Destroy(obj.gameObject);
            }

            if (_prefabHandle.IsValid())
                Addressables.Release(_prefabHandle);

            _prefab = null;
            base.Dispose();
        }
    }
}
