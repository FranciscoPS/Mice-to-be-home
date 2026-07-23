using UnityEngine;

namespace MiceToBeHome
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameConfig config = new GameConfig();

        private BalanceSettings balance;
        private SpriteLibrary sprites;
        private PhysicsMaterial slipperyMaterial;
        private const int ObstacleLayer = 8;

        private void Reset()
        {
            config ??= new GameConfig();
            config.EnsureDefaults();
        }

#if UNITY_EDITOR
        public void EditorEnsureDefaults()
        {
            config ??= new GameConfig();
            config.EnsureDefaults();
        }
#endif

        private void Awake()
        {
            config ??= new GameConfig();
            config.EnsureDefaults();
            balance = config.Balance;
            sprites = config.Sprites;
            slipperyMaterial = CreateSlipperyMaterial();

            WarnIfTextMeshProMissing();

            CreateGameManager();
            Camera camera = CreateCamera();
            AudioManager audio = CreateAudio();
            GridSystem grid = CreateEnvironment();

            CameraController cameraController = camera.gameObject.AddComponent<CameraController>();
            cameraController.Initialize(camera, balance, grid.Center);

            Transform actors = new GameObject("Actors").transform;
            actors.SetParent(transform, false);
            MousePlayerController player = CreatePlayer(actors, grid);
            CatController cat = CreateCat(actors, player, audio);

            Transform trapRoot = new GameObject("Traps").transform;
            trapRoot.SetParent(transform, false);

            PlacementController placement = CreatePlacement(grid, camera, trapRoot, audio);
            LivesSystem lives = CreateLives(player, audio);
            GameFlowController flow = CreateFlow(grid, placement, player, cat, lives, cameraController, audio);
            CreateUI(placement, flow, lives);

            GameManager.Instance.Emit();
        }

        private void CreateGameManager()
        {
            var go = new GameObject("GameManager");
            go.transform.SetParent(transform, false);
            go.AddComponent<GameManager>();
        }

        private Camera CreateCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                camera = go.AddComponent<Camera>();
            }

            if (!camera.TryGetComponent(out AudioListener _))
            {
                camera.gameObject.AddComponent<AudioListener>();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.09f, 0.13f);
            return camera;
        }

        private AudioManager CreateAudio()
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(transform, false);
            var audio = go.AddComponent<AudioManager>();
            audio.Initialize(config.Audio);
            return audio;
        }

        private GridSystem CreateEnvironment()
        {
            var gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(transform, false);
            var grid = gridObject.AddComponent<GridSystem>();
            grid.Initialize(balance);

            var environment = new GameObject("Environment").transform;
            environment.SetParent(transform, false);

            SpriteRenderer floor = VisualFactory.CreateGround("Floor", environment, sprites.floor, sprites.floorTint,
                grid.WorldSize + new Vector2(1.2f, 1.2f), -10000);
            floor.transform.position = grid.Center;

            var guides = new GameObject("Guides").transform;
            guides.SetParent(environment, false);
            foreach (Vector2Int cell in grid.AllCells())
            {
                Color tint = grid.IsFurniture(cell) ? sprites.blockedCellTint : sprites.freeCellTint;
                SpriteRenderer tile = VisualFactory.CreateGround("Cell", guides, sprites.cell, tint,
                    new Vector2(balance.cellSize * 0.94f, balance.cellSize * 0.94f), -9000);
                tile.transform.position = grid.CellToWorld(cell) + Vector3.up * 0.01f;
            }

            var furniture = new GameObject("Furniture").transform;
            furniture.SetParent(environment, false);
            foreach (Vector2Int cell in grid.FurnitureCells())
            {
                var piece = new GameObject("Furniture");
                piece.transform.SetParent(furniture, false);
                piece.transform.position = grid.CellToWorld(cell);
                piece.layer = ObstacleLayer;
                VisualFactory.CreateBillboard("Visual", piece.transform, sprites.furniture, sprites.furnitureTint,
                    SpriteShape.Square, balance.cellSize * 0.72f);
                var collider = piece.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, 0.5f, 0f);
                collider.size = new Vector3(balance.cellSize * 0.7f, 1f, balance.cellSize * 0.7f);
            }

            return grid;
        }

        private MousePlayerController CreatePlayer(Transform parent, GridSystem grid)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(parent, false);
            go.transform.position = grid.Center;

            Rigidbody body = ConfigureBody(go);
            AddActorCollider(go);

            SpriteRenderer visual = VisualFactory.CreateBillboard("Visual", go.transform, sprites.mouse, sprites.mouseTint,
                SpriteShape.Circle, balance.cellSize * 0.5f);

            var controller = go.AddComponent<MousePlayerController>();
            controller.Initialize(body, visual, balance);

            go.SetActive(false);
            return controller;
        }

        private CatController CreateCat(Transform parent, MousePlayerController player, AudioManager audio)
        {
            var go = new GameObject("Cat");
            go.transform.SetParent(parent, false);

            Rigidbody body = ConfigureBody(go);
            AddActorCollider(go);

            VisualFactory.CreateBillboard("Visual", go.transform, sprites.cat, sprites.catTint,
                SpriteShape.Circle, balance.cellSize * 0.62f);

            var controller = go.AddComponent<CatController>();
            controller.Initialize(body, player, balance, audio, 1 << ObstacleLayer);

            go.SetActive(false);
            return controller;
        }

        private PlacementController CreatePlacement(GridSystem grid, Camera camera, Transform trapRoot, AudioManager audio)
        {
            var go = new GameObject("Placement");
            go.transform.SetParent(transform, false);
            var placement = go.AddComponent<PlacementController>();
            placement.Initialize(grid, balance, camera, trapRoot, audio);
            return placement;
        }

        private LivesSystem CreateLives(MousePlayerController player, AudioManager audio)
        {
            var go = new GameObject("Lives");
            go.transform.SetParent(transform, false);
            var lives = go.AddComponent<LivesSystem>();
            lives.Initialize(player, balance.lives, audio);
            return lives;
        }

        private GameFlowController CreateFlow(GridSystem grid, PlacementController placement, MousePlayerController player,
            CatController cat, LivesSystem lives, CameraController cameraController, AudioManager audio)
        {
            var go = new GameObject("GameFlow");
            go.transform.SetParent(transform, false);
            var flow = go.AddComponent<GameFlowController>();
            flow.Initialize(grid, placement, player, cat, lives, cameraController, balance, audio);
            return flow;
        }

        private void CreateUI(PlacementController placement, GameFlowController flow, LivesSystem lives)
        {
            var go = new GameObject("UI");
            go.transform.SetParent(transform, false);
            var ui = go.AddComponent<UIManager>();
            ui.Initialize(config, placement, flow, lives);
        }

        private Rigidbody ConfigureBody(GameObject go)
        {
            var body = go.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            return body;
        }

        private void AddActorCollider(GameObject go)
        {
            var collider = go.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.radius = balance.cellSize * 0.12f;
            collider.height = 1f;
            collider.center = new Vector3(0f, 0.5f, 0f);
            collider.sharedMaterial = slipperyMaterial;
        }

        private static PhysicsMaterial CreateSlipperyMaterial()
        {
            return new PhysicsMaterial("MiceSlippery")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0f,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
        }

        private static void WarnIfTextMeshProMissing()
        {
            if (TMPro.TMP_Settings.instance == null || TMPro.TMP_Settings.defaultFontAsset == null)
            {
                Debug.LogError("[Mice to be Home] TextMeshPro is missing. Go to Window > TextMeshPro > Import TMP Essential Resources so the on-screen text renders.");
            }
        }
    }
}
