using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Runtime.CompilerServices;
using DCFApixels.ObjectPools.Internal;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DCFApixels.ObjectPools
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class ObjectPool : MonoBehaviour, IObjectPool
    {
        [SerializeField]
        private int _allUnitsCount;
        [SerializeField]
        private List<ObjectPoolUnit> _storedUnits;

        [Header("Main Settings")]
        [SerializeField]
        private ObjectPoolRef _ref;
        [SerializeField]
        private ObjectPoolUnit _prefab;
        [SerializeField]
        private int _maxInstances = -1;

        [Header("Auto Cleaning")]
        [SerializeField]
        private bool _isAutoCleaning;
        [SerializeField]
        private float _cleanTime = 6f;
        [SerializeField, Range(0f, 1f)]
        private float _cleanTimerDamper = 0.05f;
        [SerializeField]
        private int _minAllInstances = 8;
        [SerializeField]
        private int _minDisabledInstances = 4;

        private float _cleanTimer = 0f;
        internal int _id = 0;

        private bool _isInit = false;

        #region Properties
        public ObjectPoolRef Ref
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _ref; }
        }
        public ObjectPoolUnit Prefab
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _prefab; }
        }
        public int UsedCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _allUnitsCount - _storedUnits.Count; }
        }
        public bool IsAutoCleaning
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _isAutoCleaning; }
        }
        public bool IsInfinite
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _maxInstances < 0; }
        }
        public bool IsCanTake
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return IsInfinite || _allUnitsCount < _maxInstances || _storedUnits.Count > 0; }
        }
        #endregion

        #region UnityEvents
        private void Awake()
        {
            if (_isInit == false) { Init(); }
        }
        private void Init()
        {
            if (_isInit) { return; }
            _isInit = true;
            if (_storedUnits == null)
            {
                _storedUnits = new List<ObjectPoolUnit>();
                return;
            }

            foreach (var unit in _storedUnits)
            {
                unit.SourcePool = this;
                unit._isInPool = true;
            }
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (Application.isPlaying) { PlayingStart(); }
#else
            PlayingStart();
#endif
        }
        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (Application.isPlaying) { PlayingOnDestroy(); }
#else
            PlayingOnDestroy();
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PlayingStart()
        {
            ObjectPoolSceneManager.RegisterPool(this);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PlayingOnDestroy()
        {
            ObjectPoolSceneManager.UnregisterPool(this);
        }
        #endregion

        #region IObjectPool

        #region Take
        public T TakeAggressive<T>() where T : Component
        {
            return TakeInternal<T>();
        }
        public ObjectPoolUnit TakeAggressive()
        {
            return TakeInternal();
        }

        public bool TryTake<T>(out T instance) where T : Component
        {
            if (IsCanTake)
            {
                instance = TakeInternal<T>();
                return true;
            }
            else
            {
                instance = null;
                return false;
            }
        }
        public bool TryTake(out ObjectPoolUnit instance)
        {
            if (IsCanTake)
            {
                instance = TakeInternal();
                return true;
            }
            else
            {
                instance = null;
                return false;
            }
        }

        public T Take<T>() where T : Component
        {
            if (IsCanTake == false) { Throw.IsCanTakeIsFalse(); }
            return TakeInternal<T>();
        }
        public ObjectPoolUnit Take()
        {
            if (IsCanTake == false) { Throw.IsCanTakeIsFalse(); }
            return TakeInternal();
        }
        #endregion

        #region Return
        public void Return(ObjectPoolUnit unit)
        {
#if DEBUG
            if (unit.SourcePool != this) { Throw.ReturnToAnotherPool(); }
#endif
            if (unit.IsInPool == false)
            {
                unit._isInPool = true;
                _storedUnits.Add(unit);
                unit.gameObject.SetActive(false);
                unit.transform.parent = transform;

                unit.CallReturned();
                OnAnyReturned(this, unit);
            }
        }
        #endregion

        public void Destroy()
        {
            Destroy(gameObject);
        }
        #endregion

        #region InternalUpdate/AutoCleaning
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InternalUpdate(float deltaTime)
        {
            if ((_isAutoCleaning == false) || 
                (_storedUnits.Count <= _minDisabledInstances) || 
                (_allUnitsCount <= _minAllInstances)) 
            { 
                return; 
            }

            _cleanTimer += deltaTime;
            if (_cleanTimer > _cleanTime)
            {
                DestroyInstance();
                _cleanTimer = 0f;
            }
        }
        #endregion

        #region TakeInternal/NewInstance/DestroyInstance
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T TakeInternal<T>() where T : Component
        {
            ObjectPoolUnit unit = TakeInternal();
            if (typeof(T).IsSubclassOf(typeof(ObjectPoolUnit)))
            {
                return unit as T;
            }
            else
            {
                return unit.GetComponent<T>();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ObjectPoolUnit TakeInternal()
        {
            if (_isInit == false) { Init(); }

            if (_storedUnits.Count <= 0)
            {
                AddNewInstance();
                _cleanTimer = -_cleanTime;
            }
            else
            {
                _cleanTimer *= 1f - _cleanTimerDamper;
            }

            int index = _storedUnits.Count - 1;
            var unit = _storedUnits[index];
            _storedUnits.RemoveAt(index);

            unit._isInPool = false;
            unit.CallTaked();
            OnAnyTaked(this, unit);
            unit._isInPool = false;
            return unit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddNewInstance()
        {
            ObjectPoolUnit unit;
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                unit = Instantiate(_prefab);
            }
            else
            {
                unit = (PrefabUtility.InstantiatePrefab(_prefab.gameObject) as GameObject).GetComponent<ObjectPoolUnit>();
            }
#else
            unit = Instantiate(_prefab);
#endif
            unit.gameObject.SetActive(false);
            unit.transform.parent = transform;
            unit.name = GetUnitName(_allUnitsCount);
            unit.SourcePool = this;
            unit._isInPool = true;

            _storedUnits.Add(unit);
            _allUnitsCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DestroyInstance()
        {
            _allUnitsCount--;

            int index = _storedUnits.Count - 1;
            var unit = _storedUnits[index];
            _storedUnits.RemoveAt(index);

            Destroy(unit.gameObject);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetUnitName(int index)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                return _prefab.name;
            }
            else
            {
                return _prefab.name + "_" + index;
            }
#else
            return _prefab.name;
#endif
        }
        #endregion

        #region Editor
#if UNITY_EDITOR
        internal float GetCleanTimer_Editor()
        {
            return _cleanTimer;
        }
        private void OnTransformChildrenChanged()
        {
            Validate(GetComponentsInChildren<ObjectPoolUnit>(true).Concat(FindObjectsOfType<ObjectPoolUnit>(true)));
        }
        private void OnValidate()
        {
            Validate(_storedUnits);
        }
        private void Validate(IEnumerable<ObjectPoolUnit> list)
        {
            if (Application.isPlaying) { return; }

            _allUnitsCount = _storedUnits.Count;
            var validate = list.Where(o =>
                       o != null &&
                       o.transform.parent == transform &&
                       PrefabUtility.GetCorrespondingObjectFromOriginalSource(o) == _prefab &&
                       o != _prefab).Distinct().ToArray();

            _storedUnits.Clear();
            _storedUnits.AddRange(validate);

            int i = 0;
            foreach (var item in validate)
            {
                item.name = GetUnitName(i++);
                item.gameObject.SetActive(false);
            }
        }

        private void ReValidate()
        {
            if (_prefab == null && transform.childCount > 0)
            {
                ObjectPoolUnit[] childunits = GetComponentsInChildren<ObjectPoolUnit>(true);
                ObjectPoolUnit childunit = null;
                foreach (var unit in childunits)
                {
                    if (unit.transform.parent == transform)
                    {
                        childunit = unit;
                        break;
                    }
                }
                if (childunit != null)
                {
                    _prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(childunit);
                }
            }

            if (GetComponentsInChildren<ObjectPoolUnit>(true).Length > 0)
            {
                Validate(GetComponentsInChildren<ObjectPoolUnit>(true).Concat(FindObjectsOfType<ObjectPoolUnit>(true)));
            }
            else
            {
                Validate(_storedUnits);
            }
        }

        internal void ReValidate_Editor()
        {
            ReValidate();
        }

        internal void Revert_Editor()
        {
            foreach (var unit in _storedUnits)
            {
                bool isActive = unit.gameObject.activeSelf;
                PrefabUtility.RevertPrefabInstance(unit.gameObject, InteractionMode.AutomatedAction);
                unit.gameObject.SetActive(isActive);
            }
        }

        internal void SetPrefab_Editor(ObjectPoolUnit prefab)
        {
            _prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefab);
        }
        internal void SetRef_Editor(ObjectPoolRef @ref)
        {
            _ref = @ref;
        }
#endif
        #endregion

        public event ObjectPoolEventHandler OnAnyTaked = delegate { };
        public event ObjectPoolEventHandler OnAnyReturned = delegate { };
    }

    public delegate void ObjectPoolEventHandler(ObjectPool pool, ObjectPoolUnit unit);
}

#region Editor
#if UNITY_EDITOR
namespace DCFApixels.ObjectPools.Editors
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ObjectPool))]
    public class ObjectPoolEditor : CustomEditorExtended<ObjectPool>
    {
        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            var autoCleaningProp = serializedObject.FindProperty("_isAutoCleaning");
            var autoCleaningValue = autoCleaningProp.boolValue;
            int autoCleaningBlock = 0;

            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty iterator = serializedObject.GetIterator();
            iterator.NextVisible(true);
            using (Disable)
            {
                EditorGUILayout.PropertyField(iterator, true);
            }
            for (int i = 0; i < 2; i++)
            {
                iterator.NextVisible(false);
            }
            while (iterator.NextVisible(false))
            {
                using (IsDisable(autoCleaningBlock > 0))
                {
                    if (autoCleaningBlock > 0)
                    {
                        autoCleaningBlock--;
                    }
                    if (iterator.propertyPath == "_isAutoCleaning" && !autoCleaningValue)
                    {
                        autoCleaningBlock = 4;
                    }
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }

            var allUnitsCountProp = serializedObject.FindProperty("_allUnitsCount");
            var disabledUnitsProp = serializedObject.FindProperty("_storedUnits");
            var maxInstancesProp = serializedObject.FindProperty("_maxInstances");

            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Instances: " + (allUnitsCountProp.hasMultipleDifferentValues ? "--" : allUnitsCountProp.intValue.ToString()), EditorStyles.miniLabel);
            if (allUnitsCountProp.hasMultipleDifferentValues || disabledUnitsProp.hasMultipleDifferentValues)
            {
                GUILayout.Label("Used: --");
            }
            else
            {
                GUILayout.Label("Used: " + (allUnitsCountProp.intValue - disabledUnitsProp.arraySize), EditorStyles.miniLabel);
            }

            GUILayout.Label("Max: " + (maxInstancesProp.hasMultipleDifferentValues ? "--" : maxInstancesProp.intValue < 0 ? "Infinity" : maxInstancesProp.intValue.ToString()), EditorStyles.miniLabel);

            DrawAutoCleaningInfo();

            GUILayout.EndVertical();

            if (GUILayout.Button("Validate"))
            {
                Target.ReValidate_Editor();
                EditorUtility.SetDirty(Target);
                serializedObject.ApplyModifiedProperties();
            }
            if (GUILayout.Button("Revert Prefabs"))
            {
                Target.Revert_Editor();
                EditorUtility.SetDirty(Target);
                serializedObject.ApplyModifiedProperties();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAutoCleaningInfo()
        {
            var autoCleaningProp = serializedObject.FindProperty("_isAutoCleaning");

            if (autoCleaningProp.boolValue)
            {
                GUILayout.Label("AutoClean: On", EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Label("AutoClean: Off", EditorStyles.miniLabel);
            }

            if (autoCleaningProp.boolValue && !IsMultiple)
            {
                GUILayout.Label($"AutoClean Timer: {Target.GetCleanTimer_Editor()}", EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Label("AutoClean Timer: --", EditorStyles.miniLabel);
            }
        }
    }
}
#endif
#endregion