using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DCFApixels.ObjectPools
{
    [DisallowMultipleComponent]
    public class ObjectPoolUnit : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField]
        private MonoBehaviour[] _callbackListeners = Array.Empty<MonoBehaviour>();
        private Action _onTaked = delegate { };
        private Action _onReturned = delegate { };

        private ObjectPool _sourcePool;
        internal bool _isInPool;

        #region Properties
        public bool IsInPool
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _isInPool; }
        }
        #endregion

        #region ReturnToPool
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReturnToPool()
        {
            _sourcePool.Return(this);
        }
        #endregion

        #region Add/Remove Listeners
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddListener(IPoolUnitCallbacks callbacks)
        {
            _onTaked += callbacks.OnTaked;
            _onReturned += callbacks.OnReturned;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveListener(IPoolUnitCallbacks callbacks)
        {
            _onTaked -= callbacks.OnTaked;
            _onReturned -= callbacks.OnReturned;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearListeners()
        {
            _onTaked = delegate { };
            _onReturned = delegate { };
        }
        #endregion

        #region ISerializationCallbackReceiver
        void ISerializationCallbackReceiver.OnBeforeSerialize() { }
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            for (int i = 0; i < _callbackListeners.Length; i++)
            {
#if UNITY_EDITOR
                IPoolUnitCallbacks poolUnit = _callbackListeners[i] as IPoolUnitCallbacks;
                if (poolUnit == null)
                {
                    continue;
                }
#else
                IPoolUnitCallbacks poolUnit = (IPoolUnitCallbacks)_callbackListeners[i];
#endif
                AddListener(poolUnit);
            }
        }
        #endregion

        #region Internal
        internal ObjectPool SourcePool
        {
            get { return _sourcePool; }
            set { _sourcePool = value; }
        }
        internal void CallTaked()
        {
            _onTaked.Invoke();
        }
        internal void CallReturned()
        {
            _onReturned.Invoke();
        }
        #endregion

        #region Editor
#if UNITY_EDITOR
        internal void SetRefs_Editor()
        {
            SetRefs();
        }
        protected void SetRefs()
        {
            _callbackListeners = GetComponentsInChildren<MonoBehaviour>(true).Where(o => o is IPoolUnitCallbacks && o.GetComponentInParent<ObjectPoolUnit>() == this).ToArray();
        }
        protected void OnValidate()
        {
            _callbackListeners = _callbackListeners.Where(o => o != null && o is IPoolUnitCallbacks).ToArray();
        }
#endif
        #endregion
    }

    public static class ObjectPoolUnitExctensions
    {
        #region As
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static T As<T>(this ObjectPoolUnit self) where T : ObjectPoolUnit
        {
#if DEBUG
            return (T)self;
#else
            return UnsafeUtility.As<ObjectPoolUnit, T>(ref self);
#endif
        }
        #endregion
    }
}

#region Editor
#if UNITY_EDITOR
namespace DCFApixels.ObjectPools.Editors
{
    using UnityEditor;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(ObjectPoolUnit), true)]
    public class ObjectPoolUnitEditor : CustomEditorExtended<ObjectPoolUnit>
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if(TargetsCount == 1)
            {
                EditorGUILayout.Toggle("Is In Pool", Target.IsInPool);
            }
            else
            {
                using (Disable) { EditorGUILayout.Toggle("Is In Pool", false); }
            }
            if (GUILayout.Button("SetRefs"))
            {
                foreach (var item in Targets)
                {
                    item.SetRefs_Editor();
                }
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(Target);
            }
        }
    }
}
#endif
#endregion