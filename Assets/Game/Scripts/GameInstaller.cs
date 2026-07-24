using UnityEngine;

namespace MiceToBeHome
{
    public class GameInstaller : MonoBehaviour
    {
        [SerializeField] private GameConfig config = new GameConfig();

        [Header("Scene references (filled by Build Scene)")]
        public GridSystem grid;
        public MousePlayerController player;
        public CatController cat;
        public CameraController cameraController;
        public Camera mainCamera;
        public PlacementController placement;
        public LivesSystem lives;
        public GameFlowController flow;
        public AudioManager audioManager;
        public UIManager ui;
        public Transform trapParent;

        public GameConfig Config => config;

        private void Reset()
        {
            config ??= new GameConfig();
            config.EnsureDefaults();
        }

#if UNITY_EDITOR
        public GameConfig EditorEnsureConfig()
        {
            config ??= new GameConfig();
            config.EnsureDefaults();
            return config;
        }
#endif

        private void Start()
        {
            config ??= new GameConfig();
            config.EnsureDefaults();
            BalanceSettings balance = config.Balance;

            grid.Initialize(balance);
            audioManager.Initialize(config.Audio);
            player.Initialize(balance);
            cat.Initialize(player, balance, audioManager);
            cameraController.Initialize(balance, grid.Center);
            placement.Initialize(grid, balance, mainCamera, trapParent, audioManager);
            lives.Initialize(player, balance.lives, audioManager);
            flow.Initialize(grid, placement, player, cat, lives, cameraController, balance, audioManager);
            ui.Initialize(config, placement, flow, lives);

            GameManager.Instance.Emit();
        }
    }
}
