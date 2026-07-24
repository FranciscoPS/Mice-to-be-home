using UnityEngine;

namespace MiceToBeHome
{
    public class GameInstaller : MonoBehaviour
    {
        [Header("Configuration asset")]
        public GameConfig config;

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

        private void Start()
        {
            if (config == null)
            {
                Debug.LogError("[Mice to be Home] GameInstaller has no GameConfig assigned. Run Tools > Mice to be Home > Build Scene.");
                return;
            }

            config.EnsureDefaults();
            BalanceSettings balance = config.Balance;

            grid.Initialize(balance);
            audioManager.Initialize(config.Audio);
            player.Initialize(balance);
            cat.Initialize(player, balance, audioManager, grid);
            cameraController.Initialize(balance, grid.Center);
            placement.Initialize(grid, balance, mainCamera, trapParent, audioManager);
            lives.Initialize(player, balance.lives, audioManager);
            flow.Initialize(grid, placement, player, cat, lives, cameraController, balance, audioManager);
            ui.Initialize(config, placement, flow, lives);

            GameManager.Instance.Emit();
        }
    }
}
