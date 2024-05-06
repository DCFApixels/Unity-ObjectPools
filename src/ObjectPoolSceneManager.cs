using UnityEngine;
using System.Collections.Generic;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DCFApixels.ObjectPools.Internal
{
    [DefaultExecutionOrder(10000)]
    internal class ObjectPoolSceneManager : MonoBehaviour
    {
        private static object _lock = new object();
        private static ObjectPoolSceneManager _instance;
        internal static ObjectPoolSceneManager Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<ObjectPoolSceneManager>();

                        if (_instance == null)
                        {
                            _instance = new GameObject().AddComponent<ObjectPoolSceneManager>();
                            _instance.name = Consts.POOLS_ROOT;
#if UNITY_EDITOR
                            if (Application.isPlaying == false)
                            {
                                EditorUtility.SetDirty(_instance);
                            }
#endif
                        }
                    }
                    return _instance;
                }
            }
        }
        private void Awake()
        {
            if (_instance == null)
            {
                if(_instance)
                _instance = this;
                return;
            }
            if (_instance != this)
            {
                Debug.LogWarning("Multiple instances!");
            }
        }
        private const int ARRAYS_DEFAULT_SIZE = 32;

        private static int _idIncrement = 1;
        private static readonly Queue<int> _recycledIds = new Queue<int>();

        private static ObjectPool[] _pools = new ObjectPool[ARRAYS_DEFAULT_SIZE];
        private static int[] _mapping = new int[ARRAYS_DEFAULT_SIZE];
        private static int _poolsCount = 0;

        internal static void RegisterPool(ObjectPool pool)
        {
            pool.transform.SetParent(Instance.transform);

            int newId;
            if(_recycledIds.Count > 0)
            {
                newId = _recycledIds.Dequeue();
            }
            else
            {
                newId = _idIncrement++;
                if(newId >= _mapping.Length)
                {
                    Array.Resize(ref _mapping, _mapping.Length << 1);
                }
            }
            if(_poolsCount >= _pools.Length)
            {
                Array.Resize(ref _pools, _pools.Length << 1);
            }
            _mapping[newId] = _poolsCount;
            _pools[_poolsCount++] = pool;
            pool._id = newId;
        }
        internal static void UnregisterPool(ObjectPool pool)
        {
            int id = pool._id;
            if (id == 0) { Throw.InvalidPoolID(); }
            _recycledIds.Enqueue(id);
            int denseID = _mapping[id];
            int lastDenseID = --_poolsCount;

            var lastPool = _pools[lastDenseID];
            _pools[denseID] = lastPool;
            _mapping[lastPool._id] = denseID;
        }

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _poolsCount; i++)
            {
                _pools[i].InternalUpdate(deltaTime);
            }
        }

        private void OnDestroy()
        {
            
        }
    }
}
