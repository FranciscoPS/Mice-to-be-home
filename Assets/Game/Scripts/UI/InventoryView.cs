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
            public Image highlight;
        }

        [SerializeField] private TextMeshProUGUI subtitle = null;
        [SerializeField] private Transform slotsContainer = null;
        [SerializeField] private GameObject slotTemplate = null;

        private PlacementController placement;
        private readonly List<Slot> slots = new List<Slot>();

        public void Initialize(IReadOnlyList<Trap> traps, PlacementController placement, TooltipView tooltip)
        {
            this.placement = placement;
            slots.Clear();

            if (slotTemplate == null || slotsContainer == null)
            {
                return;
            }

            for (int i = 0; i < traps.Count; i++)
            {
                CreateSlot(traps[i], tooltip);
            }

            // The authored template is only a style reference; hide it once real slots exist.
            slotTemplate.SetActive(false);
            Refresh();
        }

        private void CreateSlot(Trap trapPrefab, TooltipView tooltip)
        {
            TrapDefinition definition = trapPrefab.Definition;

            GameObject go = Instantiate(slotTemplate, slotsContainer);
            go.name = "Slot_" + definition.displayName;
            go.SetActive(true);

            var group = go.GetComponent<CanvasGroup>();
            var button = go.GetComponent<Button>();

            SpriteRenderer skin = trapPrefab.GetComponentInChildren<SpriteRenderer>();
            Sprite iconSprite = skin != null ? skin.sprite : null;
            Color iconColor = skin != null ? skin.color : definition.tint;
            Image icon = FindImage(go.transform, "Icon");
            if (icon != null)
            {
                if (iconSprite != null)
                {
                    icon.sprite = iconSprite;
                }
                icon.color = iconColor;
            }

            TextMeshProUGUI info = FindText(go.transform, "Info");
            if (info != null)
            {
                info.text = $"{definition.displayName}\n<size=16><color=#C8C8D0>Stuns {definition.effectSeconds:0.##}s</color></size>";
            }

            TextMeshProUGUI count = FindText(go.transform, "Count");
            if (count != null)
            {
                count.text = string.Empty;
            }

            var trigger = go.GetComponent<TooltipTrigger>();
            if (trigger == null)
            {
                trigger = go.AddComponent<TooltipTrigger>();
            }
            trigger.Setup(tooltip, BuildTooltip(definition));

            Trap captured = trapPrefab;
            if (button != null)
            {
                button.onClick.AddListener(() => placement.SelectFromInventory(captured));
            }

            Image highlight = CreateSelectionHighlight(go.transform);

            slots.Add(new Slot { prefab = trapPrefab, button = button, group = group, count = count, highlight = highlight });
        }

        // A soft gold overlay behind the icon/text that marks the slot currently being placed.
        private static Image CreateSelectionHighlight(Transform slotRoot)
        {
            var go = new GameObject("SelectedHighlight", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(slotRoot, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsFirstSibling();

            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 0.82f, 0.25f, 0.45f);
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private static Image FindImage(Transform root, string childName)
        {
            Transform t = root.Find(childName);
            return t != null ? t.GetComponent<Image>() : null;
        }

        private static TextMeshProUGUI FindText(Transform root, string childName)
        {
            Transform t = root.Find(childName);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
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
            Trap selectedTrap = placement.SelectedTrap;
            for (int i = 0; i < slots.Count; i++)
            {
                Slot slot = slots[i];
                int remaining = placement.GetStock(slot.prefab);
                bool usable = remaining > 0 && canPlaceMore;

                if (slot.count != null)
                {
                    slot.count.text = "x" + remaining;
                }
                if (slot.button != null)
                {
                    slot.button.interactable = usable;
                }
                if (slot.group != null)
                {
                    slot.group.alpha = usable ? 1f : 0.45f;
                }
                if (slot.highlight != null)
                {
                    slot.highlight.enabled = slot.prefab == selectedTrap;
                }
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
