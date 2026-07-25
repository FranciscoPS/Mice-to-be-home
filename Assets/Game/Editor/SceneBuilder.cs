#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiceToBeHome.EditorTools
{
    public static class SceneBuilder
    {
        private const string PrefabFolder = "Assets/Game/Prefabs";
        private const string TrapPrefabFolder = "Assets/Game/Prefabs/Traps";
        private const string PlaceholderFolder = "Assets/Game/Placeholders";
        private const string ConfigFolder = "Assets/Game/Config";

        [MenuItem("Tools/Mice to be Home/Build Scene")]
        public static void BuildScene()
        {
            bool hasExisting = Object.FindFirstObjectByType<GameInstaller>() != null;
            if (hasExisting)
            {
                if (!EditorUtility.DisplayDialog("Mice to be Home",
                    "This removes the existing Game setup (and any duplicate camera scripts) and rebuilds it. Continue?",
                    "Rebuild", "Cancel"))
                {
                    return;
                }
                ClearExisting();
            }

            EnsurePrefabFolder();

            var root = new GameObject("Game");
            Undo.RegisterCreatedObjectUndo(root, "Build Mice Scene");

            var installer = root.AddComponent<GameInstaller>();
            GameConfig config = LoadOrCreateConfig();
            installer.config = config;
            BalanceSettings balance = config.Balance;
            SpriteLibrary sprites = config.Sprites;

            int cols = Mathf.Max(1, balance.gridColumns);
            int rows = Mathf.Max(1, balance.gridRows);
            float cell = Mathf.Max(0.1f, balance.cellSize);
            Vector3 center = Vector3.zero;

            GameObject furniturePrefab = BuildFurniturePrefab(sprites, cell);
            GameObject cellPrefab = BuildCellPrefab(sprites, cell);
            GameObject playerPrefab = BuildActorPrefab("Player", sprites.mouse, sprites.mouseTint, cell, cell * 0.5f, false);
            GameObject catPrefab = BuildActorPrefab("Cat", sprites.cat, sprites.catTint, cell, cell * 0.62f, true);

            BuildTrapPrefabs(config, cell);

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

            BuildWalls(gridGO.transform, cols, rows, cell, sprites);

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

            CameraController cameraController = CameraRigBuilder.EnsureRig(config);
            Camera cam = Camera.main;

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

        [MenuItem("Tools/Mice to be Home/Clear Scene")]
        public static void ClearScene()
        {
            ClearExisting();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Mice to be Home] Removed the Game setup and camera scripts. Run Build Scene to recreate it.");
        }

        private static void ClearExisting()
        {
            foreach (var installer in Object.FindObjectsByType<GameInstaller>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (installer != null)
                {
                    Object.DestroyImmediate(installer.gameObject);
                }
            }
        }

        private static void EnsurePrefabFolder()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/Game", "Prefabs");
            }
        }

        private static GameConfig LoadOrCreateConfig()
        {
            string path = ConfigFolder + "/GameConfig.asset";
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
            if (config == null)
            {
                if (!AssetDatabase.IsValidFolder(ConfigFolder))
                {
                    AssetDatabase.CreateFolder("Assets/Game", "Config");
                }
                config = ScriptableObject.CreateInstance<GameConfig>();
                config.EnsureDefaults();
                AssetDatabase.CreateAsset(config, path);
            }
            else
            {
                config.EnsureDefaults();
            }

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static GameObject BuildFurniturePrefab(SpriteLibrary sprites, float cell)
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
            box.size = new Vector3(cell * 0.45f, 1f, cell * 0.45f);
            AddBillboard(template.transform, "Visual", sprites.furniture, sprites.furnitureTint, false, cell * 0.72f);
            return SavePrefab(template, path);
        }

        private static GameObject BuildCellPrefab(SpriteLibrary sprites, float cell)
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

        private static GameObject BuildActorPrefab(string name, Sprite artwork, Color tint, float cell, float visualSize, bool isCat)
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

        private static void BuildTrapPrefabs(GameConfig config, float cell)
        {
            EnsureTrapFolder();
            GameObject basePrefab = BuildBaseTrapPrefab(cell);

            List<TrapDefinition> defaults = GameConfig.BuildDefaultTraps();
            var prefabs = new List<Trap>(defaults.Count);
            for (int i = 0; i < defaults.Count; i++)
            {
                prefabs.Add(BuildTrapVariant(basePrefab, defaults[i]));
            }

            config.EditorSetTrapPrefabs(prefabs);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Mice to be Home/Reset Traps to Default Values")]
        public static void ResetTrapValues()
        {
            if (!EditorUtility.DisplayDialog("Mice to be Home",
                "Re-apply the default stun and stock values from code to every trap prefab? Each prefab keeps its visuals and collider - only the data (stun, stock) changes.",
                "Apply values", "Cancel"))
            {
                return;
            }

            GameConfig config = LoadOrCreateConfig();
            float cell = Mathf.Max(0.1f, config.Balance.cellSize);
            BuildTrapPrefabs(config, cell);

            List<TrapDefinition> defaults = GameConfig.BuildDefaultTraps();
            for (int i = 0; i < defaults.Count; i++)
            {
                ApplyTrapData(defaults[i]);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Mice to be Home] Trap values reset to code defaults (visuals kept).");
        }

        private static void ApplyTrapData(TrapDefinition data)
        {
            string path = TrapPrefabFolder + "/" + Sanitize(data.displayName) + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            var trap = contents.GetComponent<Trap>();
            if (trap != null)
            {
                trap.EditorAssign(data);
            }
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        private static GameObject BuildBaseTrapPrefab(float cell)
        {
            string path = TrapPrefabFolder + "/BaseTrap.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing;
            }

            var template = new GameObject("Trap");
            template.AddComponent<Trap>();
            var zone = template.GetComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(0f, 0.5f, 0f);
            zone.size = new Vector3(cell * 1.15f, 1.2f, cell * 1.15f);

            ConfigureTrapVisual(template, cell);
            return SavePrefab(template, path);
        }

        private static void ConfigureTrapVisual(GameObject trapRoot, float cell)
        {
            Transform found = trapRoot.transform.Find("Visual");
            GameObject visual = found != null ? found.gameObject : NewChild(trapRoot.transform, "Visual");

            var renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = visual.AddComponent<SpriteRenderer>();
            }
            if (renderer.sprite == null)
            {
                renderer.sprite = SquareSprite;
            }

            if (visual.GetComponent<Billboard>() == null)
            {
                visual.AddComponent<Billboard>();
            }

            float size = cell * 0.9f;
            visual.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
            visual.transform.localScale = Vector3.one * size;
            visual.transform.localPosition = new Vector3(0f, size * 0.5f, 0f);

            ConfigureTrapRepair(trapRoot, cell, size);
        }

        private static void ConfigureTrapRepair(GameObject trapRoot, float cell, float size)
        {
            Transform legacy = trapRoot.transform.Find("Countdown");
            if (legacy != null)
            {
                Object.DestroyImmediate(legacy.gameObject);
            }
            Transform previous = trapRoot.transform.Find("Repair");
            if (previous != null)
            {
                Object.DestroyImmediate(previous.gameObject);
            }

            var root = new GameObject("Repair", typeof(RectTransform), typeof(Canvas));
            root.transform.SetParent(trapRoot.transform, false);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 30000;

            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(100f, 100f);

            float diameter = cell * 0.6f;
            root.transform.localScale = Vector3.one * (diameter / 100f);
            root.transform.localPosition = new Vector3(0f, size * 0.5f, 0f);
            root.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);

            var track = new GameObject("Track", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(root.transform, false);
            var trackImage = track.GetComponent<Image>();
            trackImage.sprite = CircleSprite;
            trackImage.color = new Color(0f, 0f, 0f, 0.45f);
            trackImage.raycastTarget = false;
            StretchRect(track.GetComponent<RectTransform>());

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(root.transform, false);
            var fillImage = fill.GetComponent<Image>();
            fillImage.sprite = CircleSprite;
            fillImage.color = new Color(0.45f, 1f, 0.55f, 0.95f);
            fillImage.raycastTarget = false;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360;
            fillImage.fillOrigin = (int)Image.Origin360.Top;
            fillImage.fillClockwise = true;
            fillImage.fillAmount = 0f;
            StretchRect(fill.GetComponent<RectTransform>());

            root.SetActive(false);
        }

        private static void StretchRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Trap BuildTrapVariant(GameObject basePrefab, TrapDefinition data)
        {
            string path = TrapPrefabFolder + "/" + Sanitize(data.displayName) + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing.GetComponent<Trap>();
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            var trap = instance.GetComponent<Trap>();
            trap.EditorAssign(data);

            var renderer = instance.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = data.tint;
            }

            GameObject variantAsset = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return variantAsset.GetComponent<Trap>();
        }

        private static void EnsureTrapFolder()
        {
            if (AssetDatabase.IsValidFolder(TrapPrefabFolder))
            {
                return;
            }
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/Game", "Prefabs");
            }
            AssetDatabase.CreateFolder(PrefabFolder, "Traps");
        }

        private static string Sanitize(string name)
        {
            var builder = new System.Text.StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(c);
                }
            }
            return builder.Length > 0 ? builder.ToString() : "Trap";
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

        private static Sprite squareCache;
        private static Sprite circleCache;

        private static Sprite SquareSprite => squareCache != null ? squareCache : (squareCache = GetOrCreatePlaceholder("PlaceholderSquare", false));
        private static Sprite CircleSprite => circleCache != null ? circleCache : (circleCache = GetOrCreatePlaceholder("PlaceholderCircle", true));

        private static Sprite GetOrCreatePlaceholder(string fileName, bool circle)
        {
            string path = PlaceholderFolder + "/" + fileName + ".png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                return existing;
            }

            if (!AssetDatabase.IsValidFolder(PlaceholderFolder))
            {
                AssetDatabase.CreateFolder("Assets/Game", "Placeholders");
            }

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float mid = (size - 1) * 0.5f;
            float radius = size * 0.5f - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside;
                    bool edge;
                    if (circle)
                    {
                        float distance = Mathf.Sqrt((x - mid) * (x - mid) + (y - mid) * (y - mid));
                        inside = distance <= radius;
                        edge = distance > radius - 6f && distance <= radius;
                    }
                    else
                    {
                        inside = true;
                        edge = x < 5 || y < 5 || x >= size - 5 || y >= size - 5;
                    }

                    pixels[y * size + x] = inside
                        ? (edge ? new Color32(180, 180, 180, 255) : new Color32(255, 255, 255, 255))
                        : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(Path.GetFullPath(path), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

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

        private static void BuildWalls(Transform gridParent, int cols, int rows, float cell, SpriteLibrary sprites)
        {
            Transform walls = NewChild(gridParent, "Walls").transform;

            float hx = cols * cell * 0.5f;
            float hz = rows * cell * 0.5f;
            float thickness = 0.6f;
            float colliderHeight = 2.5f;
            float wallHeight = cell * 0.9f;

            AddWallCollider(walls, "ColliderNorth", new Vector3(0f, colliderHeight * 0.5f, hz + thickness * 0.5f), new Vector3(2f * hx + 2f * thickness, colliderHeight, thickness));
            AddWallCollider(walls, "ColliderSouth", new Vector3(0f, colliderHeight * 0.5f, -(hz + thickness * 0.5f)), new Vector3(2f * hx + 2f * thickness, colliderHeight, thickness));
            AddWallCollider(walls, "ColliderEast", new Vector3(hx + thickness * 0.5f, colliderHeight * 0.5f, 0f), new Vector3(thickness, colliderHeight, 2f * hz + 2f * thickness));
            AddWallCollider(walls, "ColliderWest", new Vector3(-(hx + thickness * 0.5f), colliderHeight * 0.5f, 0f), new Vector3(thickness, colliderHeight, 2f * hz + 2f * thickness));

            AddWallVisual(walls, "WallNorth", sprites, new Vector3(0f, wallHeight * 0.5f, hz), Quaternion.identity, new Vector2(2f * hx + 2f * thickness, wallHeight));
            AddWallVisual(walls, "WallEast", sprites, new Vector3(hx, wallHeight * 0.5f, 0f), Quaternion.Euler(0f, 90f, 0f), new Vector2(2f * hz + 2f * thickness, wallHeight));
            AddWallVisual(walls, "WallWest", sprites, new Vector3(-hx, wallHeight * 0.5f, 0f), Quaternion.Euler(0f, 90f, 0f), new Vector2(2f * hz + 2f * thickness, wallHeight));
        }

        private static void AddWallCollider(Transform parent, string name, Vector3 localPosition, Vector3 size)
        {
            var go = NewChild(parent, name);
            go.transform.localPosition = localPosition;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
        }

        private static void AddWallVisual(Transform parent, string name, SpriteLibrary sprites, Vector3 localPosition, Quaternion localRotation, Vector2 size)
        {
            var go = NewChild(parent, name);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprites.wall != null ? sprites.wall : SquareSprite;
            renderer.color = sprites.wall != null ? Color.white : sprites.wallTint;
            renderer.sortingOrder = -5000;
            go.transform.localRotation = localRotation;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.transform.localPosition = localPosition;
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
    }
}
#endif
