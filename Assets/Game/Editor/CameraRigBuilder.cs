#if UNITY_EDITOR
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiceToBeHome.EditorTools
{
    /// <summary>
    /// Generates the whole Cinemachine camera rig (CinemachineBrain on the Main Camera + the
    /// CM_Frame / CM_Follow / CM_LoseZoom virtual cameras + the CameraController director that
    /// switches between them). Reuse-if-exists, so it is safe to run repeatedly. The rig lives
    /// outside the "Game" object so Build Scene never destroys it.
    /// </summary>
    public static class CameraRigBuilder
    {
        private const float Pitch = 55f;
        private const string RigName = "CameraRig";
        private const string ConfigPath = "Assets/Game/Config/GameConfig.asset";

        [MenuItem("Tools/Mice to be Home/Build Camera Rig")]
        public static void BuildCameraRigMenu()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                EditorUtility.DisplayDialog("Mice to be Home",
                    "No GameConfig found. Run Tools > Mice to be Home > Build Scene once (it creates the config), then run this.",
                    "OK");
                return;
            }

            CameraController director = EnsureRig(config);
            Selection.activeObject = director.gameObject;
            EditorGUIUtility.PingObject(director.gameObject);
            EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
            Debug.Log("[Mice to be Home] Camera rig ready: CinemachineBrain + CM_Frame / CM_Follow / CM_LoseZoom.");
        }

        public static CameraController EnsureRig(GameConfig config)
        {
            BalanceSettings balance = config.Balance;
            int cols = Mathf.Max(1, balance.gridColumns);
            int rows = Mathf.Max(1, balance.gridRows);
            float cell = Mathf.Max(0.1f, balance.cellSize);
            Vector3 center = Vector3.zero;

            float extent = Mathf.Max(cols, rows) * cell;
            float frameDistance = extent * 1.15f + 3f;
            float followDistance = extent * 0.55f + 3f;

            EnsureBrainCamera();

            GameObject rig = GameObject.Find(RigName);
            if (rig == null)
            {
                rig = new GameObject(RigName);
                Undo.RegisterCreatedObjectUndo(rig, "Build Camera Rig");
            }

            CinemachineCamera frame = EnsureVCam(rig.transform, "CM_Frame", center + OffsetFor(frameDistance), false, 0f, 45f);
            CinemachineCamera follow = EnsureVCam(rig.transform, "CM_Follow", center + OffsetFor(followDistance), true, followDistance, 45f);
            CinemachineCamera lose = EnsureVCam(rig.transform, "CM_LoseZoom", center + OffsetFor(followDistance * 0.55f), true, followDistance * 0.55f, 32f);

            CameraController director = rig.GetComponent<CameraController>();
            if (director == null)
            {
                director = rig.AddComponent<CameraController>();
            }
            director.EditorBind(frame, follow, lose);

            // Start on the overview shot (edit-mode preview shows the wide framing).
            frame.Priority = 100;
            follow.Priority = 0;
            lose.Priority = 0;

            RemoveStrayDirectors(rig);

            // Re-wire the installer so running just this tool (no full rebuild) is enough.
            GameInstaller installer = Object.FindFirstObjectByType<GameInstaller>(FindObjectsInactive.Include);
            if (installer != null)
            {
                installer.cameraController = director;
                EditorUtility.SetDirty(installer);
            }

            EditorUtility.SetDirty(director);
            EditorSceneManager.MarkSceneDirty(rig.scene);
            return director;
        }

        private static Camera EnsureBrainCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                Undo.RegisterCreatedObjectUndo(go, "Build Camera Rig");
            }

            if (cam.GetComponent<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = CameraController.BackgroundColor;

            CinemachineBrain brain = cam.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = cam.gameObject.AddComponent<CinemachineBrain>();
            }
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1.2f);
            return cam;
        }

        private static CinemachineCamera EnsureVCam(Transform parent, string name, Vector3 position, bool follows, float followDistance, float fov)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(go, "Build Camera Rig");
            }

            go.transform.rotation = Quaternion.Euler(Pitch, 0f, 0f);
            go.transform.position = position;

            CinemachineCamera vcam = go.GetComponent<CinemachineCamera>();
            if (vcam == null)
            {
                vcam = go.AddComponent<CinemachineCamera>();
            }
            vcam.Lens.FieldOfView = fov;

            CinemachineFollow body = go.GetComponent<CinemachineFollow>();
            if (follows)
            {
                if (body == null)
                {
                    body = go.AddComponent<CinemachineFollow>();
                }
                body.FollowOffset = OffsetFor(followDistance);
            }
            else if (body != null)
            {
                Object.DestroyImmediate(body);
            }

            // Camera shake via Perlin noise (reliable). Muted (AmplitudeGain 0) until a hit pulses it.
            CinemachineImpulseListener staleListener = go.GetComponent<CinemachineImpulseListener>();
            if (staleListener != null)
            {
                Object.DestroyImmediate(staleListener);
            }
            CinemachineBasicMultiChannelPerlin perlin = go.GetComponent<CinemachineBasicMultiChannelPerlin>();
            if (perlin == null)
            {
                perlin = go.AddComponent<CinemachineBasicMultiChannelPerlin>();
            }
            if (perlin.NoiseProfile == null)
            {
                perlin.NoiseProfile = AssetDatabase.LoadAssetAtPath<NoiseSettings>("Packages/com.unity.cinemachine/Presets/Noise/6D Shake.asset");
            }
            perlin.AmplitudeGain = 0f;
            perlin.FrequencyGain = 1f;

            return vcam;
        }

        private static void RemoveStrayDirectors(GameObject rig)
        {
            foreach (CameraController stray in Object.FindObjectsByType<CameraController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (stray != null && stray.gameObject != rig)
                {
                    Object.DestroyImmediate(stray);
                }
            }
        }

        private static Vector3 OffsetFor(float distance)
        {
            float radians = Pitch * Mathf.Deg2Rad;
            return new Vector3(0f, Mathf.Sin(radians) * distance, -Mathf.Cos(radians) * distance);
        }
    }
}
#endif
