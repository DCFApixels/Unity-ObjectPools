using UnityEngine;
using System.Runtime.CompilerServices;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DCFApixels.ObjectPools
{
    [CreateAssetMenu(fileName = nameof(ObjectPoolRef), menuName = Consts.PROJECT_NAME + "/" + nameof(ObjectPoolRef), order = 1)]
    public class ObjectPoolRef : ScriptableObject, IObjectPool
    {
        [SerializeField]
        private ObjectPool _prefab;
        private ObjectPool _instance;

        private OnAnyTakedEvent _onAnyTaked;
        private OnAnyReturnedEvent _onAnyReturned;

        #region Properties
        public ObjectPoolUnit Prefab
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _prefab.Prefab; }
        }
        public ObjectPool Instance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_instance == null) { FindOrCreateInstance(); }
                return _instance;
            }
        }
        public OnAnyTakedEvent OnAnyTaked
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _onAnyTaked; }
        }
        public OnAnyReturnedEvent OnAnyReturned
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _onAnyReturned; }
        }
        public bool IsCanTake
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Instance.IsCanTake; }
        }
        #endregion

        #region UnityEvents
        private void OnEnable()
        {
            _onAnyTaked = new OnAnyTakedEvent(this);
            _onAnyReturned = new OnAnyReturnedEvent(this);
        }
        #endregion

        #region IObjectPool
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T TakeAggressive<T>() where T : Component { return Instance.TakeAggressive<T>(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ObjectPoolUnit TakeAggressive() { return Instance.TakeAggressive(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryTake<T>(out T instance) where T : Component { return Instance.TryTake(out instance); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryTake(out ObjectPoolUnit instance) { return Instance.TryTake(out instance); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Take<T>() where T : Component { return Instance.Take<T>(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ObjectPoolUnit Take() { return Instance.Take(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(ObjectPoolUnit unit) { Instance.Return(unit); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Destroy() { Instance.Destroy(); }
        #endregion

        #region PoolEvents
        public readonly struct OnAnyTakedEvent
        {
            private readonly ObjectPoolRef _parent;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public OnAnyTakedEvent(ObjectPoolRef parent) { _parent = parent; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(ObjectPoolEventHandler listener) { _parent.Instance.OnAnyTaked += listener; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Remove(ObjectPoolEventHandler listener) { _parent.Instance.OnAnyTaked -= listener; }
        }
        public readonly struct OnAnyReturnedEvent
        {
            private readonly ObjectPoolRef _parent;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public OnAnyReturnedEvent(ObjectPoolRef parent) { _parent = parent; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(ObjectPoolEventHandler listener) { _parent.Instance.OnAnyTaked += listener; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Remove(ObjectPoolEventHandler listener) { _parent.Instance.OnAnyTaked -= listener; }
        }
        #endregion

        #region Other
        public void InitPool()
        {
            _ = Instance;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void FindOrCreateInstance()
        {
#if UNITY_EDITOR
            var pools = FindObjectsOfType<ObjectPool>(true);
            if (pools.Length > 0)
            {
                _instance = pools[0];
                return;
            }

            if (Application.isPlaying)
            {
                _instance = Instantiate(_prefab);
            }
            else
            {
                _instance = (PrefabUtility.InstantiatePrefab(_prefab.gameObject) as GameObject).GetComponent<ObjectPool>();
            }
#else
            _instance = Instantiate(_prefab);
#endif
        }
        #endregion

        #region Editor
#if UNITY_EDITOR
        internal ObjectPool Instance_Editor
        {
            get { return _instance; }
        }
        internal void SetInstance_Editor(ObjectPool instance)
        {
            _instance = instance;
        }
        internal void SetPrefab_Editor(ObjectPool prefab)
        {
            _prefab = prefab;
        }
#endif
        #endregion
    }
}