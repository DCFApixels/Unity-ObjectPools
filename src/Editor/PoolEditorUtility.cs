using System;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
namespace DCFApixels.ObjectPools.Editors
{
    using System.IO;
    using UnityEditor;
    public static class PoolEditorUtility
    {
        private const string DEFAULT_FOLDER_NAME = "GeneratedPools";

        [MenuItem("GameObject/Pool/Generate Pool")]
        public static void GenerateAllPools(MenuCommand menuCommand)
        {
            if (Selection.objects.Length > 1)
            {
                if (menuCommand.context != Selection.objects[0])
                {
                    return;
                }
            }


            foreach (var item in Selection.gameObjects)
            {
                GenerateOnePool(item.transform);
            }
        }
        [MenuItem("GameObject/Pool/Generate Pool", true, 10)]
        static bool ValidateGenerateAllPools(MenuCommand menuCommand)
        {
            return Selection.activeGameObject != null; //&& EditorUtility.IsPersistent(Selection.activeGameObject);
        }

        private static void GenerateOnePool(Transform selectedTransform)
        {
            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(selectedTransform);
            Transform rootTransform = PrefabUtility.GetCorrespondingObjectFromOriginalSource(selectedTransform);

            if (rootTransform == null)
            {
                rootTransform = ConvertObjectToPrefab(selectedTransform, out path);
            }
            if (rootTransform == null)
            {
                Debug.LogWarning("Пул не сгенерирован");
                return;
            }

            if (rootTransform.TryGetComponent(out ObjectPoolUnit unitPrefab))
            {
                GenerateOnePool(unitPrefab, path);
            }
            else
            {
                unitPrefab = rootTransform.gameObject.AddComponent<ObjectPoolUnit>();
                GenerateOnePool(unitPrefab, path);
            }
        }

        private static Transform ConvertObjectToPrefab(Transform root, out string path)
        {
            if (!Directory.Exists("Assets/" + DEFAULT_FOLDER_NAME))
                AssetDatabase.CreateFolder("Assets", DEFAULT_FOLDER_NAME);


            string localPath = "Assets/" + DEFAULT_FOLDER_NAME + "/" + root.name;

            while (true)
            {
                if (Directory.Exists(localPath) == false)
                {
                    AssetDatabase.CreateFolder("Assets/" + DEFAULT_FOLDER_NAME, localPath.Split("/").Last());
                    break;
                }
                localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);
            }

            localPath += "/" + root.name + ".prefab";

            bool prefabSuccess;
            GameObject result = PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject, localPath, InteractionMode.UserAction, out prefabSuccess);
            if (prefabSuccess)
            {
                result.transform.position = Vector3.zero;
                result.transform.rotation = Quaternion.identity;
                path = localPath;
                return result.transform;
            }
            else
            {
                path = "";
                return null;
            }
        }

        private static void GenerateOnePool(ObjectPoolUnit unitPrefab, string path)
        {
            //unitPrefab.SetRefs_Editor();

            int separatorPosition = Mathf.Max(path.LastIndexOf("/"), path.LastIndexOf("\\"));
            if (separatorPosition <= -1)
                throw new Exception(path);

            string folderPath = path = path.Substring(0, separatorPosition);

            separatorPosition = Mathf.Max(path.IndexOf("Assets"), path.LastIndexOf("Assets"));
            if (separatorPosition <= -1)
                throw new Exception(path);
            separatorPosition += "Assets".Length;
            string folderPathInAssets = folderPath.Substring(separatorPosition);

            #region Make pool instance
            ObjectPool newPool = new GameObject().AddComponent<ObjectPool>();
            newPool.name = unitPrefab.name + "Pool";
            newPool.SetPrefab_Editor(unitPrefab);

            for (int i = 0; i < 8; i++)
            {
                GameObject unitInstance = (GameObject)PrefabUtility.InstantiatePrefab(unitPrefab.gameObject);
                unitInstance.transform.SetParent(newPool.transform);
            }

            newPool.ReValidate_Editor();
            #endregion

            #region Make pool instance prefab
            string newPoolPrefabPath = path + "/" + newPool.name + ".prefab";
            newPoolPrefabPath = AssetDatabase.GenerateUniqueAssetPath(newPoolPrefabPath);
            var newPoolPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(newPool.gameObject, newPoolPrefabPath, InteractionMode.AutomatedAction).GetComponent<ObjectPool>();
            #endregion

            #region Make ref
            ObjectPoolRef @ref = ScriptableObject.CreateInstance<ObjectPoolRef>();
            if (AssetDatabase.IsValidFolder(folderPath) == false)
            {
                Directory.CreateDirectory(Application.dataPath + folderPathInAssets);
                AssetDatabase.Refresh();
            }
            AssetDatabase.CreateAsset(@ref, folderPath + "/" + newPoolPrefab.name + "Ref.asset");
            AssetDatabase.Refresh();

            newPoolPrefab.SetRef_Editor(@ref);
            @ref.SetPrefab_Editor(newPoolPrefab);
            #endregion

            #region Clear
            GameObject.DestroyImmediate(newPool.gameObject);
            #endregion

            EditorUtility.SetDirty(unitPrefab);
            EditorUtility.SetDirty(newPoolPrefab);
            EditorUtility.SetDirty(@ref);


        }
    }
}
#endif
