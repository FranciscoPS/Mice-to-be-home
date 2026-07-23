using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiceToBeHome
{
    public class UIManager : MonoBehaviour
    {
        private HudView hud;
        private TooltipView tooltip;
        private GameObject hudGroup;
        private GameObject menuPanel;
        private GameObject pausePanel;
        private GameObject victoryPanel;
        private GameObject defeatPanel;

        public void Initialize(GameConfig config, PlacementController placement, GameFlowController flow, LivesSystem lives)
        {
            Canvas canvas = UIFactory.CreateCanvas("GameCanvas");
            canvas.transform.SetParent(transform, false);

            var tooltipObject = new GameObject("Tooltip", typeof(RectTransform));
            tooltipObject.transform.SetParent(canvas.transform, false);
            UIFactory.Stretch(tooltipObject.GetComponent<RectTransform>());
            tooltip = tooltipObject.AddComponent<TooltipView>();
            tooltip.Build(canvas);

            hudGroup = UIFactory.CreateGroup(canvas.transform, "HUD");
            hud = hudGroup.AddComponent<HudView>();
            hud.Build(hudGroup.transform, config.Traps, placement, tooltip);

            menuPanel = BuildMenu(canvas.transform);
            pausePanel = BuildPause(canvas.transform);
            victoryPanel = BuildEndScreen(canvas.transform, "VictoryPanel", "SOBREVIVISTE",
                "Volviste a ser humana. Miauricio se queda con hambre.", new Color(0.09f, 0.16f, 0.10f, 0.9f),
                "JUGAR DE NUEVO", GameManager.Instance.Restart);
            defeatPanel = BuildEndScreen(canvas.transform, "DefeatPanel", "TE ATRAPARON",
                "Miauricio te convirtio en su bocadillo nocturno.", new Color(0.18f, 0.07f, 0.07f, 0.9f),
                "REINTENTAR", GameManager.Instance.Restart);

            tooltipObject.transform.SetAsLastSibling();

            hud.StartButton.onClick.AddListener(GameManager.Instance.BeginChase);
            hud.PauseButton.onClick.AddListener(GameManager.Instance.TogglePause);

            GameManager.Instance.StateChanged += ShowState;
            flow.TimerChanged += hud.SetTimer;
            flow.PhaseChanged += OnPhase;
            lives.Changed += hud.SetLives;
        }

        private void OnPhase(GameState state)
        {
            if (state == GameState.Editing || state == GameState.Playing)
            {
                hud.SetPhase(state);
            }
        }

        private void ShowState(GameState state)
        {
            menuPanel.SetActive(state == GameState.MainMenu);
            hudGroup.SetActive(state == GameState.Editing || state == GameState.Playing || state == GameState.Paused);
            pausePanel.SetActive(state == GameState.Paused);
            victoryPanel.SetActive(state == GameState.Victory);
            defeatPanel.SetActive(state == GameState.Defeat);
        }

        private GameObject BuildMenu(Transform parent)
        {
            var panel = UIFactory.CreatePanel(parent, "MenuPanel", new Color(0.06f, 0.06f, 0.10f, 0.86f));

            var title = UIFactory.CreateText(panel.transform, "Title", "MICE TO BE HOME", 78f,
                TextAlignmentOptions.Center, Color.white);
            UIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 250f), new Vector2(1100f, 120f));

            var subtitle = UIFactory.CreateText(panel.transform, "Subtitle",
                "Coloca trampas, conviertete en raton y escapa de Miauricio.", 30f,
                TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.9f));
            UIFactory.Anchor(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 165f), new Vector2(1000f, 60f));

            var play = UIFactory.CreateButton(panel.transform, "PlayButton", "JUGAR", new Color(0.85f, 0.35f, 0.45f),
                Color.white, 38f, out _);
            UIFactory.Anchor((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(420f, 96f));
            play.onClick.AddListener(GameManager.Instance.NewGame);

#if !UNITY_WEBGL
            var quit = UIFactory.CreateButton(panel.transform, "QuitButton", "SALIR", new Color(0.25f, 0.25f, 0.32f),
                Color.white, 32f, out _);
            UIFactory.Anchor((RectTransform)quit.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(360f, 80f));
            quit.onClick.AddListener(GameManager.Instance.QuitGame);
#endif

            var controls = UIFactory.CreateText(panel.transform, "Controls",
                "Edicion: mouse para colocar trampas, Q/E rotar.    Persecucion: WASD para moverte.    Esc: pausa.", 22f,
                TextAlignmentOptions.Center, new Color(0.75f, 0.75f, 0.82f));
            UIFactory.Anchor(controls.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 60f), new Vector2(1300f, 40f));

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private GameObject BuildPause(Transform parent)
        {
            var panel = UIFactory.CreatePanel(parent, "PausePanel", new Color(0.03f, 0.03f, 0.05f, 0.78f));

            var title = UIFactory.CreateText(panel.transform, "Title", "PAUSA", 70f, TextAlignmentOptions.Center, Color.white);
            UIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 220f), new Vector2(800f, 110f));

            var resume = MakeMenuButton(panel.transform, "Resume", "CONTINUAR", 90f, new Color(0.3f, 0.55f, 0.4f));
            resume.onClick.AddListener(GameManager.Instance.TogglePause);

            var restart = MakeMenuButton(panel.transform, "Restart", "REINICIAR", -14f, new Color(0.3f, 0.32f, 0.4f));
            restart.onClick.AddListener(GameManager.Instance.Restart);

            var menu = MakeMenuButton(panel.transform, "Menu", "MENU PRINCIPAL", -118f, new Color(0.3f, 0.32f, 0.4f));
            menu.onClick.AddListener(GameManager.Instance.ReturnToMenu);

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private GameObject BuildEndScreen(Transform parent, string name, string title, string message, Color background,
            string primaryLabel, System.Action primaryAction)
        {
            var panel = UIFactory.CreatePanel(parent, name, background);

            var titleText = UIFactory.CreateText(panel.transform, "Title", title, 80f, TextAlignmentOptions.Center, Color.white);
            UIFactory.Anchor(titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 230f), new Vector2(1100f, 120f));

            var messageText = UIFactory.CreateText(panel.transform, "Message", message, 30f,
                TextAlignmentOptions.Center, new Color(0.9f, 0.9f, 0.92f));
            UIFactory.Anchor(messageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 140f), new Vector2(1000f, 60f));

            var primary = MakeMenuButton(panel.transform, "Primary", primaryLabel, 20f, new Color(0.85f, 0.35f, 0.45f));
            primary.onClick.AddListener(() => primaryAction());

            var menu = MakeMenuButton(panel.transform, "Menu", "MENU PRINCIPAL", -84f, new Color(0.3f, 0.32f, 0.4f));
            menu.onClick.AddListener(GameManager.Instance.ReturnToMenu);

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private Button MakeMenuButton(Transform parent, string name, string label, float y, Color background)
        {
            var button = UIFactory.CreateButton(parent, name, label, background, Color.white, 34f, out _);
            UIFactory.Anchor((RectTransform)button.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(420f, 92f));
            return button;
        }
    }
}
