using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiceToBeHome
{
    public class InventoryView : MonoBehaviour
    {
        private class Slot
        {
            public Trap prefab;
            public Button button;
            public CanvasGroup group;
            public TextMeshProUGUI count;
        }

        private PlacementController placement;
        private TextMeshProUGUI subtitle;
        private readonly List<Slot> slots = new List<Slot>();

        public void Build(Transform parent, IReadOnlyList<Trap> traps, PlacementController placement, TooltipView tooltip)
        {
            this.placement = placement;

            var panel = UIFactory.CreatePanel(parent, "InventoryPanel", new Color(0.12f, 0.10f, 0.16f, 0.92f));
            UIFactory.Anchor(panel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(330f, 0f));

            var header = UIFactory.CreateText(panel.transform, "Header", "INVENTORY", 30f, TextAlignmentOptions.Center, Color.white);
            UIFactory.Anchor(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -12f), new Vector2(0f, 52f));

            subtitle = UIFactory.CreateText(panel.transform, "Subtitle", string.Empty, 18f,
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

            for (int i = 0; i < traps.Count; i++)
            {
                CreateSlot(container.transform, traps[i], tooltip);
            }

            Refresh();
        }

        private void CreateSlot(Transform parent, Trap trapPrefab, TooltipView tooltip)
        {
            TrapDefinition definition = trapPrefab.Definition;
            var go = new GameObject("Slot_" + definition.displayName, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var group = go.AddComponent<CanvasGroup>();

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

            SpriteRenderer skin = trapPrefab.GetComponentInChildren<SpriteRenderer>();
            Sprite iconSprite = skin != null ? skin.sprite : null;
            Color iconColor = skin != null ? skin.color : definition.tint;
            var icon = UIFactory.CreateIcon(go.transform, "Icon", iconSprite, iconColor);
            UIFactory.Anchor(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(12f, 0f), new Vector2(56f, 56f));

            string body = $"{definition.displayName}\n<size=16><color=#C8C8D0>Stuns {definition.effectSeconds:0.##}s</color></size>";
            var text = UIFactory.CreateText(go.transform, "Info", body, 20f, TextAlignmentOptions.Left, Color.white);
            UIFactory.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(78f, 6f);
            text.rectTransform.offsetMax = new Vector2(-42f, -6f);

            var count = UIFactory.CreateText(go.transform, "Count", string.Empty, 26f, TextAlignmentOptions.Right, Color.white);
            UIFactory.Anchor(count.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-14f, 0f), new Vector2(48f, 44f));

            var trigger = go.AddComponent<TooltipTrigger>();
            trigger.Setup(tooltip, BuildTooltip(definition));

            Trap captured = trapPrefab;
            button.onClick.AddListener(() => placement.SelectFromInventory(captured));

            slots.Add(new Slot { prefab = trapPrefab, button = button, group = group, count = count });
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (placement == null)
            {
                return;
            }

            bool canPlaceMore = placement.PlacedCount < placement.MaxTraps;
            for (int i = 0; i < slots.Count; i++)
            {
                Slot slot = slots[i];
                int remaining = placement.GetStock(slot.prefab);
                slot.count.text = "x" + remaining;

                bool usable = remaining > 0 && canPlaceMore;
                slot.button.interactable = usable;
                slot.group.alpha = usable ? 1f : 0.45f;
            }

            if (subtitle != null)
            {
                subtitle.text = $"Placed {placement.PlacedCount} / {placement.MaxTraps}";
            }
        }

        private static string BuildTooltip(TrapDefinition definition)
        {
            return $"<b>{definition.displayName}</b>\n{definition.description}\n\nStuns the cat for {definition.effectSeconds:0.##}s\nSingle use - repair by standing on it for {definition.effectSeconds:0.##}s";
        }
    }
}
