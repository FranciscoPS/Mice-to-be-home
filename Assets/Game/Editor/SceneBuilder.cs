#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiceToBeHome.EditorTools
{
    public static class SceneBuilder
    {
        private const string PrefabFolder = "Assets/Game/Prefabs";

        [MenuItem("Tools/Mice to be Home/Build Scene")]
        public static void BuildScene()
        {
            var existing = Object.FindFirstObjectByType<GameInstaller>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Mice to be Home",
                    "There is already a 'Game' object in this scene. Delete it first if you want to rebuild.", "OK");
                Selection.activeObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                return;
            }

            EnsurePrefabFolder();

            var root = new GameObject("Game");
            Undo.RegisterCreatedObjectUndo(root, "Build Mice Scene");

            var installer = root.AddComponent<GameInstaller>();
            GameConfig config = installer.EditorEnsureConfig();
            BalanceSettings balance = config.Balance;
            SpriteLibrary sprites = config.Sprites;

            int cols = Mathf.Max(1, balance.gridColumns);
            int rows = Mathf.Max(1, balance.gridRows);
            float cell = Mathf.Max(0.1f, balance.cellSize);
            Vector3 center = Vector3.zero;

            GameObject furniturePrefab = GetOrCreateFurniturePrefab(sprites, cell);
            GameObject cellPrefab = GetOrCreateCellPrefab(sprites, cell);
            GameObject playerPrefab = GetOrCreateActorPrefab("Player", sprites.mouse, sprites.mouseTint, cell, cell * 0.5f, false);
            GameObject catPrefab = GetOrCreateActorPrefab("Cat", sprites.cat, sprites.catTint, cell, cell * 0.62f, true);

            var gameManager = NewChild(root.transform, "GameManager").AddComponent<GameManager>();
            var audio = NewChild(root.transform, "Audio").AddComponent<AudioManager>();
            var placement = NewChild(root.transform, "Placement").AddComponent<PlacementController>();
            var lives = NewChild(root.transform, "Lives").AddComponent<LivesSystem>();
            var flow = NewChild(root.transform, "GameFlow").AddComponent<GameFlowController>();
            var ui = NewChild(root.transform, "UI").AddComponent<UIManager>();
            var trapParent = NewChild(root.transform, "Traps").transform;

            var gridGO = NewChild(root.transform, "Grid");
            var grid = gridGO.AddComponent<GridSystem>();

            Vector2 floorSize = new Vector2(cols * cell, rows * cell) + new Vector2(1.2f, 1.2f);
            SpriteRenderer floor = AddGround(gridGO.transform, "Floor", floorSize, sprites.floor, sprites.floorTint, -10000);
            floor.transform.position = center;

            var cellsParent = NewChild(gridGO.transform, "Cells").transform;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var tile = (GameObject)PrefabUtility.InstantiatePrefab(cellPrefab, cellsParent);
                    tile.name = $"Cell_{c}_{r}";
                    tile.transform.position = GridSystem.CellToWorld(center, cols, rows, cell, c, r) + Vector3.up * 0.01f;
                }
            }

            var furnitureParent = NewChild(gridGO.transform, "Furniture").transform;
            for (int i = 0; i < rows; i++)
            {
                string line = balance.GetFurnitureRow(i);
                int gridRow = rows - 1 - i;
                for (int c = 0; c < cols && c < line.Length; c++)
                {
                    if (line[c] == 'X' || line[c] == 'x')
                    {
                        var piece = (GameObject)PrefabUtility.InstantiatePrefab(furniturePrefab, furnitureParent);
                        piece.transform.position = GridSystem.CellToWorld(center, cols, rows, cell, c, gridRow);
                    }
                }
            }

            var playerGO = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, root.transform);
            playerGO.transform.position = center;
            var player = playerGO.GetComponent<MousePlayerController>();

            var catGO = (GameObject)PrefabUtility.InstantiatePrefab(catPrefab, root.transform);
            catGO.transform.position = center + new Vector3(balance.catStartDistance, 0f, 0f);
            var cat = catGO.GetComponent<CatController>();

            Camera cam = SetupCamera(center, cols, rows, cell);
            var cameraController = cam.gameObject.AddComponent<CameraController>();

            installer.grid = grid;
            installer.player = player;
            installer.cat = cat;
            installer.cameraController = cameraController;
            installer.mainCamera = cam;
            installer.placement = placement;
            installer.lives = lives;
            installer.flow = flow;
            installer.audioManager = audio;
            installer.ui = ui;
            installer.trapParent = trapParent;

            EditorUtility.SetDirty(installer);
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeObject = root;
            EditorGUIUtility.PingObject(root);
            Debug.Log("[Mice to be Home] Scene built. Reusable prefabs live in Assets/Game/Prefabs. Press Play.");
        }

        [MenuItem("Tools/Mice to be Home/Add Furniture Piece")]
        public static void AddFurniturePiece()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/Furniture.prefab");
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Mice to be Home", "Build the scene first (Tools > Mice to be Home > Build Scene).", "OK");
                return;
            }

            Transform parent = null;
            var installer = Object.FindFirstObjectByType<GameInstaller>();
            if (installer != null)
            {
                var grid = installer.GetComponentInChildren<GridSystem>();
                if (grid != null)
                {
                    Transform furniture = grid.transform.Find("Furniture");
                    parent = furniture != null ? furniture : grid.transform;
                }
            }

            var piece = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            piece.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(piece, "Add Furniture Piece");
            Selection.activeObject = piece;
            EditorGUIUtility.PingObject(piece);
        }

        private static void EnsurePrefabFolder()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/Game", "Prefabs");
            }
        }

        private static GameObject GetOrCreateFurniturePrefab(SpriteLibrary sprites, float cell)
        {
            string path = PrefabFolder + "/Furniture.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing;
            }

            var template = new GameObject("Furniture");
            template.AddComponent<FurniturePiece>();
            var box = template.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.5f, 0f);
            box.size = new Vector3(cell * 0.6f, 1f, cell * 0.6f);
            AddBillboard(template.transform, "Visual", sprites.furniture, sprites.furnitureTint, false, cell * 0.72f);
            return SavePrefab(template, path);
        }

        private static GameObject GetOrCreateCellPrefab(SpriteLibrary sprites, float cell)
        {
            string path = PrefabFolder + "/Cell.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing;
            }

            var template = new GameObject("Cell");
            AddGroundRenderer(template, new Vector2(cell * 0.94f, cell * 0.94f), sprites.cell, new Color(1f, 1f, 1f, 0.14f), -9000);
            return SavePrefab(template, path);
        }

        private static GameObject GetOrCreateActorPrefab(string name, Sprite artwork, Color tint, float cell, float visualSize, bool isCat)
        {
            string path = PrefabFolder + "/" + name + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing;
            }

            var template = new GameObject(name);
            var body = template.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            var capsule = template.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.radius = cell * 0.12f;
            capsule.height = 1f;
            capsule.center = new Vector3(0f, 0.5f, 0f);

            AddBillboard(template.transform, "Visual", artwork, tint, true, visualSize);

            if (isCat)
            {
                template.AddComponent<CatController>();
            }
            else
            {
                template.AddComponent<MousePlayerController>();
            }

            return SavePrefab(template, path);
        }

        private static GameObject SavePrefab(GameObject template, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(template, path);
            Object.DestroyImmediate(template);
            return prefab;
        }

        private static GameObject NewChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Sprite SquareSprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        private static Sprite CircleSprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        private static SpriteRenderer AddGround(Transform parent, string name, Vector2 size, Sprite artwork, Color tint, int sortingOrder)
        {
            var go = NewChild(parent, name);
            return AddGroundRenderer(go, size, artwork, tint, sortingOrder);
        }

        private static SpriteRenderer AddGroundRenderer(GameObject go, Vector2 size, Sprite artwork, Color tint, int sortingOrder)
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = artwork != null ? artwork : SquareSprite;
            renderer.color = artwork != null ? Color.white : tint;
            renderer.sortingOrder = sortingOrder;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            return renderer;
        }

        private static void AddBillboard(Transform parent, string name, Sprite artwork, Color tint, bool circle, float worldSize)
        {
            var go = NewChild(parent, name);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = artwork != null ? artwork : (circle ? CircleSprite : SquareSprite);
            renderer.color = artwork != null ? Color.white : tint;
            go.AddComponent<Billboard>();
            go.transform.localScale = Vector3.one * worldSize;
            go.transform.localPosition = new Vector3(0f, worldSize * 0.5f, 0f);
            go.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
        }

        private static Camera SetupCamera(Vector3 center, int cols, int rows, float cell)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }
            if (cam.GetComponent<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.09f, 0.13f);

            float extent = Mathf.Max(cols, rows) * cell;
            float distance = extent * 1.15f + 3f;
            const float pitch = 55f;
            float radians = pitch * Mathf.Deg2Rad;
            cam.transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
            cam.transform.position = center + new Vector3(0f, Mathf.Sin(radians) * distance, -Mathf.Cos(radians) * distance);
            return cam;
        }
    }
}
#endif
