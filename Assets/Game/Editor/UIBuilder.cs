#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiceToBeHome.EditorTools
{
    /// <summary>
    /// Builds the whole game UI as real, editable GameObjects under a persistent "GameUI"
    /// Canvas (kept OUTSIDE the "Game" root, like the CameraRig, so Build Scene never wipes it).
    /// The runtime view components (UIManager/HudView/InventoryView/TooltipView) only REFERENCE
    /// these widgets and keep all the game logic — restyle anything in the Inspector.
    /// </summary>
    public static class UIBuilder
    {
        private const string RootName = "GameUI";

        [MenuItem("Tools/Mice to be Home/Build UI")]
        public static void BuildUIMenu()
        {
            UIManager existing = FindManager();
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("Mice to be Home",
                    "A '" + RootName + "' UI already exists in the scene.\n\nRebuild it from scratch? This RESETS any styling you changed on the UI.",
                    "Rebuild (reset)", "Cancel"))
                {
                    return;
                }
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            UIManager ui = Build();
            WireInstaller(ui);
            Selection.activeObject = ui.gameObject;
            EditorGUIUtility.PingObject(ui.gameObject);
            EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
            Debug.Log("[Mice to be Home] UI built under '" + RootName + "'. Edit it freely in the Inspector; re-run Build UI to reset it.");
        }

        // Re-wire GameInstaller.ui to this Canvas and drop any leftover runtime-style UI holder
        // (the old in-Game "UI" object had a UIManager but no Canvas of its own), so running just
        // this tool is enough — no full Build Scene required.
        private static void WireInstaller(UIManager manager)
        {
            foreach (var stray in Object.FindObjectsByType<UIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (stray != null && stray != manager && stray.GetComponent<Canvas>() == null)
                {
                    Undo.DestroyObjectImmediate(stray.gameObject);
                }
            }

            GameInstaller installer = Object.FindFirstObjectByType<GameInstaller>();
            if (installer != null)
            {
                installer.ui = manager;
                EditorUtility.SetDirty(installer);
            }
        }

        /// <summary>Reuse-if-exists. Called by Build Scene so it never clobbers your UI edits.</summary>
        public static UIManager EnsureUI()
        {
            UIManager existing = FindManager();
            return existing != null ? existing : Build();
        }

        private static UIManager FindManager()
        {
            foreach (var manager in Object.FindObjectsByType<UIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (manager != null)
                {
                    return manager;
                }
            }
            return null;
        }

        private static UIManager Build()
        {
            Canvas canvas = UIFactory.CreateCanvas(RootName);
            Undo.RegisterCreatedObjectUndo(canvas.gameObject, "Build Mice UI");
            UIManager manager = canvas.gameObject.AddComponent<UIManager>();

            GameObject hudGroup = UIFactory.CreateGroup(canvas.transform, "HUD");
            HudView hud = hudGroup.AddComponent<HudView>();
            BuildHud(hudGroup.transform, hud);

            GameObject menuPanel = BuildMenu(canvas.transform, out Button menuPlay, out Button menuQuit);
            GameObject pausePanel = BuildPause(canvas.transform, out Button pauseResume, out Button pauseRestart, out Button pauseMenu);
            GameObject victoryPanel = BuildEndScreen(canvas.transform, "VictoryPanel", "YOU SURVIVED",
                "You turned back into a human. Miauricio goes hungry tonight.", new Color(0.09f, 0.16f, 0.10f, 0.9f),
                "PLAY AGAIN", out Button victoryPrimary, out Button victoryMenu);
            GameObject defeatPanel = BuildEndScreen(canvas.transform, "DefeatPanel", "YOU GOT CAUGHT",
                "Miauricio turned you into his midnight snack.", new Color(0.18f, 0.07f, 0.07f, 0.9f),
                "RETRY", out Button defeatPrimary, out Button defeatMenu);

            // Tooltip last so it renders on top of everything.
            GameObject tooltipGO = BuildTooltip(canvas.transform, out TooltipView tooltip);
            tooltipGO.transform.SetAsLastSibling();

            new Binder(manager)
                .Set("hud", hud)
                .Set("tooltip", tooltip)
                .Set("hudGroup", hudGroup)
                .Set("menuPanel", menuPanel)
                .Set("pausePanel", pausePanel)
                .Set("victoryPanel", victoryPanel)
                .Set("defeatPanel", defeatPanel)
                .Set("menuPlayButton", menuPlay)
                .Set("menuQuitButton", menuQuit)
                .Set("pauseResumeButton", pauseResume)
                .Set("pauseRestartButton", pauseRestart)
                .Set("pauseMenuButton", pauseMenu)
                .Set("victoryPrimaryButton", victoryPrimary)
                .Set("victoryMenuButton", victoryMenu)
                .Set("defeatPrimaryButton", defeatPrimary)
                .Set("defeatMenuButton", defeatMenu)
                .Apply();

            return manager;
        }

        private static void BuildHud(Transform parent, HudView hud)
        {
            var timerText = UIFactory.CreateText(parent, "Timer", "02:00", 74f, TextAlignmentOptions.Center, Color.white);
            UIFactory.Anchor(timerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -18f), new Vector2(360f, 90f));

            var phaseText = UIFactory.CreateText(parent, "Phase", "", 30f, TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.6f));
            UIFactory.Anchor(phaseText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -104f), new Vector2(560f, 40f));

            var livesText = UIFactory.CreateText(parent, "Lives", "Lives: 3", 34f, TextAlignmentOptions.Right, Color.white);
            UIFactory.Anchor(livesText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-28f, -22f), new Vector2(320f, 46f));

            Button pauseButton = UIFactory.CreateButton(parent, "PauseButton", "II", new Color(0.2f, 0.2f, 0.28f, 0.9f),
                Color.white, 30f, out _);
            UIFactory.Anchor((RectTransform)pauseButton.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(28f, -22f), new Vector2(70f, 70f));

            var hintText = UIFactory.CreateText(parent, "Hint",
                "Click: pick up & place a trap   -   Right click: cancel", 22f,
                TextAlignmentOptions.Center, new Color(0.9f, 0.9f, 0.95f));
            UIFactory.Anchor(hintText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 40f), new Vector2(900f, 40f));

            Button startButton = UIFactory.CreateButton(parent, "StartButton", "START   (midnight)",
                new Color(0.85f, 0.35f, 0.45f, 1f), Color.white, 34f, out _);
            UIFactory.Anchor((RectTransform)startButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 108f), new Vector2(420f, 84f));

            var inventoryGO = new GameObject("Inventory", typeof(RectTransform));
            inventoryGO.transform.SetParent(parent, false);
            UIFactory.Stretch(inventoryGO.GetComponent<RectTransform>());
            InventoryView inventory = inventoryGO.AddComponent<InventoryView>();
            BuildInventory(inventoryGO.transform, inventory);

            new Binder(hud)
                .Set("startButton", startButton)
                .Set("pauseButton", pauseButton)
                .Set("timerText", timerText)
                .Set("phaseText", phaseText)
                .Set("livesText", livesText)
                .Set("hintText", hintText)
                .Set("inventoryRoot", inventoryGO)
                .Set("inventory", inventory)
                .Apply();
        }

        private static void BuildInventory(Transform parent, InventoryView inventory)
        {
            var panel = UIFactory.CreatePanel(parent, "InventoryPanel", new Color(0.12f, 0.10f, 0.16f, 0.92f));
            UIFactory.Anchor(panel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(330f, 0f));

            var header = UIFactory.CreateText(panel.transform, "Header", "INVENTORY", 30f, TextAlignmentOptions.Center, Color.white);
            UIFactory.Anchor(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -12f), new Vector2(0f, 52f));

            var subtitle = UIFactory.CreateText(panel.transform, "Subtitle", string.Empty, 18f,
                TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.85f));
            UIFactory.Anchor(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -60f), new Vector2(0f, 26f));

            var container = new GameObject("Slots", typeof(RectTransform));
            container.transform.SetParent(panel.transform, false);
            UIFactory.Anchor(container.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(0f, 0f));

            var layout = container.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 6, 6);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = container.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject template = BuildSlotTemplate(container.transform);

            new Binder(inventory)
                .Set("subtitle", subtitle)
                .Set("slotsContainer", container.transform)
                .Set("slotTemplate", template)
                .Apply();
        }

        // One styled slot the runtime clones per trap (icon/text/count/tooltip filled in code).
        private static GameObject BuildSlotTemplate(Transform parent)
        {
            var go = new GameObject("SlotTemplate", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            go.AddComponent<CanvasGroup>();

            var background = go.AddComponent<Image>();
            background.color = new Color(0.18f, 0.16f, 0.24f, 1f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.78f, 0.82f, 0.95f);
            colors.pressedColor = new Color(0.6f, 0.65f, 0.8f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.65f);
            button.colors = colors;

            var element = go.AddComponent<LayoutElement>();
            element.minHeight = 84f;
            element.preferredHeight = 84f;

            go.AddComponent<TooltipTrigger>();

            var icon = UIFactory.CreateIcon(go.transform, "Icon", null, Color.white);
            UIFactory.Anchor(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(12f, 0f), new Vector2(56f, 56f));

            var info = UIFactory.CreateText(go.transform, "Info", "Trap name", 20f, TextAlignmentOptions.Left, Color.white);
            UIFactory.Stretch(info.rectTransform);
            info.rectTransform.offsetMin = new Vector2(78f, 6f);
            info.rectTransform.offsetMax = new Vector2(-42f, -6f);

            var count = UIFactory.CreateText(go.transform, "Count", "x0", 26f, TextAlignmentOptions.Right, Color.white);
            UIFactory.Anchor(count.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-14f, 0f), new Vector2(48f, 44f));

            return go;
        }

        private static GameObject BuildMenu(Transform parent, out Button playButton, out Button quitButton)
        {
            var panel = UIFactory.CreatePanel(parent, "MenuPanel", new Color(0.06f, 0.06f, 0.10f, 0.86f));

            var title = UIFactory.CreateText(panel.transform, "Title", "MICE TO BE HOME", 78f,
                TextAlignmentOptions.Center, Color.white);
            UIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 250f), new Vector2(1100f, 120f));

            var subtitle = UIFactory.CreateText(panel.transform, "Subtitle",
                "Place traps, turn into a mouse, and escape Miauricio.", 30f,
                TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.9f));
            UIFactory.Anchor(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 165f), new Vector2(1000f, 60f));

            playButton = UIFactory.CreateButton(panel.transform, "PlayButton", "PLAY", new Color(0.85f, 0.35f, 0.45f),
                Color.white, 38f, out _);
            UIFactory.Anchor((RectTransform)playButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(420f, 96f));

            quitButton = UIFactory.CreateButton(panel.transform, "QuitButton", "QUIT", new Color(0.25f, 0.25f, 0.32f),
                Color.white, 32f, out _);
            UIFactory.Anchor((RectTransform)quitButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(360f, 80f));

            var controls = UIFactory.CreateText(panel.transform, "Controls",
                "Edit: click to place traps (max 5).    Chase: WASD to move, stand on a faded trap to repair it.    Esc: pause.", 22f,
                TextAlignmentOptions.Center, new Color(0.75f, 0.75f, 0.82f));
            UIFactory.Anchor(controls.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 60f), new Vector2(1300f, 40f));

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private static GameObject BuildPause(Transform parent, out Button resume, out Button restart, out Button menu)
        {
            var panel = UIFactory.CreatePanel(parent, "PausePanel", new Color(0.03f, 0.03f, 0.05f, 0.78f));

            var title = UIFactory.CreateText(panel.transform, "Title", "PAUSED", 70f, TextAlignmentOptions.Center, Color.white);
            UIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 220f), new Vector2(800f, 110f));

            resume = MakeMenuButton(panel.transform, "Resume", "RESUME", 90f, new Color(0.3f, 0.55f, 0.4f));
            restart = MakeMenuButton(panel.transform, "Restart", "RESTART", -14f, new Color(0.3f, 0.32f, 0.4f));
            menu = MakeMenuButton(panel.transform, "Menu", "MAIN MENU", -118f, new Color(0.3f, 0.32f, 0.4f));

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private static GameObject BuildEndScreen(Transform parent, string name, string title, string message, Color background,
            string primaryLabel, out Button primary, out Button menu)
        {
            var panel = UIFactory.CreatePanel(parent, name, background);

            var titleText = UIFactory.CreateText(panel.transform, "Title", title, 80f, TextAlignmentOptions.Center, Color.white);
            UIFactory.Anchor(titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 230f), new Vector2(1100f, 120f));

            var messageText = UIFactory.CreateText(panel.transform, "Message", message, 30f,
                TextAlignmentOptions.Center, new Color(0.9f, 0.9f, 0.92f));
            UIFactory.Anchor(messageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 140f), new Vector2(1000f, 60f));

            primary = MakeMenuButton(panel.transform, "Primary", primaryLabel, 20f, new Color(0.85f, 0.35f, 0.45f));
            menu = MakeMenuButton(panel.transform, "Menu", "MAIN MENU", -84f, new Color(0.3f, 0.32f, 0.4f));

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private static Button MakeMenuButton(Transform parent, string name, string label, float y, Color background)
        {
            var button = UIFactory.CreateButton(parent, name, label, background, Color.white, 34f, out _);
            UIFactory.Anchor((RectTransform)button.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(420f, 92f));
            return button;
        }

        private static GameObject BuildTooltip(Transform parent, out TooltipView tooltip)
        {
            var tooltipGO = new GameObject("Tooltip", typeof(RectTransform));
            tooltipGO.transform.SetParent(parent, false);
            UIFactory.Stretch(tooltipGO.GetComponent<RectTransform>());
            tooltip = tooltipGO.AddComponent<TooltipView>();

            var image = UIFactory.CreatePanel(tooltipGO.transform, "TooltipPanel", new Color(0.05f, 0.05f, 0.08f, 0.92f));
            RectTransform panel = image.rectTransform;
            UIFactory.Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(360f, 150f));

            var label = UIFactory.CreateText(panel, "Text", string.Empty, 24f, TextAlignmentOptions.TopLeft, Color.white);
            UIFactory.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(14f, 14f);
            label.rectTransform.offsetMax = new Vector2(-14f, -14f);

            new Binder(tooltip)
                .Set("panel", panel)
                .Set("label", label)
                .Apply();

            return tooltipGO;
        }

        // Assigns private [SerializeField] fields on a component by name (no runtime API needed).
        private sealed class Binder
        {
            private readonly SerializedObject serialized;

            public Binder(Object target)
            {
                serialized = new SerializedObject(target);
            }

            public Binder Set(string field, Object value)
            {
                SerializedProperty property = serialized.FindProperty(field);
                if (property != null)
                {
                    property.objectReferenceValue = value;
                }
                else
                {
                    Debug.LogWarning("[UIBuilder] Missing serialized field '" + field + "' on " + serialized.targetObject.GetType().Name);
                }
                return this;
            }

            public void Apply()
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
