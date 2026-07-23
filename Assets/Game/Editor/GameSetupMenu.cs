#if UNITY_EDITOR
using MiceToBeHome;
using UnityEditor;
using UnityEngine;

namespace MiceToBeHome.EditorTools
{
    public static class GameSetupMenu
    {
        [MenuItem("Tools/Mice to be Home/Crear juego en la escena")]
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
            Undo.RegisterCreatedObjectUndo(go, "Crear MiceGame");
            Selection.activeObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }
}
#endif
