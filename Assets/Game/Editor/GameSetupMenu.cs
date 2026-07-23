#if UNITY_EDITOR
using MiceToBeHome;
using UnityEditor;
using UnityEngine;

namespace MiceToBeHome.EditorTools
{
    public static class GameSetupMenu
    {
        [MenuItem("Tools/Mice to be Home/Create Game In Scene")]
        public static void CreateGameInScene()
        {
            var existing = Object.FindFirstObjectByType<GameBootstrap>();
            if (existing != null)
            {
                Selection.activeObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                return;
            }

            var go = new GameObject("MiceGame");
            var bootstrap = go.AddComponent<GameBootstrap>();
            bootstrap.EditorEnsureDefaults();
            EditorUtility.SetDirty(bootstrap);
            Undo.RegisterCreatedObjectUndo(go, "Create MiceGame");
            Selection.activeObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }
}
#endif
