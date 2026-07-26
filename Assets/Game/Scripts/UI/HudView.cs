using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiceToBeHome
{
    public class HudView : MonoBehaviour
    {
        [SerializeField] private Button startButton = null;
        [SerializeField] private Button pauseButton = null;
        [SerializeField] private TextMeshProUGUI timerText = null;
        [SerializeField] private TextMeshProUGUI phaseText = null;
        [SerializeField] private TextMeshProUGUI livesText = null;
        [SerializeField] private TextMeshProUGUI hintText = null;
        [SerializeField] private GameObject inventoryRoot = null;
        [SerializeField] private InventoryView inventory = null;

        public Button StartButton => startButton;
        public Button PauseButton => pauseButton;

        public void Initialize(IReadOnlyList<Trap> traps, PlacementController placement, TooltipView tooltip)
        {
            if (inventory != null)
            {
                inventory.Initialize(traps, placement, tooltip);
            }
        }

        public void SetTimer(float seconds)
        {
            int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            timerText.text = $"{total / 60:00}:{total % 60:00}";
            timerText.color = seconds <= 10f ? new Color(1f, 0.4f, 0.4f) : Color.white;
        }

        public void SetLives(int current, int max)
        {
            livesText.text = $"Lives: {current}";
        }

        public void SetPhase(GameState state)
        {
            bool editing = state == GameState.Editing;
            bool playing = state == GameState.Playing;
            phaseText.text = editing ? "Place your traps" : "Survive the cat";

            if (editing)
            {
                hintText.text = "Click: place / pick up a trap   -   Right click: cancel   -   Max 5 traps";
            }
            else if (playing)
            {
                hintText.text = "WASD / arrows: run   -   Stand on a faded trap to repair it";
            }

            inventoryRoot.SetActive(editing);
            StartButton.gameObject.SetActive(editing);
            hintText.gameObject.SetActive(editing || playing);
        }
    }
}
