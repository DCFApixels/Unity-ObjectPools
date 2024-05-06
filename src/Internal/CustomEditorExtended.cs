#if UNITY_EDITOR
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace DCFApixels.ObjectPools.Editors
{
    public class CustomEditorExtended<T> : Editor where T : UnityObject
    {
        protected T Target => (T)target;
        protected T[] Targets
        {
            get
            {
                UnityObject[] targets = base.targets;
                return UnsafeUtility.As<UnityObject[], T[]>(ref targets);
            }
        }
        protected int TargetsCount => targets.Length;
        protected bool IsMultiple => targets.Length > 1;

        public ColorScore Color(float r, float g, float b, float a = 1f)
        {
            return new ColorScore(new Color(r, g, b, a));
        }
        public ColorScore Color(Color value)
        {
            return new ColorScore(value);
        }
        public EnableScore IsEnable(bool value)
        {
            return new EnableScore(value);
        }
        public EnableScore IsDisable(bool value)
        {
            return new EnableScore(!value);
        }
        public EnableScore Enable
        {
            get { return new EnableScore(true); }
        }
        public EnableScore Disable
        {
            get { return new EnableScore(false); }
        }

        public readonly ref struct ColorScore
        {
            private readonly Color _value;
            public ColorScore(Color value)
            {
                _value = GUI.color;
                GUI.color = value;
            }

            public void Dispose()
            {
                GUI.color = _value;
            }
        }
        public readonly ref struct EnableScore
        {
            private readonly bool _value;
            public EnableScore(bool value)
            {
                _value = GUI.enabled;
                GUI.enabled = value;
            }

            public void Dispose()
            {
                GUI.enabled = _value;
            }
        }
    }
}
#endif