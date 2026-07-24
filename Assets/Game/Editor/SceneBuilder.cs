#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiceToBeHome.EditorTools
{
    public static class SceneBuilder
    {
        [MenuItem("Tools/Mice to be Home/Build Scene")]
        public static void BuildScene()
        {
            var existing = Object.FindFirstObjectByType<GameInstaller>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Mice to be Home",
                    "There is already a 'Game' object in this scene. Delete it first if you want to rebuild from scratch.", "OK");
                Selection.activeObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                return;
            }

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
                    SpriteRenderer tile = AddGround(cellsParent, $"Cell_{c}_{r}", new Vector2(cell * 0.94f, cell * 0.94f),
                        sprites.cell, new Color(1f, 1f, 1f, 0.14f), -9000);
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
                        CreateFurnitureObject(furnitureParent, GridSystem.CellToWorld(center, cols, rows, cell, c, gridRow), cell, sprites);
                    }
                }
            }

            GameObject playerGO = CreateActor(root.transform, "Player", sprites.mouse, sprites.mouseTint, cell, cell * 0.5f);
            playerGO.transform.position = center;
            var player = playerGO.AddComponent<MousePlayerController>();

            GameObject catGO = CreateActor(root.transform, "Cat", sprites.cat, sprites.catTint, cell, cell * 0.62f);
            catGO.transform.position = center + new Vector3(balance.catStartDistance, 0f, 0f);
            var cat = catGO.AddComponent<CatController>();

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
            Debug.Log("[Mice to be Home] Scene built. Assign sprites on each 'Visual' child (or on Game > Config) and press Play.");
        }

        [MenuItem("Tools/Mice to be Home/Add Furniture Piece")]
        public static void AddFurniturePiece()
        {
            var installer = Object.FindFirstObjectByType<GameInstaller>();
            float cell = 2f;
            SpriteLibrary sprites = new SpriteLibrary();
            Transform parent = null;

            if (installer != null)
            {
                GameConfig config = installer.EditorEnsureConfig();
                cell = Mathf.Max(0.1f, config.Balance.cellSize);
                sprites = config.Sprites;
                var grid = installer.GetComponentInChildren<GridSystem>();
                if (grid != null)
                {
                    Transform furniture = grid.transform.Find("Furniture");
                    parent = furniture != null ? furniture : grid.transform;
                }
            }

            GameObject piece = CreateFurnitureObject(parent, Vector3.zero, cell, sprites);
            Undo.RegisterCreatedObjectUndo(piece, "Add Furniture Piece");
            Selection.activeObject = piece;
            EditorGUIUtility.PingObject(piece);
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
            go.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
        }

        private static GameObject CreateFurnitureObject(Transform parent, Vector3 position, float cell, SpriteLibrary sprites)
        {
            var go = new GameObject("Furniture");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            go.transform.position = position;

            go.AddComponent<FurniturePiece>();
            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.5f, 0f);
            box.size = new Vector3(cell * 0.6f, 1f, cell * 0.6f);

            AddBillboard(go.transform, "Visual", sprites.furniture, sprites.furnitureTint, false, cell * 0.72f);
            return go;
        }

        private static GameObject CreateActor(Transform parent, string name, Sprite artwork, Color tint, float cell, float visualSize)
        {
            var go = NewChild(parent, name);

            var body = go.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.radius = cell * 0.12f;
            capsule.height = 1f;
            capsule.center = new Vector3(0f, 0.5f, 0f);

            AddBillboard(go.transform, "Visual", artwork, tint, true, visualSize);
            return go;
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
