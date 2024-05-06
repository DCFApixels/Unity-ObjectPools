#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DCFApixels.ObjectPools
{
    [FilePath(Consts.AUTOR + "/ObjectPools/" + nameof(SettingsPrefs) + ".prefs", FilePathAttribute.Location.ProjectFolder)]
    public class SettingsPrefs : ScriptableSingleton<SettingsPrefs>
    {
        [SerializeField]
        private bool _isShowInterfaces = false;
        public bool IsShowInterfaces
        {
            get => _isShowInterfaces;
            set
            {
                _isShowInterfaces = value;
                Save(false);
            }
        }
    }
}
#endif