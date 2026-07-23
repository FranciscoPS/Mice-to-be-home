using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiceToBeHome
{
    public class InventoryView : MonoBehaviour
    {
        public void Build(Transform parent, IReadOnlyList<TrapDefinition> traps, PlacementController placement, TooltipView tooltip)
        {
            var panel = UIFactory.CreatePanel(parent, "InventoryPanel", new Color(0.12f, 0.10f, 0.16f, 0.92f));
            UIFactory.Anchor(panel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(330f, 0f));

            var header = UIFactory.CreateText(panel.transform, "Header", "INVENTARIO", 30f, TextAlignmentOptions.Center, Color.white);
            UIFactory.Anchor(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -12f), new Vector2(0f, 52f));

            var subtitle = UIFactory.CreateText(panel.transform, "Subtitle", "Trampas ilimitadas", 18f,
                TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.85f));
            UIFactory.Anchor(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -60f), new Vector2(0f, 26f));

            var container = new GameObject("Slots", typeof(RectTransform));
            container.transform.SetParent(panel.transform, false);
            UIFactory.Anchor(container.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(0f, 0f));

            var layout = container.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 6, 6);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = container.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int i = 0; i < traps.Count; i++)
            {
                CreateSlot(container.transform, traps[i], placement, tooltip);
            }
        }

        private void CreateSlot(Transform parent, TrapDefinition definition, PlacementController placement, TooltipView tooltip)
        {
            var go = new GameObject("Slot_" + definition.displayName, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var background = go.AddComponent<Image>();
            background.color = new Color(0.18f, 0.16f, 0.24f, 1f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.78f, 0.82f, 0.95f);
            colors.pressedColor = new Color(0.6f, 0.65f, 0.8f);
            button.colors = colors;

            var element = go.AddComponent<LayoutElement>();
            element.minHeight = 86f;
            element.preferredHeight = 86f;

            var icon = UIFactory.CreateIcon(go.transform, "Icon", definition.sprite, definition.tint);
            UIFactory.Anchor(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(14f, 0f), new Vector2(62f, 62f));

            string body = $"{definition.displayName}\n<size=17><color=#C8C8D0>Tamano {definition.gridSize} - Distrae {definition.effectSeconds:0}s</color></size>";
            var text = UIFactory.CreateText(go.transform, "Info", body, 21f, TextAlignmentOptions.Left, Color.white);
            UIFactory.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(88f, 6f);
            text.rectTransform.offsetMax = new Vector2(-8f, -6f);

            var trigger = go.AddComponent<TooltipTrigger>();
            trigger.Setup(tooltip, BuildTooltip(definition));

            TrapDefinition captured = definition;
            button.onClick.AddListener(() => placement.SelectFromInventory(captured));
        }

        private static string BuildTooltip(TrapDefinition definition)
        {
            return $"<b>{definition.displayName}</b>\n{definition.description}\n\nDistrae al gato {definition.effectSeconds:0}s\nOcupa {definition.gridSize} casilla(s)";
        }
    }
}
