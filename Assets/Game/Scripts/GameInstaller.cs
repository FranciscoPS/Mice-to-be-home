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
            EnsureBumpEmitter(player.gameObject);
            EnsureBumpEmitter(cat.gameObject);
            cameraController.Initialize(balance, grid.Center);
            placement.Initialize(grid, balance, mainCamera, trapParent, audioManager, config.Traps);
            lives.Initialize(player, balance.lives, audioManager);
            flow.Initialize(grid, placement, player, cat, lives, cameraController, balance, audioManager);

            if (ui == null)
            {
                ui = FindFirstObjectByType<UIManager>();
            }
            if (ui != null)
            {
                ui.Initialize(config, placement, flow, lives);
            }
            else
            {
                Debug.LogError("[Mice to be Home] No UIManager in the scene. Run Tools > Mice to be Home > Build UI.");
            }

            player.Hit += () =>
            {
                cameraController.Shake(balance.hitShakeAmplitude, balance.hitShakeDuration);
                audioManager.PlayHit();
            };

            GameManager.Instance.Emit();
        }

        private static void EnsureBumpEmitter(GameObject actor)
        {
            if (actor.GetComponent<BumpEmitter>() == null)
            {
                actor.AddComponent<BumpEmitter>();
            }
        }
    }
}
