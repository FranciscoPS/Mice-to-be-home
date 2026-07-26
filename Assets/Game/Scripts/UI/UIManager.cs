using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MiceToBeHome
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private HudView hud = null;
        [SerializeField] private TooltipView tooltip = null;
        [SerializeField] private GameObject hudGroup = null;
        [SerializeField] private GameObject menuPanel = null;
        [SerializeField] private GameObject pausePanel = null;
        [SerializeField] private GameObject victoryPanel = null;
        [SerializeField] private GameObject defeatPanel = null;

        [SerializeField] private Button menuPlayButton = null;
        [SerializeField] private Button menuQuitButton = null;
        [SerializeField] private Button pauseResumeButton = null;
        [SerializeField] private Button pauseRestartButton = null;
        [SerializeField] private Button pauseMenuButton = null;
        [SerializeField] private Button victoryPrimaryButton = null;
        [SerializeField] private Button victoryMenuButton = null;
        [SerializeField] private Button defeatPrimaryButton = null;
        [SerializeField] private Button defeatMenuButton = null;

        public void Initialize(GameConfig config, PlacementController placement, GameFlowController flow, LivesSystem lives)
        {
            if (tooltip != null)
            {
                tooltip.Initialize();
            }
            if (hud != null)
            {
                hud.Initialize(config.Traps, placement, tooltip);
            }

            Wire(menuPlayButton, GameManager.Instance.NewGame);
            Wire(menuQuitButton, GameManager.Instance.QuitGame);
#if UNITY_WEBGL
            if (menuQuitButton != null)
            {
                menuQuitButton.gameObject.SetActive(false);
            }
#endif
            Wire(pauseResumeButton, GameManager.Instance.TogglePause);
            Wire(pauseRestartButton, GameManager.Instance.Restart);
            Wire(pauseMenuButton, GameManager.Instance.ReturnToMenu);
            Wire(victoryPrimaryButton, GameManager.Instance.Restart);
            Wire(victoryMenuButton, GameManager.Instance.ReturnToMenu);
            Wire(defeatPrimaryButton, GameManager.Instance.Restart);
            Wire(defeatMenuButton, GameManager.Instance.ReturnToMenu);

            if (hud != null)
            {
                // Cambiado: el StartButton ahora pide "RequestBeginChase" para que GameFlowController
                // pueda reproducir la animación antes de hacer BeginChase.
                Wire(hud.StartButton, GameManager.Instance.RequestBeginChase);
                Wire(hud.PauseButton, GameManager.Instance.TogglePause);
                flow.TimerChanged += hud.SetTimer;
                lives.Changed += hud.SetLives;
            }

            GameManager.Instance.StateChanged += ShowState;
            flow.PhaseChanged += OnPhase;
        }

        private static void Wire(Button button, UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private void OnPhase(GameState state)
        {
            if (hud != null && (state == GameState.Editing || state == GameState.Playing))
            {
                hud.SetPhase(state);
            }
        }

        private void ShowState(GameState state)
        {
            if (menuPanel != null)
            {
                menuPanel.SetActive(state == GameState.MainMenu);
            }
            if (hudGroup != null)
            {
                hudGroup.SetActive(state == GameState.Editing || state == GameState.Playing || state == GameState.Paused);
            }
            if (pausePanel != null)
            {
                pausePanel.SetActive(state == GameState.Paused);
            }
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(state == GameState.Victory);
            }
            if (defeatPanel != null)
            {
                defeatPanel.SetActive(state == GameState.Defeat);
            }
        }
    }
}
