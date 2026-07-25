using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MiceToBeHome
{
    public class GameFlowController : MonoBehaviour
    {
        public event Action<float> TimerChanged;
        public event Action<GameState> PhaseChanged;

        private GridSystem grid;
        private PlacementController placement;
        private MousePlayerController player;
        private CatController cat;
        private LivesSystem lives;
        private CameraController cameraController;
        private BalanceSettings balance;
        private AudioManager audioManager;

        private readonly Countdown editCountdown = new Countdown();
        private readonly Countdown surviveCountdown = new Countdown();

        public void Initialize(GridSystem gridSystem, PlacementController placementController, MousePlayerController playerController,
            CatController catController, LivesSystem livesSystem, CameraController camera, BalanceSettings balanceSettings, AudioManager audioManager)
        {
            grid = gridSystem;
            placement = placementController;
            player = playerController;
            cat = catController;
            lives = livesSystem;
            cameraController = camera;
            balance = balanceSettings;
            this.audioManager = audioManager;

            editCountdown.Changed += OnTimerChanged;
            surviveCountdown.Changed += OnTimerChanged;
            editCountdown.Finished += HandleEditFinished;
            surviveCountdown.Finished += HandleSurviveFinished;
            lives.Depleted += HandleLivesDepleted;

            GameManager.Instance.StateChanged += ConfigureState;
        }

        private void Update()
        {
            HandlePauseInput();

            switch (GameManager.Instance.State)
            {
                case GameState.Editing:
                    editCountdown.Tick(Time.deltaTime);
                    break;
                case GameState.Playing:
                    surviveCountdown.Tick(Time.deltaTime);
                    break;
            }
        }

        private void HandlePauseInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)
            {
                GameState state = GameManager.Instance.State;
                if (state == GameState.Playing || state == GameState.Editing || state == GameState.Paused)
                {
                    GameManager.Instance.TogglePause();
                }
            }
        }

        private void ConfigureState(GameState state)
        {
            bool resuming = GameManager.Instance.PreviousState == GameState.Paused;

            switch (state)
            {
                case GameState.MainMenu:
                    EnterMenu();
                    break;
                case GameState.Editing:
                    if (!resuming)
                    {
                        StartEditPhase();
                    }
                    break;
                case GameState.Playing:
                    if (!resuming)
                    {
                        StartChasePhase();
                    }
                    break;
                case GameState.Victory:
                    FreezeActors();
                    audioManager.PlayVictory();
                    break;
                case GameState.Defeat:
                    FreezeActors();
                    audioManager.PlayDefeat();
                    break;
            }

            PhaseChanged?.Invoke(state);
        }

        private void EnterMenu()
        {
            editCountdown.Stop();
            surviveCountdown.Stop();
            placement.SetActive(false);
            player.gameObject.SetActive(false);
            cat.gameObject.SetActive(false);
            cameraController.FrameGrid();
            audioManager.PlayMenuMusic();
        }

        private void StartEditPhase()
        {
            placement.ResetLevel();
            lives.ResetLives();

            player.gameObject.SetActive(false);
            cat.gameObject.SetActive(false);

            placement.SetActive(true);
            placement.SetTrapsArmed(false);
            cameraController.FrameGrid();

            editCountdown.Begin(balance.editSeconds);
            audioManager.PlayEditMusic();
        }

        private void StartChasePhase()
        {
            placement.SetActive(false);
            placement.SetTrapsArmed(true);

            Vector3 center = grid.Center;

            player.gameObject.SetActive(true);
            player.Teleport(center);

            Vector3 catStart = center + new Vector3(balance.catStartDistance, 0f, 0f);
            cat.gameObject.SetActive(true);
            cat.ResetForChase(catStart);
            cat.SetActive(false);

            cameraController.Follow(player.transform);
            audioManager.PlayChaseMusic();

            // The player transforms in place; the cat waits frozen until the animation finishes.
            player.BeginIntro(BeginSurvival);
        }

        private void BeginSurvival()
        {
            player.SetActive(true);
            cat.SetActive(true);
            surviveCountdown.Begin(balance.surviveSeconds);
        }

        private void FreezeActors()
        {
            player.SetActive(false);
            cat.SetActive(false);
        }

        private void OnTimerChanged(float remaining) => TimerChanged?.Invoke(remaining);

        private void HandleEditFinished() => GameManager.Instance.BeginChase();

        private void HandleSurviveFinished() => GameManager.Instance.Win();

        private void HandleLivesDepleted() => GameManager.Instance.Lose();
    }
}
