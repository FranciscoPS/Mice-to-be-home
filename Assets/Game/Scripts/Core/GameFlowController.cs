using System;
using System.Collections;
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
        private bool losing;
        private Coroutine chaseLeadInRoutine;

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

            // Suscribirse a la petición de empezar la persecución (trigger desde UI).
            GameManager.Instance.BeginChaseRequested += OnBeginChaseRequested;
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
            bool resuming = GameManager.Instance.Resuming;

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
            StopChaseLeadIn();
            editCountdown.Stop();
            surviveCountdown.Stop();
            placement.SetActive(false);
            player.gameObject.SetActive(false);
            cat.gameObject.SetActive(false);
            grid.SetGridVisible(false);
            cameraController.FrameGrid();
            audioManager.PlayMenuMusic();
        }

        private void StartEditPhase()
        {
            StopChaseLeadIn();
            placement.ResetLevel();
            lives.ResetLives();
            losing = false;

            player.gameObject.SetActive(false);
            cat.gameObject.SetActive(false);

            placement.SetActive(true);
            placement.SetTrapsArmed(false);
            grid.SetGridVisible(true);
            cameraController.FrameGrid();

            editCountdown.Begin(balance.editSeconds);
            audioManager.PlayEditMusic();
        }

        private void StartChasePhase()
        {
            StopChaseLeadIn();

            placement.SetActive(false);
            placement.SetTrapsArmed(true);
            grid.SetGridVisible(false);

            Vector3 center = grid.Center;

            player.gameObject.SetActive(true);
            player.Teleport(center);

            Vector3 catStart = center + new Vector3(balance.catStartDistance, 0f, 0f);
            cat.gameObject.SetActive(true);
            cat.ResetForChase(catStart);

            cameraController.Follow(player.transform);

            // The chase intro music starts now, but the mouse and cat stay frozen for a short
            // lead-in so the intro can play a beat before gameplay actually begins.
            audioManager.PlayChaseMusic();
            player.SetActive(false);
            cat.SetActive(false);
            OnTimerChanged(balance.surviveSeconds);

            float leadIn = balance != null ? balance.chaseLeadInSeconds : 0f;
            if (leadIn > 0f)
            {
                chaseLeadInRoutine = StartCoroutine(ChaseLeadIn(leadIn));
            }
            else
            {
                GoLiveChase();
            }
        }

        private IEnumerator ChaseLeadIn(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            chaseLeadInRoutine = null;
            if (GameManager.Instance.State == GameState.Playing)
            {
                GoLiveChase();
            }
        }

        private void GoLiveChase()
        {
            player.SetActive(true);
            cat.SetActive(true);
            surviveCountdown.Begin(balance.surviveSeconds);
        }

        private void StopChaseLeadIn()
        {
            if (chaseLeadInRoutine != null)
            {
                StopCoroutine(chaseLeadInRoutine);
                chaseLeadInRoutine = null;
            }
        }

        private void FreezeActors()
        {
            player.SetActive(false);
            cat.SetActive(false);
        }

        private void OnTimerChanged(float remaining) => TimerChanged?.Invoke(remaining);

        // Cuando la edición termina (tiempo) reproducir la animación y luego comenzar la persecución.
        private void HandleEditFinished()
        {
            StartIntroSequence();
        }

        // Cuando UI pide comenzar (botón START), manejar la petición para reproducir la intro si estamos en Editing.
        private void OnBeginChaseRequested()
        {
            StartIntroSequence();
        }

        private void StartIntroSequence()
        {
            if (player == null)
            {
                GameManager.Instance.BeginChase();
                return;
            }

            // Si no estamos en Editing, proceder directamente.
            if (GameManager.Instance.State != GameState.Editing)
            {
                GameManager.Instance.BeginChase();
                return;
            }

            // Ejecutar animación del jugador conectada desde Any State y esperar a que termine.
            StartCoroutine(PlayIntroThenBegin());
        }

        private IEnumerator PlayIntroThenBegin()
        {
            // Preparar jugador para que el Animator pueda reproducir la animación:
            Vector3 center = grid != null ? grid.Center : Vector3.zero;
            player.gameObject.SetActive(true);
            player.Teleport(center);
            // Mantener congelado durante la intro (evita que el jugador se mueva)
            player.SetActive(false);

            // Asegurar que la cámara enfoque al jugador para la intro.
            if (cameraController != null)
            {
                cameraController.Follow(player.transform);
            }

            bool finished = false;
            player.PlayStartAnimation(() => finished = true);

            // Esperar hasta que la animación acabe.
            while (!finished)
            {
                yield return null;
            }

            // Asegurarse de que seguimos en Editing (no se haya cambiado manualmente el estado).
            if (GameManager.Instance.State == GameState.Editing)
            {
                GameManager.Instance.BeginChase();
            }
        }

        private void HandleSurviveFinished() => GameManager.Instance.Win();

        private void HandleLivesDepleted()
        {
            if (losing)
            {
                return;
            }
            losing = true;
            StartCoroutine(LoseSequence());
        }

        private IEnumerator LoseSequence()
        {
            // Hold the "caught" tableau (stops movement and any further hits).
            FreezeActors();
            player.PlayDeath();

            // Dramatic zoom onto the player (Cinemachine blends CM_LoseZoom in).
            if (cameraController != null)
            {
                cameraController.PlayLoseZoom(player.transform);
            }

            // Ease time down to a near-stop, Yu-Gi-Oh style. The cubic ease keeps time high
            // early so the camera blend can play, then plunges into slow motion.
            float duration = Mathf.Max(0.1f, balance.loseSlowDuration);
            float minScale = Mathf.Clamp01(balance.loseSlowMinScale);
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                Time.timeScale = Mathf.Lerp(1f, minScale, k * k * k);
                yield return null;
            }
            Time.timeScale = minScale;

            if (balance.loseHoldSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(balance.loseHoldSeconds);
            }

            // Trigger the real defeat: GameManager sets timeScale = 0 and shows the defeat screen.
            GameManager.Instance.Lose();
        }
    }
}
