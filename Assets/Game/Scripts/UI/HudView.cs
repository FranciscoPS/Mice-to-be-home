using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiceToBeHome
{
    public class HudView : MonoBehaviour
    {
        public Button StartButton { get; private set; }
        public Button PauseButton { get; private set; }

        private TextMeshProUGUI timerText;
        private TextMeshProUGUI phaseText;
        private TextMeshProUGUI livesText;
        private TextMeshProUGUI hintText;
        private GameObject inventoryRoot;

        public void Build(Transform parent, IReadOnlyList<TrapDefinition> traps, PlacementController placement, TooltipView tooltip)
        {
            timerText = UIFactory.CreateText(parent, "Timer", "02:00", 74f, TextAlignmentOptions.Center, Color.white);
            UIFactory.Anchor(timerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -18f), new Vector2(360f, 90f));

            phaseText = UIFactory.CreateText(parent, "Phase", "", 30f, TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.6f));
            UIFactory.Anchor(phaseText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -104f), new Vector2(560f, 40f));

            livesText = UIFactory.CreateText(parent, "Lives", "Vidas: 3", 34f, TextAlignmentOptions.Right, Color.white);
            UIFactory.Anchor(livesText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-28f, -22f), new Vector2(320f, 46f));

            PauseButton = UIFactory.CreateButton(parent, "PauseButton", "II", new Color(0.2f, 0.2f, 0.28f, 0.9f),
                Color.white, 30f, out _);
            UIFactory.Anchor(((RectTransform)PauseButton.transform), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(28f, -22f), new Vector2(70f, 70f));

            hintText = UIFactory.CreateText(parent, "Hint",
                "Clic: tomar y colocar trampa   -   Q / E: rotar   -   Clic derecho: cancelar", 22f,
                TextAlignmentOptions.Center, new Color(0.9f, 0.9f, 0.95f));
            UIFactory.Anchor(hintText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 40f), new Vector2(900f, 40f));

            StartButton = UIFactory.CreateButton(parent, "StartButton", "EMPEZAR   (medianoche)",
                new Color(0.85f, 0.35f, 0.45f, 1f), Color.white, 34f, out _);
            UIFactory.Anchor(((RectTransform)StartButton.transform), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 108f), new Vector2(420f, 84f));

            var inventory = new GameObject("Inventory", typeof(RectTransform));
            inventory.transform.SetParent(parent, false);
            UIFactory.Stretch(inventory.GetComponent<RectTransform>());
            inventoryRoot = inventory;
            inventory.AddComponent<InventoryView>().Build(inventory.transform, traps, placement, tooltip);
        }

        public void SetTimer(float seconds)
        {
            int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            timerText.text = $"{total / 60:00}:{total % 60:00}";
            timerText.color = seconds <= 10f ? new Color(1f, 0.4f, 0.4f) : Color.white;
        }

        public void SetLives(int current, int max)
        {
            livesText.text = $"Vidas: {current}";
        }

        public void SetPhase(GameState state)
        {
            bool editing = state == GameState.Editing;
            phaseText.text = editing ? "Coloca tus trampas" : "Sobrevive al gato";

            inventoryRoot.SetActive(editing);
            StartButton.gameObject.SetActive(editing);
            hintText.gameObject.SetActive(editing);
        }
    }
}
